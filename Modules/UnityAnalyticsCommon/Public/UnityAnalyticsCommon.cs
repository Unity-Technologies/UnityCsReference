// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections;

namespace UnityEngine.Analytics
{
    public enum AnalyticsResult
    {
        Ok,
        NotInitialized,
        AnalyticsDisabled,
        TooManyItems,
        SizeLimitReached,
        TooManyRequests,
        InvalidData,
        UnsupportedPlatform
    }

    public static partial class AnalyticsCommon
    {
        public static bool ugsAnalyticsEnabled
        {
            get
            {
                return ugsAnalyticsEnabledInternal;
            }
            set
            {
                ugsAnalyticsEnabledInternal = value;
            }
        }
    }

    public interface UGSAnalyticsInternalTools
    {
        public static void SetPrivacyStatus(bool status)
        {
            AnalyticsCommon.ugsAnalyticsEnabled = status;
        }
    }
}
