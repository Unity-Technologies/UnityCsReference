// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.U2D.PhysicsCore2D.Profiler.UI
{
    record PhysicsWorldTreeData
    {
        public readonly int worldIndex;
        public readonly string name;
        public readonly List<PhysicsModuleData> modules;

        public PhysicsWorldTreeData(PhysicsCore2DFrameData frameData)
        {
            this.worldIndex = frameData.worldIndex;
            this.name = frameData.worldIndex == Unity.U2D.Physics.PhysicsWorld.defaultWorld.index ? "Default World" : $"World {frameData.worldIndex}";
            this.modules = PhysicsModuleData.ExtractModules(frameData);
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

        public static TreeNodeData CreateWorldNode(int id, PhysicsWorldTreeData worldData)
        {
            float totalTime = 0f;
            foreach (var module in worldData.modules)
                totalTime += module.totalTime;
            return new TreeNodeData(id, TreeNodeType.World, worldData.name, $"{totalTime:F3}ms", totalTime);
        }

        public static TreeNodeData CreateModuleNode(int id, PhysicsModuleData module)
        {
            return new TreeNodeData(id, TreeNodeType.Module, module.name, $"{module.totalTime:F3}ms", module.totalTime);
        }

        public static TreeNodeData CreateTimingEntryNode(int id, PhysicsTimingEntry entry)
        {
            return new TreeNodeData(id, TreeNodeType.TimingEntry, entry.name, $"{entry.timeMs:F3}ms", entry.timeMs);
        }
    }
}
