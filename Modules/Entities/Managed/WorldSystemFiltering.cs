// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Entities
{
    /// <summary>
    /// Prevents a system from being automatically created and run.
    /// </summary>
    /// <remarks>
    /// By default, all systems (classes derived from <see cref="ComponentSystemBase"/> or <see cref="ISystem"/>) are automatically discovered,
    /// instantiated, and added to the default <see cref="World"/> when that World is created.
    ///
    /// Add this attribute to a system class that you do not want created automatically. Note that the attribute is not
    /// inherited by any subclasses.
    ///
    /// <code>
    /// using Unity.Entities;
    ///
    /// [DisableAutoCreation]
    /// public partial class CustomSystem : SystemBase
    /// { // Implementation... }
    /// </code>
    ///
    /// You can also apply this attribute to an entire assembly to prevent any system class in that assembly from being
    /// created automatically. This is useful for test assemblies containing many systems that expect to be tested
    /// in isolation.
    ///
    /// To declare an assembly attribute, place it in any C# file compiled into the assembly, outside the namespace
    /// declaration:
    /// <code>
    /// using Unity.Entities;
    ///
    /// [assembly: DisableAutoCreation]
    /// namespace Tests{}
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Assembly, Inherited=false)]
    public sealed class DisableAutoCreationAttribute : Attribute
    {
    }

    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <remarks>Defines where internal Unity systems should be created. The existence of these flags and
    /// the specialized Worlds they represent are subject to change.</remarks>
    [Flags]
    public enum WorldSystemFilterFlags : uint
    {
        /// <summary>
        /// When specifying the Default flag on a [WorldSystemFilter] the flag will be removed and expand to
        /// what was specified as ChildDefaultFilterFlags by the group the system is in. This means the Default
        /// flag will never be set when querying a system for its flags.
        /// If the system does not have a [UpdateInGroup] the system will be in the SimulationSystemGroup and
        /// get the ChildDefaultFilterFlags from that group.
        /// When creating a world - or calling GetSystems directly - default expands to LocalSimulation | Presentation
        /// to create a standard single player world.
        /// </summary>
        Default                         = 1 << 0,
        /// <summary>
        /// Systems explicitly disabled via the [DisableAutoCreation] attribute are marked with this flag.
        /// </summary>
        Disabled = 1 << 1,
        /// <summary>
        /// A specialized World created for optimizing scene rendering.
        /// </summary>
        EntitySceneOptimizations        = 1 << 2,
        /// <summary>
        /// A specialized World created for processing a scene after load.
        /// </summary>
        ProcessAfterLoad                = 1 << 3,
        /// <summary>
        /// The main World created when running in the Editor.
        /// Example: Editor LiveConversion system
        /// </summary>
        Editor                          = 1 << 6,
        /// <summary>
        /// Baking systems running after the BakingSystem system responsible from baking GameObjects to entities.
        /// </summary>
        BakingSystem                    = 1 << 7,
        /// <summary>
        /// Worlds using local simulation, without any multiplayer client / server support.
        /// </summary>
        LocalSimulation                 = 1 << 8,
        /// <summary>
        /// Worlds using server simulation.
        /// </summary>
        ServerSimulation                = 1 << 9,
        /// <summary>
        /// Worlds using client simulation.
        /// </summary>
        ClientSimulation                = 1 << 10,
        /// <summary>
        /// Worlds using thin client simulation. A thin client is a client running the bare minimum set of systems to connect to and communicate with a server. It does not run the full simulation and cannot generally present the simulation state.
        /// </summary>
        ThinClientSimulation            = 1 << 11,
        /// <summary>
        /// Worlds presenting a rendered world.
        /// </summary>
        Presentation                    = 1 << 12,
        /// <summary>
        /// Worlds supporting streaming
        /// </summary>
        Streaming                       = 1 << 13,
        /// <summary>
        /// Worlds baking
        /// </summary>
        EntityProxy                     = 1 << 14,
        /// <summary>
        /// Worlds baking in preview mode
        /// </summary>
        EntityProxyPreview                     = 1 << 15,
        /// <summary>
        /// Flag to include all system groups defined above, including systems decorated with [DisableAutoCreation].
        /// </summary>
        All                             = ~0u
    }


    /// <summary>
    /// For internal use only.
    /// </summary>
    /// <remarks>Defines where internal Unity systems should be created. The existence of these Worlds
    /// is subject to change.</remarks>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public sealed class WorldSystemFilterAttribute : Attribute
    {
        /// <summary>
        /// The World the system belongs in.
        /// </summary>
        public WorldSystemFilterFlags FilterFlags;

        /// <summary>
        /// The World children of this system (group) should belong in by default.
        /// </summary>
        public WorldSystemFilterFlags ChildDefaultFilterFlags;

        /// <summary>For internal use only.</summary>
        /// <param name="flags">Defines where internal Unity systems should be created.</param>
        /// <param name="childDefaultFlags">Defines where children of this system group should be created if they do not have explicit filters. This parameter is only used for system groups, specifying it on a non-group system has no effect.</param>
        public WorldSystemFilterAttribute(WorldSystemFilterFlags flags, WorldSystemFilterFlags childDefaultFlags = WorldSystemFilterFlags.Default)
        {
            FilterFlags = flags;
            ChildDefaultFilterFlags = childDefaultFlags;
        }
    }
}
