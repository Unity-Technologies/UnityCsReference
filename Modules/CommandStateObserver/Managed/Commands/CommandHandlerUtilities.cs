// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace Unity.CSO;

[VisibleToOtherModules("UnityEditor.CommandStateObserverModule")]
static class CommandHandlerUtilities
{
    [VisibleToOtherModules("UnityEditor.CommandStateObserverModule")]
    public enum BindingState
    {
        Unbound,
        Bound,
    }

}
