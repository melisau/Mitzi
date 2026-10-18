"""Verify signed Mitzi Android packages. Never accepts signing passwords as arguments."""
import argparse,base64,hashlib,json,os,re,struct,subprocess,sys,zipfile
from pathlib import Path
import xml.etree.ElementTree as ET

def elf_report(data):
    if data[:4]!=b"\x7fELF" or data[4]!=2 or data[5]!=1:
        raise ValueError("Expected little-endian ELF64")
    machine=struct.unpack_from("<H",data,18)[0]
    offset=struct.unpack_from("<Q",data,32)[0]
    size,count=struct.unpack_from("<HH",data,54)
    loads=[];relro=[]
    for i in range(count):
        kind,flags,file_offset,virtual,physical,file_size,memory_size,alignment=struct.unpack_from("<IIQQQQQQ",data,offset+i*size)
        if kind==1:
            loads.append({"alignment":alignment,"congruent_16kb":file_offset%16384==virtual%16384,
                          "virtual_address":virtual,"memory_size":memory_size,"writable":bool(flags&2)})
        if kind==0x6474e552:
            relro.append({"virtual_address":virtual,"memory_size":memory_size,"end_mod_16kb":(virtual+memory_size)%16384})
    for segment in relro:
        start=segment["virtual_address"];end=start+segment["memory_size"]
        protected_start=start//16384*16384;protected_end=(end+16383)//16384*16384
        # Android permits an unaligned RELRO end in a separate LOAD when page
        # rounding cannot protect subsequent writable data. RELRO may include
        # padding beyond its containing LOAD's actual memory end (LLVM layout).
        separate=any(s["virtual_address"]==start and s["virtual_address"]+s["memory_size"]<=end for s in loads)
        overlap=any(s["writable"] and
                    max(s["virtual_address"],protected_start)<min(s["virtual_address"]+s["memory_size"],protected_end) and
                    (max(s["virtual_address"],protected_start)<start or min(s["virtual_address"]+s["memory_size"],protected_end)>end)
                    for s in loads)
        segment["separate_load_safe"]=separate and not overlap
    return {"machine":machine,"load_segments":loads,"relro_segments":relro,
            "load_alignment_pass":machine==183 and bool(loads) and all(s["alignment"]>=16384 and s["congruent_16kb"] for s in loads),
            "relro_alignment_pass":all(s["end_mod_16kb"]==0 or s["separate_load_safe"] for s in relro)}

def run(command):
    result=subprocess.run([str(x) for x in command],capture_output=True,text=True,encoding="utf-8",errors="replace")
    if result.returncode:raise RuntimeError("Command failed: "+str(command[0])+"\n"+result.stderr[-1600:]+result.stdout[-1600:])
    return result.stdout+result.stderr

def verify(args):
    java=Path(args.unity_android)/"OpenJDK/bin/java.exe"
    jarsigner=java.with_name("jarsigner.exe")
    bundletool=Path(args.unity_android)/"Tools/bundletool-all-1.17.2.jar"
    run([java,"-jar",bundletool,"validate","--bundle="+str(args.aab)])
    signed=run([jarsigner,"-J-Duser.language=en","-verify",args.aab])
    if "jar verified" not in signed.lower():raise RuntimeError("AAB signature was not verified")
    xml=run([java,"-jar",bundletool,"dump","manifest","--bundle="+str(args.aab),"--module=base"])
    root=ET.fromstring(xml);ns="{http://schemas.android.com/apk/res/android}"
    app=root.find("application");sdk=root.find("uses-sdk")
    permissions=[n.get(ns+"name") for n in root.findall("uses-permission")]
    manifest={"package":root.get("package"),"version_code":root.get(ns+"versionCode"),"min_sdk":sdk.get(ns+"minSdkVersion"),"target_sdk":sdk.get(ns+"targetSdkVersion"),"debuggable":app.get(ns+"debuggable","false"),"extract_native_libs":app.get(ns+"extractNativeLibs"),"permissions":permissions}
    errors=[]
    if manifest["package"]!="com.mitzicat.game":errors.append("Unexpected application package")
    if manifest["target_sdk"]!="36":errors.append("Unexpected target SDK")
    if manifest["debuggable"]!="false":errors.append("Package is debuggable")
    if "com.google.android.gms.permission.AD_ID" in permissions:errors.append("AD_ID permission remains")
    native=[]
    with zipfile.ZipFile(args.aab) as bundle:
        libs=[n for n in bundle.namelist() if "/lib/" in n and n.endswith(".so")]
        if not libs:errors.append("No native libraries found")
        if not any(n.endswith("/libil2cpp.so") for n in libs):errors.append("IL2CPP library missing")
        for name in libs:
            if "/arm64-v8a/" not in name:errors.append("Unexpected ABI: "+name);continue
            result=elf_report(bundle.read(name));result["path"]=name;native.append(result)
            if not result["load_alignment_pass"]:errors.append("ELF LOAD alignment below 16 KB: "+name)
            if not result["relro_alignment_pass"]:errors.append("GNU_RELRO end is not 16 KB aligned: "+name)
    apk_info=None
    if args.apk:
        buildtools=Path(args.build_tools)
        apk_signature=run([java,"-jar",buildtools/"lib/apksigner.jar","verify","--verbose","--print-certs",args.apk])
        run([buildtools/"zipalign.exe","-v","-c","-P","16","4",args.apk])
        cert_match=re.search(r"certificate SHA-256 digest: ([0-9a-fA-F]+)",apk_signature)
        apk_info={"signature_verified":True,"zipalign_16kb_verified":True,"certificate_sha256":cert_match.group(1).lower() if cert_match else None,"native_packaging":[]}
        with zipfile.ZipFile(args.apk) as apk:
            for info in apk.infolist():
                if info.filename.startswith("lib/") and info.filename.endswith(".so"):
                    apk_info["native_packaging"].append({"path":info.filename,"compressed":info.compress_type!=zipfile.ZIP_STORED})
        if args.certificate:
            pem=Path(args.certificate).read_text();der=base64.b64decode("".join(line for line in pem.splitlines() if not line.startswith("---")))
            expected=hashlib.sha256(der).hexdigest()
            if expected!=apk_info["certificate_sha256"]:errors.append("APK signed with unexpected certificate")
    report={"aab":str(args.aab),"aab_sha256":hashlib.sha256(Path(args.aab).read_bytes()).hexdigest(),"bundle_structure_verified":True,"aab_signature_verified":True,"manifest":manifest,"native_libraries":native,"apk":apk_info,"static_checks_pass":not errors,"errors":errors,"device_test":"NOT_RUN","production_admob":"NOT_CONFIGURED","public_privacy_policy":"NOT_PUBLISHED"}
    Path(args.report).write_text(json.dumps(report,indent=2,ensure_ascii=False),encoding="utf-8")
    print(json.dumps({"static_checks_pass":not errors,"native_libraries":len(native),"errors":errors,"report":str(args.report)},ensure_ascii=False))
    return 0 if not errors else 1

if __name__=="__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("aab",type=Path);parser.add_argument("--apk",type=Path);parser.add_argument("--report",type=Path,required=True)
    parser.add_argument("--unity-android",default="C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Data/PlaybackEngines/AndroidPlayer")
    parser.add_argument("--build-tools",default=str(Path(os.environ["LOCALAPPDATA"])/"Mitzi/AndroidSdk/build-tools/35.0.0"))
    parser.add_argument("--certificate",type=Path)
    try:sys.exit(verify(parser.parse_args()))
    except Exception as error:print(str(error),file=sys.stderr);sys.exit(2)
