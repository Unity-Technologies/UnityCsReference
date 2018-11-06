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

        public InspectorElement() : this(null) {}

        public InspectorElement(Object obj) : this(obj, Mode.Normal) {}

        internal InspectorElement(Object obj, Mode mode)
        {
            AddToClassList(ussClassName);

            this.mode = mode;

            if (obj == null)
                return;

            this.Bind(new SerializedObject(obj));
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

            var customEditor = GetOrCreateEditor(bindObject);
            var customInspector = CreateInspectorElementFromEditor(bindObject, customEditor);

            if (customInspector != this)
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
                    hierarchy.Add(field);
                }
                while (property.NextVisible(false));
            }

            AddToClassList(uIEDefaultVariantUssClassName);
            AddToClassList(uIEInspectorVariantUssClassName);

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
                inspector.AddToClassList(customInspectorUssClassName);

            inspector.AddToClassList(iMGUIContainerUssClassName);

            AddToClassList(iMGUIInspectorVariantUssClassName);

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
                inspectorElement = (editor as UnityEditor.Experimental.UIElementsEditor)?.CreateInspectorGUI();
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
    }
}
