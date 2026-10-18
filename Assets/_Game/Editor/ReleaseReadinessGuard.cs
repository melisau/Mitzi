#if UNITY_EDITOR
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor;
public sealed class ReleaseReadinessGuard : IPreprocessBuildWithReport
{
    public int callbackOrder=>-2000;
    public void OnPreprocessBuild(BuildReport report)
    {
        if(report.summary.platform==BuildTarget.Android && (report.summary.options&BuildOptions.Development)==0 && !PawPath.Editor.AndroidReleaseBuilder.IsDeviceTestBuild)
            ValidateAndroidConfiguration(EditorUserBuildSettings.buildAppBundle, EditorUserBuildSettings.exportAsGoogleAndroidProject);
    }

    public static void ValidateAndroidConfiguration(bool appBundle, bool exportProject)
    {
        var config = PawPath.Core.ReleaseConfiguration.Current;
        if (!appBundle && !exportProject && config != null && !config.advertisingEnabled)
        {
            UnityEngine.Debug.Log("Device-test APK: live advertisements are disabled. Store readiness checks still apply to AAB/export builds.");
            return;
        }
        PawPath.Editor.AndroidReleaseBuilder.RequireProductionConfiguration();
    }
}
#endif
