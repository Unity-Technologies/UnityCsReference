// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;


namespace UnityEngine.Analytics
{
    public enum Gender
    {
        Male,
        Female,
        Unknown
    }

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

    public static partial class Analytics
    {
        public static bool playerOptedOut
        {
            get
            {
                if (!IsInitialized())
                    return false;
                return playerOptedOutInternal;
            }
        }

        public static bool limitUserTracking
        {
            get
            {
                if (!IsInitialized())
                    return false;
                return limitUserTrackingInternal;
            }
            set
            {
                if (IsInitialized())
                    limitUserTrackingInternal = value;
            }
        }

        public static bool deviceStatsEnabled
        {
            get
            {
                if (!IsInitialized())
                    return false;
                return deviceStatsEnabledInternal;
            }
            set
            {
                if (IsInitialized())
                    deviceStatsEnabledInternal = value;
            }
        }

        public static bool enabled
        {
            get
            {
                if (!IsInitialized())
                    return false;
                return enabledInternal;
            }
            set
            {
                if (IsInitialized())
                    enabledInternal = value;
            }
        }

        public static AnalyticsResult FlushEvents()
        {
            if (!IsInitialized())
                return AnalyticsResult.NotInitialized;
            return FlushArchivedEvents() ? AnalyticsResult.Ok : AnalyticsResult.NotInitialized;
        }

        [Serializable]
        private struct UserInfo
        {
            public string custom_userid;
            public string sex;
        };

        public static AnalyticsResult SetUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("Cannot set userId to an empty or null string");
            return SendUserInfoEvent(new UserInfo() { custom_userid = userId });
        }

        public static AnalyticsResult SetUserGender(Gender gender)
        {
            return SendUserInfoEvent(new UserInfo() { sex = gender == Gender.Male ? "M" : gender == Gender.Female ? "F" : "U" });
        }

        [Serializable]
        private struct UserInfoBirthYear
        {
            public int birth_year;
        };

        public static AnalyticsResult SetUserBirthYear(int birthYear)
        {
            return SendUserInfoEvent(new UserInfoBirthYear() { birth_year = birthYear });
        }

        private static AnalyticsResult SendUserInfoEvent(object param)
        {
            if (!IsInitialized())
                return AnalyticsResult.NotInitialized;
            QueueEvent("userInfo", param, 1, String.Empty);
            return AnalyticsResult.Ok;
        }

        public static AnalyticsResult Transaction(string productId, decimal amount, string currency)
        {
            return Transaction(productId, amount, currency, null, null, false);
        }

        public static AnalyticsResult Transaction(string productId, decimal amount, string currency, string receiptPurchaseData, string signature)
        {
            return Transaction(productId, amount, currency, receiptPurchaseData, signature, false);
        }

        public static AnalyticsResult Transaction(string productId, decimal amount, string currency, string receiptPurchaseData, string signature, bool usingIAPService)
        {
            if (string.IsNullOrEmpty(productId))
                throw new ArgumentException("Cannot set productId to an empty or null string");
            if (string.IsNullOrEmpty(currency))
                throw new ArgumentException("Cannot set currency to an empty or null string");
            if (!IsInitialized())
                return AnalyticsResult.NotInitialized;
            if (receiptPurchaseData == null)
                receiptPurchaseData = string.Empty;
            if (signature == null)
                signature = string.Empty;
            return Transaction(productId, Convert.ToDouble(amount), currency, receiptPurchaseData, signature, usingIAPService);
        }

        public static AnalyticsResult CustomEvent(string customEventName)
        {
            if (string.IsNullOrEmpty(customEventName))
                throw new ArgumentException("Cannot set custom event name to an empty or null string");
            if (!IsInitialized())
                return AnalyticsResult.NotInitialized;
            return SendCustomEventName(customEventName);
        }

        public static AnalyticsResult CustomEvent(string customEventName, Vector3 position)
        {
            if (string.IsNullOrEmpty(customEventName))
                throw new ArgumentException("Cannot set custom event name to an empty or null string");
            if (!IsInitialized())
                return AnalyticsResult.NotInitialized;
            CustomEventData customEvent = new CustomEventData(customEventName);
            customEvent.AddDouble("x", (double)System.Convert.ToDecimal(position.x));
            customEvent.AddDouble("y", (double)System.Convert.ToDecimal(position.y));
            customEvent.AddDouble("z", (double)System.Convert.ToDecimal(position.z));
            var result = SendCustomEvent(customEvent);
            customEvent.Dispose();
            return result;
        }

        public static AnalyticsResult CustomEvent(string customEventName, IDictionary<string, object> eventData)
        {
            if (string.IsNullOrEmpty(customEventName))
                throw new ArgumentException("Cannot set custom event name to an empty or null string");
            if (!IsInitialized())
                return AnalyticsResult.NotInitialized;
            if (eventData == null)
                return SendCustomEventName(customEventName);
            CustomEventData customEvent = new CustomEventData(customEventName);
            customEvent.AddDictionary(eventData);
            var result = SendCustomEvent(customEvent);
            customEvent.Dispose();
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static AnalyticsResult RegisterEvent(string eventName, int maxEventPerHour, int maxItems, string vendorKey = "", string prefix = "")
        {
            string n = String.Empty;
            return RegisterEvent(eventName, maxEventPerHour, maxItems, vendorKey, 1, prefix, n);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static AnalyticsResult RegisterEvent(string eventName, int maxEventPerHour, int maxItems, string vendorKey, int ver, string prefix = "")
        {
            string n = String.Empty;
            return RegisterEvent(eventName, maxEventPerHour, maxItems, vendorKey, ver, prefix, n);
        }

        private static AnalyticsResult RegisterEvent(string eventName, int maxEventPerHour, int maxItems, string vendorKey, int ver, string prefix, string assemblyInfo)
        {
            if (string.IsNullOrEmpty(eventName))
                throw new ArgumentException("Cannot set event name to an empty or null string");
            if (!IsInitialized())
                return AnalyticsResult.NotInitialized;
            return RegisterEventWithLimit(eventName, maxEventPerHour, maxItems, vendorKey, ver, prefix, assemblyInfo, true);
        }

        public static AnalyticsResult SendEvent(string eventName, object parameters, int ver = 1, string prefix = "")
        {
            if (string.IsNullOrEmpty(eventName))
                throw new ArgumentException("Cannot set event name to an empty or null string");
            if (parameters == null)
                throw new ArgumentException("Cannot set parameters to null");
            if (!IsInitialized())
                return AnalyticsResult.NotInitialized;
            return SendEventWithLimit(eventName, parameters, ver, prefix);
        }
    }
}

