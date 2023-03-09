// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Threading;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Modules;
using UnityEditor.Utils;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Modules
{
    internal class DesktopStandaloneSettingsEditorExtension : DefaultPlayerSettingsEditorExtension
    {
        public struct ServerSettingsBox
        {
            public MethodInfo mi;
            public GUIContent title;
            public int order;

            public ServerSettingsBox(MethodInfo mi, string title, int order)
            {
                this.mi = mi;
                this.title = EditorGUIUtility.TrTextContent(title);
                this.order = order;
            }
        };

        private List<ServerSettingsBox> m_boxes;

        private PlayerSettingsSectionAttribute GetSectionAttribute(MethodInfo mi)
        {
            foreach (var attr in mi.GetCustomAttributes())
            {
                if (attr is PlayerSettingsSectionAttribute)
                    return (PlayerSettingsSectionAttribute)attr;
            }
            return null;
        }

        private bool IsValidSectionSetting(MethodInfo mi)
        {
            if (!mi.IsStatic)
            {
                Debug.LogError($"Method {mi.Name} with attribute PlayerSettingsSection must be static.");
                return false;
            }
            if (mi.IsGenericMethod || mi.IsGenericMethodDefinition)
            {
                Debug.LogError($"Method {mi.Name} with attribute PlayerSettingsSection cannot be generic.");
                return false;
            }
            if (mi.GetParameters().Length != 0)
            {
                Debug.LogError($"Method {mi.Name} with attribute PlayerSettingsSection does not have the correct signature, expected: static void {mi.Name}()");
                return false;
            }
            return true;
        }

        public override void OnEnable(PlayerSettingsEditor pse)
        {
            base.OnEnable(pse);
            m_boxes = new List<ServerSettingsBox>();

            foreach (var method in TypeCache.GetMethodsWithAttribute<PlayerSettingsSectionAttribute>())
            {
                if (IsValidSectionSetting(method))
                {
                    PlayerSettingsSectionAttribute attr = GetSectionAttribute(method);
                    if (attr.TargetName == NamedBuildTarget.Server.TargetName)
                        m_boxes.Add(new ServerSettingsBox(method, attr.Title, attr.Order));
                }        
            }

            m_boxes.Sort((a, b) => a.order.CompareTo(b.order));
        }

        public override bool HasDedicatedServerSections() 
        {
            return m_boxes.Count > 0;
        }

        public override void DedicatedServerSectionsGUI(ref int nextIndex)
        {
            foreach (var box in m_boxes)
            {
                if (m_playerSettingsEditor.BeginSettingsBox(nextIndex, box.title))
                {
                    box.mi.Invoke(null, null);
                }
                m_playerSettingsEditor.EndSettingsBox();
                nextIndex++;
            }
        }
    }
}
