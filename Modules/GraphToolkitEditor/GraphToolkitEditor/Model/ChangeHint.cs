// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Hints about what changed on a model.
    /// </summary>
    /// <remarks>A tool can declare new hints by declaring new static fields of this type.</remarks>
    [UnityRestricted]
    internal class ChangeHint : Enumeration
    {
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
        public static readonly ChangeHint Unspecified;

        /// <summary>
        /// The position or dimension of the element changed.
        /// </summary>
        public static readonly ChangeHint Layout;

        /// <summary>
        /// The visual style (color, etc.) of the element changed.
        /// </summary>
        public static readonly ChangeHint Style;

        /// <summary>
        /// Model data (for example, an inspectable field) changed.
        /// </summary>
        public static readonly ChangeHint Data;

        /// <summary>
        /// Graph topology changed; typically, a wire was connected or disconnected.
        /// </summary>
        public static readonly ChangeHint GraphTopology;

        /// <summary>
        /// Grouping of variable in the blackboard changed.
        /// </summary>
        public static readonly ChangeHint Grouping;

        /// <summary>
        /// UI hints changed.
        /// </summary>
        public static readonly ChangeHint UIHints;

        /// <summary>
        /// Animation state of the element changed.
        /// </summary>
        public static readonly ChangeHint Animation;

        /// <summary>
        /// No model change, but a redraw is needed.
        /// </summary>
        public static readonly ChangeHint NeedsRedraw;
    }
}
