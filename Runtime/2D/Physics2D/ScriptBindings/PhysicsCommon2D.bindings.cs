// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    public enum SimulationMode2D
    {
        FixedUpdate = 0,
        Update = 1,
        Script = 2
    }

    public enum RigidbodyType2D
    {
        // Dynamic body.
        Dynamic = 0,

        // Kinematic body.
        Kinematic = 1,

        // Static body.
        Static = 2,
    }

    [Flags]
    public enum RigidbodyConstraints2D
    {
        // No constraints
        None = 0,

        // Freeze motion along the X-axis.
        FreezePositionX = 1 << 0,

        // Freeze motion along the Y-axis.
        FreezePositionY = 1 << 1,

        // Freeze rotation along the Z-axis.
        FreezeRotation = 1 << 2,

        // Freeze motion along all axes.
        FreezePosition = FreezePositionX | FreezePositionY,

        // Freeze rotation and motion along all axes.
        FreezeAll = FreezePosition | FreezeRotation,
    }

    // The method used to combine both material values.
    public enum PhysicsMaterialCombine2D
    {
        // The average of both material values.
        Average = 0,

        // The geometric mean of both material values.
        Mean,

        // The product of both material values.
        Multiply,

        // The minium of both material values.
        Minimum,

        // The maximum of both material values.
        Maximum
    }
}
