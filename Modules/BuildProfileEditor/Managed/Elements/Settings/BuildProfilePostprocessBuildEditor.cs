// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile.Elements
{
    /// <summary>
    /// Custom editor for the Post-process Build component. The default inspector can't instantiate an
    /// <see cref="IBuildProfilePostprocessBuild"/> for a <c>[SerializeReference]</c> element, so this editor
    /// offers an add dropdown listing the concrete implementers. Callbacks run in list order, so the list is reorderable.
    /// </summary>
    [CustomEditor(typeof(BuildProfilePostprocessBuild))]
    class BuildProfilePostprocessBuildEditor : Editor
    {
        const string k_Uxml = "BuildProfile/UXML/BuildProfilePostprocessBuild.uxml";
        const string k_MissingCallbackIconUssClass = "postprocess-build-missing-callback-icon";

        SerializedProperty m_Callbacks;
        ListView m_List;
        HelpBox m_NoCallbacksHelpBox;
        HelpBox m_MissingCallbacksHelpBox;

        // Each row's callback
        readonly List<object> m_CallbackValues = new List<object>();
        // Each row's missing type name, or null for a callback that isn't missing
        readonly List<string> m_MissingCallbackFullyQualifiedNames = new List<string>();
        // What each row was last drawn with: the callback's reference id, and its type, missing type name, or null if empty
        List<(long referenceId, object kind)> m_RowContents = new List<(long referenceId, object kind)>();

        // The warning last written to the console for each component, kept across inspectors and code reloads so
        // reopening the component doesn't log it again. Holds only ids and strings.
        [NoAutoStaticsCleanup]
        static readonly Dictionary<EntityId, string> s_LoggedMissingCallbacksWarnings = new Dictionary<EntityId, string>();

        public override VisualElement CreateInspectorGUI()
        {
            m_Callbacks = serializedObject.FindProperty("m_Callbacks");

            var root = new VisualElement();
            var visualTree = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
            var styleSheet = EditorGUIUtility.LoadRequired(Util.k_StyleSheet) as StyleSheet;
            visualTree.CloneTree(root);
            root.styleSheets.Add(styleSheet);

            m_NoCallbacksHelpBox = root.Q<HelpBox>("no-callbacks-help-box");
            m_NoCallbacksHelpBox.text = TrText.postprocessBuildNoCallbacks;
            m_MissingCallbacksHelpBox = root.Q<HelpBox>("missing-callbacks-help-box");

            m_List = root.Q<ListView>("postprocess-build-callbacks");
            // The help box above the list says it's empty
            m_List.makeNoneElement = () => new VisualElement();
            m_List.makeItem = () => new VisualElement();
            m_List.bindItem = BindRow;
            // The binding's default unbind expects a bindable element in each row, and a row past the end of the array is left empty
            m_List.unbindItem = (row, _) =>
            {
                row.Unbind();
                row.Clear();
            };
            m_List.overridingAddButtonBehavior = (list, button) => ShowAddMenu();
            m_List.onRemove = _ => RemoveCallback(RemovalIndex());
            m_List.selectedIndicesChanged += _ => UpdateAllowRemove();

            root.Bind(serializedObject);
            root.TrackSerializedObjectValue(serializedObject, _ => Refresh());

            serializedObject.Update();
            Refresh();
            return root;
        }

        // Updates everything that depends on which callbacks are missing
        void Refresh()
        {
            GetObjectsForSerializedCallbacks(m_Callbacks, m_CallbackValues);
            GetMissingCallbackFullyQualifiedNames(m_Callbacks, m_CallbackValues, m_MissingCallbackFullyQualifiedNames);
            bool hasMissingCallbacks = m_MissingCallbackFullyQualifiedNames.Exists(name => name != null);

            if (m_Callbacks.arraySize == 0)
                m_NoCallbacksHelpBox.Show();
            else
                m_NoCallbacksHelpBox.Hide();

            var missingCallbacksWarning = hasMissingCallbacks ? MissingCallbacksWarning(m_MissingCallbackFullyQualifiedNames) : null;
            m_MissingCallbacksHelpBox.text = missingCallbacksWarning ?? string.Empty;
            if (missingCallbacksWarning != null)
                m_MissingCallbacksHelpBox.Show();
            else
                m_MissingCallbacksHelpBox.Hide();
            LogMissingCallbacksWarning(missingCallbacksWarning);

            // Keeps each missing callback at its index, which its data and class name lookup depend on. 
            m_List.reorderable = !hasMissingCallbacks;
            UpdateAllowRemove();

            // The list only rebinds rows when the array changes size, so a row now holding a different callback, for
            // example after a script reorders the array, would keep showing the old one's title and field values
            if (RowContentsChanged())
                m_List.RefreshItems();
        }

        bool RowContentsChanged()
        {
            var rowContents = new List<(long referenceId, object kind)>(m_CallbackValues.Count);
            for (int i = 0; i < m_CallbackValues.Count; i++)
            {
                long referenceId = m_Callbacks.GetArrayElementAtIndex(i).managedReferenceId;
                rowContents.Add((referenceId, m_CallbackValues[i]?.GetType() ?? (object)m_MissingCallbackFullyQualifiedNames[i]));
            }

            bool changed = rowContents.Count != m_RowContents.Count;
            for (int i = 0; !changed && i < rowContents.Count; i++)
                changed = !Equals(rowContents[i], m_RowContents[i]);

            m_RowContents = rowContents;
            return changed;
        }

        int RemovalIndex() => m_List.selectedIndex >= 0 ? m_List.selectedIndex : m_Callbacks.arraySize - 1;

        void UpdateAllowRemove() => m_List.allowRemove = CanRemoveCallback(m_MissingCallbackFullyQualifiedNames, RemovalIndex());

        void BindRow(VisualElement row, int index)
        {
            row.Clear();
            if (index >= m_Callbacks.arraySize)
                return;

            var element = m_Callbacks.GetArrayElementAtIndex(index);
            var callback = element.managedReferenceValue;
            var missingFullyQualifiedName = callback == null ? GetMissingCallbackFullyQualifiedName(element) : null;

            if (missingFullyQualifiedName != null)
                row.Add(MissingCallbackRow(missingFullyQualifiedName));
            else if (callback == null)
                row.Add(new Label(element.displayName));
            else
                row.Add(CallbackRow(element, CallbackLabel(callback).text));
        }

        static VisualElement MissingCallbackRow(string missingFullyQualifiedName)
        {
            var content = CallbackLabel(null, missingFullyQualifiedName);
            var row = new VisualElement { tooltip = content.tooltip };
            row.AddToClassList("flex-row");
            row.AddToClassList("align-items-center");

            var icon = new Image { image = content.image };
            icon.AddToClassList(k_MissingCallbackIconUssClass);
            row.Add(icon);
            row.Add(new Label(content.text));
            return row;
        }

        // Draws each serialized field of the callback, so a callback with no fields is a plain label
        VisualElement CallbackRow(SerializedProperty element, string label)
        {
            var foldout = new Foldout { text = label, value = element.isExpanded };
            foldout.RegisterValueChangedCallback(evt =>
            {
                if (evt.target == foldout)
                    element.isExpanded = evt.newValue;
            });

            var field = element.Copy();
            var end = element.GetEndProperty();
            if (field.NextVisible(true))
            {
                while (!SerializedProperty.EqualContents(field, end))
                {
                    var propertyField = new PropertyField(field.Copy());
                    propertyField.Bind(serializedObject);
                    foldout.Add(propertyField);
                    if (!field.NextVisible(false))
                        break;
                }
            }

            return foldout.childCount > 0 ? foldout : new Label(label);
        }

        // The serializer finds a missing callback's type by the property path it had when loaded
        internal static string GetMissingCallbackFullyQualifiedName(SerializedProperty element) =>
            SerializationUtility.GetMissingTypeFullyQualifiedNameAtPropertyPath(element.serializedObject.targetObject, element.propertyPath);

        internal static void GetObjectsForSerializedCallbacks(SerializedProperty callbacks, List<object> values)
        {
            values.Clear();
            for (int i = 0; i < callbacks.arraySize; i++)
                values.Add(callbacks.GetArrayElementAtIndex(i).managedReferenceValue);
        }

        // A callback whose class was renamed, moved, or deleted deserializes as null, the same as an empty row, so the
        // serializer's record of the missing type tells them apart
        internal static void GetMissingCallbackFullyQualifiedNames(SerializedProperty callbacks, List<object> values, List<string> names)
        {
            names.Clear();
            for (int i = 0; i < values.Count; i++)
                names.Add(values[i] == null ? GetMissingCallbackFullyQualifiedName(callbacks.GetArrayElementAtIndex(i)) : null);
        }

        internal static GUIContent CallbackLabel(object callback, string missingFullyQualifiedName = null)
        {
            if (callback != null)
                return new GUIContent(ObjectNames.NicifyVariableName(callback.GetType().Name));

            var typeName = TypeName(missingFullyQualifiedName);
            var className = typeName.Substring(typeName.LastIndexOfAny(new[] { '.', '/' }) + 1);
            return new GUIContent(
                string.Format(TrText.postprocessBuildNamedMissingCallback, ObjectNames.NicifyVariableName(className)),
                EditorGUIUtility.IconContent("console.warnicon.sml").image,
                string.Format(TrText.postprocessBuildNamedMissingCallbackTooltip, missingFullyQualifiedName));
        }

        // The serializer records the type as Namespace.Outer/Inner, Assembly
        static string TypeName(string missingFullyQualifiedName)
        {
            int assemblySeparator = missingFullyQualifiedName.IndexOf(", ", StringComparison.Ordinal);
            return assemblySeparator < 0 ? missingFullyQualifiedName : missingFullyQualifiedName.Substring(0, assemblySeparator);
        }

        // Built from the rows, because the serializer keeps the missing type of a removed row until the profile is saved
        internal static string MissingCallbacksWarning(List<string> missingFullyQualifiedNames)
        {
            var names = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var missingFullyQualifiedName in missingFullyQualifiedNames)
            {
                if (missingFullyQualifiedName != null)
                    names.Add(TypeName(missingFullyQualifiedName));
            }

            return string.Format(TrText.postprocessBuildMissingCallbacksWarning, string.Join(", ", names));
        }

        // Writes the warning to the console when it appears or changes for this component
        internal void LogMissingCallbacksWarning(string warning)
        {
            var id = target.GetEntityId();
            s_LoggedMissingCallbacksWarnings.TryGetValue(id, out var loggedWarning);
            if (warning == loggedWarning)
                return;

            if (warning == null)
            {
                s_LoggedMissingCallbacksWarnings.Remove(id);
                return;
            }

            s_LoggedMissingCallbacksWarnings[id] = warning;
            Debug.LogWarning(warning, target);
        }

        // The serializer restores a missing callback's data by its array index, so removing a row above it loses the data
        internal static bool CanRemoveCallback(List<string> missingFullyQualifiedNames, int index) =>
            missingFullyQualifiedNames.FindLastIndex(name => name != null) <= index;

        internal void RemoveCallback(int index)
        {
            serializedObject.Update();
            GetObjectsForSerializedCallbacks(m_Callbacks, m_CallbackValues);
            GetMissingCallbackFullyQualifiedNames(m_Callbacks, m_CallbackValues, m_MissingCallbackFullyQualifiedNames);
            if (index < 0 || index >= m_CallbackValues.Count || !CanRemoveCallback(m_MissingCallbackFullyQualifiedNames, index))
                return;

            m_Callbacks.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();

            // Clearing the selection recomputes whether removal is allowed, so the missing callbacks must be current first
            Refresh();
            m_List.ClearSelection();
        }

        void ShowAddMenu()
        {
            var entries = new List<(string name, Type type)>();
            foreach (var type in GetEligibleCallbackTypes())
                entries.Add((ObjectNames.NicifyVariableName(type.Name), type));

            // Types with the same name in different namespaces keep a fixed order
            entries.Sort((a, b) =>
            {
                int byName = EditorUtility.NaturalCompare(a.name, b.name);
                return byName != 0 ? byName : string.CompareOrdinal(a.type.FullName, b.type.FullName);
            });

            var menu = new GenericMenu();
            foreach (var (name, type) in entries)
                menu.AddItem(new GUIContent(name), false, () => AddCallback(type));

            if (entries.Count == 0)
                menu.AddDisabledItem(new GUIContent(TrText.postprocessBuildNoTypesFound));

            menu.ShowAsContext();
        }

        void AddCallback(Type type)
        {
            serializedObject.Update();
            int index = m_Callbacks.arraySize;
            m_Callbacks.InsertArrayElementAtIndex(index);
            m_Callbacks.GetArrayElementAtIndex(index).managedReferenceValue = Activator.CreateInstance(type);
            serializedObject.ApplyModifiedProperties();
        }

        internal static IEnumerable<Type> GetEligibleCallbackTypes()
        {
            foreach (var type in TypeCache.GetTypesDerivedFrom<IBuildProfilePostprocessBuild>())
            {
                if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)
                    continue;

                // SerializeReference only stores [Serializable] classes that don't derive from UnityEngine.Object
                if (!type.IsClass || !type.IsSerializable || typeof(UnityEngine.Object).IsAssignableFrom(type))
                    continue;

                if (type.GetConstructor(Type.EmptyTypes) == null)
                    continue;

                yield return type;
            }
        }
    }
}
