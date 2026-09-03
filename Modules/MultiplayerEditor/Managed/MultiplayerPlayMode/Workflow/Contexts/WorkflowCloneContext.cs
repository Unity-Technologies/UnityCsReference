// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: HeadlessRuntime not yet converted
using System.IO;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class WorkflowCloneContext
    {
        internal CloneDataFile CloneDataFile { get; }
        internal ClonedPlayerSystems ClonedPlayerSystems { get; }

        static readonly string DefaultCloneEditorPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        internal WorkflowCloneContext(CloneContext cloneContext)
        {
            ClonedPlayerSystems = new ClonedPlayerSystems();
            {
                CloneDataFile = new CloneDataFile
                {
                    Path = CloneDataFile.CloneDataPath(DefaultCloneEditorPath),
                    Data = CloneData.NewDefault(),
                };
                var workflow = new StandardCloneWorkflow();
                workflow.Initialize(mppmContext: this, vpContext: cloneContext);
            }
            ClonedPlayerSystems.Listen(mppmContext: this, vpContext: cloneContext);
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
