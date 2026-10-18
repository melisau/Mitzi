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
            PawPath.Editor.AndroidReleaseBuilder.RequireProductionConfiguration();
    }
}
#endif
