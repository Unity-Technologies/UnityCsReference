// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Build;

namespace UnityEditor.ShaderVariantAnalyser
{
    class VariantsDatabaseWriterScope : IPostprocessBuildWithContext, IPreprocessBuildWithContext
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildCallbackContext ctx)
        {
            var guid = ctx.Report.summary.buildSessionGuid;
            if (BuildHistory.TryGetBuildReportDirectory(guid, out var dir))
            {
                if (!ctx.Report.summary.options.HasFlag(BuildOptions.BuildScriptsOnly))
                {
                    dir = System.IO.Path.Combine(dir, "variant_analyzer");
                    System.IO.Directory.CreateDirectory(dir);
                    DataRecorder.InitializeAt(dir);
                }
            }
        }

        public void OnPostprocessBuild(BuildCallbackContext ctx)
        {
            DataRecorder.FinishWriting();

            // If the build failed don't leave partial files there
            if (ctx.Report.summary.result != Build.Reporting.BuildResult.Succeeded)
            {
                var guid = ctx.Report.summary.buildSessionGuid;
                if (BuildHistory.TryGetBuildReportDirectory(guid, out var dir))
                {
                    dir = System.IO.Path.Combine(dir, "variant_analyzer");
                    if (System.IO.Directory.Exists(dir))
                        System.IO.Directory.Delete(dir, true);
                }
            }
        }
    }
}
