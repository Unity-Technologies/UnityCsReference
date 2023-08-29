// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor;

internal interface IBuildPlatformProperties : IPlatformProperties
{
    // The BuildEventsHandlerPostProcess.OnPostprocessBuild method uses this method to report permissions for a build target.
    // This method replaces the BuildEventsHandlerPostProcess.ReportBuildTargetPermissions private method.
    public void ReportBuildTargetPermissions() {}

    // The BuildPlayerWindow.BuildPlayerAndRun method uses this method to set the build location for those build targets
    // that require special handling.  Only the stand-alone Windows build targets implement this method.
    public string ValidateBuildLocation() => null;
}
