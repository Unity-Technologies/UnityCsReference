// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // The rendering mode for particle systems
    public enum ParticleSystemRenderMode
    {
        Billboard = 0,              // Render particles as billboards facing the player.
        Stretch = 1,                // Stretch particles in the direction of motion.
        HorizontalBillboard = 2,    // Render particles as billboards always facing up along the y-Axis.
        VerticalBillboard = 3,      // Render particles as billboards always facing the player, but not pitching along the x-Axis.
        Mesh = 4,                   // Render particles as meshes.
        None = 5                    // Don't render particles. (e.g. useful when using the Trail or Lights Module)
    }

    // The sorting mode for particle systems
    public enum ParticleSystemSortMode
    {
        None = 0,                   // No sorting.
        Distance = 1,               // Sort based on distance.
        OldestInFront = 2,          // Sort the oldest particles to the front.
        YoungestInFront = 3,        // Sort the youngest particles to the front.
    }

    // The world collision quality
    public enum ParticleSystemCollisionQuality
    {
        High = 0,
        Medium = 1,
        Low = 2
    }

    // The rendering space for particle systems
    public enum ParticleSystemRenderSpace
    {
        View = 0,                   // Particles face the camera plane.
        World = 1,                  // Particles align with the world.
        Local = 2,                  // Particles align with their local transform.
        Facing = 3,                 // Particles face the eye position.
        Velocity = 4                // Particles are aligned based on their velocity.
    }

    // The particle curve mode
    public enum ParticleSystemCurveMode
    {
        Constant = 0,               // Emit using a single value.
        Curve = 1,                  // Emit based on a curve.
        TwoCurves = 2,              // Emit based on a random value between 2 curves.
        TwoConstants = 3            // Emit based on a random value between 2 constants.
    }

    // The particle gradient mode
    public enum ParticleSystemGradientMode
    {
        Color = 0,                  // Emit using a single color.
        Gradient = 1,               // Emit based on a color gradient.
        TwoColors = 2,              // Emit based on a random value between 2 colors.
        TwoGradients = 3,           // Emit based on a random value between 2 color gradients.
        RandomColor = 4             // Emit by picking a random color from a list.
    }

    // The emission shape
    public enum ParticleSystemShapeType
    {
        Sphere = 0,                 // Emit from the volume of a sphere.
        [Obsolete("SphereShell is deprecated and does nothing. Please use ShapeModule.radiusThickness instead, to control edge emission.", false)]
        SphereShell = 1,            // Emit from the surface of a sphere.
        Hemisphere = 2,             // Emit from the volume of a half-sphere.
        [Obsolete("HemisphereShell is deprecated and does nothing. Please use ShapeModule.radiusThickness instead, to control edge emission.", false)]
        HemisphereShell = 3,        // Emit from the surface of a half-sphere.
        Cone = 4,                   // Emit from the base surface of a cone.
        Box = 5,                    // Emit from the volume of a box.
        Mesh = 6,                   // Emit from a mesh.
        [Obsolete("ConeShell is deprecated and does nothing. Please use ShapeModule.radiusThickness instead, to control edge emission.", false)]
        ConeShell = 7,              // Emit from the base surface of a cone.
        ConeVolume = 8,             // Emit from the volume of a cone.
        [Obsolete("ConeVolumeShell is deprecated and does nothing. Please use ShapeModule.radiusThickness instead, to control edge emission.", false)]
        ConeVolumeShell = 9,        // Emit from the surface of a cone.
        Circle = 10,                // Emit from a circle.
        [Obsolete("CircleEdge is deprecated and does nothing. Please use ShapeModule.radiusThickness instead, to control edge emission.", false)]
        CircleEdge = 11,            // Emit from the edge of a circle.
        SingleSidedEdge = 12,       // Emit from an edge.
        MeshRenderer = 13,          // Emit from a mesh renderer.
        SkinnedMeshRenderer = 14,   // Emit from a skinned mesh renderer.
        BoxShell = 15,              // Emit from the surface of a box.
        BoxEdge = 16,               // Emit from the edges of a box.
        Donut = 17,                 // Emit in a donut volume.
        Rectangle = 18              // Emit from a rectangle.
    }

    // The mesh emission type
    public enum ParticleSystemMeshShapeType
    {
        Vertex = 0,                 // Emit from the vertices of the mesh.
        Edge = 1,                   // Emit from the edges of the mesh.
        Triangle = 2                // Emit from the surface of the mesh.
    }

    // The texture channel used for discarding particles
    public enum ParticleSystemShapeTextureChannel
    {
        Red = 0,
        Green = 1,
        Blue = 2,
        Alpha = 3
    }

    // The animation mode
    public enum ParticleSystemAnimationMode
    {
        Grid = 0,                   // A regular grid of frames.
        Sprites = 1                 // Sprite frames.
    }

    // The animation type
    public enum ParticleSystemAnimationType
    {
        WholeSheet = 0,
        SingleRow = 1
    }

    // The collision type
    public enum ParticleSystemCollisionType
    {
        Planes = 0,
        World = 1
    }

    // The collision mode
    public enum ParticleSystemCollisionMode
    {
        Collision3D = 0,
        Collision2D = 1
    }

    // The overlap action
    public enum ParticleSystemOverlapAction
    {
        Ignore = 0,
        Kill = 1,
        Callback = 2
    }

    // The simulation space for particle systems
    public enum ParticleSystemSimulationSpace
    {
        Local = 0,                  // Use local simulation space.
        World = 1,                  // Use world simulation space.
        Custom = 2                  // Use custom simulation space, relative to a custom transform component.
    }

    // What action to take when a Particle Systme finishes emitting
    public enum ParticleSystemStopBehavior
    {
        StopEmittingAndClear = 0,   // Stop emitting and remove existing particles.
        StopEmitting = 1            // Stop emitting and allow existing particles to finish.
    }

    // The scaling mode for particle systems
    public enum ParticleSystemScalingMode
    {
        Hierarchy = 0,              // Use full hierarchy scale.
        Local = 1,                  // Use only the local scaling.
        Shape = 2                   // Only apply scaling to the Shape module.
    }

    // The action to perform when a particle system stops
    public enum ParticleSystemStopAction
    {
        None = 0,
        Disable = 1,
        Destroy = 2,
        Callback = 3                // Calls OnParticleSystemStopped.
    }

    // The emitter velocity mode for particle systems
    public enum ParticleSystemEmitterVelocityMode
    {
        Transform = 0,              // Use the Transform component for calculating velocity
        Rigidbody = 1               // Use the Rigidbody or Rigidbody2D component for calculating velocity.
    }

    // The mode used for velocity inheritence
    public enum ParticleSystemInheritVelocityMode
    {
        Initial = 0,                // Emitter velocity is inherited over the particle's lifetime using the emitter velocity when the particle was born.
        Current = 1                 // Emitter velocity is inherited over the particle's lifetime using the current emitter velocity.
    }

    // The types of trigger events
    public enum ParticleSystemTriggerEventType
    {
        Inside = 0,                 // Triggered when particles are inside the collision volume.
        Outside = 1,                // Triggered when particles are outside the collision volume.
        Enter = 2,                  // Triggered when particles enter the collision volume.
        Exit = 3                    // Triggered when particles leave the collision volume.
    }

    // The custom streams
    [UsedByNativeCode]
    public enum ParticleSystemVertexStream
    {
        Position,
        Normal,
        Tangent,
        Color,
        UV,
        UV2,
        UV3,
        UV4,
        AnimBlend,
        AnimFrame,
        Center,
        VertexID,
        SizeX,
        SizeXY,
        SizeXYZ,
        Rotation,
        Rotation3D,
        RotationSpeed,
        RotationSpeed3D,
        Velocity,
        Speed,
        AgePercent,
        InvStartLifetime,
        StableRandomX,
        StableRandomXY,
        StableRandomXYZ,
        StableRandomXYZW,
        VaryingRandomX,
        VaryingRandomXY,
        VaryingRandomXYZ,
        VaryingRandomXYZW,
        Custom1X,
        Custom1XY,
        Custom1XYZ,
        Custom1XYZW,
        Custom2X,
        Custom2XY,
        Custom2XYZ,
        Custom2XYZW,
        NoiseSumX,
        NoiseSumXY,
        NoiseSumXYZ,
        NoiseImpulseX,
        NoiseImpulseXY,
        NoiseImpulseXYZ
    }

    // The available vertex streams
    public enum ParticleSystemCustomData
    {
        Custom1,
        Custom2
    }

    // The custom stream modes
    public enum ParticleSystemCustomDataMode
    {
        Disabled,
        Vector,
        Color
    }

    // The number of dimensions used for noise
    public enum ParticleSystemNoiseQuality
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    // The various types of subemitter
    public enum ParticleSystemSubEmitterType
    {
        Birth = 0,
        Collision = 1,
        Death = 2,
        Trigger = 3,
        Manual = 4
    }

    // The subemitter properties
    [Flags]
    public enum ParticleSystemSubEmitterProperties
    {
        InheritNothing = 0,
        InheritEverything = InheritColor | InheritSize | InheritRotation | InheritLifetime,
        InheritColor = 1 << 0,
        InheritSize = 1 << 1,
        InheritRotation = 1 << 2,
        InheritLifetime = 1 << 3,
    }

    // The mode used for generating Particle Trails (Shuriken).
    public enum ParticleSystemTrailMode
    {
        PerParticle = 0,            // Trails are generated from each particle.
        Ribbon = 1                  // Trails are rendered between each particle.
    }

    // The mode applied to the U coordiante on Particle Trails
    public enum ParticleSystemTrailTextureMode
    {
        Stretch = 0,                // Stretch the texture over the entire trail length.
        Tile = 1,                   // Repeat the texture along the trail.
        DistributePerSegment = 2,   // Stretch the texture over the entire trail, but treat each segment as though it is of equal length.
        RepeatPerSegment = 3        // Repeat the texture along the trail, at a rate of one repetition per segment.
    }

    // The mode used to generate new points in a shape
    public enum ParticleSystemShapeMultiModeValue
    {
        Random = 0,                 // Generate points randomly.
        Loop = 1,                   // Animate the emission point around the shape.
        PingPong = 2,               // Animate the emission point around the shape, alternating between clockwise and counter-clockwise directions.
        BurstSpread = 3             // Distribute new particles around the shape evenly.
    }
}

namespace UnityEngine.Rendering
{
    // Control which UV channels are affected by the Texture Animation Module
    [Flags]
    public enum UVChannelFlags
    {
        UV0 = 1,
        UV1 = 2,
        UV2 = 4,
        UV3 = 8
    }
}
