// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: AudioAuthoring not yet converted
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Audio.UIElements
{
    internal class OnAudioFilterReadLevelMeter : IMGUIContainer
    {
        AudioFilterGUI m_IMGUI_AudioFilterGUI = new AudioFilterGUI();

        public OnAudioFilterReadLevelMeter(MonoBehaviour behaviour)
        {
            onGUIHandler = () =>
            {
                if (GUIView.current != null)
                {
                    m_IMGUI_AudioFilterGUI.DrawAudioFilterGUI(behaviour);
                }
            };
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
