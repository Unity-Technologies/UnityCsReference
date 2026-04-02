// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.EditorTools
{
    // This serves as the default tool setting implementation.
    [CustomEditor(typeof(EditorToolContext), true)]
    class GameObjectToolContextCustomEditor : Editor, ICreateToolbar
    {
        string[] k_ToolbarIds = Array.Empty<string>();

        public IEnumerable<string> toolbarElements => k_ToolbarIds;

        public EditorWindow containerWindow { get; set; }

        public override VisualElement CreateInspectorGUI()
        {
            return new EditorToolbar(toolbarElements, containerWindow).rootVisualElement;
        }
    }

    public abstract class EditorToolContext : ScriptableObject, IEditor
    {
        bool m_Active;

        [HideInInspector]
        [SerializeField]
        internal UnityObject[] m_Targets;

        [HideInInspector]
        [SerializeField]
        internal UnityObject m_Target;

        public IEnumerable<UnityObject> targets => targetList;

        internal IReadOnlyList<UnityObject> targetList => m_Targets != null && m_Targets.Length > 0
            ? m_Targets
            : Selection.objects;

        public UnityObject target => m_Target == null ? Selection.activeObject : m_Target;

        public virtual bool overridesDefaultSelection
        {
            get { return false; }
        }

        [SerializeField]
        string m_ContextOwnerTypeName;

        Type m_ContextOwnerType;
        Type contextOwnerType
        {
            get
            {
                if (m_ContextOwnerType == null && !String.IsNullOrEmpty(m_ContextOwnerTypeName))
                    m_ContextOwnerType = Type.GetType(m_ContextOwnerTypeName);
                
                if (m_ContextOwnerType == null)
                    m_ContextOwnerType = typeof(SceneView);

                return m_ContextOwnerType;
            }
        }
        
        internal void Activate()
        {
            if(m_Active
            // Prevent to reenable the context if this is not the active one anymore
            // Can happen when entering playmode due to the delayCall in EditorToolManager.OnEnable
                || this != EditorToolManager.GetActiveToolContext(contextOwnerType))
                return;

            OnActivated();
            m_Active = true;
        }

        internal void Deactivate()
        {
            if(!m_Active)
                return;

            OnWillBeDeactivated();
            m_Active = false;
        }

        public virtual void OnActivated() {}

        public virtual void OnWillBeDeactivated() {}

        public virtual void PopulateMenu(DropdownMenu menu) {}

        void IEditor.SetTarget(UnityObject value) => m_Target = value;

        void IEditor.SetTargets(UnityObject[] value) => m_Targets = value;

        internal void SetContextOwner(Type contextOwnerType) => m_ContextOwnerTypeName = contextOwnerType.AssemblyQualifiedName;

        public virtual void OnToolGUI(EditorWindow window) {}

        public Type ResolveTool(Tool tool)
        {
            switch (tool)
            {
                case Tool.None:
                    return typeof(NoneTool);

                case Tool.View:
                    var toolOwnerIsNullOrSceneView = contextOwnerType == null || contextOwnerType == typeof(SceneView);
                    // Do not allow overriding ViewTool if context owner is SceneView
                    if (toolOwnerIsNullOrSceneView) 
                        return typeof(ViewModeTool);
                 
                    // Try resolving for custom owner
                    return DoResolveTool(tool);
                
                case Tool.Custom:
                    return null;

                default:
                    return DoResolveTool(tool);
            }
        }
        
        Type DoResolveTool(Tool tool)
        {
            var resolved = GetEditorToolType(tool);

            // Returning null is valid here, but types that do not inherit EditorTool or are abstract are not.
            if (resolved != null && (!typeof(EditorTool).IsAssignableFrom(resolved) || resolved.IsAbstract))
                Debug.LogError($"Tool context \"{GetType()}\" resolved {tool} to an invalid EditorTool type. " +
                               $"Resolved types must inherit EditorTool and not be abstract.");
            else
                return resolved;
            return null;
        }

        protected virtual Type GetEditorToolType(Tool tool)
        {
            const string k_ExceptionMsg = "EditorToolContext should only be used to resolve transform tools. " +
                "View, Custom, and None are not applicable.";
            switch (tool)
            {
                case Tool.View:
                    if (contextOwnerType == null || contextOwnerType == typeof(SceneView))
                        throw new ArgumentException(k_ExceptionMsg);
                    return typeof(ViewModeTool);
                case Tool.Move:
                    return typeof(MoveTool);
                case Tool.Rotate:
                    return typeof(RotateTool);
                case Tool.Scale:
                    return typeof(ScaleTool);
                case Tool.Rect:
                    return typeof(RectTool);
                case Tool.Transform:
                    return typeof(TransformTool);
                default:
                    throw new ArgumentException(k_ExceptionMsg);
            }
        }

        public virtual IEnumerable<Type> GetAdditionalToolTypes() => Array.Empty<Type>();
    }
}
