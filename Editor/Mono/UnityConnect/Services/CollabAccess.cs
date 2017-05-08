// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEditor;
using UnityEditorInternal;
using UnityEditor.Connect;
using UnityEditor.Collaboration;
using UnityEngine;

namespace UnityEditor.Web
{
    [InitializeOnLoad]
    internal class CollabAccess : CloudServiceAccess
    {
        private const string kServiceName = "Collab";
        private const string kServiceDisplayName = "Unity Collab";
        private const string kServiceUrl = "https://public-cdn.cloud.unity3d.com/editor/production/cloud/collab";

        static private CollabAccess s_instance;
        public static CollabAccess Instance
        {
            get
            {
                return s_instance;
            }
        }

        public override string GetServiceName()
        {
            return kServiceName;
        }

        public override string GetServiceDisplayName()
        {
            return kServiceDisplayName;
        }

        public override void EnableService(bool enabled)
        {
            base.EnableService(enabled);
            Collab.instance.SendNotification();
            Collab.instance.SetCollabEnabledForCurrentProject(enabled);

            AssetDatabase.Refresh();                    // If auto-refresh was off, make sure we refresh when setting it back on
        }

        static CollabAccess()
        {
            s_instance = new CollabAccess();

            var serviceData = new UnityConnectServiceData(kServiceName, kServiceUrl, s_instance, "unity/project/cloud/collab");
            UnityConnectServiceCollection.instance.AddService(serviceData);
        }

        public bool IsCollabUIAccessible()
        {
            return true;
        }
    }
}

