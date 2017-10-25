// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Purchasing
{

    [NativeHeader("Modules/UnityConnect/UnityPurchasing/UnityPurchasingSettings.h")]
    [StaticAccessor("GetUnityPurchasingSettings()", StaticAccessorType.Dot)]
    public static partial class PurchasingSettings
    {
        [ThreadAndSerializationSafe()]
        public static extern bool enabled { get; set; }

        internal static extern bool enabledForPlatform { get; }

        internal static extern void ApplyEnableSettings(BuildTarget target);

        internal static extern void SetEnabledServiceWindow(bool enabled);
    }

}
