// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.U2D.PhysicsCore2D.Profiler
{
    // One timing from a world's profile, with the timings it contains beneath it.
    // The profile's values nest: the step covers the whole simulation, solving is part of that step, solving
    // constraints is part of solving, and so on. Presenting them as a tree keeps a value's time out of its
    // own parent's siblings, so no row double counts another.
    record PhysicsTimingNode
    {
        public readonly string name;
        public readonly float timeMs;
        public readonly List<PhysicsTimingNode> children;

        public PhysicsTimingNode(string name, float timeMs, List<PhysicsTimingNode> children = null)
        {
            this.name = name;
            this.timeMs = timeMs;
            this.children = children ?? new List<PhysicsTimingNode>();
        }

        // The whole step for one world, as the root of everything it contains: pair finding, collision and
        // solving, the solver's own stages beneath solving, and the per-substep stages beneath constraint
        // solving. Writing transforms is included, since a step is what the world cost this frame and where
        // each piece of it happens to be measured is of no interest to someone reading the times.
        public static PhysicsTimingNode BuildStep(PhysicsCore2DFrameData data)
        {
            var profile = data.worldProfile;

            var constraintStages = new List<PhysicsTimingNode>
            {
                new PhysicsTimingNode("Prepare Constraints", profile.prepareConstraints),
                new PhysicsTimingNode("Integrate Velocities", profile.integrateVelocities),
                new PhysicsTimingNode("Warm Starting", profile.warmStarting),
                new PhysicsTimingNode("Solve Impulses", profile.solveImpulses),
                new PhysicsTimingNode("Integrate Transforms", profile.integrateTransforms),
                new PhysicsTimingNode("Relax Impulses", profile.relaxImpulses),
                new PhysicsTimingNode("Store Impulses", profile.storeImpulses)
            };

            var solvingStages = new List<PhysicsTimingNode>
            {
                new PhysicsTimingNode("Prepare Stages", profile.solverSetup),
                new PhysicsTimingNode("Solve Constraints", profile.constraints, constraintStages),
                new PhysicsTimingNode("Body Transforms", profile.bodyTransforms),
                new PhysicsTimingNode("Split Islands", profile.splitIslands),
                new PhysicsTimingNode("Sleep Islands", profile.sleepIslands),
                new PhysicsTimingNode("Continuous Collision", profile.solveContinuous),
                new PhysicsTimingNode("Broadphase Refit", profile.broadphaseUpdates),
                new PhysicsTimingNode("Trigger Hits", profile.fastTriggers),
                new PhysicsTimingNode("Joint Events", profile.jointEvents),
                new PhysicsTimingNode("Hit Events", profile.hitEvents)
            };

            var stepStages = new List<PhysicsTimingNode>
            {
                new PhysicsTimingNode("Contact Pairs", profile.contactPairs),
                new PhysicsTimingNode("Collision Detection", profile.contactUpdates),
                new PhysicsTimingNode("Solving", profile.solving, solvingStages),
                new PhysicsTimingNode("Triggers", profile.updateTriggers),
                new PhysicsTimingNode("Write Transforms", profile.writeTransforms)
            };

            return new PhysicsTimingNode("Simulation Step", profile.simulationStep + profile.writeTransforms, stepStages);
        }

        // The same tree with every world's time added together, for the view that shows no world level.
        // Each world reports the same timings, so the trees have the same shape and merge position by position.
        public static PhysicsTimingNode Merge(List<PhysicsTimingNode> nodes)
        {
            var first = nodes[0];

            float totalTime = 0f;
            foreach (var node in nodes)
                totalTime += node.timeMs;

            var mergedChildren = new List<PhysicsTimingNode>(first.children.Count);
            for (int childIndex = 0; childIndex < first.children.Count; ++childIndex)
            {
                var childNodes = new List<PhysicsTimingNode>(nodes.Count);
                foreach (var node in nodes)
                {
                    if (childIndex < node.children.Count)
                        childNodes.Add(node.children[childIndex]);
                }

                mergedChildren.Add(Merge(childNodes));
            }

            return new PhysicsTimingNode(first.name, totalTime, mergedChildren);
        }
    }
}
