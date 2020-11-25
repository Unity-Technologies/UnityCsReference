// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Bee.BeeDriver;
using NiceIO;
using ScriptCompilationBuildProgram.Data;

namespace UnityEditor.Scripting.ScriptCompilation
{
    /// <summary>
    /// this classed is used to give to a BeeDriver.  The BeeDriver will call the virutal Report() method as it makes progress through the build. This
    /// class will route that information to Unity's UnityEditor.Progress api which is what gives the background progress bar in the activity window in the bottom
    /// right of the unity window
    /// </summary>
    class UnityProgressAPI : ProgressAPI
    {
        private string Title { get; }

        public UnityProgressAPI(string title)
        {
            Title = title;
        }

        public override ProgressToken Start() => new UnityProgressAPIToken(Title);

        private class UnityProgressAPIToken : ProgressToken
        {
            private readonly int _token;
            public UnityProgressAPIToken(string msg) => _token = Progress.Start(msg);

            public override void Report(string msg, int currentStep, int totalStep)
            {
                Progress.Report(_token, currentStep, totalStep, msg);
            }

            public override void Report(NodeResult nodeResult)
            {
                if (nodeResult.outputfile == null)
                {
                    var msg = nodeResult.annotation.Substring(nodeResult.annotation.LastIndexOf('/') + 1);
                    Report(msg, nodeResult.processed_node_count, nodeResult.amount_of_nodes_ever_queued);
                    return;
                }

                var outputFile = new NPath(nodeResult.outputfile);
                if (outputFile.HasExtension(Constants.MovedFromExtension))
                    return;

                Report(outputFile.FileName, nodeResult.processed_node_count, nodeResult.amount_of_nodes_ever_queued);
            }

            public override void Finish() => Progress.Finish(_token);
        }
    }
}
