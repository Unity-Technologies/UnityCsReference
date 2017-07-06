// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor
{
    internal class AudioSourceExtensionEditor : AudioExtensionEditor
    {
        public virtual void OnAudioSourceGUI() {}
        public virtual void OnAudioSourceSceneGUI(AudioSource source) {}

        protected override int GetNumSerializedExtensionProperties(Object obj)
        {
            AudioSource source = obj as AudioSource;
            int numSerializedExtensionProperties = source ? source.GetNumExtensionProperties() : 0;

            return numSerializedExtensionProperties;
        }
    }
}
