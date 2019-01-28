// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;


using UnityEngine.Connect;
using uei = UnityEngine.Internal;

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
            return s_UnityAnalyticsHandler;
        }

        [uei.ExcludeFromDocs]
        public static bool initializeOnStartup
        {
            get
            {
                return UnityAnalyticsHandler.initializeOnStartup;
            }
            set
            {
                UnityAnalyticsHandler.initializeOnStartup = value;
            }
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

        [uei.ExcludeFromDocs]
        public static AnalyticsResult ResumeInitialization()
        {
            UnityAnalyticsHandler unityAnalyticsHandler = GetUnityAnalyticsHandler();
            if (unityAnalyticsHandler == null)
                return AnalyticsResult.NotInitialized;

            return (AnalyticsResult)unityAnalyticsHandler.ResumeInitialization();
        }

        public static AnalyticsResult FlushEvents()
        {
            UnityAnalyticsHandler unityAnalyticsHandler = GetUnityAnalyticsHandler();
            if (unityAnalyticsHandler == null)
                return AnalyticsResult.NotInitialized;

            return (AnalyticsResult)unityAnalyticsHandler.FlushEvents();
        }

        public static AnalyticsResult SetUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("Cannot set userId to an empty or null string");
            UnityAnalyticsHandler unityAnalyticsHandler = GetUnityAnalyticsHandler();
            if (unityAnalyticsHandler == null)
                return AnalyticsResult.NotInitialized;
            return (AnalyticsResult)unityAnalyticsHandler.SetUserId(userId);
        }

        public static AnalyticsResult SetUserGender(Gender gender)
        {
            UnityAnalyticsHandler unityAnalyticsHandler = GetUnityAnalyticsHandler();
            if (unityAnalyticsHandler == null)
                return AnalyticsResult.NotInitialized;
            return (AnalyticsResult)unityAnalyticsHandler.SetUserGender(gender);
        }

        public static AnalyticsResult SetUserBirthYear(int birthYear)
        {
            UnityAnalyticsHandler unityAnalyticsHandler = GetUnityAnalyticsHandler();
            if (s_UnityAnalyticsHandler == null)
                return AnalyticsResult.NotInitialized;
            return (AnalyticsResult)unityAnalyticsHandler.SetUserBirthYear(birthYear);
        }

        public static AnalyticsResult Transaction(string productId, decimal amount, string currency)
        {
            UnityAnalyticsHandler unityAnalyticsHandler = GetUnityAnalyticsHandler();
            if (unityAnalyticsHandler == null)
                return AnalyticsResult.NotInitialized;
            return (AnalyticsResult)unityAnalyticsHandler.Transaction(productId, Convert.ToDouble(amount), currency, null, null);
        }

        public static AnalyticsResult Transaction(string productId, decimal amount, string currency, string receiptPurchaseData, string signature)
        {
            UnityAnalyticsHandler unityAnalyticsHandler = GetUnityAnalyticsHandler();
            if (unityAnalyticsHandler == null)
                return AnalyticsResult.NotInitialized;
            return (AnalyticsResult)unityAnalyticsHandler.Transaction(productId, Convert.ToDouble(amount), currency, receiptPurchaseData, signature);
        }

        public static AnalyticsResult Transaction(string productId, decimal amount, string currency, string receiptPurchaseData, string signature, bool usingIAPService)
        {
            UnityAnalyticsHandler unityAnalyticsHandler = GetUnityAnalyticsHandler();
            if (unityAnalyticsHandler == null)
                return AnalyticsResult.NotInitialized;
            return (AnalyticsResult)unityAnalyticsHandler.Transaction(productId, Convert.ToDouble(amount), currency, receiptPurchaseData, signature, usingIAPService);
        }

        public static AnalyticsResult CustomEvent(string customEventName)
        {
            if (string.IsNullOrEmpty(customEventName))
                throw new ArgumentException("Cannot set custom event name to an empty or null string");
            UnityAnalyticsHandler unityAnalyticsHandler = GetUnityAnalyticsHandler();
            if (unityAnalyticsHandler == null)
                return AnalyticsResult.NotInitialized;
            return (AnalyticsResult)unityAnalyticsHandler.CustomEvent(customEventName);
        }

        public static AnalyticsResult CustomEvent(string customEventName, Vector3 position)
        {
            if (string.IsNullOrEmpty(customEventName))
                throw new ArgumentException("Cannot set custom event name to an empty or null string");
            UnityAnalyticsHandler unityAnalyticsHandler = GetUnityAnalyticsHandler();
            if (unityAnalyticsHandler == null)
                return AnalyticsResult.NotInitialized;
            CustomEventData customEvent = new CustomEventData(customEventName);
            customEvent.Add("x", (double)System.Convert.ToDecimal(position.x));
            customEvent.Add("y", (double)System.Convert.ToDecimal(position.y));
            customEvent.Add("z", (double)System.Convert.ToDecimal(position.z));
            return (AnalyticsResult)unityAnalyticsHandler.CustomEvent(customEvent);
        }

        public static AnalyticsResult CustomEvent(string customEventName, IDictionary<string, object> eventData)
        {
            if (string.IsNullOrEmpty(customEventName))
                throw new ArgumentException("Cannot set custom event name to an empty or null string");
            UnityAnalyticsHandler unityAnalyticsHandler = GetUnityAnalyticsHandler();
            if (unityAnalyticsHandler == null)
                return AnalyticsResult.NotInitialized;
            if (eventData == null)
                return (AnalyticsResult)unityAnalyticsHandler.CustomEvent(customEventName);
            CustomEventData customEvent = new CustomEventData(customEventName);
            customEvent.Add(eventData);
            return (AnalyticsResult)unityAnalyticsHandler.CustomEvent(customEvent);
        }
    }
}

