// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    interface IFramableContainer
    {
        void FrameObject(EntityId instanceID, bool ping);
    }
}
