// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Linq;

namespace UnityEditor
{


public partial class CustomEditor : System.Attribute
{
    public CustomEditor(System.Type inspectedType)
        {
            if (inspectedType == null)
                Debug.LogError("Failed to load CustomEditor inspected type");
            m_InspectedType = inspectedType;
            m_EditorForChildClasses = false;
        }
    
    
    public CustomEditor(System.Type inspectedType, bool editorForChildClasses)
        {
            if (inspectedType == null)
                Debug.LogError("Failed to load CustomEditor inspected type");
            m_InspectedType = inspectedType;
            m_EditorForChildClasses = editorForChildClasses;
        }
    
            internal Type m_InspectedType;
            internal bool m_EditorForChildClasses;
    
            public bool isFallback { get; set; }
}

public sealed partial class CanEditMultipleObjects : System.Attribute
{
}

[StructLayout(LayoutKind.Sequential)]
[RequiredByNativeCode]
public partial class Editor : ScriptableObject
{
    Object[] m_Targets;
    
    
    Object m_Context;
    
    
    int    m_IsDirty;
    
    
    private int m_ReferenceTargetIndex = 0;
    
    
    private PropertyHandlerCache m_PropertyHandlerCache = new PropertyHandlerCache();
    
    
    private IPreviewable m_DummyPreview;
    
            internal SerializedObject m_SerializedObject = null;
            OptimizedGUIBlock m_OptimizedBlock;
            internal InspectorMode m_InspectorMode = InspectorMode.Normal;
            internal const float kLineHeight = 16;
    
    
    internal bool hideInspector = false;
    
    
    internal bool canEditMultipleObjects
        {
            get { return GetType().GetCustomAttributes(typeof(CanEditMultipleObjects), false).Length > 0; }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Editor CreateEditorWithContext (Object[] targetObjects, Object context, [uei.DefaultValue("null")]  Type editorType ) ;

    [uei.ExcludeFromDocs]
    public static Editor CreateEditorWithContext (Object[] targetObjects, Object context) {
        Type editorType = null;
        return CreateEditorWithContext ( targetObjects, context, editorType );
    }

    public static void CreateCachedEditorWithContext(Object targetObject, Object context, Type editorType, ref Editor previousEditor)
        {
            CreateCachedEditorWithContext(new Object[] {targetObject}, context, editorType, ref previousEditor);
        }
    
    
    public static void CreateCachedEditorWithContext(Object[] targetObjects, Object context, Type editorType, ref Editor previousEditor)
        {
            if (previousEditor != null && ArrayUtility.ArrayEquals(previousEditor.m_Targets, targetObjects) && previousEditor.m_Context == context)
                return;

            if (previousEditor != null)
                DestroyImmediate(previousEditor);
            previousEditor = CreateEditorWithContext(targetObjects, context, editorType);
        }
    
    
    public static void CreateCachedEditor(Object targetObject, Type editorType, ref Editor previousEditor)
        {
            CreateCachedEditorWithContext(new Object[] {targetObject}, null, editorType, ref previousEditor);
        }
    
    
    public static void CreateCachedEditor(Object[] targetObjects, Type editorType, ref Editor previousEditor)
        {
            CreateCachedEditorWithContext(targetObjects, null, editorType, ref previousEditor);
        }
    
    
    [uei.ExcludeFromDocs]
public static Editor CreateEditor (Object targetObject) {
    Type editorType = null;
    return CreateEditor ( targetObject, editorType );
}

public static Editor CreateEditor(Object targetObject, [uei.DefaultValue("null")]  Type editorType )
        {
            return CreateEditorWithContext(new Object[] {targetObject}, null, editorType);
        }

    
    
    [uei.ExcludeFromDocs]
public static Editor CreateEditor (Object[] targetObjects) {
    Type editorType = null;
    return CreateEditor ( targetObjects, editorType );
}

public static Editor CreateEditor(Object[] targetObjects, [uei.DefaultValue("null")]  Type editorType )
        {
            return CreateEditorWithContext(targetObjects, null, editorType);
        }

    
    
    public Object target { get { return m_Targets[referenceTargetIndex]; } set { throw new InvalidOperationException("You can't set the target on an editor."); } }
    
    
    public Object[] targets
        {
            get
            {
                if (!m_AllowMultiObjectAccess)
                    Debug.LogError("The targets array should not be used inside OnSceneGUI or OnPreviewGUI. Use the single target property instead.");
                return m_Targets;
            }
        }
    internal virtual int referenceTargetIndex
        {
            get { return Mathf.Clamp(m_ReferenceTargetIndex, 0, m_Targets.Length - 1); }
            set { m_ReferenceTargetIndex = (Math.Abs(value * m_Targets.Length) + value) % m_Targets.Length; }
        }
    
    
    internal virtual string targetTitle
        {
            get
            {
                if (m_Targets.Length == 1 || !m_AllowMultiObjectAccess)
                    return target.name;
                else
                    return m_Targets.Length + " " + ObjectNames.NicifyVariableName(ObjectNames.GetTypeName(target)) + "s";
            }
        }
    
    
    public SerializedObject serializedObject
        {
            get
            {
                if (!m_AllowMultiObjectAccess)
                    Debug.LogError("The serializedObject should not be used inside OnSceneGUI or OnPreviewGUI. Use the target property directly instead.");
                return GetSerializedObjectInternal();
            }
        }
    
    
    internal virtual SerializedObject GetSerializedObjectInternal()
        {
            if (m_SerializedObject == null)
                m_SerializedObject = new SerializedObject(targets, m_Context);

            return m_SerializedObject;
        }
    
    private void CleanupPropertyEditor()
        {
            if (m_OptimizedBlock != null)
            {
                m_OptimizedBlock.Dispose();
                m_OptimizedBlock = null;
            }
            if (m_SerializedObject != null)
            {
                m_SerializedObject.Dispose();
                m_SerializedObject = null;
            }
        }
    
    private void OnDisableINTERNAL()
        {
            CleanupPropertyEditor();
        }
    
    internal virtual void OnForceReloadInspector()
        {
            if (m_SerializedObject != null)
                m_SerializedObject.SetIsDifferentCacheDirty();
        }
    
    internal bool GetOptimizedGUIBlockImplementation(bool isDirty, bool isVisible, out OptimizedGUIBlock block, out float height)
        {
            if (isDirty && m_OptimizedBlock != null)
            {
                m_OptimizedBlock.Dispose();
                m_OptimizedBlock = null;
            }

            if (!isVisible)
            {
                if (m_OptimizedBlock == null)
                    m_OptimizedBlock = new OptimizedGUIBlock();
                block = m_OptimizedBlock;
                height = 0;
                return true;
            }

            if (m_SerializedObject == null)
                m_SerializedObject = new SerializedObject(targets, m_Context);
            else
                m_SerializedObject.Update();
            m_SerializedObject.inspectorMode = m_InspectorMode;

            SerializedProperty property = m_SerializedObject.GetIterator();
            height = EditorGUI.kControlVerticalSpacing;
            bool expand = true;
            while (property.NextVisible(expand))
            {
                if (!EditorGUI.CanCacheInspectorGUI(property))
                {
                    if (m_OptimizedBlock != null)
                        m_OptimizedBlock.Dispose();
                    block = m_OptimizedBlock = null;
                    return false;
                }

                height += EditorGUI.GetPropertyHeight(property, null, true) + EditorGUI.kControlVerticalSpacing;
                expand = false;
            }

            if (height == EditorGUI.kControlVerticalSpacing)
                height = 0;

            if (m_OptimizedBlock == null)
                m_OptimizedBlock = new OptimizedGUIBlock();
            block = m_OptimizedBlock;
            return true;
        }
    
    internal bool OptimizedInspectorGUIImplementation(Rect contentRect)
        {
            SerializedProperty property = m_SerializedObject.GetIterator();

            bool childrenAreExpanded = true;

            bool wasEnabled = GUI.enabled;
            contentRect.xMin += InspectorWindow.kInspectorPaddingLeft;
            contentRect.xMax -= InspectorWindow.kInspectorPaddingRight;
            contentRect.y += EditorGUI.kControlVerticalSpacing;

            while (property.NextVisible(childrenAreExpanded))
            {
                contentRect.height = EditorGUI.GetPropertyHeight(property, null, false);
                EditorGUI.indentLevel = property.depth;
                using (new EditorGUI.DisabledScope(m_InspectorMode == InspectorMode.Normal && "m_Script" == property.propertyPath))
                {
                    childrenAreExpanded = EditorGUI.PropertyField(contentRect, property);
                }
                contentRect.y += contentRect.height + EditorGUI.kControlVerticalSpacing;
            }
            GUI.enabled = wasEnabled;

            bool valuesChanged = m_SerializedObject.ApplyModifiedProperties();

            return valuesChanged;
        }
    
    protected internal static void DrawPropertiesExcluding(SerializedObject obj, params string[] propertyToExclude)
        {
            SerializedProperty property = obj.GetIterator();
            bool expanded = true;
            while (property.NextVisible(expanded))
            {
                expanded = false;

                if (propertyToExclude.Contains(property.name))
                    continue;

                EditorGUILayout.PropertyField(property, true);
            }
        }
    
    
    public bool DrawDefaultInspector()
        {
            return DoDrawDefaultInspector();
        }
    
    public virtual void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }
    
    public virtual bool RequiresConstantRepaint()
        {
            return false;
        }
    
    
    internal void InternalSetTargets(Object[] t) { m_Targets = t; }
    internal void InternalSetHidden(bool hidden) { hideInspector = hidden; }
    internal void InternalSetContextObject(Object context) { m_Context = context; }
    
    
    internal virtual bool GetOptimizedGUIBlock(bool isDirty, bool isVisible, out OptimizedGUIBlock block, out float height) { block = null; height = -1; return false; }
    internal virtual bool OnOptimizedInspectorGUI(Rect contentRect) { Debug.LogError("Not supported"); return false; }
    internal bool isInspectorDirty { get { return m_IsDirty != 0; } set { m_IsDirty = value ? 1 : 0; } }
    
    
    
    public void Repaint() { InspectorWindow.RepaintAllInspectors(); }
    
    
    public virtual bool HasPreviewGUI()
        {
            return preview.HasPreviewGUI();
        }
    
    
    public virtual GUIContent GetPreviewTitle()
        {
            return preview.GetPreviewTitle();
        }
    
    
    public virtual Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            return null;
        }
    
    
    public virtual void OnPreviewGUI(Rect r, GUIStyle background)
        {
            preview.OnPreviewGUI(r, background);
        }
    
    
    public virtual void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            OnPreviewGUI(r, background);
        }
    
    
    public virtual void OnPreviewSettings()
        {
            preview.OnPreviewSettings();
        }
    
    
    public virtual string GetInfoString()
        {
            return preview.GetInfoString();
        }
    
    
    internal static bool m_AllowMultiObjectAccess = true;
    
    
    internal virtual void OnAssetStoreInspectorGUI()
        {
        }
    
    
    public virtual void ReloadPreviewInstances()
        {
            preview.ReloadPreviewInstances();
        }
    
    
}

}
