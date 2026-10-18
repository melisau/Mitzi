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

No signed AAB has been produced or verified yet. Upload signing credentials and actual build/device validation are still required. Do not treat the settings changes as confirmation of Play approval.

References: [Google Play target API](https://support.google.com/googleplay/android-developer/answer/11926878?hl=en), [Android 16 KB page support](https://developer.android.com/guide/practices/page-sizes), [Unity 16 KB support](https://discussions.unity.com/t/android-unity-engine-support-for-16-kb-memory-page-sizes-android-15/1589588).
