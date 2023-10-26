// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;

namespace UnityEngine
{
    public partial class Rigidbody
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("The sleepVelocity is no longer supported. Use sleepThreshold. Note that sleepThreshold is energy but not velocity.", true)]
        public float sleepVelocity { get { return 0; } set { } }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("The sleepAngularVelocity is no longer supported. Use sleepThreshold to specify energy.", true)]
        public float sleepAngularVelocity { get { return 0; } set { } }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use Rigidbody.maxAngularVelocity instead.")]
        public void SetMaxAngularVelocity(float a) { maxAngularVelocity = a; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Cone friction is no longer supported.", true)]
        public bool useConeFriction { get { return false; } set { } }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Please use Rigidbody.solverIterations instead. (UnityUpgradable) -> solverIterations")]
        public int solverIterationCount { get { return solverIterations; } set { solverIterations = value; } }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Please use Rigidbody.solverVelocityIterations instead. (UnityUpgradable) -> solverVelocityIterations")]
        public int solverVelocityIterationCount { get { return solverVelocityIterations; } set { solverVelocityIterations = value; } }
    }
}
