// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Capabilities objects can have.
    /// </summary>
    /// <remarks>
    /// Mostly used on <see cref="GraphElementModel"/>.
    /// Acts as an extensible enum.
    /// </remarks>
    // ReSharper disable InconsistentNaming
    [InitializeOnLoad]
    class Capabilities : Enumeration
    {
        const string k_CapabilityPrefix = "";
        const string k_OldCapabilityPrefix = "GraphToolsFoundation";

        static readonly Dictionary<int, Capabilities> s_Capabilities = new Dictionary<int, Capabilities>();
        static readonly Dictionary<int, Capabilities> s_CapabilitiesByName = new Dictionary<int, Capabilities>();

        static int s_NextId;

        /// <summary>
        /// Can be selected.
        /// </summary>
        public static readonly Capabilities Selectable;

        /// <summary>.
        /// Can be deleted
        /// </summary>
        public static readonly Capabilities Deletable;

        /// <summary>
        /// Can be dropped.
        /// </summary>
        public static readonly Capabilities Droppable;

        /// <summary>
        /// Can be copied.
        /// </summary>
        public static readonly Capabilities Copiable;

        /// <summary>
        /// Can be renamed.
        /// </summary>
        public static readonly Capabilities Renamable;

        /// <summary>
        /// Can be moved.
        /// </summary>
        public static readonly Capabilities Movable;

        /// <summary>
        /// Can be resized.
        /// </summary>
        public static readonly Capabilities Resizable;

        /// <summary>
        /// Can be collapsed.
        /// </summary>
        public static readonly Capabilities Collapsible;

        /// <summary>
        /// Can change color.
        /// </summary>
        public static readonly Capabilities Colorable;

        /// <summary>
        /// Should be sent to front when selected.
        /// </summary>
        public static readonly Capabilities Ascendable;

        /// <summary>
        /// Can only be added to a container
        /// </summary>
        public static readonly Capabilities NeedsContainer;

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
        }

        protected Capabilities(string name, string prefix = k_CapabilityPrefix)
            : this(s_NextId++, prefix + "." + name)
        {}

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
