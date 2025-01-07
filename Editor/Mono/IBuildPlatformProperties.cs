// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor;

internal interface IBuildPlatformProperties : IPlatformProperties
{
    // The BuildPlayerWindow.ActiveBuildTargetsGUI method uses this property to determine whether or not to display certain
    // build options based on whether or not the desired build target is compatible with the OS on which the editor is running.
    bool CanBuildOnCurrentHostPlatform => true;

    // The BuildEventsHandlerPostProcess.OnPostprocessBuild method uses this method to report permissions for a build target.
    // This method replaces the BuildEventsHandlerPostProcess.ReportBuildTargetPermissions private method.
    public void ReportBuildTargetPermissions(BuildOptions buildOptions) {}

    // The BuildPlayerWindow.BuildPlayerAndRun method uses this method to set the build location for those build targets
    // that require special handling.  Only the stand-alone Windows build targets implement this method.
    public string ValidateBuildLocation() => null;

    // The BuildProfileWindow uses this property to determine if required packages should be installed
    // when activating the classic platform for a given build target.
    public bool ShouldInstallRequiredPackagesOnActivationOfClassicPlatform => false;
}
