// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.U2D.PhysicsCore2D.Profiler
{
    enum PhysicsModuleType
    {
        Simulation,
        Collision,
        Solving,
        Islands,
        Transforms,
        Triggers,
        Continuous
    }

    record PhysicsModuleData
    {
        public readonly PhysicsModuleType moduleType;
        public readonly string name;
        public readonly float totalTime;
        public readonly System.Collections.Generic.List<PhysicsTimingEntry> entries;

        public PhysicsModuleData(PhysicsModuleType type, string name, float totalTime, System.Collections.Generic.List<PhysicsTimingEntry> entries)
        {
            this.moduleType = type;
            this.name = name;
            this.totalTime = totalTime;
            this.entries = entries ?? new System.Collections.Generic.List<PhysicsTimingEntry>();
        }

        public static System.Collections.Generic.List<PhysicsModuleData> ExtractModules(PhysicsCore2DFrameData data)
        {
            var modules = new System.Collections.Generic.List<PhysicsModuleData>();
            var profile = data.worldProfile;

            modules.Add(new PhysicsModuleData(
                PhysicsModuleType.Simulation,
                "Simulation",
                profile.simulationStep,
                new System.Collections.Generic.List<PhysicsTimingEntry> { new PhysicsTimingEntry("Simulation Step", profile.simulationStep) }
            ));

            modules.Add(new PhysicsModuleData(
                PhysicsModuleType.Collision,
                "Collision Detection",
                profile.contactPairs + profile.contactUpdates + profile.broadphaseUpdates,
                new System.Collections.Generic.List<PhysicsTimingEntry> { new PhysicsTimingEntry("Contact Pairs", profile.contactPairs), new PhysicsTimingEntry("Contact Updates", profile.contactUpdates), new PhysicsTimingEntry("Broadphase Updates", profile.broadphaseUpdates) }
            ));

            modules.Add(new PhysicsModuleData(
                PhysicsModuleType.Solving,
                "Constraint Solving",
                profile.solving + profile.constraints,
                new System.Collections.Generic.List<PhysicsTimingEntry>
                {
                    new PhysicsTimingEntry("Solving", profile.solving),
                    new PhysicsTimingEntry("Prepare Stages", profile.solverSetup),
                    new PhysicsTimingEntry("Solve Constraints", profile.constraints),
                    new PhysicsTimingEntry("Prepare Constraints", profile.prepareConstraints),
                    new PhysicsTimingEntry("Warm Starting", profile.warmStarting),
                    new PhysicsTimingEntry("Solve Impulses", profile.solveImpulses),
                    new PhysicsTimingEntry("Relax Impulses", profile.relaxImpulses),
                    new PhysicsTimingEntry("Store Impulses", profile.storeImpulses)
                }
            ));

            modules.Add(new PhysicsModuleData(
                PhysicsModuleType.Islands,
                "Island Management",
                profile.splitIslands + profile.sleepIslands,
                new System.Collections.Generic.List<PhysicsTimingEntry> { new PhysicsTimingEntry("Split Islands", profile.splitIslands), new PhysicsTimingEntry("Sleep Islands", profile.sleepIslands) }
            ));

            modules.Add(new PhysicsModuleData(
                PhysicsModuleType.Transforms,
                "Transform Integration",
                profile.integrateVelocities + profile.integrateTransforms + profile.bodyTransforms + profile.writeTransforms,
                new System.Collections.Generic.List<PhysicsTimingEntry> { new PhysicsTimingEntry("Integrate Velocities", profile.integrateVelocities), new PhysicsTimingEntry("Integrate Transforms", profile.integrateTransforms), new PhysicsTimingEntry("Body Transforms", profile.bodyTransforms), new PhysicsTimingEntry("Write Transforms", profile.writeTransforms) }
            ));

            modules.Add(new PhysicsModuleData(
                PhysicsModuleType.Triggers,
                "Triggers & Events",
                profile.fastTriggers + profile.updateTriggers + profile.jointEvents + profile.hitEvents,
                new System.Collections.Generic.List<PhysicsTimingEntry> { new PhysicsTimingEntry("Fast Triggers", profile.fastTriggers), new PhysicsTimingEntry("Update Triggers", profile.updateTriggers), new PhysicsTimingEntry("Joint Events", profile.jointEvents), new PhysicsTimingEntry("Hit Events", profile.hitEvents) }
            ));

            modules.Add(new PhysicsModuleData(
                PhysicsModuleType.Continuous,
                "Continuous Collision",
                profile.solveContinuous,
                new System.Collections.Generic.List<PhysicsTimingEntry> { new PhysicsTimingEntry("Solve Continuous", profile.solveContinuous) }
            ));

            return modules;
        }
    }

    record PhysicsTimingEntry
    {
        public readonly string name;
        public readonly float timeMs;

        public PhysicsTimingEntry(string name, float timeMs)
        {
            this.name = name;
            this.timeMs = timeMs;
        }
    }
}
