// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.U2D.PhysicsCore2D.Profiler.UI
{
    record PhysicsWorldTreeData
    {
        public readonly string name;
        public readonly PhysicsTimingNode timing;

        public PhysicsWorldTreeData(PhysicsCore2DFrameData frameData, string name)
        {
            this.name = name;
            this.timing = PhysicsTimingNode.BuildStep(frameData);
        }
    }

    enum TreeNodeType
    {
        World,
        Module,
        TimingEntry,
    }

    record TreeNodeData
    {
        public readonly int id;
        public readonly TreeNodeType type;
        public readonly string name;
        public readonly string value;
        public readonly float timeMs;

        public TreeNodeData(int id, TreeNodeType type, string name, string value, float timeMs = 0f)
        {
            this.id = id;
            this.type = type;
            this.name = name;
            this.value = value;
            this.timeMs = timeMs;
        }

        // A world's row carries its whole step, which is everything that world cost this frame.
        public static TreeNodeData CreateWorldNode(int id, PhysicsWorldTreeData worldData)
        {
            var totalTime = worldData.timing.timeMs;

            return new TreeNodeData(id, TreeNodeType.World, worldData.name, $"{totalTime:F3}ms", totalTime);
        }

        // A timing's own row. A timing with others beneath it reads as a module so it is styled as a heading,
        // and one with nothing beneath it reads as a plain entry.
        public static TreeNodeData CreateTimingNode(int id, PhysicsTimingNode timing)
        {
            var type = timing.children.Count > 0 ? TreeNodeType.Module : TreeNodeType.TimingEntry;

            return new TreeNodeData(id, type, timing.name, $"{timing.timeMs:F3}ms", timing.timeMs);
        }
    }
}
