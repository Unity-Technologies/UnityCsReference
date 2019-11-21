// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor.Collaboration;
using UnityEditor.StyleSheets;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Mono.UnityConnect.Services
{
    internal class AnalyticsValidatorEvent
    {
        readonly DateTime m_TimeStamp;
        readonly string m_Type;

        protected readonly string m_JsonParams;

        readonly string m_Platform;
        readonly string m_SdkVersion;

        const string k_JsonKeyTime = "event_time"; //Value store is Milliseconds since beginning of k_UnixEpoch
        const string k_JsonKeyType = "event_type";
        const string k_JsonKeyParams = "json_param_string";
        const string k_JsonKeyPlatfrom = "platform";
        const string k_JsonKeySdkVersion = "sdk_ver";

        static readonly DateTime k_UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        internal AnalyticsValidatorEvent(JSONValue json)
        {
            m_TimeStamp = k_UnixEpoch.AddMilliseconds(Convert.ToDouble(json.AsDict()[k_JsonKeyTime].AsObject()));
            m_Type = json.AsDict()[k_JsonKeyType].AsString();
            m_JsonParams = json.AsDict()[k_JsonKeyParams].AsString();

            var jsonParser = new JSONParser(m_JsonParams);
            try
            {
                var jsonInnerData = jsonParser.Parse();
                m_Platform = jsonInnerData.AsDict()[k_JsonKeyPlatfrom].AsString();
                m_SdkVersion = jsonInnerData.AsDict()[k_JsonKeySdkVersion].AsString();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public DateTime GetTimeStamp()
        {
            return m_TimeStamp.ToUniversalTime();
        }

        public string GetTimeStampText()
        {
            return m_TimeStamp.ToUniversalTime().ToString();
        }

        public string GetTypeText()
        {
            return m_Type;
        }

        public string GetPlatformText()
        {
            return m_Platform;
        }

        public string GetSdkVersionText()
        {
            return m_SdkVersion;
        }
    }

    internal class CustomValidatorEvent : AnalyticsValidatorEvent
    {
        readonly string m_Name;
        readonly Dictionary<string, JSONValue> m_CustomParams;

        const string k_JsonKeyName = "name";
        const string k_JsonKeyCustomParams = "custom_params";

        internal CustomValidatorEvent(JSONValue json)
            : base(json)
        {
            var jsonParser = new JSONParser(m_JsonParams);
            try
            {
                var jsonInnerData = jsonParser.Parse();
                m_Name = jsonInnerData.AsDict()[k_JsonKeyName].AsString();
                m_CustomParams = jsonInnerData.AsDict()[k_JsonKeyCustomParams].AsDict();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public string GetNameText()
        {
            return m_Name;
        }

        public List<string> GetCustomParamsTexts(string formatTemplate)
        {
            var outputTexts = new List<string>();

            foreach (var customParam in m_CustomParams)
            {
                outputTexts.Add(string.Format(formatTemplate, customParam.Key, customParam.Value.ToString()));
            }

            return outputTexts;
        }
    }

    internal class DeviceInfoValidatorEvent : AnalyticsValidatorEvent
    {
        readonly string m_DeviceType;
        readonly string m_OSVersion;
        readonly string m_AppVersion;
        readonly string m_BundleId;
        readonly string m_Processor;
        readonly int m_SystemMemory;
        readonly string m_UnityEngine;

        const string k_JsonKeyDeviceType = "model";
        const string k_JsonKeyOSVersion = "os_ver";
        const string k_JsonKeyAppVersion = "app_ver";
        const string k_JsonKeyBundleId = "app_name";
        const string k_JsonKeyProcessor = "processor_type";
        const string k_JsonKeySystemMemory = "system_memory_size";
        const string k_JsonKeyUnityEngine = "engine_ver";

        private const string k_ValueForMissingKey = "not specified";

        internal DeviceInfoValidatorEvent(JSONValue json)
            : base(json)
        {
            var jsonParser = new JSONParser(m_JsonParams);
            try
            {
                var jsonInnerData = jsonParser.Parse().AsDict();
                m_DeviceType = GetAssignmentString(jsonInnerData, k_JsonKeyDeviceType);
                m_OSVersion = GetAssignmentString(jsonInnerData, k_JsonKeyOSVersion);
                m_AppVersion = GetAssignmentString(jsonInnerData, k_JsonKeyAppVersion);
                m_BundleId = GetAssignmentString(jsonInnerData, k_JsonKeyBundleId);
                m_Processor = GetAssignmentString(jsonInnerData, k_JsonKeyProcessor);
                m_SystemMemory = jsonInnerData.ContainsKey(k_JsonKeySystemMemory) ? Convert.ToInt32(jsonInnerData[k_JsonKeySystemMemory].AsObject()) : 0;
                m_UnityEngine = GetAssignmentString(jsonInnerData, k_JsonKeyUnityEngine);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        string GetAssignmentString(Dictionary<string, JSONValue> dict, string key)
        {
            if (dict.ContainsKey(key))
            {
                return dict[key].ToString();
            }
            else
            {
                return k_ValueForMissingKey;
            }
        }

        public string GetDeviceTypeText()
        {
            return m_DeviceType;
        }

        public string GetOSVersionText()
        {
            return m_OSVersion;
        }

        public string GetAppVersionText()
        {
            return m_AppVersion;
        }

        public string GetBundleIdText()
        {
            return m_BundleId;
        }

        public string GetProcessorText()
        {
            return m_Processor;
        }

        public string GetSystemMemoryText()
        {
            return (m_SystemMemory > 0) ? m_SystemMemory.ToString() : k_ValueForMissingKey;
        }

        public string GetUnityEngineText()
        {
            return m_UnityEngine;
        }
    }

    internal class TransactionValidatorEvent : AnalyticsValidatorEvent
    {
        readonly float m_Price;
        readonly string m_Currency;
        readonly string m_ProductID;
        readonly bool m_HasReceipt;

        const string k_JsonKeyPrice = "amount";
        const string k_JsonKeyCurrency = "currency";
        const string k_JsonKeyProductId = "productid";
        const string k_JsonKeyHasReceipt = "receipt";

        internal TransactionValidatorEvent(JSONValue json)
            : base(json)
        {
            var jsonParser = new JSONParser(m_JsonParams);
            try
            {
                var jsonInnerData = jsonParser.Parse();
                m_Price = jsonInnerData.AsDict()[k_JsonKeyPrice].AsFloat();
                m_Currency = jsonInnerData.AsDict()[k_JsonKeyCurrency].AsString();
                m_ProductID = jsonInnerData.AsDict()[k_JsonKeyProductId].AsString();
                m_HasReceipt = jsonInnerData.AsDict().ContainsKey(k_JsonKeyHasReceipt);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public string GetPriceText()
        {
            return m_Price.ToString();
        }

        public string GetCurrencyText()
        {
            return m_Currency;
        }

        public string GetProductIdText()
        {
            return m_ProductID;
        }

        public bool HasReceipt()
        {
            return m_HasReceipt;
        }
    }
}
