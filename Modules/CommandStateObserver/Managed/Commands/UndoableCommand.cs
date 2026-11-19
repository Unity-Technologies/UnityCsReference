// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace Unity.CSO
{
    /// <summary>
    /// Base class for undoable commands.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.GraphToolkitModule")]
    internal abstract class UndoableCommand : ICommand
    {
        /// <summary>
        /// The string that should appear in the Edit/Undo menu after this command is executed.
        /// </summary>
        public string UndoString { get; set; }
    }
}
