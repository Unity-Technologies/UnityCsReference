// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor
{
    internal class AudioListenerExtensionEditor : AudioExtensionEditor
    {
        public virtual void OnAudioListenerGUI() {}

        protected override int GetNumSerializedExtensionProperties(Object obj)
        {
            AudioListener listener = obj as AudioListener;
            int numSerializedExtensionProperties = listener ? listener.GetNumExtensionProperties() : 0;

            return numSerializedExtensionProperties;
        }
    }
}
