// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Modules;
using UnityEditorInternal;

namespace UnityEditorInternal
{
    internal class ProvisioningProfileGUI
    {
        internal delegate void ProvisioningProfileChangedDelegate(ProvisioningProfile profile);

        internal static void ShowProvisioningProfileUIWithProperty(GUIContent titleWithToolTip, ProvisioningProfile profile, SerializedProperty prop)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(titleWithToolTip, EditorStyles.label);

            Rect controlRect = EditorGUILayout.GetControlRect(false, 0);
            GUIContent label = EditorGUIUtility.TextContent("Profile ID:");
            EditorGUI.BeginProperty(controlRect, label, prop);

            if (GUILayout.Button("Browse", EditorStyles.miniButton))
            {
                ProvisioningProfile provisioningProfile = Browse("");
                if (provisioningProfile != null && !string.IsNullOrEmpty(provisioningProfile.UUID))
                {
                    profile = provisioningProfile;
                    prop.stringValue = profile.UUID;
                    prop.serializedObject.ApplyModifiedProperties();
                    GUI.FocusControl("");
                }
                GUIUtility.ExitGUI();
            }

            EditorGUI.EndProperty();
            GUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            EditorGUI.indentLevel++;

            controlRect = EditorGUILayout.GetControlRect(true, 0);
            label = EditorGUIUtility.TextContent("Profile ID:");

            EditorGUI.BeginProperty(controlRect, label, prop);
            profile.UUID = EditorGUILayout.TextField(label, profile.UUID);

            if (EditorGUI.EndChangeCheck())
                prop.stringValue = profile.UUID;

            EditorGUI.EndProperty();
            EditorGUI.indentLevel--;
        }

        internal static void ShowProvisioningProfileUIWithCallback(GUIContent titleWithToolTip, ProvisioningProfile profile, ProvisioningProfileChangedDelegate callback)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(titleWithToolTip, EditorStyles.label);

            if (GUILayout.Button("Browse", EditorStyles.miniButton))
            {
                ProvisioningProfile provisioningProfile = Browse("");
                if (provisioningProfile != null && !string.IsNullOrEmpty(provisioningProfile.UUID))
                {
                    profile = provisioningProfile;
                    callback(profile);

                    GUI.FocusControl("");
                }
                GUIUtility.ExitGUI();
            }

            GUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            EditorGUI.indentLevel++;

            GUIContent label = EditorGUIUtility.TextContent("Profile ID:");
            profile.UUID = EditorGUILayout.TextField(label, profile.UUID);
            EditorGUI.indentLevel--;
            if (EditorGUI.EndChangeCheck())
            {
                callback(profile);
            }
        }

        internal static ProvisioningProfile Browse(string path)
        {
            var msg = "Select the Provising Profile used for Manual Signing";

            var defaultFolder = path;

            if (InternalEditorUtility.inBatchMode)
            {
                return null;
            }

            ProvisioningProfile provisioningProfile = null;
            do
            {
                path = EditorUtility.OpenFilePanel(msg, defaultFolder, "mobileprovision");

                // user pressed cancel?
                if (path.Length == 0)
                    return null;
            }
            while (!GetProvisioningProfileId(path, out provisioningProfile));

            return provisioningProfile;
        }

        internal static bool GetProvisioningProfileId(string filePath, out ProvisioningProfile provisioningProfile)
        {
            ProvisioningProfile profile = ProvisioningProfile.ParseProvisioningProfileAtPath(filePath);
            provisioningProfile = profile;
            return profile.UUID != null;
        }

        internal static void ShowUIWithDefaults(string provisioningPrefKey, SerializedProperty enableAutomaticSigningProp, GUIContent automaticSigningGUI, SerializedProperty manualSigningIDProp, GUIContent manualSigningProfileGUI, SerializedProperty appleDevIDProp, GUIContent teamIDGUIContent)
        {
            int signingValue = GetDefaultAutomaticSigningValue(enableAutomaticSigningProp, iOSEditorPrefKeys.kDefaultiOSAutomaticallySignBuild);
            bool oldValue = GetBoolForAutomaticSigningValue(signingValue);
            Rect toggleRect = EditorGUILayout.GetControlRect(true, 0);
            EditorGUI.BeginProperty(toggleRect, automaticSigningGUI, enableAutomaticSigningProp);
            bool newValue = EditorGUILayout.Toggle(automaticSigningGUI, oldValue);
            if (newValue != oldValue)
            {
                enableAutomaticSigningProp.intValue = GetIntValueForAutomaticSigningBool(newValue);
            }
            EditorGUI.EndProperty();

            if (!newValue)
            {
                ShowProvisioningProfileUIWithDefaults(provisioningPrefKey, manualSigningIDProp, manualSigningProfileGUI);
            }
            else
            {
                string oldTeamID = GetDefaultStringValue(appleDevIDProp, iOSEditorPrefKeys.kDefaultiOSAutomaticSignTeamId);
                string newTeamID = null;
                Rect controlRect = EditorGUILayout.GetControlRect(true, 0);
                EditorGUI.BeginProperty(controlRect, teamIDGUIContent, appleDevIDProp);
                EditorGUI.BeginChangeCheck();
                newTeamID = EditorGUILayout.TextField(teamIDGUIContent, oldTeamID);
                if (EditorGUI.EndChangeCheck())
                {
                    appleDevIDProp.stringValue = newTeamID;
                }
                EditorGUI.EndProperty();
            }
        }

        private static void ShowProvisioningProfileUIWithDefaults(string defaultPreferenceKey, SerializedProperty uuidProp, GUIContent title)
        {
            string val = uuidProp.stringValue;
            if (string.IsNullOrEmpty(val))
            {
                val = EditorPrefs.GetString(defaultPreferenceKey);
            }
            ProvisioningProfileGUI.ShowProvisioningProfileUIWithProperty(title, new ProvisioningProfile(val), uuidProp);
        }

        private static bool GetBoolForAutomaticSigningValue(int signingValue)
        {
            return signingValue == (int)iOSAutomaticallySignValue.AutomaticallySignValueTrue;
        }

        private static int GetIntValueForAutomaticSigningBool(bool automaticallySign)
        {
            return (int)(automaticallySign ? iOSAutomaticallySignValue.AutomaticallySignValueTrue : iOSAutomaticallySignValue.AutomaticallySignValueFalse);
        }

        private static int GetDefaultAutomaticSigningValue(SerializedProperty prop, string editorPropKey)
        {
            int val = prop.intValue;
            if (val == (int)iOSAutomaticallySignValue.AutomaticallySignValueNotSet)
            {
                val = (int)(EditorPrefs.GetBool(editorPropKey, true) ? iOSAutomaticallySignValue.AutomaticallySignValueTrue : iOSAutomaticallySignValue.AutomaticallySignValueFalse);
            }
            return val;
        }

        private static string GetDefaultStringValue(SerializedProperty prop, string editorPrefKey)
        {
            string val = prop.stringValue;
            if (string.IsNullOrEmpty(val))
            {
                val = EditorPrefs.GetString(editorPrefKey, "");
            }
            return val;
        }
    }
}
