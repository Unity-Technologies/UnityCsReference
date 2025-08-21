// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEngine.AdaptivePerformance
{
    internal static class AdaptivePerformanceAnalytics
    {
        /*#if UNITY_ANALYTICS
                [Serializable]
                internal struct ProviderData
                {
                    public bool enabled;
                    public string id;
                    public string version;
                    public string customData;
                }

                [Serializable]
                internal struct FeatureData
                {
                    public bool enabled;
                    public string id;
                    public string customData;
                }

                [Serializable]
                internal class AdaptivePerformanceAnalyticsEvent
                {
                    // Is Adaptive Performance enable at all or only added as package and disabled
                    public bool enabled;
                    // Is Adaptive Performance active and has at least one provider initialized
                    public bool initialized;
                    // Name of the currently active provider
                    public string activeProvider;
                    public ProviderData[] providerData = new ProviderData[0];
                    public string ctrlMode;
                    public FeatureData[] featureData = new FeatureData[0];

                    public AdaptivePerformanceAnalyticsEvent()
                    {
                        UpdateProviderData();
                        UpdateFeatureData();
                    }

                    void UpdateProviderData()
                    {
                        List<Provider.AdaptivePerformanceSubsystemDescriptor> perfDescriptors = Provider.AdaptivePerformanceSubsystemRegistry.GetRegisteredDescriptors();
                        if (perfDescriptors == null)
                            return;

                        if (perfDescriptors.Count == providerData.Length)
                            return;

                        Array.Resize<ProviderData>(ref providerData, perfDescriptors.Count);
                        for (var i = 0; i < providerData.Length; ++i)
                        {
                            providerData[i].id = perfDescriptors[i].id;
                            providerData[i].version = "0.0";
                            providerData[i].enabled = false;
                            providerData[i].customData = "";
                        }
                    }

                    public void UpdateFeatureData()
                    {
                        if (s_Features.Count == featureData.Length)
                            return;

                        featureData = s_Features.ToArray();
                    }

                    public void UpdateGeneralEventData()
                    {
                        var ap = Holder.Instance;
                        if (ap == null)
                            return;

                        enabled = ap.Active;
                        if (ap.DevicePerformanceControl != null)
                        {
                            ctrlMode = ap.DevicePerformanceControl.PerformanceControlMode.ToString();
                        }
                    }
                }

                [Serializable]
                internal struct AdaptivePerformanceThermalAnalyticsEvent
                {
                    public long numThrottlingEventSinceStartup;
                    public long numThrottlingImminentEventSinceStartup;
                    public long numNoWarningEventSinceStartup;
                    public float currentTempTrend;
                    public float currentTempLevel;
                }

                enum EventName
                {
                    AdaptivePerformance,
                    AdaptivePerformanceThermal
                }

                const string    k_VendorKey                     = "unity.adaptiveperformance";
                const int       k_MaxEventsPerHour              = 100;
                const int       k_MaxNumberOfElementsInStruct   = 10;

                static AdaptivePerformanceAnalyticsEvent s_AdaptivePerformanceEvent;
                static AdaptivePerformanceThermalAnalyticsEvent s_AdaptivePerformanceThermalEvent;
                static List<FeatureData> s_Features = new List<FeatureData>();
                static WarningLevel s_LastWarningLevel = WarningLevel.NoWarning;
                static bool s_IsRegistered;
        #endif/*/

                // A feature registers itself with a feature name and once a event is sent, the feature list of the event is updated with this new event
                [Conditional("UNITY_ANALYTICS")]
                public static void RegisterFeature(string feature, bool status)
                {
       /* #if UNITY_ANALYTICS
                    Assert.IsFalse(feature.Equals(string.Empty));

                    s_Features.Add(new FeatureData { id = feature, enabled = status, customData = "" });
        #endif*/
                }

                [Conditional("UNITY_ANALYTICS")]
                public static void SendAdaptiveStartupEvent(Provider.AdaptivePerformanceSubsystem subsystem)
                {
       /* #if UNITY_ANALYTICS

                    if (s_AdaptivePerformanceEvent == null)
                        s_AdaptivePerformanceEvent = new AdaptivePerformanceAnalyticsEvent();

                    s_AdaptivePerformanceEvent.initialized = subsystem != null ? subsystem.Initialized : false;
                    s_AdaptivePerformanceEvent.activeProvider = subsystem != null ? subsystem.subsystemDescriptor.id : "NoSubsystemLoaded";

                    s_AdaptivePerformanceEvent.UpdateGeneralEventData();
                    var providerData = s_AdaptivePerformanceEvent.providerData;
                    if (subsystem != null)
                    {
                        for (var i = 0; i < providerData.Length; ++i)
                        {
                            if (providerData[i].id == subsystem.subsystemDescriptor.id)
                            {
                                providerData[i].version = subsystem.Version.ToString();
                                providerData[i].enabled = subsystem.running;
                            }
                        }
                    }
                    s_AdaptivePerformanceEvent.UpdateFeatureData();

                    Send(EventName.AdaptivePerformance, s_AdaptivePerformanceEvent);
        #endif//*/
                }

                // If the status of a feature changes it uses this method to update the AdaptivePerformanceEvent and sends the update. Features should not change often as it has performance implications using string comparison.
                [Conditional("UNITY_ANALYTICS")]
                public static void SendAdaptiveFeatureUpdateEvent(string feature, bool status)
                {
      /*  #if UNITY_ANALYTICS
                    // When features are initialized adaptivePerformanceEvent is not created yet but SendAdaptiveStartupEvent will send a status update for all events during creation of the event.
                    if (s_AdaptivePerformanceEvent == null)
                        return;

                    s_AdaptivePerformanceEvent.UpdateGeneralEventData();
                    s_AdaptivePerformanceEvent.UpdateFeatureData();

                    for (var i = 0; i < s_AdaptivePerformanceEvent.featureData.Length; ++i)
                    {
                        if (s_AdaptivePerformanceEvent.featureData[i].id.Equals(feature))
                        {
                            if (s_AdaptivePerformanceEvent.featureData[i].enabled == status)
                                return;

                            s_AdaptivePerformanceEvent.featureData[i].enabled = status;
                        }
                    }

                    Send(EventName.AdaptivePerformance, s_AdaptivePerformanceEvent);
        #endif*/
                }

                [Conditional("UNITY_ANALYTICS")]
                public static void SendAdaptivePerformanceThermalEvent(ThermalMetrics thermalMetrics)
                {
       /* #if UNITY_ANALYTICS
                    // Temperature level and trend will call the method more often but we do not want to send events
                    if (s_LastWarningLevel == thermalMetrics.WarningLevel)
                        return;

                    switch (thermalMetrics.WarningLevel)
                    {
                        case WarningLevel.Throttling:
                            s_AdaptivePerformanceThermalEvent.numThrottlingEventSinceStartup++; break;
                        case WarningLevel.ThrottlingImminent:
                            s_AdaptivePerformanceThermalEvent.numThrottlingImminentEventSinceStartup++; break;
                        case WarningLevel.NoWarning:
                            s_AdaptivePerformanceThermalEvent.numNoWarningEventSinceStartup++; break;
                    }

                    s_AdaptivePerformanceThermalEvent.currentTempLevel = thermalMetrics.TemperatureLevel;
                    s_AdaptivePerformanceThermalEvent.currentTempTrend = thermalMetrics.TemperatureTrend;

                    s_LastWarningLevel = thermalMetrics.WarningLevel;

                    Send(EventName.AdaptivePerformanceThermal, s_AdaptivePerformanceThermalEvent);
        #endif*/
                }

        /*#if UNITY_ANALYTICS
                static bool RegisterEvents()
                {
                    if (s_IsRegistered)
                        return true;

                    var allEventNames = Enum.GetNames(typeof(EventName));
                    for (var i = 0; i < allEventNames.Length; ++i)
                    {
                        if (!RegisterEvent(allEventNames[i]))
                            return false;
                    }

                    s_IsRegistered = true;
                    return s_IsRegistered;
                }

                static bool RegisterEvent(string eventName)
                {
                    var result = Analytics.Analytics.RegisterEvent(eventName, k_MaxEventsPerHour, k_MaxNumberOfElementsInStruct, k_VendorKey);
                    switch (result)
                    {
                        case Analytics.AnalyticsResult.Ok:
                            AnalyticsLog.Debug("Registered event: {0}", eventName);
                            return true;
                        case Analytics.AnalyticsResult.TooManyRequests:
                            // this is fine - event registration survives domain reload (native)
                            return true;
                        default:
                            AnalyticsLog.Debug("Failed to register event {0}. Result: {1}", eventName, result);
                            return false;
                    }
                }

                static void Send(EventName eventName, object eventData)
                {
                    if (!RegisterEvents())
                    {
                        AnalyticsLog.Debug("Disabled: event='{0}', time='{1}', payload={2}", eventName, DateTime.Now, JsonUtility.ToJson(eventData));
                        return;
                    }

                    try
                    {
                        var result = Analytics.Analytics.SendEvent(eventName.ToString(), eventData);
                        if (result == Analytics.AnalyticsResult.Ok)
                            AnalyticsLog.Debug("Event sent: event='{0}', time='{1}', payload={2}", eventName, DateTime.Now, JsonUtility.ToJson(eventData));
                        else
                            AnalyticsLog.Debug("Failed to send event {0}. Result: {1}", eventName, result);
                    }
                    catch (Exception ex)
                    {
                        AnalyticsLog.Debug("Failed to send event {0}. Result: {1}", eventName, ex);
                    }
                }

        #endif*/
                internal static class AnalyticsLog
                {
                    [Conditional("ADAPTIVE_PERFORMANCE_ANALYTICS_LOGGING")]
                    public static void Debug(string format, params object[] args)
                    {
                       // IAdaptivePerformanceSettings settings = AdaptivePerformanceGeneralSettings.Instance?.Manager.ActiveLoaderAs<AdaptivePerformanceLoader>()?.GetSettings();
                      //  if (settings != null && settings.logging)
                      //      UnityEngine.Debug.Log(System.String.Format("[Analytics] " + format, args));
                    }
                }
    }
}
