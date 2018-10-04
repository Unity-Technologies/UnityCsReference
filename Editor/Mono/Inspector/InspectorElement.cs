// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.UIElements
{
    public class InspectorElement : BindableElement
    {
        private static readonly string s_InspectorClassName = "unity-inspector-element";
        private static readonly string s_CustomInspectorClassName = "unity-inspector-element__custom-inspector-container";
        private static readonly string s_IMGUIContainerClassName = "unity-inspector-element__imgui-container";

        private static readonly string s_IMGUIInspectorClassName = "unity-inspector-element--imgui";
        private static readonly string s_UIEInspectorClassName = "unity-inspector-element--uie";

        private static readonly string s_NoInspectorFoundClassName = "unity-inspector-element--no-inspector-found";
        private static readonly string s_UIECustomFoundClassName = "unity-inspector-element--uie-custom";
        private static readonly string s_IMGUICustomFoundClassName = "unity-inspector-element--imgui-custom";
        private static readonly string s_IMGUIDefaultFoundClassName = "unity-inspector-element--imgui-default";
        private static readonly string s_UIEDefaultFoundClassName = "unity-inspector-element--uie-default";
        private static readonly string s_DebugModeClassName = "unity-inspector-element--debug";
        private static readonly string s_DebugInternalModeClassName = "unity-inspector-element--debug-internal";

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

        public InspectorElement() : this(null) {}

        public InspectorElement(Object obj) : this(obj, Mode.Normal) {}

        internal InspectorElement(Object obj, Mode mode)
        {
            AddToClassList(s_InspectorClassName);

            this.mode = mode;

            if (obj == null)
                return;

            this.Bind(new SerializedObject(obj));
        }

        private void Reset(SerializedObjectBindEvent evt)
        {
            Clear();

            RemoveFromClassList(s_IMGUIInspectorClassName);
            RemoveFromClassList(s_UIEInspectorClassName);
            RemoveFromClassList(s_NoInspectorFoundClassName);
            RemoveFromClassList(s_UIECustomFoundClassName);
            RemoveFromClassList(s_IMGUICustomFoundClassName);
            RemoveFromClassList(s_IMGUIDefaultFoundClassName);
            RemoveFromClassList(s_UIEDefaultFoundClassName);
            RemoveFromClassList(s_DebugModeClassName);
            RemoveFromClassList(s_DebugInternalModeClassName);

            var bindObject = evt.bindObject;
            if (bindObject == null)
                return;

            var customEditor = GetOrCreateEditor(bindObject);
            var customInspector = CreateInspectorElementFromEditor(bindObject, customEditor);

            if (customInspector != this)
                shadow.Add(customInspector);
        }

        protected internal override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            var bindEvent = evt as SerializedObjectBindEvent;
            if (bindEvent == null)
                return;

            Reset(bindEvent);
        }

        private bool HasCustomEditor(SerializedObject serializedObject)
        {
            var target = serializedObject?.targetObject;
            if (target == null)
                return false;

            return ActiveEditorTracker.HasCustomEditor(target);
        }

        private Editor GetOrCreateEditor(SerializedObject serializedObject)
        {
            var target = serializedObject?.targetObject;
            if (target == null)
                return null;

            var activeEditors = ActiveEditorTracker.sharedTracker?.activeEditors;
            var editor = activeEditors?.FirstOrDefault((e) => e.target == target);
            if (editor == null)
                editor = Editor.CreateEditor(target);

            return editor;
        }

        private Object GetInspectedObject()
        {
            var activeEditors = ActiveEditorTracker.sharedTracker?.activeEditors;
            if (activeEditors == null)
                return null;

            Editor editor = InspectorWindowUtils.GetFirstNonImportInspectorEditor(activeEditors);
            if (editor == null)
                return null;

            return editor.target;
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
                    shadow.Add(field);
                }
                while (property.NextVisible(false));
            }

            AddToClassList(s_UIEDefaultFoundClassName);
            AddToClassList(s_UIEInspectorClassName);

            return this;
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
                editor.InternalSetTargets(new UnityEngine.Object[] {serializedObject.targetObject});
            }

            if (editor is GenericInspector)
            {
                AddToClassList(s_IMGUIDefaultFoundClassName);
                if ((mode & Mode.DebugMod) > 0)
                {
                    AddToClassList(s_DebugModeClassName);
                    editor.m_InspectorMode = InspectorMode.Debug;
                }
                else if ((mode & Mode.DebugInternalMod) > 0)
                {
                    AddToClassList(s_DebugInternalModeClassName);
                    editor.m_InspectorMode = InspectorMode.DebugInternal;
                }
            }
            else
            {
                AddToClassList(s_IMGUICustomFoundClassName);
            }

            var inspector = new IMGUIContainer(() =>
            {
                var originalWideMode = EditorGUIUtility.wideMode;
                EditorGUIUtility.wideMode = true;
                GUIStyle editorWrapper = (editor.UseDefaultMargins() ? EditorStyles.inspectorDefaultMargins : GUIStyle.none);
                EditorGUILayout.BeginVertical(editorWrapper);
                {
                    GUI.changed = false;

                    try
                    {
                        editor.OnInspectorGUI();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUIUtility.wideMode = originalWideMode;
            });

            if (!(editor is GenericInspector))
                inspector.AddToClassList(s_CustomInspectorClassName);

            inspector.AddToClassList(s_IMGUIContainerClassName);

            AddToClassList(s_IMGUIInspectorClassName);

            return inspector;
        }

        private VisualElement CreateInspectorElementFromEditor(SerializedObject serializedObject, Editor editor)
        {
            var target = serializedObject?.targetObject;
            if (target == null)
                return null;

            VisualElement inspectorElement = null;

            if ((mode & Mode.UIECustom) > 0)
            {
                inspectorElement = (editor as UIElementsEditor)?.CreateInspectorGUI();
                if (inspectorElement != null)
                {
                    AddToClassList(s_UIECustomFoundClassName);
                    AddToClassList(s_UIEInspectorClassName);
                    inspectorElement.AddToClassList(s_CustomInspectorClassName);
                }
            }

            if (inspectorElement == null)
                inspectorElement = CreateIMGUIInspectorFromEditor(serializedObject, editor);

            if (inspectorElement == null && (mode & Mode.UIEDefault) > 0)
                inspectorElement = CreateDefaultInspector(serializedObject);

            if (inspectorElement == null)
            {
                AddToClassList(s_NoInspectorFoundClassName);
                AddToClassList(s_UIEInspectorClassName);
                inspectorElement = new Label("No inspector found given the current Inspector.Mode.");
            }

            return inspectorElement;
        }
    }
}
