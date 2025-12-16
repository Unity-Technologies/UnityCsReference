// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Audio;
using UnityEngine;

namespace UnityEditor
{
    internal class AudioMixerUtility
    {
        static public void RepaintAudioMixerAndInspectors()
        {
            InspectorWindow.RepaintAllInspectors();
            AudioMixerWindow.RepaintAudioMixerWindow();
        }

        static public void ToggleEffectWetMix(AudioMixerEffectController effect)
        {
            var undoEntryPrefix = effect.enableWetMix ? "Disable" : "Enable";
            Undo.RecordObject(effect, $"{undoEntryPrefix} Wet Mixing");
            effect.enableWetMix = !effect.enableWetMix;
            RepaintAudioMixerAndInspectors();
        }

        public class VisitorFetchInstanceIDs
        {
            public List<EntityId> entityIds = new List<EntityId>();
            public void Visitor(AudioMixerGroupController group)
            {
                entityIds.Add(group.GetEntityId());
            }
        }

        public static void VisitGroupsRecursivly(AudioMixerGroupController group, Action<AudioMixerGroupController> visitorCallback)
        {
            foreach (var child in group.children)
                VisitGroupsRecursivly(child, visitorCallback);

            if (visitorCallback != null)
                visitorCallback(group);
        }
    }
}
// namespace
