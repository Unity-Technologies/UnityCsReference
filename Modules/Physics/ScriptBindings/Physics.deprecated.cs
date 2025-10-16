// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using UnityEngine.Internal;

namespace UnityEngine
{
    public partial class Physics
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Physics.autoSimulation has been replaced by Physics.simulationMode", false)]
        [ExcludeFromDocs]
        public static bool autoSimulation
        {
            get { return simulationMode != SimulationMode.Script; }
            set { simulationMode = value ? SimulationMode.FixedUpdate : SimulationMode.Script; }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Physics.autoSyncTransforms has been deprecated please use Physics.SyncTransforms instead to manually sync physics transforms when required.", false)]
        [ExcludeFromDocs]
        extern public static bool autoSyncTransforms { get; set; }
    }
}
