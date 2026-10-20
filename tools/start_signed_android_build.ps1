param(
    [Parameter(Mandatory=$true)][string]$ProjectPath,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [Parameter(Mandatory=$true)][string]$LogPath,
    [switch]$ApkOnly,
    [switch]$Store,
    [string]$UnityPath="C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Unity.exe",
    [string]$CredentialPath=(Join-Path $env:USERPROFILE '.mitzi-signing/signing.credentials.xml')
)
$ErrorActionPreference='Stop'
$projectRoot=(Resolve-Path -LiteralPath $ProjectPath).Path
if(!(Test-Path -LiteralPath (Join-Path $projectRoot 'Assets/Scenes/PawPath.unity'))){throw 'Unexpected Unity project'}
if(!(Test-Path -LiteralPath $CredentialPath)){throw 'Signing credentials missing. Create an upload key first.'}
if($Store -and $ApkOnly){throw 'Store creates an AAB; do not combine with ApkOnly.'}
$signing=Import-Clixml -LiteralPath $CredentialPath
try {
    $env:MITZI_ANDROID_KEYSTORE_PATH=$signing.KeystorePath
    $env:MITZI_ANDROID_KEY_ALIAS=$signing.Alias
    $env:MITZI_ANDROID_KEYSTORE_PASSWORD=[pscredential]::new('upload',$signing.StorePassword).GetNetworkCredential().Password
    $env:MITZI_ANDROID_KEY_PASSWORD=[pscredential]::new('upload',$signing.KeyPassword).GetNetworkCredential().Password
    $env:MITZI_ANDROID_OUTPUT_DIR=[IO.Path]::GetFullPath($OutputDirectory)
    $buildMethod=if($Store){'PawPath.Editor.AndroidReleaseBuilder.Build'}elseif($ApkOnly){'PawPath.Editor.AndroidReleaseBuilder.BuildCandidateApk'}else{'PawPath.Editor.AndroidReleaseBuilder.BuildCandidatePackages'}
    $arguments=@('-batchmode','-quit','-buildTarget','Android','-projectPath',('"'+$projectRoot+'"'),'-executeMethod',$buildMethod,'-logFile',('"'+[IO.Path]::GetFullPath($LogPath)+'"'))
    $buildProcess=Start-Process -WindowStyle Hidden -FilePath $UnityPath -ArgumentList $arguments -PassThru
    Write-Output ('Android build PID: '+$buildProcess.Id)
} finally {
    foreach($name in @('MITZI_ANDROID_KEYSTORE_PATH','MITZI_ANDROID_KEY_ALIAS','MITZI_ANDROID_KEYSTORE_PASSWORD','MITZI_ANDROID_KEY_PASSWORD','MITZI_ANDROID_OUTPUT_DIR')){Remove-Item -LiteralPath ('Env:'+ $name) -ErrorAction SilentlyContinue}
    $signing=$null
}
