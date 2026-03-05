// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace UnityEngineInternal.Input
{
    /// <summary>
    /// Flags indicating various focus states for the application and editor.
    /// </summary>
    internal enum FocusFlags : ushort
    {
        /// <summary>
        /// No focus state is active.
        /// </summary>
        None = 0,

        /// <summary>
        /// The application has focus.
        /// </summary>
        ApplicationFocus = (1 << 0)
    };
}
