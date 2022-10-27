// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.CommandStateObserver;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A way to fix an error.
    /// </summary>
    class QuickFix
    {
        /// <summary>
        /// The description of the fix.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// The action to execute to fix the error.
        /// </summary>
        public Action<ICommandTarget> QuickFixAction { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuickFix" /> class.
        /// </summary>
        /// <param name="description">The description of the fix.</param>
        /// <param name="quickFixAction">The action to execute to fix the error.</param>
        public QuickFix(string description, Action<ICommandTarget> quickFixAction)
        {
            Description = description;
            QuickFixAction = quickFixAction;
        }
    }
}
