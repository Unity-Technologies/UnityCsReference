// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
namespace UnityEngine
{
    [NativeHeader("Runtime/Cloth/Cloth.h")]
    public partial class Cloth
    {
        extern public float sleepThreshold { get; set; }

        // Bending stiffness of the cloth.
        extern public float bendingStiffness { get; set; }

        // Stretching stiffness of the cloth.
        extern public float stretchingStiffness { get; set; }

        // Damp cloth motion.
        extern public float damping { get; set; }

        // A constant, external acceleration applied to the cloth.
        extern public Vector3 externalAcceleration { get; set; }

        // A random, external acceleration applied to the cloth.
        extern public Vector3 randomAcceleration { get; set; }

        // Should gravity affect the cloth simulation?
        extern public bool useGravity { get; set; }

        // Is this cloth enabled?
        extern public bool enabled { get; set; }

        // The friction of the cloth when colliding with the character.
        extern public float friction { get; set; }

        // How much to increase mass of colliding particles
        extern public float collisionMassScale { get; set; }

        // Enable continuous collision to improve collision stability
        extern public bool enableContinuousCollision { get; set; }

        // Add 1 virtual particle per triangle to improve collision stability
        extern public float useVirtualParticles { get; set; }

        // How much world-space movement of the character will affect cloth vertices.
        extern public float worldVelocityScale { get; set; }

        // How much world-space acceleration of the character will affect cloth vertices.
        extern public float worldAccelerationScale { get; set; }

        extern public float clothSolverFrequency { get; set; }

        extern public bool useTethers { get; set; }

        extern public float stiffnessFrequency { get; set; }

        extern public float selfCollisionDistance { get; set; }

        extern public float selfCollisionStiffness { get; set; }
    }
}
