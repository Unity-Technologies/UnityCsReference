// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Hints about what changed on a model.
    /// </summary>
    /// <remarks>A tool can declare new hints by declaring new static fields of this type.</remarks>
    [UnityRestricted]
    internal class ChangeHint : Enumeration
    {
        [NoAutoStaticsCleanup] // ID counter initialized once by static ctor; value is stable across reloads
        static int s_NextId;

        static ChangeHint()
        {
            s_NextId = 0;

            Unspecified = new ChangeHint(nameof(Unspecified));
            Layout = new ChangeHint(nameof(Layout));
            Style = new ChangeHint(nameof(Style));
            Data = new ChangeHint(nameof(Data));
            GraphTopology = new ChangeHint(nameof(GraphTopology));
            Grouping = new ChangeHint(nameof(Grouping));
            UIHints = new ChangeHint(nameof(UIHints));
            Animation =  new ChangeHint(nameof(Animation));
            NeedsRedraw = new ChangeHint(nameof(NeedsRedraw));
            RecreateView = new ChangeHint(nameof(RecreateView));

            NonDataHints = new[] { Layout, Style, Grouping, UIHints, Animation, NeedsRedraw };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeHint"/> class.
        /// </summary>
        /// <param name="name">The name of the hint.</param>
        public ChangeHint(string name)
            : base(s_NextId++, name)
        { }

        /// <summary>
        /// Unspecified changes. Assume anything could have change.
        /// </summary>
        [NoAutoStaticsCleanup] // enum-like hint constant; value is a fixed identifier
        public static readonly ChangeHint Unspecified;

        /// <summary>
        /// The position or dimension of the element changed.
        /// </summary>
        [NoAutoStaticsCleanup] // enum-like hint constant; value is a fixed identifier
        public static readonly ChangeHint Layout;

        /// <summary>
        /// The visual style (color, etc.) of the element changed.
        /// </summary>
        [NoAutoStaticsCleanup] // enum-like hint constant; value is a fixed identifier
        public static readonly ChangeHint Style;

        /// <summary>
        /// Model data (for example, an inspectable field) changed.
        /// </summary>
        [NoAutoStaticsCleanup] // enum-like hint constant; value is a fixed identifier
        public static readonly ChangeHint Data;

        /// <summary>
        /// Graph topology changed; typically, a wire was connected or disconnected.
        /// </summary>
        [NoAutoStaticsCleanup] // enum-like hint constant; value is a fixed identifier
        public static readonly ChangeHint GraphTopology;

        /// <summary>
        /// Grouping of variable in the blackboard changed.
        /// </summary>
        [NoAutoStaticsCleanup] // enum-like hint constant; value is a fixed identifier
        public static readonly ChangeHint Grouping;

        /// <summary>
        /// UI hints changed.
        /// </summary>
        [NoAutoStaticsCleanup] // enum-like hint constant; value is a fixed identifier
        public static readonly ChangeHint UIHints;

        /// <summary>
        /// Animation state of the element changed.
        /// </summary>
        [NoAutoStaticsCleanup] // enum-like hint constant; value is a fixed identifier
        public static readonly ChangeHint Animation;

        /// <summary>
        /// No model change, but a redraw is needed.
        /// </summary>
        [NoAutoStaticsCleanup] // enum-like hint constant; value is a fixed identifier
        public static readonly ChangeHint NeedsRedraw;

        /// <summary>
        /// The view for this model must be torn down and recreated, e.g. because the view type changed.
        /// </summary>
        [NoAutoStaticsCleanup] // enum-like hint constant; value is a fixed identifier
        public static readonly ChangeHint RecreateView;

        /// <summary>
        /// The hints that do not affect the data of a model.
        /// </summary>
        /// <remarks>When declaring a new hint, add it to this list if it never affects model data.</remarks>
        [NoAutoStaticsCleanup] // enum-like hint set; built once by static ctor from fixed identifiers
        internal static readonly IReadOnlyList<ChangeHint> NonDataHints;
    }
}
#pragma warning restore UAL0010,UAL0011,UAL0012,UAL0013,UAL0014
