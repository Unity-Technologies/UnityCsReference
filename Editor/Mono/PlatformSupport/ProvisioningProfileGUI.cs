// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Modules;
using UnityEditorInternal;

namespace UnityEditor.PlatformSupport
{
    internal class ProvisioningProfileGUI
    {
        internal delegate void ProvisioningProfileChangedDelegate(ProvisioningProfile profile);

        internal static void ShowProvisioningProfileUIWithProperty(GUIContent titleWithToolTip, ProvisioningProfile profile, SerializedProperty profileIDProp, SerializedProperty profileTypeProp)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(titleWithToolTip, EditorStyles.label);

            Rect controlRect = EditorGUILayout.GetControlRect(false, 0);
            GUIContent labelID = EditorGUIUtility.TrTextContent("Profile ID:");
            EditorGUI.BeginProperty(controlRect, labelID, profileIDProp);

            if (GUILayout.Button("Browse", EditorStyles.miniButton))
            {
                ProvisioningProfile provisioningProfile = Browse("");
                if (provisioningProfile != null && !string.IsNullOrEmpty(provisioningProfile.UUID))
                {
                    profile = provisioningProfile;
                    profileIDProp.stringValue = profile.UUID;
                    profileTypeProp.intValue = (int)profile.type;
                    profileIDProp.serializedObject.ApplyModifiedProperties();
                    profileTypeProp.serializedObject.ApplyModifiedProperties();
                    GUI.FocusControl("");
                }
                GUIUtility.ExitGUI();
            }

            EditorGUI.EndProperty();
            GUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            EditorGUI.indentLevel++;

            EditorGUI.BeginProperty(controlRect, labelID, profileIDProp);
            profile.UUID = EditorGUILayout.TextField(labelID, profile.UUID);
            EditorGUI.EndProperty();

            GUIContent labelType = EditorGUIUtility.TrTextContent("Profile Type:");

            EditorGUI.BeginProperty(controlRect, labelType, profileTypeProp);
            profile.type = (ProvisioningProfileType)EditorGUILayout.EnumPopup(labelType, profile.type);
            EditorGUI.EndProperty();

            if (EditorGUI.EndChangeCheck())
            {
                profileIDProp.stringValue = profile.UUID;
                profileTypeProp.intValue = (int)profile.type;
            }

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

            GUIContent labelID = EditorGUIUtility.TrTextContent("Profile ID:");
            GUIContent labelType = EditorGUIUtility.TrTextContent("Profile Type:");

            profile.UUID = EditorGUILayout.TextField(labelID, profile.UUID);
            profile.type = (ProvisioningProfileType)EditorGUILayout.EnumPopup(labelType, profile.type);

            if (EditorGUI.EndChangeCheck())
            {
                callback(profile);
            }

            EditorGUI.indentLevel--;
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

        internal static void ShowUIWithDefaults(string provisioningPrefUUIDKey, string provisioningPrefTypeKey, SerializedProperty enableAutomaticSigningProp, GUIContent automaticSigningGUI, SerializedProperty manualSigningIDProp, SerializedProperty manualSigningProfileTypeProp, GUIContent manualSigningProfileGUI, SerializedProperty appleDevIDProp, GUIContent teamIDGUIContent)
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
                ShowProvisioningProfileUIWithDefaults(provisioningPrefUUIDKey, provisioningPrefTypeKey, manualSigningIDProp, manualSigningProfileTypeProp, manualSigningProfileGUI);
            }
        }

        private static void ShowProvisioningProfileUIWithDefaults(string defaultPreferenceKey, string provisioningPrefTypeKey, SerializedProperty uuidProp, SerializedProperty typeProp, GUIContent title)
        {
            string uuidVal = uuidProp.stringValue;
            ProvisioningProfileType typeVal = (ProvisioningProfileType)typeProp.intValue;
            if (string.IsNullOrEmpty(uuidVal))
            {
                uuidVal = EditorPrefs.GetString(defaultPreferenceKey);
                typeVal = string.IsNullOrEmpty(uuidVal) ? typeVal : (ProvisioningProfileType)EditorPrefs.GetInt(provisioningPrefTypeKey);
            }

            ProvisioningProfileGUI.ShowProvisioningProfileUIWithProperty(title, new ProvisioningProfile(uuidVal, typeVal), uuidProp, typeProp);
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
