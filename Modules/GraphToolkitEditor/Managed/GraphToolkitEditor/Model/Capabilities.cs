// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Capabilities objects can have.
    /// </summary>
    /// <remarks>
    /// 'Capabilities' is primarily used on <see cref="GraphElementModel"/> and functions as an extensible enumeration, which allows objects to define their supported behaviors.
    /// The default capabilities provided include: <see cref="Selectable"/>, <see cref="Deletable"/>, <see cref="Droppable"/>, <see cref="Copiable"/>, <see cref="Renamable"/>,
    /// <see cref="Movable"/>, <see cref="Resizable"/>, <see cref="Collapsible"/>, <see cref="Colorable"/>, <see cref="Ascendable"/>,
    /// <see cref="NeedsContainer"/>, <see cref="Disableable"/>, and <see cref="Editable"/>.
    /// </remarks>
    // ReSharper disable InconsistentNaming
    [UnityRestricted]
    internal class Capabilities : Enumeration
    {
        const string k_CapabilityPrefix = "";
        const string k_OldCapabilityPrefix = "GraphToolsFoundation";

        [NoAutoStaticsCleanup] // capability lookup tables; populated once by static ctor, stable across reloads
        static readonly Dictionary<int, Capabilities> s_Capabilities = new Dictionary<int, Capabilities>();
        [NoAutoStaticsCleanup] // capability lookup tables; populated once by static ctor, stable across reloads
        static readonly Dictionary<int, Capabilities> s_CapabilitiesByName = new Dictionary<int, Capabilities>();

        [NoAutoStaticsCleanup] // ID counter initialized once by static ctor; value is stable across reloads
        static int s_NextId;

        /// <summary>
        /// Can be selected.
        /// </summary>
        [NoAutoStaticsCleanup] // enum-like capability constant; value is a fixed identifier
        public static readonly Capabilities Selectable;

        /// <summary>.
        /// Can be deleted
        /// </summary>
        [NoAutoStaticsCleanup] // enum-like capability constant; value is a fixed identifier
        public static readonly Capabilities Deletable;

        /// <summary>
        /// Can be dropped.
        /// </summary>
        [NoAutoStaticsCleanup] // enum-like capability constant; value is a fixed identifier
        public static readonly Capabilities Droppable;

        /// <summary>
        /// Can be copied.
        /// </summary>
        [NoAutoStaticsCleanup] // enum-like capability constant; value is a fixed identifier
        public static readonly Capabilities Copiable;

        /// <summary>
        /// Can be renamed.
        /// </summary>
        [NoAutoStaticsCleanup] // enum-like capability constant; value is a fixed identifier
        public static readonly Capabilities Renamable;

        /// <summary>
        /// Can be moved.
        /// </summary>
        [NoAutoStaticsCleanup] // enum-like capability constant; value is a fixed identifier
        public static readonly Capabilities Movable;

        /// <summary>
        /// Can be resized.
        /// </summary>
        [NoAutoStaticsCleanup] // enum-like capability constant; value is a fixed identifier
        public static readonly Capabilities Resizable;

        /// <summary>
        /// Can be collapsed.
        /// </summary>
        [NoAutoStaticsCleanup] // enum-like capability constant; value is a fixed identifier
        public static readonly Capabilities Collapsible;

        /// <summary>
        /// Can change color.
        /// </summary>
        [NoAutoStaticsCleanup] // enum-like capability constant; value is a fixed identifier
        public static readonly Capabilities Colorable;

        /// <summary>
        /// Should be sent to front when selected.
        /// </summary>
        [NoAutoStaticsCleanup] // enum-like capability constant; value is a fixed identifier
        public static readonly Capabilities Ascendable;

        /// <summary>
        /// Can only be added to a container
        /// </summary>
        [NoAutoStaticsCleanup] // enum-like capability constant; value is a fixed identifier
        public static readonly Capabilities NeedsContainer;

        /// <summary>
        /// Can be disabled.
        /// </summary>
        [NoAutoStaticsCleanup] // enum-like capability constant; value is a fixed identifier
        public static readonly Capabilities Disableable;

        /// <summary>
        /// Can be edited.
        /// </summary>
        [NoAutoStaticsCleanup] // enum-like capability constant; value is a fixed identifier
        public static readonly Capabilities Editable;

        /// <summary>
        /// Can be animated.
        /// </summary>
        [NoAutoStaticsCleanup] // enum-like capability constant; value is a fixed identifier
        public static readonly Capabilities Animatable;

        static Capabilities()
        {
            s_NextId = 0;

            Selectable = new Capabilities(nameof(Selectable));
            Deletable = new Capabilities(nameof(Deletable));
            Droppable = new Capabilities(nameof(Droppable));
            Copiable = new Capabilities(nameof(Copiable));
            Renamable = new Capabilities(nameof(Renamable));
            Movable = new Capabilities(nameof(Movable));
            Resizable = new Capabilities(nameof(Resizable));
            Collapsible = new Capabilities(nameof(Collapsible));
            Colorable = new Capabilities(nameof(Colorable));
            Ascendable = new Capabilities(nameof(Ascendable));
            NeedsContainer = new Capabilities(nameof(NeedsContainer));
            Disableable = new Capabilities(nameof(Disableable));
            Editable = new Capabilities(nameof(Editable));
            Animatable = new Capabilities(nameof(Animatable));
        }

        protected Capabilities(string name, string prefix = k_CapabilityPrefix)
            : this(s_NextId++, prefix + "." + name)
        { }

        Capabilities(int id, string name) : base(id, name)
        {
            if (s_Capabilities.ContainsKey(id))
                throw new ArgumentException($"Id {id} used for Capability {Name} is already used for Capability {s_Capabilities[id].Name}");
            s_Capabilities[id] = this;

            int hash = Name.GetHashCode();
            if (s_CapabilitiesByName.ContainsKey(hash))
                throw new ArgumentException($"Name {Name} is already used for Capability.");
            s_CapabilitiesByName[hash] = this;
        }

        // Lets tests that define capability subclasses drop them so the static tables do not keep the test types alive.
        internal static void Unregister(Capabilities capability)
        {
            s_Capabilities.Remove(capability.Id);
            s_CapabilitiesByName.Remove(capability.Name.GetHashCode());
        }

        public static Capabilities Get(int id) => s_Capabilities[id];

        public static Capabilities Get(string fullname)
        {
            // TODO JOCE Remove this check before we go to 1.0
            if (fullname.StartsWith(k_OldCapabilityPrefix))
                fullname = fullname.Substring(k_OldCapabilityPrefix.Length);
            return s_CapabilitiesByName[fullname.GetHashCode()];
        }
    }
}
