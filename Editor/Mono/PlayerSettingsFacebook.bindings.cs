// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngine.Bindings;

namespace UnityEditor
{
    public partial class PlayerSettings : UnityEngine.Object
    {
        [NativeHeader("Runtime/Misc/PlayerSettings.h")]
        public partial class Facebook
        {
            [NativeProperty("facebookSdkVersion")]
            public extern static string sdkVersion
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            [NativeProperty("facebookAppId")]
            public extern static string appId
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            [NativeProperty("facebookCookies", TargetType.Field)]
            public extern static bool useCookies
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            [NativeProperty("facebookLogging", TargetType.Field)]
            internal extern static bool useLogging
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            [NativeProperty("facebookStatus", TargetType.Field)]
            public extern static bool useStatus
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            [NativeProperty("facebookXfbml", TargetType.Field)]
            internal extern static bool useXfbml
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            [NativeProperty("facebookFrictionlessRequests", TargetType.Field)]
            public extern static bool useFrictionlessRequests
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }
        }
    }
}
