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
        private static readonly string s_InspectorClassName = "unity-inspector";
        private static readonly string s_CustumInspectorClassName = "unity-inspector-custom";

        public new class UxmlFactory : UxmlFactory<InspectorElement, UxmlTraits> {}

        public enum Mode
        {
            Normal, // Create custom inspector if available, otherwise create default inspector.
            Default, // Force the creation of the default inspector.
            Custom, // Force the creation of the custom inspector (if one is defined).
            IMGUI // Force the creation of an IMGUIContainer with the IMGUI inspector.
        }

        public Mode mode { get; private set; }

        public InspectorElement() : this(null) {}

        public InspectorElement(Object obj) : this(obj, Mode.Normal) {}

        public InspectorElement(Object obj, Mode mode)
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

            var bindObject = evt.bindObject;
            if (bindObject == null)
                return;

            switch (mode)
            {
                case Mode.Normal:
                {
                    var customInspector = CreateCustomInspector(bindObject);
                    if (customInspector != null)
                        shadow.Add(customInspector);
                    else
                        CreateDefaultInspector(bindObject);

                    break;
                }
                case Mode.Default:
                {
                    CreateDefaultInspector(bindObject);
                    break;
                }
                case Mode.Custom:
                case Mode.IMGUI:
                {
                    var customInspector = CreateCustomInspector(bindObject);
                    if (customInspector != null)
                        shadow.Add(customInspector);
                    else
                        shadow.Add(new Label("No custom inspector found and Inspector.Mode is set to Custom."));
                    break;
                }
            }
        }

        protected internal override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            var bindEvent = evt as SerializedObjectBindEvent;
            if (bindEvent == null)
                return;

            Reset(bindEvent);
        }

        private void CreateDefaultInspector(SerializedObject serializedObject)
        {
            if (serializedObject == null)
                return;

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
        }

        private VisualElement CreateCustomInspector(SerializedObject serializedObject)
        {
            var target = serializedObject?.targetObject;
            if (target == null)
                return null;

            if (!ActiveEditorTracker.HasCustomEditor(target))
                return null;

            var activeEditors = ActiveEditorTracker.sharedTracker?.activeEditors;
            var editor = activeEditors?.FirstOrDefault((e) => e.target == target);
            if (editor == null)
                editor = Editor.CreateEditor(target);

            VisualElement customInspector = null;

            if (mode != Mode.IMGUI)
            {
                customInspector = (editor as UIElementsEditor)?.CreateInspectorGUI();
            }

            if (customInspector == null)
            {
                customInspector = new IMGUIContainer(() =>
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
            }

            customInspector.AddToClassList(s_CustumInspectorClassName);

            return customInspector;
        }
    }
}
