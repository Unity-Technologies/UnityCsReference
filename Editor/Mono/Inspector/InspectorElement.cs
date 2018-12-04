// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    public class InspectorElement : BindableElement
    {
        public static readonly string ussClassName = "unity-inspector-element";
        public static readonly string customInspectorUssClassName = ussClassName + "__custom-inspector-container";
        public static readonly string iMGUIContainerUssClassName = ussClassName + "__imgui-container";

        public static readonly string iMGUIInspectorVariantUssClassName = ussClassName + "--imgui";
        public static readonly string uIEInspectorVariantUssClassName = ussClassName + "--uie";

        public static readonly string noInspectorFoundVariantUssClassName = ussClassName + "--no-inspector-found";
        public static readonly string uIECustomVariantUssClassName = ussClassName + "--uie-custom";
        public static readonly string iMGUICustomVariantUssClassName = ussClassName + "--imgui-custom";
        public static readonly string iMGUIDefaultVariantUssClassName = ussClassName + "--imgui-default";
        public static readonly string uIEDefaultVariantUssClassName = ussClassName + "--uie-default";
        public static readonly string debugVariantUssClassName = ussClassName + "--debug";
        public static readonly string debugInternalVariantUssClassName = ussClassName + "--debug-internal";

        public new class UxmlFactory : UxmlFactory<InspectorElement, UxmlTraits> {}

        [Flags]
        internal enum Mode
        {
            UIECustom = 1 << 0,
            IMGUICustom = 1 << 1,
            IMGUIDefault = 1 << 2,
            UIEDefault = 1 << 3,

            DebugMod = 1 << 4,
            DebugInternalMod = 1 << 5,

            Normal = UIECustom | IMGUICustom | IMGUIDefault | UIEDefault,
            Default = IMGUIDefault | UIEDefault,
            Custom = UIECustom | IMGUICustom,
            IMGUI = IMGUICustom | IMGUIDefault,
            UIE = UIECustom | UIEDefault,

            Debug = Default | DebugMod,
            DebugInternal = Default | DebugInternalMod
        }

        internal Mode mode { get; private set; }

        public InspectorElement() : this(null as Object) {}

        public InspectorElement(Object obj) : this(obj, Mode.Normal) {}

        internal InspectorElement(Object obj, Mode mode)
        {
            AddToClassList(ussClassName);

            this.mode = mode;
            if (obj == null)
            {
                if (!GenericInspector.ObjectIsMonoBehaviourOrScriptableObject(obj))
                {
                    return;
                }
            }

            this.Bind(new SerializedObject(obj));
        }

        public InspectorElement(SerializedObject obj) : this(obj, Mode.Normal) {}

        internal InspectorElement(SerializedObject obj, Mode mode)
        {
            AddToClassList(ussClassName);

            this.mode = mode;
            if (obj.targetObject == null)
            {
                if (!GenericInspector.ObjectIsMonoBehaviourOrScriptableObject(obj.targetObject))
                {
                    return;
                }
            }

            this.Bind(obj);
        }

        public InspectorElement(Editor editor) : this(editor, Mode.Normal) {}

        internal InspectorElement(Editor editor, Mode mode)
        {
            AddToClassList(ussClassName);

            this.mode = mode;

            if (editor.targets.Length == 0)
            {
                return;
            }

            var targetObject = editor.targets[0];
            if (targetObject == null)
            {
                if (!GenericInspector.ObjectIsMonoBehaviourOrScriptableObject(targetObject))
                {
                    return;
                }
            }

            this.Bind(editor.serializedObject);
        }

        private void Reset(SerializedObjectBindEvent evt)
        {
            Clear();

            RemoveFromClassList(iMGUIInspectorVariantUssClassName);
            RemoveFromClassList(uIEInspectorVariantUssClassName);
            RemoveFromClassList(noInspectorFoundVariantUssClassName);
            RemoveFromClassList(uIECustomVariantUssClassName);
            RemoveFromClassList(iMGUICustomVariantUssClassName);
            RemoveFromClassList(iMGUIDefaultVariantUssClassName);
            RemoveFromClassList(uIEDefaultVariantUssClassName);
            RemoveFromClassList(debugVariantUssClassName);
            RemoveFromClassList(debugInternalVariantUssClassName);

            var bindObject = evt.bindObject;
            if (bindObject == null)
                return;

            var editor = GetOrCreateEditor(bindObject);
            if (editor == null)
            {
                return;
            }

            var customInspector = CreateInspectorElementFromEditor(editor);
            if (customInspector == null)
            {
                customInspector = CreateDefaultInspector(bindObject);
            }

            if (customInspector != null && customInspector != this)
                hierarchy.Add(customInspector);
        }

        protected internal override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            var bindEvent = evt as SerializedObjectBindEvent;
            if (bindEvent == null)
                return;

            Reset(bindEvent);
        }

        private Editor GetOrCreateEditor(SerializedObject serializedObject)
        {
            var target = serializedObject?.targetObject;
            var activeEditors = ActiveEditorTracker.sharedTracker?.activeEditors;
            var editor =  activeEditors?.FirstOrDefault((e) => e.target == target || e.serializedObject == serializedObject);

            if (editor == null)
            {
                editor = Editor.CreateEditor(target);
                // TODO - When we create an editor here, we never destroy it. Need to store a reference to the editor and call DestroyImmediate on it when the InspectorElement is being destroyed or unparented.
            }

            return editor;
        }

        private VisualElement CreateDefaultInspector(SerializedObject serializedObject)
        {
            if (serializedObject == null)
                return null;

            SerializedProperty property = serializedObject.GetIterator();
            if (property.NextVisible(true)) // Expand first child.
            {
                do
                {
                    var field = new PropertyField(property);
                    field.name = "PropertyField:" + property.propertyPath;
                    hierarchy.Add(field);
                }
                while (property.NextVisible(false));
            }

            if (serializedObject.targetObject == null)
            {
                AddMissingScriptLabel(serializedObject);
            }

            AddToClassList(uIEDefaultVariantUssClassName);
            AddToClassList(uIEInspectorVariantUssClassName);

            return this;
        }

        bool AddMissingScriptLabel(SerializedObject serializedObject)
        {
            SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
            if (scriptProperty != null)
            {
                hierarchy.Add(new IMGUIContainer(() => GenericInspector.CheckIfScriptLoaded(scriptProperty)));
                return true;
            }

            return false;
        }

        private VisualElement CreateIMGUIInspectorFromEditor(SerializedObject serializedObject, Editor editor)
        {
            if ((mode & (Mode.IMGUICustom | Mode.IMGUIDefault)) == 0)
                return null;

            if ((mode & Mode.IMGUICustom) > 0 && (mode & Mode.IMGUIDefault) == 0 && editor is GenericInspector)
                return null;

            if ((mode & Mode.IMGUICustom) == 0 && (mode & Mode.IMGUIDefault) > 0 && !(editor is GenericInspector))
            {
                editor = ScriptableObject.CreateInstance<GenericInspector>();
                editor.hideFlags = HideFlags.HideAndDontSave;
                editor.InternalSetTargets(new[] { serializedObject.targetObject });
            }

            if (editor is GenericInspector)
            {
                AddToClassList(iMGUIDefaultVariantUssClassName);
                if ((mode & Mode.DebugMod) > 0)
                {
                    AddToClassList(debugVariantUssClassName);
                    editor.m_InspectorMode = InspectorMode.Debug;
                }
                else if ((mode & Mode.DebugInternalMod) > 0)
                {
                    AddToClassList(debugInternalVariantUssClassName);
                    editor.m_InspectorMode = InspectorMode.DebugInternal;
                }
            }
            else
            {
                AddToClassList(iMGUICustomVariantUssClassName);
            }

            IMGUIContainer inspector = null;
            inspector = new IMGUIContainer(() =>
            {
                if (!editor.serializedObject.isValid)
                {
                    return;
                }

                EditorGUIUtility.ResetGUIState();
                using (new EditorGUI.DisabledScope(!editor.IsEnabled()))
                {
                    var genericEditor = editor as GenericInspector;
                    if (genericEditor != null)
                    {
                        switch (mode)
                        {
                            case Mode.Normal:
                                genericEditor.m_InspectorMode = InspectorMode.Normal;
                                break;
                            case Mode.Default:
                                genericEditor.m_InspectorMode = InspectorMode.Debug;
                                break;
                            case Mode.Custom:
                                genericEditor.m_InspectorMode = InspectorMode.DebugInternal;
                                break;
                            case Mode.IMGUI:
                                break;
                        }
                    }

                    //set the current PropertyHandlerCache to the current editor
                    ScriptAttributeUtility.propertyHandlerCache = editor.propertyHandlerCache;

                    var originalWideMode = EditorGUIUtility.wideMode;

                    EditorGUIUtility.hierarchyMode = true;
                    var inspectorWidth = inspector.layout.width;
                    // the inspector's width can be NaN if this is our first layout check.
                    // If that's the case we'll set wideMode to true to avoid computing too tall an inspector on the first layout calculation
                    if (!float.IsNaN(inspectorWidth))
                    {
                        EditorGUIUtility.wideMode = inspectorWidth > Editor.k_WideModeMinWidth;
                    }
                    else
                    {
                        EditorGUIUtility.wideMode = true;
                    }

                    GUIStyle editorWrapper = (editor.UseDefaultMargins() ? EditorStyles.inspectorDefaultMargins : GUIStyle.none);
                    try
                    {
                        GUI.changed = false;

                        using (new InspectorWindowUtils.LayoutGroupChecker())
                        {
                            EditorGUILayout.BeginVertical(editorWrapper);
                            {
                                try
                                {
                                    var rebuildOptimizedGUIBlocks = GetRebuildOptimizedGUIBlocks(editor.target);
                                    rebuildOptimizedGUIBlocks |= editor.isInspectorDirty;
                                    float height;
                                    if (editor.GetOptimizedGUIBlock(rebuildOptimizedGUIBlocks, visible, out height))
                                    {
                                        var contentRect = GUILayoutUtility.GetRect(0, visible ? height : 0);

                                        // Layout events are ignored in the optimized code path
                                        // The exception is when we are drawing a GenericInspector, they always use the optimized path and must therefore run at least one layout calculation in it
                                        if (Event.current.type == EventType.Layout && !(editor is GenericInspector))
                                        {
                                            return;
                                        }

                                        InspectorWindowUtils.DrawAddedComponentBackground(contentRect, editor.targets);

                                        // Draw content
                                        if (visible)
                                        {
                                            GUI.changed = false;
                                            editor.OnOptimizedInspectorGUI(contentRect);
                                        }
                                    }
                                    else
                                    {
                                        InspectorWindowUtils.DrawAddedComponentBackground(contentRect, editor.targets);
                                        editor.OnInspectorGUI();
                                    }
                                }
                                catch (Exception e)
                                {
                                    if (GUIUtility.ShouldRethrowException(e))
                                    {
                                        throw;
                                    }

                                    Debug.LogException(e);
                                }
                            }
                            EditorGUILayout.EndVertical();
                        }
                    }
                    finally
                    {
                        EditorGUIUtility.wideMode = originalWideMode;
                    }
                }
            });

            inspector.style.overflow = Overflow.Visible;

            if (!(editor is GenericInspector))
                inspector.AddToClassList(customInspectorUssClassName);

            inspector.AddToClassList(iMGUIContainerUssClassName);

            AddToClassList(iMGUIInspectorVariantUssClassName);

            return inspector;
        }

        private VisualElement CreateInspectorElementFromEditor(Editor editor)
        {
            var serializedObject = editor.serializedObject;
            var target = editor.targets[0];
            if (target == null)
            {
                if (!GenericInspector.ObjectIsMonoBehaviourOrScriptableObject(target))
                {
                    return null;
                }
            }

            VisualElement inspectorElement = null;

            if ((mode & Mode.UIECustom) > 0)
            {
                inspectorElement = editor.CreateInspectorGUI();
                if (inspectorElement != null)
                {
                    AddToClassList(uIECustomVariantUssClassName);
                    AddToClassList(uIEInspectorVariantUssClassName);
                    inspectorElement.AddToClassList(customInspectorUssClassName);
                }
            }

            if (inspectorElement == null)
                inspectorElement = CreateIMGUIInspectorFromEditor(serializedObject, editor);

            if (inspectorElement == null && (mode & Mode.UIEDefault) > 0)
                inspectorElement = CreateDefaultInspector(serializedObject);

            if (inspectorElement == null)
            {
                AddToClassList(noInspectorFoundVariantUssClassName);
                AddToClassList(uIEInspectorVariantUssClassName);
                inspectorElement = new Label("No inspector found given the current Inspector.Mode.");
            }

            return inspectorElement;
        }

        bool m_IsOpenForEdit;
        bool m_InvalidateGUIBlockCache = true;

        private bool GetRebuildOptimizedGUIBlocks(Object inspectedObject)
        {
            var rebuildOptimizedGUIBlocks = false;

            if (Event.current.type == EventType.Repaint)
            {
                string msg;
                if (inspectedObject != null
                    && m_IsOpenForEdit != Editor.IsAppropriateFileOpenForEdit(inspectedObject, out msg))
                {
                    m_IsOpenForEdit = !m_IsOpenForEdit;
                    rebuildOptimizedGUIBlocks = true;
                }

                if (m_InvalidateGUIBlockCache)
                {
                    rebuildOptimizedGUIBlocks = true;
                    m_InvalidateGUIBlockCache = false;
                }
            }
            else if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == EventCommandNames.EyeDropperUpdate)
            {
                rebuildOptimizedGUIBlocks = true;
            }

            return rebuildOptimizedGUIBlocks;
        }
    }
}
