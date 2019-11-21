// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Runtime/Network/PlayerCommunicator/GeneralConnection.h")]
    enum GeneralConnectionSendMode
    {
        NoBlocking,
        AllowBlocking
    }

    [StaticAccessor("EditorConnection::Get()", StaticAccessorType.Dot)]
    [NativeHeader("Runtime/Network/PlayerCommunicator/EditorConnection.h")]
    [NativeHeader("Runtime/Network/PlayerCommunicator/ManagedProxy/EditorConnectionManaged.h")]
    internal class EditorConnectionInternal : IPlayerEditorConnectionNative
    {
        void IPlayerEditorConnectionNative.SendMessage(Guid messageId, byte[] data, int playerId)
        {
            if (messageId == Guid.Empty)
            {
                throw new ArgumentException(nameof(messageId) + " must not be empty");
            }
            SendMessage(messageId.ToString("N"), data, playerId);
        }

        bool IPlayerEditorConnectionNative.TrySendMessage(Guid messageId, byte[] data, int playerId)
        {
            if (messageId == Guid.Empty)
            {
                throw new ArgumentException(nameof(messageId) + " must not be empty");
            }
            return TrySendMessage(messageId.ToString("N"), data, playerId);
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

        void IPlayerEditorConnectionNative.DisconnectAll()
        {
            DisconnectAll();
        }

        public bool IsConnected()
        {
            throw new NotSupportedException("Check the connected players list instead");
        }

        [NativeConditional("ENABLE_PLAYERCONNECTION && UNITY_EDITOR")]
        [FreeFunction("EditorConnectionManaged::Get")]
        public extern static void Initialize();

        public static void UnregisterInternal(string messageId)
        {
            UnregisterInternal(new GUID(messageId));
        }

        [NativeConditional("ENABLE_PLAYERCONNECTION && UNITY_EDITOR")]
        [StaticAccessor("EditorConnectionManaged::Get()", StaticAccessorType.Dot)]
        [NativeName("Unregister")]
        private extern static void UnregisterInternal(UnityEditor.GUID messageId);

        public static void RegisterInternal(string messageId) { RegisterInternal(new GUID(messageId)); }

        [NativeConditional("ENABLE_PLAYERCONNECTION && UNITY_EDITOR")]
        [StaticAccessor("EditorConnectionManaged::Get()", StaticAccessorType.Dot)]
        [NativeName("Register")]
        private extern static void RegisterInternal(UnityEditor.GUID messageId);


        [NativeConditional("ENABLE_PLAYERCONNECTION && UNITY_EDITOR")]
        [NativeName("SendMessage")]
        private extern unsafe static void SendMessageInternal(int playerId, GUID messageId, byte* data, int dataSize, GeneralConnectionSendMode sendMode);

        //playerId 0 is ANY_PLAYERCONNECTION
        public static void SendMessage(string messageId, byte[] data, int playerId)
        {
            unsafe
            {
                fixed(byte* dataBytes = data)
                SendMessageInternal(playerId, new GUID(messageId), dataBytes, data?.Length ?? 0, GeneralConnectionSendMode.AllowBlocking);
            }
        }

        [NativeConditional("ENABLE_PLAYERCONNECTION && UNITY_EDITOR")]
        [NativeName("TrySendMessage")]
        private extern unsafe static bool TrySendMessageInternal(int playerId, GUID messageId, byte* data, int dataSize);

        //playerId 0 is ANY_PLAYERCONNECTION
        public static bool TrySendMessage(string messageId, byte[] data, int playerId)
        {
            unsafe
            {
                fixed(byte* dataBytes = data)
                return TrySendMessageInternal(playerId, new GUID(messageId), dataBytes, data?.Length ?? 0);
            }
        }

        [NativeName("Poll")]
        public extern static void PollInternal();

        public extern static int ConnectPlayerProxy(string IP, int port);

        [NativeConditional("ENABLE_PLAYERCONNECTION && UNITY_EDITOR")]
        public extern static void DisconnectAll();

        [NativeConditional("ENABLE_PLAYERCONNECTION")]
        public extern static UInt32 GetLocalGuid();

        [NativeConditional("ENABLE_PLAYERCONNECTION && UNITY_EDITOR")]
        extern public static string GetMulticastAddress();

        [NativeConditional("ENABLE_PLAYERCONNECTION && UNITY_EDITOR")]
        extern public static UInt32 GetMulticastPort();

        [NativeConditional("ENABLE_PLAYERCONNECTION && UNITY_EDITOR")]
        [NativeName("BuildServerIDString")]
        extern public static string BuildServerIdentificationFormat(string localIP, UInt32 listenPort, int flags, UInt32 localGuid, UInt32 editorGuid, string idString, bool allowDebugging, string packageString, string projectName);
    }
}
