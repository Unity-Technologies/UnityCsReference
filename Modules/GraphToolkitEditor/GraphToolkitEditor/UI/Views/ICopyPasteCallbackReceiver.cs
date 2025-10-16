// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Interface for classes that want to be notified when they are copied and pasted.
    /// </summary>
    [UnityRestricted]
    internal interface ICopyPasteCallbackReceiver
    {
        /// <summary>
        /// Called before the object is added to the <see cref="CopyPasteData"/> object.
        /// </summary>
        /// <remarks>This is called before the object is serialized. Use this to set up data that should be serialized in the copy operation.</remarks>
        void OnBeforeCopy();

        /// <summary>
        /// Called after serialization for the copy operation is done.
        /// </summary>
        /// <remarks>This is called after the object is serialized. Use this to clean up data that was set up in <see cref="OnBeforeCopy"/>.</remarks>
        void OnAfterCopy();

        /// <summary>
        /// Called after the object is pasted into the <see cref="GraphModel"/>.
        /// </summary>
        /// <remarks>At this point, the object is added to the <see cref="GraphModel"/>.</remarks>
        void OnAfterPaste();
    }
}
