// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine;
using uei = UnityEngine.Internal;

namespace UnityEditor
{
    // Derive from this class to create an editor wizard.
    [UIFramework(UIFrameworkUsage.IMGUI)]
    public class ScriptableWizard : EditorWindow
    {
        GenericInspector m_Inspector;
        string m_HelpString = "";
        string m_ErrorString = "";
        bool m_IsValid = true;
        Vector2 m_ScrollPosition;
        string m_CreateButton = "Create";
        string m_OtherButton = "";

        void OnDestroy()
        {
            DestroyImmediate(m_Inspector);
        }

        void InvokeWizardUpdate()
        {
            const BindingFlags kInstanceInvokeFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
            var method = GetType().GetMethod("OnWizardUpdate", kInstanceInvokeFlags);
            if (method != null)
                method.Invoke(this, null);
        }

        static class Styles
        {
            public const string errorText = "Wizard Error";
        }

        //@TODO: Force repaint if scripts recompile
        void OnGUI()
        {
            EditorGUIUtility.labelWidth = 150;
            GUILayout.Label(m_HelpString, EditorStyles.wordWrappedLabel, GUILayout.ExpandHeight(true));

            // Render contents using Generic Inspector GUI
            m_ScrollPosition = EditorGUILayout.BeginVerticalScrollView(m_ScrollPosition, false, GUI.skin.verticalScrollbar, "OL Box");
            GUIUtility.GetControlID(645789, FocusType.Passive);
            bool modified = DrawWizardGUI();
            EditorGUILayout.EndScrollView();

            // Create and Other Buttons
            GUILayout.BeginVertical();
            if (m_ErrorString != string.Empty)
                GUILayout.Label(m_ErrorString, Styles.errorText, GUILayout.MinHeight(32));
            else
                GUILayout.Label(string.Empty, GUILayout.MinHeight(32));
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.enabled = m_IsValid;

            const BindingFlags kInstanceInvokeFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
            if (m_OtherButton != "" && GUILayout.Button(m_OtherButton, GUILayout.MinWidth(100)))
            {
                MethodInfo method = GetType().GetMethod("OnWizardOtherButton", kInstanceInvokeFlags);
                if (method != null)
                {
                    method.Invoke(this, null);
                    GUIUtility.ExitGUI();
                }
                else
                    Debug.LogError("OnWizardOtherButton has not been implemented in script");
            }

            if (m_CreateButton != "" && GUILayout.Button(m_CreateButton, GUILayout.MinWidth(100)))
            {
                MethodInfo method = GetType().GetMethod("OnWizardCreate", kInstanceInvokeFlags);
                if (method != null)
                    method.Invoke(this, null);
                else
                    Debug.LogError("OnWizardCreate has not been implemented in script");
                Close();
                GUIUtility.ExitGUI();
            }
            GUI.enabled = true;

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            if (modified)
                InvokeWizardUpdate();

            GUILayout.Space(8);
        }

        protected virtual bool DrawWizardGUI()
        {
            if (m_Inspector == null)
            {
                m_Inspector = CreateInstance<GenericInspector>();
                m_Inspector.hideFlags = HideFlags.HideAndDontSave;
                m_Inspector.InternalSetTargets(new UnityEngine.Object[] {this});
            }
            return m_Inspector.DrawDefaultInspector();
        }

        public static T DisplayWizard<T>(string title) where T : ScriptableWizard
        {
            return DisplayWizard<T>(title, "Create", "");
        }

        public static T DisplayWizard<T>(string title, string createButtonName) where T : ScriptableWizard
        {
            return DisplayWizard<T>(title, createButtonName, "");
        }

        public static T DisplayWizard<T>(string title, string createButtonName, string otherButtonName) where T : ScriptableWizard
        {
            return (T)DisplayWizard(title, typeof(T), createButtonName, otherButtonName);
        }

        [uei.ExcludeFromDocsAttribute]
        public static ScriptableWizard DisplayWizard(string title, Type klass, string createButtonName)
        {
            return DisplayWizard(title, klass, createButtonName, "");
        }

        [uei.ExcludeFromDocsAttribute]
        public static ScriptableWizard DisplayWizard(string title, Type klass)
        {
            return DisplayWizard(title, klass, "Create", "");
        }

        public static ScriptableWizard DisplayWizard(string title, Type klass, [uei.DefaultValueAttribute("\"Create\"")]  string createButtonName , [uei.DefaultValueAttribute("\"\"")]  string otherButtonName)
        {
            var wizard = CreateInstance(klass) as ScriptableWizard;
            if (wizard == null)
                return null;
            wizard.m_CreateButton = createButtonName;
            wizard.m_OtherButton = otherButtonName;
            wizard.titleContent = new GUIContent(title);
            wizard.InvokeWizardUpdate();
            wizard.ShowUtility();
            return wizard;
        }

        // // Magic Methods

        // // This is called when the wizard is opened or whenever the user changes something in the wizard.
        // void OnWizardUpdate();

        // // This is called when the user clicks on the Create button.
        // void OnWizardCreate();

        // Allows you to set the help text of the wizard.
        public string helpString
        {
            get => m_HelpString;
            set
            {
                var newString = value ?? string.Empty;
                if (m_HelpString != newString)
                {
                    m_HelpString = newString;
                    Repaint();
                }
            }
        }

        // Allows you to set the error text of the wizard.
        public string errorString
        {
            get => m_ErrorString;
            set
            {
                var newString = value ?? string.Empty;
                if (m_ErrorString != newString)
                {
                    m_ErrorString = newString;
                    Repaint();
                }
            }
        }

        // Allows you to set the create button text of the wizard.
        public string createButtonName
        {
            get => m_CreateButton;
            set
            {
                var newString = value ?? string.Empty;
                if (m_CreateButton != newString)
                {
                    m_CreateButton = newString;
                    Repaint();
                }
            }
        }

        // Allows you to set the other button text of the wizard.
        public string otherButtonName
        {
            get => m_OtherButton;
            set
            {
                var newString = value ?? string.Empty;
                if (m_OtherButton != newString)
                {
                    m_OtherButton = newString;
                    Repaint();
                }
            }
        }

        // Allows you to enable and disable the wizard create button, so that the user can not click it.
        public bool isValid
        {
            get => m_IsValid;
            set => m_IsValid = value;
        }
    }
}
