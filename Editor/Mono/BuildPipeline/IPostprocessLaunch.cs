// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Build
{
    ///<summary>Describes the outcome of the application launch process.</summary>
    public enum LaunchResult
    {
        ///<summary>Unity attempted to launch the application, but it's unknown whether it's successfully launched.</summary>
        Unknown = 0,
        ///<summary>The application is successfully launched.</summary>
        Succeeded = 1,
        // For ex., when running on multiple Android devices, some succeeded, some didn't
        ///<summary>The application is successfully launched on some target devices, but not all.</summary>
        ///<remarks>For example, Android allows an application to launch on multiple devices at once. In this case, the application might fail to launch on some devices, but succeed on others.</remarks>
        PartiallySucceeded = 2,
        ///<summary>The application is failed to launch.</summary>
        Failed = 3
    }

    ///<summary>Interface to receive information about the application launch.</summary>
    ///<remarks>Implemented by <see cref="Build.IPostprocessLaunch.OnPostprocessLaunch" /></remarks>
    public interface ILaunchReport
    {
        ///<summary>The target platform on which the application build was launched.</summary>
        public NamedBuildTarget buildTarget { get; }

        ///<summary>The outcome of the application launch.</summary>
        public LaunchResult result { get; }
    }

    // Unity will provide DefaultLaunchReport for OnPostprocessLaunch callback if platform doesn't provide its own launch report
    ///<summary>Provides information about the application launch.</summary>
    ///<remarks>Use this class to obtain information about the application launched on a target platform that does not provide its own launch report.</remarks>
    public class DefaultLaunchReport : ILaunchReport
    {
        ///<summary>The target platform on which the application build was launched.</summary>
        public NamedBuildTarget buildTarget { get; }

        ///<summary>The outcome of the application launch.</summary>
        public LaunchResult result { get; }

        internal DefaultLaunchReport(NamedBuildTarget buildTarget, LaunchResult result)
        {
            this.buildTarget = buildTarget;
            this.result = result;
        }
    }

    ///<summary>Interface for receiving a callback after the application is launched on a target device.</summary>
    ///<remarks>Unity invokes <c>OnPostprocessLaunch</c> callback after attempting to launch the application. For more information about build callbacks, refer to [Use build callbacks](xref:um-build-callbacks)</remarks>
    public interface IPostprocessLaunch : IOrderedCallback
    {
        ///<summary>Implement this method to receive a callback after Unity attempts to launch the application.</summary>
        ///<remarks>Unity invokes this callback regardless of whether the application launch was successful or not.
        ///
        ///The following platforms are not supported:
        ///
        ///* Nintendo Switch
        ///* PlayStation 4
        ///* PlayStation 5
        ///* Xbox One
        ///* Xbox Series X|S.</remarks>
        ///<param name="launchReport">A report containing information about the launch, such as the target platform on which the application was launched and outcome of the process. Some platforms such as Android provide additional launch information which you can obtain by performing a necessary cast.</param>
        ///<example>
        ///  <code source="../../../Modules/ContentBuild/Tests/local.test.build-examples/Editor/BuildPipeline/IPostprocessLaunch_OnPostprocessLaunch.cs"/>
        ///</example>
        ///<seealso cref="Android.AndroidLaunchReportExtensions.AsAndroidReport" />
        void OnPostprocessLaunch(ILaunchReport launchReport);
    }
}
