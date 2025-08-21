// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Multiplayer.PlayMode.Editor
{
    internal interface IConnectableNode
    {
        public NodeInput<ConnectionData> ConnectionDataIn { get; }
        public NodeOutput<ConnectionData> ConnectionDataOut { get; }
    }
}
