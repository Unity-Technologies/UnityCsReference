// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.Connect
{
    [NativeHeader("Modules/UnityConnect/UnityConnectSettings.h")]
    internal class UnityConnectSettings : Object
    {
        [StaticAccessor("GetUnityConnectSettings()", StaticAccessorType.Dot)]
        public extern static bool enabled
        {
            get;
            set;
        }

        [StaticAccessor("GetUnityConnectSettings()", StaticAccessorType.Dot)]
        public extern static bool testMode
        {
            get;
            set;
        }

        [StaticAccessor("GetUnityConnectSettings()", StaticAccessorType.Dot)]
        public extern static string eventUrl
        {
            get;
            set;
        }

        [StaticAccessor("GetUnityConnectSettings()", StaticAccessorType.Dot)]
        public extern static string eventOldUrl
        {
            get;
            set;
        }

        [StaticAccessor("GetUnityConnectSettings()", StaticAccessorType.Dot)]
        public extern static string configUrl
        {
            get;
            set;
        }

        [StaticAccessor("GetUnityConnectSettings()", StaticAccessorType.Dot)]
        public extern static int testInitMode
        {
            get;
            set;
        }
    }
}
