// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;


namespace UnityEditor.Analytics
{
    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    public class LicensingErrorAnalytic : UnityEngine.Analytics.AnalyticsEventBase
    {
        public LicensingErrorAnalytic() : base("license_error", 1) { }

        [UsedByNativeCode]
        internal static LicensingErrorAnalytic CreateLicensingErrorAnalytic() { return new LicensingErrorAnalytic(); }

        public string licensingErrorType;
        public string additionalData;
        public string errorMessage;
        public string correlationId;
        public string sessionId;
    }

    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    public class LicensingInitAnalytic : UnityEngine.Analytics.AnalyticsEventBase
    {
        public LicensingInitAnalytic() : base("license_init", 1) { }

        [UsedByNativeCode]
        internal static LicensingInitAnalytic CreateLicensingInitAnalytic() { return new LicensingInitAnalytic(); }

        public string licensingProtocolVersion;
        public string licensingClientVersion;
        public string channelType;
        public double initTime;
        public bool isLegacy;
        public string sessionId;
        public string correlationId;
    }
}
