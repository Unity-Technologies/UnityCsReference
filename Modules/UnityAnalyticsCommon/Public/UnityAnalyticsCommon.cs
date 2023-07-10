// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using UnityEngine.Scripting;


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

    [UnityEngine.Internal.ExcludeFromDocs]
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

    [UnityEngine.Internal.ExcludeFromDocs]
    public interface UGSAnalyticsInternalTools
    {
        public static void SetPrivacyStatus(bool status)
        {
            AnalyticsCommon.ugsAnalyticsEnabled = status;
        }
    }

    [UnityEngine.Internal.ExcludeFromDocs]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class AnalyticInfoAttribute : Attribute
    {
        public int version { get; }
        public string vendorKey { get; }
        public string eventName { get; }
        internal int maxEventsPerHour { get; }
        internal int maxNumberOfElements { get; }

        /// <summary>
        /// Constructs the attribute
        /// </summary>
        /// <param name="eventName">The event name</param>
        /// <param name="vendorKey">The vendor key</param>
        /// <param name="version">The version</param>
        /// <param name="maxEventsPerHour">Maximum events per hour</param>
        /// <param name="maxNumberOfElements">Max number of elements</param>
        public AnalyticInfoAttribute(string eventName, string vendorKey = "", int version = 1, int maxEventsPerHour = 1000, int maxNumberOfElements = 1000)
        {
            this.version = version;
            this.vendorKey = vendorKey;
            this.eventName = eventName;
            this.maxEventsPerHour = maxEventsPerHour;
            this.maxNumberOfElements = maxNumberOfElements;
        }
    }
    /// <summary>
    /// Public interface to declare an analytic
    /// </summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    public interface IAnalytic
    {
        /// <summary>
        /// The data that will be sent to the server
        /// </summary>
        public interface IData { }

        public abstract bool TryGatherData(out IData data, [NotNullWhen(false)] out Exception error);

        /// <summary>
        /// Multiple data that will be sent to the server
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public struct DataList<T> : IEnumerable, IData
            where T : struct
        {
            readonly T[] m_UsageData;

            /// <summary>
            /// Constructs the collection
            /// </summary>
            /// <param name="datas">The elements</param>
            public DataList(T[] datas)
            {
                m_UsageData = datas;
            }

            public IEnumerator GetEnumerator() => m_UsageData.GetEnumerator();
        }
    }

    [UnityEngine.Internal.ExcludeFromDocs]
    public class Analytic : UnityEngine.Analytics.AnalyticsEventBase
    {
        public readonly IAnalytic instance;
        public readonly AnalyticInfoAttribute info;

        public Analytic(IAnalytic instance, AnalyticInfoAttribute info) : base(info.eventName, info.version)
        {
            this.instance = instance;
            this.info = info;
        }
    }
}
