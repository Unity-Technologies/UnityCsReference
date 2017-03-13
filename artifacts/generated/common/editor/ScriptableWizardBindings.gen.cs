// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Reflection;

namespace UnityEditor
{


public partial class ScriptableWizard : EditorWindow
{
    
            private GenericInspector m_Inspector;
            private string m_HelpString = "";
            private string m_ErrorString = "";
            private bool m_IsValid = true;
            private Vector2 m_ScrollPosition;
            private string m_CreateButton = "Create";
            private string m_OtherButton = "";
    
    private void OnDestroy()
        {
            DestroyImmediate(m_Inspector);
        }
    
    private void InvokeWizardUpdate()
        {
            const BindingFlags kInstanceInvokeFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
            MethodInfo method = GetType().GetMethod("OnWizardUpdate", kInstanceInvokeFlags);
            if (method != null)
                method.Invoke(this, null);
        }
    
            private class Styles
        {
            public static string errorText = "Wizard Error";
            public static string box = "Wizard Box";
        }
    
    private void OnGUI()
        {
            EditorGUIUtility.labelWidth = 150;
            GUILayout.Label(m_HelpString, EditorStyles.wordWrappedLabel, GUILayout.ExpandHeight(true));

            m_ScrollPosition = EditorGUILayout.BeginVerticalScrollView(m_ScrollPosition, false, GUI.skin.verticalScrollbar, "OL Box");
            GUIUtility.GetControlID(645789, FocusType.Passive);
            bool modified = DrawWizardGUI();
            EditorGUILayout.EndScrollView();

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
        }
    
    protected virtual bool DrawWizardGUI()
        {
            if (m_Inspector == null)
            {
                m_Inspector = ScriptableObject.CreateInstance<GenericInspector>();
                m_Inspector.hideFlags = HideFlags.HideAndDontSave;
                m_Inspector.InternalSetTargets(new Object[] {this});
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
    
    
    [uei.ExcludeFromDocs]
public static ScriptableWizard DisplayWizard (string title, System.Type klass, string createButtonName ) {
    string otherButtonName = "";
    return DisplayWizard ( title, klass, createButtonName, otherButtonName );
}

[uei.ExcludeFromDocs]
public static ScriptableWizard DisplayWizard (string title, System.Type klass) {
    string otherButtonName = "";
    string createButtonName = "Create";
    return DisplayWizard ( title, klass, createButtonName, otherButtonName );
}

public static ScriptableWizard DisplayWizard(string title, System.Type klass, [uei.DefaultValue("\"Create\"")]  string createButtonName , [uei.DefaultValue("\"\"")]  string otherButtonName )
        {
            ScriptableWizard wizard = ScriptableObject.CreateInstance(klass) as ScriptableWizard;
            wizard.m_CreateButton = createButtonName;
            wizard.m_OtherButton = otherButtonName;
            wizard.titleContent = new GUIContent(title);
            if (wizard != null)
            {
                wizard.InvokeWizardUpdate();
                wizard.ShowUtility();
            }
            return wizard;
        }

    
    
    public string helpString
        {
            get { return m_HelpString; }
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
    
    
    public string errorString
        {
            get { return m_ErrorString; }
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
    
    
    public string createButtonName
        {
            get { return m_CreateButton; }
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
    
    
    public string otherButtonName
        {
            get { return m_OtherButton; }
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
    
    
    public bool isValid
        {
            get { return m_IsValid; }
            set { m_IsValid = value; }
        }
    
    
}



} 
