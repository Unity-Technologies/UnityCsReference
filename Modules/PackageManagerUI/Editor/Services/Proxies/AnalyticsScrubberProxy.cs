// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics.CodeAnalysis;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IAnalyticsScrubberProxy : IService
    {
        string ScrubPackageId(string packageId);
        string ScrubUserPaths(string text);
    }

    [ExcludeFromCodeCoverage]
    internal class AnalyticsScrubberProxy : BaseService<IAnalyticsScrubberProxy>, IAnalyticsScrubberProxy
    {
        public string ScrubPackageId(string packageId)
        {
            return AnalyticsScrubber.ScrubPackageId(packageId);
        }

        public string ScrubUserPaths(string text)
        {
            return AnalyticsScrubber.ScrubUserPaths(text);
        }
    }
}
