// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Hints about what changed on a model.
    /// </summary>
    /// <remarks>A tool can declare new hints by declaring new static fields of this type.</remarks>
    class ChangeHint : Enumeration
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
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeHint"/> class.
        /// </summary>
        /// <param name="name">The name of the hint.</param>
        public ChangeHint(string name)
            : base(s_NextId++, name)
        {}

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
    }

    /// <summary>
    /// Helpers for checking if a group of hints contains some specific hint(s).
    /// </summary>
    static class ChangeHintHelpers
    {
        /// <summary>
        /// Checks if a group of change hints contains a specific change hint or <see cref="ChangeHint.Unspecified"/>.
        /// </summary>
        /// <param name="changes">The group of hints.</param>
        /// <param name="needle">The hint to find.</param>
        /// <returns>True if <paramref name="changes"/> contains <paramref name="needle"/> or <see cref="ChangeHint.Unspecified"/>, false otherwise.</returns>
        public static bool HasChange(this IReadOnlyList<ChangeHint> changes, ChangeHint needle)
        {
            for (var i = 0; i < changes.Count; i++)
            {
                var hint = changes[i];
                if (hint == needle || hint == ChangeHint.Unspecified)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a group of change hints contains a specific change hint or <see cref="ChangeHint.Unspecified"/>.
        /// </summary>
        /// <param name="changes">The group of hints.</param>
        /// <param name="needle1">The hint to find.</param>
        /// <param name="needle2">The hint to find.</param>
        /// <returns>True if <paramref name="changes"/> contains <paramref name="needle1"/> or <paramref name="needle2"/> or <see cref="ChangeHint.Unspecified"/>, false otherwise.</returns>
        public static bool HasAnyChange(this IReadOnlyList<ChangeHint> changes, ChangeHint needle1, ChangeHint needle2)
        {
            for (var i = 0; i < changes.Count; i++)
            {
                var hint = changes[i];
                if (hint == needle1 || hint == needle2 || hint == ChangeHint.Unspecified)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a group of change hints contains a specific change hint or <see cref="ChangeHint.Unspecified"/>.
        /// </summary>
        /// <param name="changes">The group of hints.</param>
        /// <param name="needles">The hints to find.</param>
        /// <returns>True if <paramref name="changes"/> contains at least one of the hint in <paramref name="needles"/> or <see cref="ChangeHint.Unspecified"/>, false otherwise.</returns>
        public static bool HasAnyChange(this IReadOnlyList<ChangeHint> changes, params ChangeHint[] needles)
        {
            for (var i = 0; i < changes.Count; i++)
            {
                var hint = changes[i];
                if (hint == ChangeHint.Unspecified || needles.Contains(hint))
                    return true;
            }

            return false;
        }
    }
}
