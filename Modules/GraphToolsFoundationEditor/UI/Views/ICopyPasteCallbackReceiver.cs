// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Interface for classes that want to be notified when they are copied and pasted.
    /// </summary>
    interface ICopyPasteCallbackReceiver
    {
        /// <summary>
        /// Called before the object is added to the <see cref="CopyPasteData"/> object.
        /// </summary>
        /// <remarks>This is called before object is serialized.</remarks>
        void OnBeforeCopy();

        /// <summary>
        /// Called after the object is pasted into the <see cref="GraphModel"/>.
        /// </summary>
        /// <remarks>At this point, the object is added to the <see cref="GraphModel"/>.</remarks>
        void OnAfterPaste();
    }
}
