// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    internal abstract class NodeParameter
    {
        [EditorBrowsable(EditorBrowsableState.Never)] internal abstract ExecutionNode GetNode();
        [EditorBrowsable(EditorBrowsableState.Never)] internal abstract T GetValue<T>();
        [EditorBrowsable(EditorBrowsableState.Never)] internal abstract void SetValue<T>(T value);
    }
}
