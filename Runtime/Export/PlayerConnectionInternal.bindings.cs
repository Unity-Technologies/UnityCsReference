// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    internal interface IPlayerEditorConnectionNative
    {
        void Initialize();
        void DisconnectAll();
        void SendMessage(Guid messageId, byte[] data, int playerId);
        void Poll();
        void RegisterInternal(Guid messageId);
        void UnregisterInternal(Guid messageId);
        bool IsConnected();
    }

    [NativeHeader("Runtime/Export/PlayerConnectionInternal.bindings.h")]
    internal class PlayerConnectionInternal : IPlayerEditorConnectionNative
    {
        void IPlayerEditorConnectionNative.SendMessage(Guid messageId, byte[] data, int playerId)
        {
            if (messageId == Guid.Empty)
            {
                throw new ArgumentException("messageId must not be empty");
            }

            SendMessage(messageId.ToString("N"), data, playerId);
        }

        void IPlayerEditorConnectionNative.Poll()
        {
            PollInternal();
        }

        void IPlayerEditorConnectionNative.RegisterInternal(Guid messageId)
        {
            RegisterInternal(messageId.ToString("N"));
        }

        void IPlayerEditorConnectionNative.UnregisterInternal(Guid messageId)
        {
            UnregisterInternal(messageId.ToString("N"));
        }

        void IPlayerEditorConnectionNative.Initialize()
        {
            Initialize();
        }

        bool IPlayerEditorConnectionNative.IsConnected()
        {
            return IsConnected();
        }

        void IPlayerEditorConnectionNative.DisconnectAll()
        {
            DisconnectAll();
        }

        [FreeFunction("PlayerConnection_Bindings::IsConnected")]
        extern static bool IsConnected();

        [FreeFunction("PlayerConnection_Bindings::Initialize")]
        extern static void Initialize();

        [FreeFunction("PlayerConnection_Bindings::RegisterInternal")]
        extern static void RegisterInternal(string messageId);

        [FreeFunction("PlayerConnection_Bindings::UnregisterInternal")]
        extern static void UnregisterInternal(string messageId);

        //playerId 0 is ANY_PLAYERCONNECTION
        [FreeFunction("PlayerConnection_Bindings::SendMessage")]
        extern static void SendMessage(string messageId, byte[] data, int playerId);

        [FreeFunction("PlayerConnection_Bindings::PollInternal")]
        extern static void PollInternal();

        [FreeFunction("PlayerConnection_Bindings::DisconnectAll")]
        extern static void DisconnectAll();
    }
}
