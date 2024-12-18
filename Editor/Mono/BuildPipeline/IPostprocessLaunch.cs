// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Build
{
    public enum LaunchResult
    {
        Unknown = 0,
        Succeeded = 1,
        // For ex., when running on multiple Android devices, some succeeded, some didn't
        PartiallySucceeded = 2,
        Failed = 3
    }

    public interface ILaunchReport
    {
        public NamedBuildTarget buildTarget { get; }

        public LaunchResult result { get; }
    }

    // Unity will provide DefaultLaunchReport for OnPostprocessLaunch callback if platform doesn't provide its own launch report
    public class DefaultLaunchReport : ILaunchReport
    {
        public NamedBuildTarget buildTarget { get; }

        public LaunchResult result { get; }

        internal DefaultLaunchReport(NamedBuildTarget buildTarget, LaunchResult result)
        {
            this.buildTarget = buildTarget;
            this.result = result;
        }
    }

    public interface IPostprocessLaunch : IOrderedCallback
    {
        void OnPostprocessLaunch(ILaunchReport launchReport);
    }
}
