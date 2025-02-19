// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;

namespace UnityEngine
{
    public partial class Physics
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Physics.autoSimulation has been replaced by Physics.simulationMode", false)]
        public static bool autoSimulation
        {
            get { return simulationMode != SimulationMode.Script; }
            set { simulationMode = value ? SimulationMode.FixedUpdate : SimulationMode.Script; }
        }

        [Obsolete("Physics.RebuildBroadphaseRegions has been deprecated alongside Multi Box Pruning. Use Automatic Box Pruning instead.", false)]
        public static void RebuildBroadphaseRegions(Bounds worldBounds, int subdivisions)
        {
            return;
        }
    }
}
