// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Audio;

namespace UnityEditor
{
    internal class AudioMixerUtility
    {
        static public void RepaintAudioMixerAndInspectors()
        {
            InspectorWindow.RepaintAllInspectors();
            AudioMixerWindow.RepaintAudioMixerWindow();
        }

        public class VisitorFetchInstanceIDs
        {
            public List<int> instanceIDs = new List<int>();
            public void Visitor(AudioMixerGroupController group)
            {
                instanceIDs.Add(group.GetInstanceID());
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
