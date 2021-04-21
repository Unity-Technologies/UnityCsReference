// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bee.BeeDriver;

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

        public class UnityProgressAPIToken : ProgressToken
        {
            private readonly int _token;
            private int currentNodeIndex;

            // Contains all the nodes currently in progress, with a index which is a sequencial number
            // defining the order when they have been scheduled.
            private Dictionary<string, int> nodesInFlight = new Dictionary<string, int>();
            public UnityProgressAPIToken(string msg) => _token = Progress.Start(msg);

            public override void Report(string msg)
            {
                Progress.SetDescription(_token, msg);
            }

            public virtual void Report(float progress)
            {
                Progress.Report(_token, progress);
            }

            void UpdateReportedNode()
            {
                // Of all the nodes currently in progress, report the one most recently scheduled.
                if (nodesInFlight.Any())
                    Report(nodesInFlight.OrderByDescending(x => x.Value).First().Key);
            }

            public override void Report(NodeResult nodeResult)
            {
                nodesInFlight.Remove(nodeResult.annotation);
                UpdateReportedNode();

                Report(nodeResult.processed_node_count / (float)nodeResult.number_of_nodes_ever_queued);
            }

            public override void ReportNodeStarted(NodeResult nodeResult)
            {
                nodesInFlight[nodeResult.annotation] = currentNodeIndex++;
                UpdateReportedNode();
            }

            public override void Finish()
            {
                // Why do we run Progress.Finish on a Task here?
                // The BeeDriver will read tundra output logs on a thread, causing `ProgressToken.Report(NodeResult nodeResult)`
                // to be called on a non-main thread. Unity's `Progress` API implementation puts all non-main thread requests on
                // a queue which will be executed at a later point from the main thread. But then, the BeeDriver will call
                // `ProgressToken.Finish` from the main thread. This will be called directly, causing Finish to be executed before
                // `Report` - which causes errors. By calling Finish from a Task, we make sure that it _also_ gets called of the main
                // thread, and thus won't skip the queue. A better fix might be to change the progress implementation to flush all pending
                // calls on Finish.
                Task.Run(() => Progress.Finish(_token));
            }
        }
    }
}
