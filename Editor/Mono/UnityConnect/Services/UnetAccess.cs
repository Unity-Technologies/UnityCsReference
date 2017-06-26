// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using UnityEditor.Connect;

namespace UnityEditor.Web
{
    [InitializeOnLoad]
    internal class UnetAccess : CloudServiceAccess
    {
        const string kServiceName = "UNet";
        const string kServiceDisplayName = "Multiplayer";
        const string kServiceUrl = "https://public-cdn.cloud.unity3d.com/editor/production/cloud/unet";

        public override string GetServiceName()
        {
            return kServiceName;
        }

        public override string GetServiceDisplayName()
        {
            return kServiceDisplayName;
        }

        [Serializable]
        public struct UnetServiceState { public bool unet; }
        override public void EnableService(bool enabled)
        {
            if (IsServiceEnabled() != enabled)
            {
                base.EnableService(enabled);
                EditorAnalytics.SendEventServiceInfo(new UnetServiceState() { unet =  enabled });
            }
        }

        static UnetAccess()
        {
            var serviceData = new UnityConnectServiceData(kServiceName, kServiceUrl, new UnetAccess(), "unity/project/cloud/networking");
            UnityConnectServiceCollection.instance.AddService(serviceData);
        }

        public void SetMultiplayerId(int id)
        {
        }
    }
}

