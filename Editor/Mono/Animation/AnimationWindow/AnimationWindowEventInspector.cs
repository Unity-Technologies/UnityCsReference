// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [CustomEditor(typeof(AnimationWindowEvent))]
    [CanEditMultipleObjects]
    internal class AnimationWindowEventInspector : Editor
    {
        const string kNotSupportedPostFix = " (Function Not Supported)";
        const string kNoneSelected = "(No Function Selected)";

        public override void OnInspectorGUI()
        {
            var awes = targets.Select(o => o as AnimationWindowEvent).ToArray();
            OnEditAnimationEvents(awes);
        }

        protected override void OnHeaderGUI()
        {
            string targetTitle = (targets.Length == 1) ? "Animation Event" : targets.Length + " Animation Events";
            DrawHeaderGUI(this, targetTitle);
        }

        public static void OnEditAnimationEvent(AnimationWindowEvent awe)
        {
            OnEditAnimationEvents(new AnimationWindowEvent[] {awe});
        }

        public static void OnEditAnimationEvents(AnimationWindowEvent[] awEvents)
        {
            AnimationWindowEventData data = GetData(awEvents);
            if (data.events == null || data.selectedEvents == null || data.selectedEvents.Length == 0)
                return;

            AnimationEvent firstEvent = data.selectedEvents[0];

            bool singleFunctionName = Array.TrueForAll(data.selectedEvents, evt => evt.functionName == firstEvent.functionName);

            GUI.changed = false;

            if (data.root != null)
            {
                List<AnimationWindowEventMethod> methods = CollectSupportedMethods(data.root);

                var methodsFormatted = new List<string>(methods.Count);

                for (int i = 0; i < methods.Count; ++i)
                {
                    AnimationWindowEventMethod method = methods[i];

                    string postFix = " ( )";
                    if (method.parameterType != null)
                    {
                        if (method.parameterType == typeof(float))
                            postFix = " ( float )";
                        else if (method.parameterType == typeof(int))
                            postFix = " ( int )";
                        else
                            postFix = string.Format(" ( {0} )", method.parameterType.Name);
                    }

                    methodsFormatted.Add(method.name + postFix);
                }

                int notSupportedIndex = methods.Count;
                int selected = methods.FindIndex(method => method.name == firstEvent.functionName);
                if (selected == -1)
                {
                    selected = methods.Count;

                    AnimationWindowEventMethod newMethod = new AnimationWindowEventMethod();
                    newMethod.name = firstEvent.functionName;
                    newMethod.parameterType = null;

                    methods.Add(newMethod);

                    if (string.IsNullOrEmpty(firstEvent.functionName))
                        methodsFormatted.Add(kNoneSelected);
                    else
                        methodsFormatted.Add(firstEvent.functionName + kNotSupportedPostFix);
                }

                EditorGUIUtility.labelWidth = 130;

                EditorGUI.showMixedValue = !singleFunctionName;
                int wasSelected = singleFunctionName ? selected : -1;
                selected = EditorGUILayout.Popup("Function: ", selected, methodsFormatted.ToArray());
                if (wasSelected != selected && selected != -1 && selected != notSupportedIndex)
                {
                    foreach (var evt in data.selectedEvents)
                    {
                        evt.functionName = methods[selected].name;
                        evt.stringParameter = string.Empty;
                    }
                }
                EditorGUI.showMixedValue = false;

                var selectedParameter = methods[selected].parameterType;

                if (singleFunctionName && selectedParameter != null)
                {
                    EditorGUILayout.Space();
                    if (selectedParameter == typeof(AnimationEvent))
                        EditorGUILayout.PrefixLabel("Event Data");
                    else
                        EditorGUILayout.PrefixLabel("Parameters");

                    DoEditRegularParameters(data.selectedEvents, selectedParameter);
                }
            }
            else
            {
                EditorGUI.showMixedValue = !singleFunctionName;
                string oldFunctionName = singleFunctionName ? firstEvent.functionName : "";
                string functionName = EditorGUILayout.TextField(new GUIContent("Function"), oldFunctionName);
                if (functionName != oldFunctionName)
                {
                    foreach (var evt in data.selectedEvents)
                    {
                        evt.functionName = functionName;
                        evt.stringParameter = string.Empty;
                    }
                }
                EditorGUI.showMixedValue = false;

                if (singleFunctionName)
                {
                    DoEditRegularParameters(data.selectedEvents, typeof(AnimationEvent));
                }
                else
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        AnimationEvent dummyEvent = new AnimationEvent();
                        DoEditRegularParameters(new AnimationEvent[] {dummyEvent}, typeof(AnimationEvent));
                    }
                }
            }

            if (GUI.changed)
                SetData(awEvents, data);
        }

        public static void OnDisabledAnimationEvent()
        {
            AnimationEvent dummyEvent = new AnimationEvent();

            using (new EditorGUI.DisabledScope(true))
            {
                dummyEvent.functionName = EditorGUILayout.TextField(new GUIContent("Function"), dummyEvent.functionName);
                DoEditRegularParameters(new AnimationEvent[] {dummyEvent}, typeof(AnimationEvent));
            }
        }

        public static List<AnimationWindowEventMethod> CollectSupportedMethods(GameObject gameObject)
        {
            List<AnimationWindowEventMethod> supportedMethods = new List<AnimationWindowEventMethod>();

            if (gameObject == null)
                return supportedMethods;

            MonoBehaviour[] behaviours = gameObject.GetComponents<MonoBehaviour>();
            HashSet<string> ambiguousMethods = new HashSet<string>();

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null)
                    continue;

                Type type = behaviour.GetType();
                while (type != typeof(MonoBehaviour) && type != null)
                {
                    MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    for (int i = 0; i < methods.Length; i++)
                    {
                        MethodInfo method = methods[i];
                        string name = method.Name;

                        if (!IsSupportedMethodName(name))
                            continue;

                        ParameterInfo[] parameters = method.GetParameters();
                        if (parameters.Length > 1)
                            continue;

                        Type parameterType = null;

                        if (parameters.Length == 1)
                        {
                            parameterType = parameters[0].ParameterType;
                            if (!(parameterType == typeof(string) ||
                                  parameterType == typeof(float) ||
                                  parameterType == typeof(int) ||
                                  parameterType == typeof(AnimationEvent) ||
                                  parameterType == typeof(UnityEngine.Object) ||
                                  parameterType.IsSubclassOf(typeof(UnityEngine.Object)) ||
                                  parameterType.IsEnum))
                                continue;
                        }

                        AnimationWindowEventMethod newMethod = new AnimationWindowEventMethod();
                        newMethod.name = method.Name;
                        newMethod.parameterType = parameterType;

                        int existingMethodIndex = supportedMethods.FindIndex(m => m.name == name);
                        if (existingMethodIndex != -1)
                        {
                            // The method is only ambiguous if it has a different signature to the one we saw before
                            if (supportedMethods[existingMethodIndex].parameterType != parameterType)
                                ambiguousMethods.Add(name);
                        }

                        supportedMethods.Add(newMethod);
                    }
                    type = type.BaseType;
                }
            }

            // Since AnimationEvents only stores method name, it can't handle functions with multiple overloads
            // So we remove all the ambiguous methods (overloads) from the list
            foreach (string ambiguousMethod in ambiguousMethods)
            {
                for (int i = supportedMethods.Count - 1; i >= 0; --i)
                {
                    if (supportedMethods[i].name.Equals(ambiguousMethod))
                        supportedMethods.RemoveAt(i);
                }
            }

            return supportedMethods;
        }

        public static string FormatEvent(GameObject root, AnimationEvent evt)
        {
            if (string.IsNullOrEmpty(evt.functionName))
                return kNoneSelected;

            if (!IsSupportedMethodName(evt.functionName))
                return evt.functionName + kNotSupportedPostFix;

            if (root == null)
                return evt.functionName + kNotSupportedPostFix;

            foreach (var behaviour in root.GetComponents<MonoBehaviour>())
            {
                if (behaviour == null) continue;

                var type = behaviour.GetType();
                if (type == typeof(MonoBehaviour) ||
                    (type.BaseType != null && type.BaseType.Name == "GraphBehaviour"))
                    continue;

                MethodInfo method = null;
                try
                {
                    method = type.GetMethod(evt.functionName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                }
                catch (AmbiguousMatchException)
                {
                }

                if (method == null)
                    continue;

                var parameterTypes = method.GetParameters().Select(p => p.ParameterType);
                return evt.functionName + FormatEventArguments(parameterTypes, evt);
            }

            return evt.functionName + kNotSupportedPostFix;
        }

        private static void DoEditRegularParameters(AnimationEvent[] events, Type selectedParameter)
        {
            AnimationEvent firstEvent = events[0];

            if (selectedParameter == typeof(AnimationEvent) || selectedParameter == typeof(float))
            {
                bool singleParamValue = Array.TrueForAll(events, evt => evt.floatParameter == firstEvent.floatParameter);

                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = !singleParamValue;
                float newValue = EditorGUILayout.FloatField("Float", firstEvent.floatParameter);
                EditorGUI.showMixedValue = false;

                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var evt in events)
                        evt.floatParameter = newValue;
                }
            }

            if (selectedParameter == typeof(AnimationEvent) || selectedParameter == typeof(int) || selectedParameter.IsEnum)
            {
                bool singleParamValue = Array.TrueForAll(events, evt => evt.intParameter == firstEvent.intParameter);

                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = !singleParamValue;
                int newValue = 0;
                if (selectedParameter.IsEnum)
                    newValue = EnumPopup("Enum", selectedParameter, firstEvent.intParameter);
                else
                    newValue = EditorGUILayout.IntField("Int", firstEvent.intParameter);
                EditorGUI.showMixedValue = false;

                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var evt in events)
                        evt.intParameter = newValue;
                }
            }

            if (selectedParameter == typeof(AnimationEvent) || selectedParameter == typeof(string))
            {
                bool singleParamValue = Array.TrueForAll(events, evt => evt.stringParameter == firstEvent.stringParameter);

                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = !singleParamValue;
                string newValue = EditorGUILayout.TextField("String", firstEvent.stringParameter);
                EditorGUI.showMixedValue = false;

                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var evt in events)
                        evt.stringParameter = newValue;
                }
            }

            if (selectedParameter == typeof(AnimationEvent) || selectedParameter.IsSubclassOf(typeof(UnityEngine.Object)) || selectedParameter == typeof(UnityEngine.Object))
            {
                bool singleParamValue = Array.TrueForAll(events, evt => evt.objectReferenceParameter == firstEvent.objectReferenceParameter);

                EditorGUI.BeginChangeCheck();
                Type type = typeof(UnityEngine.Object);
                if (selectedParameter != typeof(AnimationEvent))
                    type = selectedParameter;

                EditorGUI.showMixedValue = !singleParamValue;
                bool allowSceneObjects = false;
                Object newValue = EditorGUILayout.ObjectField(ObjectNames.NicifyVariableName(type.Name), firstEvent.objectReferenceParameter, type, allowSceneObjects);
                EditorGUI.showMixedValue = false;

                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var evt in events)
                        evt.objectReferenceParameter = newValue;
                }
            }
        }

        private static int EnumPopup(string label, Type enumType, int selected)
        {
            if (!enumType.IsEnum)
                throw new Exception("parameter _enum must be of type System.Enum");

            string[] enumStrings = System.Enum.GetNames(enumType);
            int i = System.Array.IndexOf(enumStrings, Enum.GetName(enumType, selected));

            i = EditorGUILayout.Popup(label, i, enumStrings, EditorStyles.popup);

            if (i == -1)
                return selected;
            else
            {
                System.Enum res = (System.Enum)Enum.Parse(enumType, enumStrings[i]);
                return Convert.ToInt32(res);
            }
        }

        private static bool IsSupportedMethodName(string name)
        {
            if (name == "Main" || name == "Start" || name == "Awake" || name == "Update")
                return false;

            return true;
        }

        private static string FormatEventArguments(IEnumerable<Type> paramTypes, AnimationEvent evt)
        {
            if (!paramTypes.Any())
                return " ( )";

            if (paramTypes.Count() > 1)
                return kNotSupportedPostFix;

            var paramType = paramTypes.First();

            if (paramType == typeof(string))
                return " ( \"" + evt.stringParameter + "\" )";

            if (paramType == typeof(float))
                return " ( " + evt.floatParameter + " )";

            if (paramType == typeof(int))
                return " ( " + evt.intParameter + " )";

            if (paramType.IsEnum)
                return " ( " + paramType.Name + "." + Enum.GetName(paramType, evt.intParameter) + " )";

            if (paramType == typeof(AnimationEvent))
                return " ( "
                    + evt.floatParameter + " / "
                    + evt.intParameter + " / \""
                    + evt.stringParameter + "\" / "
                    + (evt.objectReferenceParameter == null ? "null" : evt.objectReferenceParameter.name) + " )";

            if (paramType.IsSubclassOf(typeof(UnityEngine.Object)) || paramType == typeof(UnityEngine.Object))
                return " ( " + (evt.objectReferenceParameter == null ? "null" : evt.objectReferenceParameter.name) + " )";

            return kNotSupportedPostFix;
        }

        private struct AnimationWindowEventData
        {
            public GameObject root;
            public AnimationClip clip;
            public AnimationClipInfoProperties clipInfo;

            public AnimationEvent[] events;
            public AnimationEvent[] selectedEvents;
        }

        private static AnimationWindowEventData GetData(AnimationWindowEvent[] awEvents)
        {
            var data = new AnimationWindowEventData();
            if (awEvents.Length == 0)
                return data;

            AnimationWindowEvent firstAwEvent = awEvents[0];
            data.root = firstAwEvent.root;
            data.clip = firstAwEvent.clip;
            data.clipInfo = firstAwEvent.clipInfo;

            if (data.clip != null)
                data.events = AnimationUtility.GetAnimationEvents(data.clip);
            else if (data.clipInfo != null)
                data.events = data.clipInfo.GetEvents();

            if (data.events != null)
            {
                List<AnimationEvent> selectedEvents = new List<AnimationEvent>();
                foreach (var awEvent in awEvents)
                {
                    if (awEvent.eventIndex >= 0 && awEvent.eventIndex < data.events.Length)
                        selectedEvents.Add(data.events[awEvent.eventIndex]);
                }

                data.selectedEvents = selectedEvents.ToArray();
            }

            return data;
        }

        private static void SetData(AnimationWindowEvent[] awEvents, AnimationWindowEventData data)
        {
            if (data.events == null)
                return;

            if (data.clip != null)
            {
                Undo.RegisterCompleteObjectUndo(data.clip, "Animation Event Change");
                AnimationUtility.SetAnimationEvents(data.clip, data.events);
            }
            else if (data.clipInfo != null)
            {
                foreach (var awEvent in awEvents)
                {
                    if (awEvent.eventIndex >= 0 && awEvent.eventIndex < data.events.Length)
                        data.clipInfo.SetEvent(awEvent.eventIndex, data.events[awEvent.eventIndex]);
                }
            }
        }

        [MenuItem("CONTEXT/AnimationWindowEvent/Reset")]
        static void ResetValues(MenuCommand command)
        {
            AnimationWindowEvent awEvent = command.context as AnimationWindowEvent;
            AnimationWindowEvent[] awEvents = new AnimationWindowEvent[] {awEvent};

            AnimationWindowEventData data = GetData(awEvents);
            if (data.events == null || data.selectedEvents == null || data.selectedEvents.Length == 0)
                return;

            foreach (var evt in data.selectedEvents)
            {
                evt.functionName = "";
                evt.stringParameter = string.Empty;
                evt.floatParameter = 0f;
                evt.intParameter = 0;
                evt.objectReferenceParameter = null;
            }

            SetData(awEvents, data);
        }
    }
}
