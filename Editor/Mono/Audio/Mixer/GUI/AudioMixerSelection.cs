// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Audio;

namespace UnityEditor
{
    internal class AudioMixerSelection
    {
        public AudioMixerSelection(AudioMixerController controller)
        {
            m_Controller = controller;
            ChannelStripSelection = new List<AudioMixerGroupController>();
            SyncToUnitySelection();
        }

        private AudioMixerController m_Controller;
        public List<AudioMixerGroupController> ChannelStripSelection { get; private set; }

        // Channelstrip selection
        // We rely on SyncToUnitySelection is being called after setting Selection.objects through AudioWindow::OnSelectionChange ()
        public void SyncToUnitySelection()
        {
            if (m_Controller != null)
                RefreshCachedChannelStripSelection();
        }

        public void SetChannelStrips(List<AudioMixerGroupController> newSelection)
        {
            Selection.objects = newSelection.ToArray();
            //Debug.Log("SetChannelStrips " + DebugUtils.ListToString(Selection.instanceIDs));
        }

        public void SetSingleChannelStrip(AudioMixerGroupController group)
        {
            Selection.objects = new[] {group};
            //Debug.Log("SetSingleChannelStrip " + DebugUtils.ListToString(Selection.instanceIDs));
        }

        public void ToggleChannelStrip(AudioMixerGroupController group)
        {
            var selection = new List<Object>(Selection.objects);
            if (selection.Contains(group))
                selection.Remove(group);
            else
                selection.Add(group);
            Selection.objects = selection.ToArray();
        }

        public void ClearChannelStrips()
        {
            Selection.objects = new Object[0];
            //Debug.Log("ClearChannelStrips " + DebugUtils.ListToString(Selection.instanceIDs));
        }

        public bool HasSingleChannelStripSelection()
        {
            return ChannelStripSelection.Count == 1;
        }

        private void RefreshCachedChannelStripSelection()
        {
            var selected = Selection.GetFiltered(typeof(AudioMixerGroupController), SelectionMode.Deep);
            ChannelStripSelection = new List<AudioMixerGroupController>();
            List<AudioMixerGroupController> allGroups = m_Controller.GetAllAudioGroupsSlow();

            foreach (var g in allGroups)
                if (selected.Contains(g))
                    ChannelStripSelection.Add(g);
        }

        // Call this after making changes to the group topology (when removing groups)
        public void Sanitize()
        {
            RefreshCachedChannelStripSelection();
        }
    }
}
