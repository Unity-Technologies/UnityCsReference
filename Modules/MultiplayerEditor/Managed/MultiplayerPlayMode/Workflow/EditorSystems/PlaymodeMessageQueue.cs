// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    enum PlayModeMessageTypes
    {
        Pause,
        Unpause,
        Step,
        Play,
        Stop,
    }

    // Done this way since SessionStateRepository only works on serializable objects
    [Serializable]
    class QueueInfo
    {
        [SerializeField] public int index;
    }

    class PlaymodeMessageQueue
    {
        readonly SessionStateJsonRepository<PlayModeMessageTypes, QueueInfo> m_Messages = SessionStateJsonRepository<PlayModeMessageTypes, QueueInfo>.GetMain(
            SessionStateRepository.Get,
            nameof(m_Messages), out _);
        int m_Adder;

        // Squashes play mode events of the same type but allows us to be able to tell their order
        public void AddEvent(PlayModeMessageTypes type)
        {
            if (m_Messages.ContainsKey(type))
            {
                // We don't truly expect events of the same type to occur based on how the UI works so log out something
                MppmLog.Debug($"Received duplicate message of {type}.");
                m_Messages.Update(type, info => info.index = m_Adder, out _);
            }
            else
            {
                m_Messages.Create(type, new QueueInfo { index = m_Adder });
            }

            m_Adder++;
        }

        public bool ReadEvent(out PlayModeMessageTypes playModeMessageType)
        {
            var min = int.MaxValue;
            playModeMessageType = default;
            foreach (var (playModeMessageTypes, order) in m_Messages.GetAll())
            {
                if (order.index < min)
                {
                    min = order.index;
                    playModeMessageType = playModeMessageTypes;
                }
            }

            if (min != int.MaxValue)
            {
                m_Messages.Delete(playModeMessageType);
                return true;
            }

            return false;
        }
    }
}
