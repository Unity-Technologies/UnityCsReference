// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Audio.UIElements
{
    internal class OnAudioFilterReadLevelMeter : IMGUIContainer
    {
        AudioFilterGUI m_IMGUI_AudioFilterGUI = new AudioFilterGUI();

        public OnAudioFilterReadLevelMeter(MonoBehaviour behaviour)
        {
            onGUIHandler = () => { m_IMGUI_AudioFilterGUI.DrawAudioFilterGUI(behaviour); };
        }
    }
}
