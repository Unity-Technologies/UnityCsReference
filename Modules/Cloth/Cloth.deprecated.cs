// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{ 
    public sealed partial class Cloth
    {
        [Obsolete("Parameter solverFrequency is obsolete and no longer supported. Please use clothSolverFrequency instead.")]
        public bool solverFrequency
        {
            get { return clothSolverFrequency > 0.0f; }
            set { clothSolverFrequency = value == true ? 120f : 0.0f; }  // use the default value
        }

        [Obsolete("useContinuousCollision is no longer supported, use enableContinuousCollision instead")]
        public float useContinuousCollision { get; set; }

        [Obsolete("Deprecated.Cloth.selfCollisions is no longer supported since Unity 5.0.", true)]
        public bool selfCollision { get; }
    }
}
