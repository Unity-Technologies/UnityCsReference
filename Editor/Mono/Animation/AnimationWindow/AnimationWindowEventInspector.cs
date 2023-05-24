// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;
using UnityEditor;
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
        public static GUIContent s_OverloadWarning = EditorGUIUtility.TrTextContent("Some functions were overloaded in MonoBehaviour components and may not work as intended if used with Animation Events!");
        public static GUIContent s_DuplicatesWarning = EditorGUIUtility.TrTextContent("Some functions have the same name across several Monobehaviour components and may not work as intended if used with Animation Events!");

        const string kNotSupportedPostFix = " (Function Not Supported)";
        const string kNoneSelected = "(No Function Selected)";

        AnimationEventEditorState m_State = new();

        public override void OnInspectorGUI()
        {
            var awes = targets.Select(o => o as AnimationWindowEvent).ToArray();
            OnEditAnimationEvents(awes, m_State);
        }

        protected override void OnHeaderGUI()
        {
            string targetTitle = (targets.Length == 1) ? "Animation Event" : targets.Length + " Animation Events";
            DrawHeaderGUI(this, targetTitle);
        }

        public static void OnEditAnimationEvent(AnimationWindowEvent awe, AnimationEventEditorState state)
        {
            OnEditAnimationEvents(new AnimationWindowEvent[] {awe}, state);
        }

        // These are used so we don't alloc new lists on every call
        static List<AnimationMethodMap> supportedMethods;
        static List<AnimationMethodMap> overloads;
        static List<AnimationMethodMap> duplicates;

        public static void OnEditAnimationEvents(AnimationWindowEvent[] awEvents, AnimationEventEditorState state)
        {
            AnimationWindowEventData data = GetData(awEvents);
            if (data.events == null || data.selectedEvents == null || data.selectedEvents.Length == 0)
                return;

            AnimationEvent firstEvent = data.selectedEvents[0];

            bool singleFunctionName = Array.TrueForAll(data.selectedEvents, evt => evt.functionName == firstEvent.functionName);

            EditorGUI.BeginChangeCheck();

            if (data.root != null)
            {
                supportedMethods ??= new List<AnimationMethodMap>();
                overloads ??= new List<AnimationMethodMap>();
                duplicates ??= new List<AnimationMethodMap>();

                supportedMethods.Clear();
                overloads.Clear();
                duplicates.Clear();
                CollectSupportedMethods(data.root, supportedMethods, overloads, duplicates);

                int selected = supportedMethods.FindIndex(method => method.Name == firstEvent.functionName);

                // A non-empty array used for rendering the contents of the popup
                // It is of size 1 greater than the list of supported methods to account for the "None" option
                string[] methodsFormatted = new string[supportedMethods.Count + 1];

                for (int i = 0; i < supportedMethods.Count; ++i)
                {
                    AnimationMethodMap methodMap = supportedMethods[i];
                    string menuPath = methodMap.methodMenuPath;
                    methodsFormatted[i] = menuPath;
                }

                // Add a final option to set the function to no selected function
                int notSupportedIndex = supportedMethods.Count;
                if (selected == -1)
                {
                    selected = notSupportedIndex;

                    // Display that the current function is not supported if applicable
                    if (string.IsNullOrEmpty(firstEvent.functionName))
                        methodsFormatted[notSupportedIndex] = kNoneSelected;
                    else
                        methodsFormatted[notSupportedIndex] = firstEvent.functionName + kNotSupportedPostFix;

                    var emptyMethodMap = new AnimationMethodMap();
                    supportedMethods.Add(emptyMethodMap);
                }

                EditorGUIUtility.labelWidth = 130;

                EditorGUI.showMixedValue = !singleFunctionName;
                int wasSelected = singleFunctionName ? selected : -1;
                selected = EditorGUILayout.Popup("Function: ", selected, methodsFormatted);
                if (wasSelected != selected && selected != -1 && selected != notSupportedIndex)
                {
                    foreach (var evt in data.selectedEvents)
                    {
                        evt.functionName = supportedMethods[selected].Name;
                        evt.stringParameter = string.Empty;
                    }
                }
                EditorGUI.showMixedValue = false;

                var selectedParameter = supportedMethods[selected].parameterType;

                if (singleFunctionName && selectedParameter != null)
                {
                    EditorGUILayout.Space();
                    if (selectedParameter == typeof(AnimationEvent))
                        EditorGUILayout.PrefixLabel("Event Data");
                    else
                        EditorGUILayout.PrefixLabel("Parameters");

                    DoEditRegularParameters(data.selectedEvents, selectedParameter);
                }

                if (overloads.Count > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox(s_OverloadWarning.text, MessageType.Warning, true);
                    state.ShowOverloadedFunctionsDetails = EditorGUILayout.Foldout(state.ShowOverloadedFunctionsDetails, "Show Details");
                    if (state.ShowOverloadedFunctionsDetails)
                    {
                        string overloadedFunctionDetails = "Overloaded Functions: \n" + GetFormattedMethodsText(overloads);
                        GUILayout.Label(overloadedFunctionDetails, EditorStyles.helpBox);
                    }
                }

                if (duplicates.Count > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox(s_DuplicatesWarning.text, MessageType.Warning, true);
                    state.ShowDuplicatedFunctionsDetails = EditorGUILayout.Foldout(state.ShowDuplicatedFunctionsDetails, "Show Details");
                    if (state.ShowDuplicatedFunctionsDetails)
                    {
                        string duplicatedFunctionDetails = "Duplicated Functions: \n" + GetFormattedMethodsText(duplicates);
                        GUILayout.Label(duplicatedFunctionDetails, EditorStyles.helpBox);
                    }
                }
            }
            else
            {
                EditorGUI.showMixedValue = !singleFunctionName;
                string oldFunctionName = singleFunctionName ? firstEvent.functionName : "";
                string functionName = EditorGUILayout.TextField(EditorGUIUtility.TrTextContent("Function"), oldFunctionName).Replace(" ", "");
                if (functionName != oldFunctionName)
                {
                    foreach (var evt in data.selectedEvents)
                    {
                        evt.functionName = functionName;
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
                        DoEditRegularParameters(new AnimationEvent[] { dummyEvent }, typeof(AnimationEvent));
                    }
                }
            }

            if (EditorGUI.EndChangeCheck())
                SetData(awEvents, data);
        }

        static string GetFormattedMethodsText(List<AnimationMethodMap> methods)
        {
            string text = "";
            foreach (AnimationMethodMap methodMap in methods)
            {
                text += string.Format("{0}.{1} ( {2} )\n", methodMap.sourceBehaviour.GetType().Name, methodMap.Name, GetTypeName(methodMap.parameterType));
            }
            text = text.Trim();
            return text;
        }

        static string GetTypeName(Type t)
        {
            if (t == null)
                return "";
            if (t == typeof(int))
                return "int";
            if (t == typeof(float))
                return "float";
            if (t == typeof(string))
                return "string";
            if (t == typeof(bool))
                return "bool";
            return t.Name;
        }

        static string GetFormattedMethodName(AnimationMethodMap methodMap)
        {
            string targetName = methodMap.sourceBehaviour.GetType().Name;
            string methodName = methodMap.Name;
            string args = GetTypeName(methodMap.parameterType);

            if (methodName.StartsWith("set_") || methodName.StartsWith("get_"))
                return string.Format("{0}/Properties/{1} ( {2} )", targetName, methodName, args);
            else
                return string.Format("{0}/Methods/{1} ( {2} )", targetName, methodName, args);
        }

        public static void OnDisabledAnimationEvent()
        {
            AnimationEvent dummyEvent = new AnimationEvent();

            using (new EditorGUI.DisabledScope(true))
            {
                dummyEvent.functionName = EditorGUILayout.TextField(EditorGUIUtility.TrTextContent("Function"), dummyEvent.functionName);
                DoEditRegularParameters(new AnimationEvent[] { dummyEvent }, typeof(AnimationEvent));
            }
        }

        static Dictionary<Type, IReadOnlyList<AnimationMethodMap>> s_TypeAnimationMethodMapCache = new Dictionary<Type, IReadOnlyList<AnimationMethodMap>>();

        static void CollectSupportedMethods(GameObject gameObject, List<AnimationMethodMap> supportedMethods, List<AnimationMethodMap> overloadedMethods, List<AnimationMethodMap> duplicatedMethods)
        {
            if (gameObject == null)
                return;

            MonoBehaviour[] behaviours = gameObject.GetComponents<MonoBehaviour>();

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null)
                    continue;

                Type type = behaviour.GetType();
                while (type != typeof(MonoBehaviour) && type != null)
                {
                    if (!s_TypeAnimationMethodMapCache.TryGetValue(type, out IReadOnlyList<AnimationMethodMap> validMethods))
                    {
                        var pendingValidMethods = new List<AnimationMethodMap>();
                        MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly );
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

                            AnimationMethodMap newMethodMap = new AnimationMethodMap
                            {
                                sourceBehaviour = behaviour,
                                methodInfo = method,
                                parameterType = parameterType
                            };

                            newMethodMap.methodMenuPath = GetFormattedMethodName(newMethodMap);

                            pendingValidMethods.Add(newMethodMap);
                        }

                        validMethods = pendingValidMethods.AsReadOnly();
                        s_TypeAnimationMethodMapCache.Add(type, validMethods);
                    }

                    foreach (var method in validMethods)
                    {
                        // Since AnimationEvents only stores method name, it can't handle functions with multiple overloads.
                        // or functions with the same name across multiple monobehaviours
                        // Only retrieve first found method, and discard overloads and duplicate names.
                        int existingMethodIndex = supportedMethods.FindIndex(m => m.Name == method.Name);
                        if (existingMethodIndex != -1)
                        {
                            // The method is only ambiguous if it has a different signature to the one we saw before
                            if (supportedMethods[existingMethodIndex].parameterType != method.parameterType)
                            {
                                overloadedMethods.Add(method);
                            }
                            // Otherwise, there is another monobehaviour with the same method name.
                            else
                            {
                                duplicatedMethods.Add(method);
                            }
                        }
                        else
                        {
                            supportedMethods.Add(method);
                        }
                    }

                    type = type.BaseType;
                }
            }
        }

        /// <summary>
        /// Maps the methodInfo and paramter type of a considered animation method to a source monobeheaviour.
        /// Mimics the structure of <see cref="UnityEditorInternal.UnityEventDrawer.ValidMethodMap"/>
        /// </summary>
        struct AnimationMethodMap
        {
            public Object sourceBehaviour;
            public MethodInfo methodInfo;
            public Type parameterType;

            // Used for caching
            public string methodMenuPath;

            public string Name => methodInfo?.Name ?? "";
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

                var parameterTypes = method.GetParameters();
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
            return name != "Main" && name != "Start" && name != "Awake" && name != "Update";
        }

        private static string FormatEventArguments(ParameterInfo[] paramTypes, AnimationEvent evt)
        {
            if (paramTypes.Length == 0)
                return " ( )";

            if (paramTypes.Length > 1)
                return kNotSupportedPostFix;

            var paramType = paramTypes[0].ParameterType;

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


        // this are used so we don't alloc new lists on every call
        static List<AnimationEvent> getDataSelectedEvents;
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
                getDataSelectedEvents ??= new List<AnimationEvent>();
                getDataSelectedEvents.Clear();
                foreach (var awEvent in awEvents)
                {
                    if (awEvent.eventIndex >= 0 && awEvent.eventIndex < data.events.Length)
                        getDataSelectedEvents.Add(data.events[awEvent.eventIndex]);
                }

                data.selectedEvents = getDataSelectedEvents.ToArray();
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

        [MenuItem("CONTEXT/AnimationWindowEvent/Reset", secondaryPriority = 7)]
        static void ResetValues(MenuCommand command)
        {
            AnimationWindowEvent awEvent = command.context as AnimationWindowEvent;
            AnimationWindowEvent[] awEvents = new AnimationWindowEvent[] { awEvent };

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
