# Android release

The release preparation command selects ARM64 and IL2CPP, target API 36, version `1.0.0`, and Android version code `1`. Keep code `1` only if no previous Play upload has used it. Increment the version code before each new upload; update the visible version when releasing a new version.

## Required toolchain

The checked-in project uses Unity **2022.3.62f3**. Install Android Build Support with SDK/NDK Tools and OpenJDK. Confirm Android SDK Platform 36 is installed in the SDK selected under Unity Preferences > External Tools. Validate the actual release bundle's native libraries for 16 KB page support; the editor version alone is not sufficient.

## Upload signing

Use the existing upload key if this app is already registered in Play Console. For a new app, create an upload keystore with Unity's Keystore Manager, back it up securely, and use Play App Signing. Keep the key outside the repository. Never use the Android debug key for the release.

Set these environment variables in the process that launches Unity (or inject them through CI secrets), then restart Unity so it inherits them:

- `MITZI_ANDROID_KEYSTORE_PATH`: absolute path to the upload keystore.
- `MITZI_ANDROID_KEY_ALIAS`: upload key alias.
- `MITZI_ANDROID_KEYSTORE_PASSWORD`: keystore password.
- `MITZI_ANDROID_KEY_PASSWORD`: key password.

The build reads these values only during signing and restores the previous Player Settings afterward. It never logs passwords. Keystore extensions and `.env` files are ignored by Git. Do not put passwords into command-line arguments or committed files.

## Build

1. Open `Assets/Scenes/PawPath.unity` in the verified editor.
2. Run **Mitzi > Android > Prepare Release Settings**. This selects AAB, disables Development Build/debugging/profiler, and applies Android settings.
3. Run **Mitzi > Android > Validate Release Setup**, then **Build Signed AAB**.
4. Find the output in `Builds/Android/Mitzi-<version>-<versionCode>.aab` (ignored by Git).

For CI, invoke Unity with `-batchmode -quit -buildTarget Android -projectPath <project> -executeMethod PawPath.Editor.AndroidReleaseBuilder.Build -logFile <log>`. Inject signing variables into the process environment. The build refuses to overwrite an existing output with the same version.

## Verify the actual AAB before upload

- Run `bundletool validate --bundle=<file.aab>` using the bundletool JAR from the chosen Unity Android tools, and `jarsigner -verify <file.aab>` from its OpenJDK.
- Use `bundletool dump manifest --bundle=<file.aab> --module=base` to confirm package `com.mitzicat.game`, target SDK 36, intended version name/code, and that the app is not debuggable.
- Inspect `base/lib/arm64-v8a` inside the AAB. Verify all native libraries support 16 KB pages, including Unity, IL2CPP, Burst and any plugins. The editor version check alone does not prove this.
- Upload to Play internal testing and review App Bundle Explorer/device compatibility. Install the generated release on a real ARM64 phone and a 16 KB emulator/device; follow `QA_CHECKLIST.md`.

## Verified device-test packages — 18 October 2026

Signed non-development ARM64/IL2CPP packages were generated in an isolated project copy:

- `Builds/Android/Mitzi-1.0.0-1-device-test.aab`
- `Builds/Android/Mitzi-1.0.0-1-device-test.apk`
- Machine-readable report: `Builds/Android/android-release-verification.json`.

Bundle structure, AAB/APK signatures, upload certificate match, API 36 manifest, absence of the Google AAID permission, native LOAD/RELRO page safety and APK `zipalign -P 16` passed. RELRO checking accounts for a separate LOAD with no writable-data overlap, as described in Android documentation. These static checks do not replace a 16 KB runtime test or testing Play-generated APKs. Native libraries use compressed packaging with AGP 7.4; the project also explicitly disables uncompressed native libraries for future AAB builds.

The connected ARM64 Android 16/API 36 phone uses 4 KB pages. Initial installation was rejected by device permission, then succeeded on retry. The first-level animated tutorial was observed on the real phone and no Unity/AndroidRuntime error appeared in the checked app-process log. This is an opening smoke test, not full gameplay or 16 KB runtime validation. The privacy update was subsequently installed successfully with the same upload signature, preserving current progress.

The generated upload key is outside Git in `%USERPROFILE%/.mitzi-signing`, alias `mitzi-upload`. Local credentials use Windows DPAPI; they are not a portable backup. Keep a separate secure backup of the key and passwords. `tools/start_signed_android_build.ps1` launches an isolated candidate build without passwords in command arguments. `tools/verify_android_release.py` verifies artifacts without accepting signing passwords.

The candidate uses the new paw icon and disables rewarded advertisements. Normal store builds require reviewed public HTTPS privacy policy and real AdMob configuration; the editor build guard rejects incomplete production settings. The privacy text is available inside the game and in `Docs/privacy.html`, and was published with user authorization at https://melisau.github.io/Mitzi/privacy.html (HTTP 200 verified). See [AdMob setup](Docs/ADMOB_SETUP_TR.md) and [Data Safety preparation](Docs/PLAY_DATA_SAFETY_TR.md). Do not upload the device-test candidate as the finished store release.

References: [Google Play target API](https://support.google.com/googleplay/android-developer/answer/11926878?hl=en), [Android 16 KB page support](https://developer.android.com/guide/practices/page-sizes), [Unity 16 KB support](https://discussions.unity.com/t/android-unity-engine-support-for-16-kb-memory-page-sizes-android-15/1589588).

### Privacy update candidate

The latest signed AAB/APK and verification report are in `Builds/Android/PrivacyUpdate/`. The privacy text identifies the app as Mitzi and omits the developer's personal name, retaining the supplied contact email. The AAB was additionally converted with bundletool to a universal APK to confirm all four native libraries remain compressed; that verification APK uses the local debug certificate and is not the signed device-release APK. Only the signed device-test APK was installed. Public policy hosting is complete at https://melisau.github.io/Mitzi/privacy.html; production AdMob IDs have been configured; account/app approval, live-ad enablement and Play declarations remain pending.

CI Android store builds remain disabled until repository variable `MITZI_STORE_BUILD_READY=true` is explicitly configured after real ad IDs, policy review and signing-secret verification. The compile job still runs on main pushes. Existing remote signing secrets have not been replaced; check they match the chosen upload certificate before enabling store CI.
