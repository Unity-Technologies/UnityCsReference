// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.Experimental.Licensing
{
    [NativeHeader("Modules/Licensing/Public/LicensingUtility.bindings.h")]
    public static class LicensingUtility
    {
        [NativeMethod("HasEntitlement")]
        public extern static bool HasEntitlement(string entitlement);

        [NativeMethod("HasEntitlements")]
        public extern static string[] HasEntitlements(string[] entitlements);

        [NativeMethod("IsOnPremiseLicensingEnabled")]
        internal extern static bool IsOnPremiseLicensingEnabled();
    }
}
