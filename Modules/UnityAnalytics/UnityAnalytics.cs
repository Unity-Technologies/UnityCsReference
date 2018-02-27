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

    public static class Analytics
    {
        private static UnityAnalyticsHandler s_UnityAnalyticsHandler;

        internal static UnityAnalyticsHandler GetUnityAnalyticsHandler()
        {
            if (s_UnityAnalyticsHandler == null)
                s_UnityAnalyticsHandler = new UnityAnalyticsHandler();
            if (s_UnityAnalyticsHandler.IsInitialized())
                return s_UnityAnalyticsHandler;
            return null;
        }

        public static bool limitUserTracking
        {
            get
            {
                return UnityAnalyticsHandler.limitUserTracking;
            }
            set
            {
                UnityAnalyticsHandler.limitUserTracking = value;
            }
        }

        public static bool deviceStatsEnabled
        {
            get
            {
                return UnityAnalyticsHandler.deviceStatsEnabled;
            }
            set
            {
                UnityAnalyticsHandler.deviceStatsEnabled = value;
            }
        }

        public static bool enabled
        {
            get
            {
                UnityAnalyticsHandler unityAnalyticsHandler = GetUnityAnalyticsHandler();
                if (unityAnalyticsHandler == null)
                    return false;
                return unityAnalyticsHandler.enabled;
            }
            set
            {
                UnityAnalyticsHandler unityAnalyticsHandler = GetUnityAnalyticsHandler();
                if (unityAnalyticsHandler != null)
                    unityAnalyticsHandler.enabled = value;
            }
        }

        public static AnalyticsResult FlushEvents()
        {
            UnityAnalyticsHandler unityAnalyticsHandler = GetUnityAnalyticsHandler();
            if (unityAnalyticsHandler == null)
                return AnalyticsResult.NotInitialized;

            return unityAnalyticsHandler.FlushEvents() ? AnalyticsResult.Ok : AnalyticsResult.NotInitialized;
        }

        public static AnalyticsResult SetUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("Cannot set userId to an empty or null string");
            UnityAnalyticsHandler unityAnalyticsHandler = GetUnityAnalyticsHandler();
            if (unityAnalyticsHandler == null)
                return AnalyticsResult.NotInitialized;
            return unityAnalyticsHandler.SetUserId(userId);
        }

        public static AnalyticsResult SetUserGender(Gender gender)
        {
            UnityAnalyticsHandler unityAnalyticsHandler = GetUnityAnalyticsHandler();
            if (unityAnalyticsHandler == null)
                return AnalyticsResult.NotInitialized;
            return unityAnalyticsHandler.SetUserGender(gender);
        }

        public static AnalyticsResult SetUserBirthYear(int birthYear)
        {
            UnityAnalyticsHandler unityAnalyticsHandler = GetUnityAnalyticsHandler();
            if (s_UnityAnalyticsHandler == null)
                return AnalyticsResult.NotInitialized;
            return unityAnalyticsHandler.SetUserBirthYear(birthYear);
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
            UnityAnalyticsHandler unityAnalyticsHandler = GetUnityAnalyticsHandler();
            if (unityAnalyticsHandler == null)
                return AnalyticsResult.NotInitialized;
            if (receiptPurchaseData == null)
                receiptPurchaseData = string.Empty;
            if (signature == null)
                signature = string.Empty;
            return unityAnalyticsHandler.Transaction(productId, Convert.ToDouble(amount), currency, receiptPurchaseData, signature, usingIAPService);
        }

        public static AnalyticsResult CustomEvent(string customEventName)
        {
            if (string.IsNullOrEmpty(customEventName))
                throw new ArgumentException("Cannot set custom event name to an empty or null string");
            UnityAnalyticsHandler unityAnalyticsHandler = GetUnityAnalyticsHandler();
            if (unityAnalyticsHandler == null)
                return AnalyticsResult.NotInitialized;
            return unityAnalyticsHandler.SendCustomEventName(customEventName);
        }

        public static AnalyticsResult CustomEvent(string customEventName, Vector3 position)
        {
            if (string.IsNullOrEmpty(customEventName))
                throw new ArgumentException("Cannot set custom event name to an empty or null string");
            UnityAnalyticsHandler unityAnalyticsHandler = GetUnityAnalyticsHandler();
            if (unityAnalyticsHandler == null)
                return AnalyticsResult.NotInitialized;
            CustomEventData customEvent = new CustomEventData(customEventName);
            customEvent.AddDouble("x", (double)System.Convert.ToDecimal(position.x));
            customEvent.AddDouble("y", (double)System.Convert.ToDecimal(position.y));
            customEvent.AddDouble("z", (double)System.Convert.ToDecimal(position.z));
            return unityAnalyticsHandler.SendCustomEvent(customEvent);
        }

        public static AnalyticsResult CustomEvent(string customEventName, IDictionary<string, object> eventData)
        {
            if (string.IsNullOrEmpty(customEventName))
                throw new ArgumentException("Cannot set custom event name to an empty or null string");
            UnityAnalyticsHandler unityAnalyticsHandler = GetUnityAnalyticsHandler();
            if (unityAnalyticsHandler == null)
                return AnalyticsResult.NotInitialized;
            if (eventData == null)
                return unityAnalyticsHandler.SendCustomEventName(customEventName);
            CustomEventData customEvent = new CustomEventData(customEventName);
            customEvent.AddDictionary(eventData);
            return unityAnalyticsHandler.SendCustomEvent(customEvent);
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
            UnityAnalyticsHandler unityAnalyticsHandler = GetUnityAnalyticsHandler();
            if (unityAnalyticsHandler == null)
                return AnalyticsResult.NotInitialized;
            return unityAnalyticsHandler.RegisterEvent(eventName, maxEventPerHour, maxItems, vendorKey, ver, prefix, assemblyInfo);
        }

        public static AnalyticsResult SendEvent(string eventName, object parameters, int ver = 1, string prefix = "")
        {
            if (string.IsNullOrEmpty(eventName))
                throw new ArgumentException("Cannot set event name to an empty or null string");
            if (parameters == null)
                throw new ArgumentException("Cannot set parameters to null");
            UnityAnalyticsHandler unityAnalyticsHandler = GetUnityAnalyticsHandler();
            if (unityAnalyticsHandler == null)
                return AnalyticsResult.NotInitialized;
            return unityAnalyticsHandler.SendEvent(eventName, parameters, ver, prefix);
        }
    }
}

