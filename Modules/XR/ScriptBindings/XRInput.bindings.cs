// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.XR
{
    [NativeHeader("Modules/XR/Subsystems/Input/Public/XRInputTrackingFacade.h")]
    [NativeConditional("ENABLE_VR")]
    [StaticAccessor("XRInputTrackingFacade::Get()", StaticAccessorType.Dot)]
    public partial class InputTracking
    {
        [NativeConditional("ENABLE_VR", "Vector3f::zero")]
        extern public static Vector3 GetLocalPosition(XRNode node);

        [NativeConditional("ENABLE_VR", "Quaternionf::identity()")]
        extern public static Quaternion GetLocalRotation(XRNode node);

        [NativeConditional("ENABLE_VR")]
        extern public static void Recenter();

        [NativeConditional("ENABLE_VR")]
        extern public static string GetNodeName(ulong uniqueId);

        public static void GetNodeStates(List<XRNodeState> nodeStates)
        {
            if (null == nodeStates)
            {
                throw new ArgumentNullException("nodeStates");
            }

            nodeStates.Clear();
            GetNodeStates_Internal(nodeStates);
        }

        [NativeConditional("ENABLE_VR && !ENABLE_DOTNET")]
        extern private static void GetNodeStates_Internal(List<XRNodeState> nodeStates);

        [NativeConditional("ENABLE_VR && ENABLE_DOTNET")]
        extern private static XRNodeState[] GetNodeStates_Internal_WinRT();

        [NativeConditional("ENABLE_VR")]
        extern public static bool disablePositionalTracking
        {
            [NativeName("GetPositionalTrackingDisabled")]
            get;
            [NativeName("SetPositionalTrackingDisabled")]
            set;
        }
    }
}
