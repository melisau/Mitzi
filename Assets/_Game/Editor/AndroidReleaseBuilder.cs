using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PawPath.Editor
{
    public static class AndroidReleaseBuilder
    {
        const int TargetApi = 36;

        [MenuItem("Mitzi/Android/Prepare Release Settings")]
        public static void Prepare()
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)TargetApi;
            if (string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion == "1.0")
                PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.Android.bundleVersionCode = Math.Max(1, PlayerSettings.Android.bundleVersionCode);
            EditorUserBuildSettings.buildAppBundle = true;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.connectProfiler = false;
            AssetDatabase.SaveAssets();
            Debug.Log("Android release settings applied: ARM64, IL2CPP, API 36, non-development AAB.");
        }

        [MenuItem("Mitzi/Android/Validate Release Setup")]
        public static void Validate()
        {
            Prepare();
            ValidateToolchain();
            RequireSigningEnvironment();
            Debug.Log("Android release prerequisites passed. The generated AAB still needs bundletool and device validation.");
        }

        [MenuItem("Mitzi/Android/Build Signed AAB")]
        public static void Build()
        {
            Prepare();
            ValidateToolchain();
            RequireSigningEnvironment();
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled)
                .Select(scene => scene.path).ToArray();
            if (scenes.Length == 0 || scenes.Any(scene => !File.Exists(scene)))
                throw new BuildFailedException("Enable valid release scenes in Build Settings.");

            string output = Path.GetFullPath(Path.Combine("Builds", "Android",
                $"Mitzi-{PlayerSettings.bundleVersion}-{PlayerSettings.Android.bundleVersionCode}.aab"));
            if (File.Exists(output))
                throw new BuildFailedException("An AAB with this version already exists. Move it or increment the version code before building.");

            bool previousCustomSigning = PlayerSettings.Android.useCustomKeystore;
            string previousKeystore = PlayerSettings.Android.keystoreName;
            string previousAlias = PlayerSettings.Android.keyaliasName;
            string previousStorePassword = PlayerSettings.Android.keystorePass;
            string previousKeyPassword = PlayerSettings.Android.keyaliasPass;
            try
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = Environment.GetEnvironmentVariable("MITZI_ANDROID_KEYSTORE_PATH");
                PlayerSettings.Android.keyaliasName = Environment.GetEnvironmentVariable("MITZI_ANDROID_KEY_ALIAS");
                PlayerSettings.Android.keystorePass = Environment.GetEnvironmentVariable("MITZI_ANDROID_KEYSTORE_PASSWORD");
                PlayerSettings.Android.keyaliasPass = Environment.GetEnvironmentVariable("MITZI_ANDROID_KEY_PASSWORD");
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = output,
                    target = BuildTarget.Android,
                    options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded || !File.Exists(output))
                    throw new BuildFailedException("Android AAB build failed. See the Unity build log.");
                Debug.Log($"Signed Android AAB created: {output}. Validate its manifest, native libraries and signature before uploading.");
            }
            finally
            {
                PlayerSettings.Android.keystorePass = previousStorePassword;
                PlayerSettings.Android.keyaliasPass = previousKeyPassword;
                PlayerSettings.Android.keystoreName = previousKeystore;
                PlayerSettings.Android.keyaliasName = previousAlias;
                PlayerSettings.Android.useCustomKeystore = previousCustomSigning;
            }
        }

        static void ValidateToolchain()
        {
            // 2022.3.50 predates Unity's Android 16 KB native-library support.
            string[] version = Application.unityVersion.Split('.');
            int patch = int.Parse(new string(version[2].TakeWhile(char.IsDigit).ToArray()));
            int major = int.Parse(version[0]);
            bool supportedEditor = major >= 6000 ||
                major == 2022 && version[1] == "3" && patch >= 56;
            if (!supportedEditor)
                throw new BuildFailedException("Use Unity 2022.3.56f1 or a newer compatible patched editor for Android 16 KB support. Upgrade and test the project before release.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new BuildFailedException("Install Android Build Support, Android SDK/NDK Tools and OpenJDK for this Unity editor.");

            // Resolve Unity's actual External Tools SDK without making this editor
            // script depend on Android extension types when the module is absent.
            Type toolsType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("UnityEditor.Android.AndroidExternalToolsSettings"))
                .FirstOrDefault(type => type != null);
            string sdk = toolsType?.GetProperty("sdkRootPath")?.GetValue(null) as string;
            if (string.IsNullOrWhiteSpace(sdk))
                throw new BuildFailedException("Unity's configured Android SDK path could not be resolved. Check Preferences > External Tools.");
            if (!File.Exists(Path.Combine(sdk, "platforms", $"android-{TargetApi}", "android.jar")))
                throw new BuildFailedException("Android API 36 is missing from the configured SDK. Install it and verify External Tools SDK configuration.");
            if (PlayerSettings.Android.bundleVersionCode <= 0)
                throw new BuildFailedException("Android version code must be positive and higher than every previously uploaded release.");
        }

        static void RequireSigningEnvironment()
        {
            string[] names = { "MITZI_ANDROID_KEYSTORE_PATH", "MITZI_ANDROID_KEY_ALIAS",
                "MITZI_ANDROID_KEYSTORE_PASSWORD", "MITZI_ANDROID_KEY_PASSWORD" };
            foreach (string name in names)
                if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name)))
                    throw new BuildFailedException($"Missing signing environment variable: {name}. See ANDROID_RELEASE.md. Passwords must not be committed.");
            if (!File.Exists(Environment.GetEnvironmentVariable(names[0])))
                throw new BuildFailedException("The configured Android upload keystore file does not exist.");
        }
    }
}
