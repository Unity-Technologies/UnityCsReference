// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEditor.UIElements.Bindings;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.Presets
{
    [CustomEditor(typeof(Preset))]
    [CanEditMultipleObjects]
    internal class PresetEditor : Editor
    {
        static class Style
        {
            public static GUIContent presetType = EditorGUIUtility.TrTextContent("Preset Type", "The Object type this Preset can be applied to.");
            public static GUIStyle centerStyle = new GUIStyle() { alignment = TextAnchor.MiddleCenter };

            public static GUIContent addToDefault = EditorGUIUtility.TrTextContent("Add to {0} default", "The Preset will be added first in the default list with an empty filter.");
            public static GUIContent removeFromDefault = EditorGUIUtility.TrTextContent("Remove from {0} default", "All entry using this Preset will be removed from the default list.");
            public static GUIContent enableProperty = EditorGUIUtility.TrTextContent("Include Property");
            public static GUIContent disableProperty = EditorGUIUtility.TrTextContent("Exclude Property");

            public static readonly string excludedUssClassName = "unity-binding--preset-ignore";
            public static readonly string excludedBarName = "unity-binding-preset-ignore-bar";
            public static readonly string excludedBarContainerName = "unity-preset-override-bars-container";
            public static readonly string excludedBarUssClassName = "unity-binding__preset-ignore-bar";
        }

        class ReferenceCount
        {
            public int count;
            public UnityObject reference;
            public Hash128 presetHash;
        }
        static Dictionary<int, ReferenceCount> s_References = new Dictionary<int, ReferenceCount>();
        List<int> m_PresetsInstanceIds = new List<int>();

        Editor m_InternalEditor = null;
        ICoupledEditor m_CoupledEditor = null;
        VisualElement m_Root;

        string m_PresetTypeName;
        string m_HeaderTitle;

        string[] m_ExcludedProperties;

        string m_NotSupportedEditorName = null;
        Texture2D m_UnsupportedIcon = null;

        Dictionary<string, VisualElement> m_BoundElements = new Dictionary<string, VisualElement>();


        internal override void OnForceReloadInspector()
        {
            base.OnForceReloadInspector();
            UpdateInvalidReferences();
        }

        void UpdateInvalidReferences()
        {
            foreach (var o in targets)
            {
                var instanceID = o.GetInstanceID();
                if (s_References.ContainsKey(instanceID))
                {
                    var presetHash = s_References[instanceID].presetHash;
                    var currentHash = AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetOrScenePath(o));

                    // The asset changed on disk. Update the reference.
                    if (presetHash != currentHash)
                    {
                        s_References[instanceID].presetHash = currentHash;
                        s_References[instanceID].reference = (o as Preset)?.GetReferenceObject();
                    }
                }
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            m_Root = root;

            if (target is Preset p && !p.IsValid())
            {
                root.Add(new HelpBox("Unable to load this Preset, the type is not supported.", HelpBoxMessageType.Error));
            }
            else
            {
                var label = new TextField();
                label.label = Style.presetType.text;
                label.tooltip = Style.presetType.tooltip;
                label.value = m_PresetTypeName;
                label.isReadOnly = true;
                root.Add(label);
            }

            // This is the current workaround to track this property change.
            var hiddenField = new PropertyField(serializedObject.FindProperty("m_ExcludedProperties"));
            hiddenField.style.display = DisplayStyle.None;
            hiddenField.RegisterCallback<ChangeEvent<string>>(evt => UpdateVisualBindings());
            hiddenField.RegisterCallback<ChangeEvent<int>>(evt => UpdateVisualBindings());
            //TODO: Better step one: use RegisterValueChangeCallback when it's being reported for parent properties and not only children
            //hiddenField.RegisterValueChangeCallback(prop => UpdateVisualBindings()); // Looks like a bug, need to be reported
            root.Add(hiddenField);
            //TODO: Better step two: not using a hidden field, just track the PropertyValue from the root element.
            //BindingExtensions.TrackPropertyValue(root, serializedObject.FindProperty("m_ExcludedProperties"), so => UpdateVisualBindings()); // throwing error because it only works on leaf properties.

            var innerRoot = new VisualElement();
            root.Add(innerRoot);

            if (m_InternalEditor != null)
            {
                VisualElement internalInspector = null;
                if (PropertyEditor.IsMultiEditingSupported(m_InternalEditor, m_InternalEditor.target,
                    InspectorMode.Normal))
                {
                    internalInspector = new InspectorElement(m_InternalEditor);
                }
                else
                {
                    internalInspector = new IMGUIContainer(() => GUILayout.Label("Multi-object editing not supported.", EditorStyles.helpBox));
                }

                if (m_InternalEditor.target is Component)
                {
                    innerRoot.Add(new IMGUIContainer(() => DrawComponentTitleBar(internalInspector)));
                }
                else
                {
                    innerRoot.Add(new IMGUIContainer(DrawInternalEditorHeader));
                }

                innerRoot.Add(internalInspector);
                internalInspector.TrackSerializedObjectValue(m_InternalEditor.serializedObject, SaveTargetChangesToPreset);
                if (m_CoupledEditor != null && m_CoupledEditor.coupledComponent != null)
                {
                    var hiddenElement = new VisualElement();
                    hiddenElement.style.display = DisplayStyle.None;
                    hiddenElement.TrackSerializedObjectValue(m_CoupledEditor.coupledComponent, SaveTargetChangesToPreset);
                    innerRoot.Add(hiddenElement);
                }
            }
            else if (!string.IsNullOrEmpty(m_NotSupportedEditorName))
            {
                var container = new IMGUIContainer(DrawUnsupportedInspector);
                innerRoot.Add(container);
            }

            return root;
        }

        void DrawComponentTitleBar(VisualElement internalInspector)
        {
            PresetEditorHelper.InspectedObjects = targets;
            try
            {
                bool wasVisible = InternalEditorUtility.GetIsInspectorExpanded(m_InternalEditor.target);
                bool isVisible = EditorGUILayout.InspectorTitlebar(wasVisible, m_InternalEditor);
                if (isVisible != wasVisible)
                {
                    internalInspector.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
                    InternalEditorUtility.SetIsInspectorExpanded(m_InternalEditor.target, isVisible);
                }
            }
            finally
            {
                PresetEditorHelper.InspectedObjects = null;
            }
        }

        void DrawInternalEditorHeader()
        {
            var prevHierarchyMode = EditorGUIUtility.hierarchyMode;
            PresetEditorHelper.InspectedObjects = targets;
            try
            {
                // This value is used in Editor.DrawHeader() to Begin/End vertical layout groups and to
                // override some style properties. It is inconsistent between calls (there's a public
                // callback there that users/packages can subscribe to) and therefore sometimes breaks
                // the layout and buttons. Forcing its value here (to any value) prevents this from happening.
                EditorGUIUtility.hierarchyMode = true;
                using (new EditorGUILayout.VerticalScope())
                {
                    m_InternalEditor.DrawHeader();
                }
            }
            finally
            {
                EditorGUIUtility.hierarchyMode = prevHierarchyMode;
                PresetEditorHelper.InspectedObjects = null;
            }
        }

        public override bool UseDefaultMargins()
        {
            // Makes inspector be full width
            return false;
        }

        void UpdateVisualBindings()
        {
            var newExclusion = ((Preset)target).excludedProperties;
            var changes = new HashSet<string>(newExclusion.Except(m_ExcludedProperties).Concat(m_ExcludedProperties.Except(newExclusion)));
            var oldBind = new Dictionary<string, VisualElement>(m_BoundElements);
            foreach (var boundElement in oldBind)
            {
                if (boundElement.Value is IBindable bindable)
                {
                    if (!(bindable.binding is SerializedObjectBindingBase binding) || binding.boundProperty == null)
                        continue;

                    var key = boundElement.Key;
                    while (!string.IsNullOrEmpty(key))
                    {
                        if (changes.Contains(key))
                        {
                            BindingsStyleHelpers.UpdateElementStyle(boundElement.Value, binding.boundProperty);
                            break;
                        }

                        var index = key.LastIndexOf('.');
                        key = index > 0 ? key.Substring(0, index) : null;
                    }
                }
            }

            m_ExcludedProperties = newExclusion;
        }

        void SaveTargetChangesToPreset(SerializedObject o)
        {
            serializedObject.ApplyModifiedProperties();
            for (int i = 0; i < m_InternalEditor.targets.Length; i++)
            {
                ((Preset)targets[i]).UpdateProperties(m_InternalEditor.targets[i]);
            }
            serializedObject.Update();
        }

        void OnEnable()
        {
            var first = (Preset)target;
            bool isValidAndAllSame = true;
            m_PresetTypeName = first.GetTargetFullTypeName();
            m_HeaderTitle = $"{m_PresetTypeName} Presets ({targets.Length})";

            foreach (var preset in targets.Cast<Preset>().Skip(1))
            {
                var type = preset.GetTargetFullTypeName();
                if (type != m_PresetTypeName)
                {
                    isValidAndAllSame = false;
                    m_PresetTypeName = EditorGUI.mixedValueContent.text;
                    m_HeaderTitle = $"Multiple Types Presets ({targets.Length})";
                    break;
                }
            }

            if (isValidAndAllSame)
            {
                GenerateInternalEditor();
            }

            m_ExcludedProperties = first.excludedProperties;

            //TODO: Bind VisualElement root of the second editor to a callback, see last TODO inside CreateInspectorGUI
            BindingsStyleHelpers.updateBindingStateStyle += UpdatePropertyStyle;
            EditorGUIUtility.contextualPropertyMenu += DisableEnableProperty;
            EditorGUIUtility.beginProperty += BeginProperty;
        }

        void OnDisable()
        {
            m_Root?.Clear();
            DestroyInternalEditor();

            EditorGUIUtility.beginProperty -= BeginProperty;
            EditorGUIUtility.contextualPropertyMenu -= DisableEnableProperty;
            BindingsStyleHelpers.updateBindingStateStyle -= UpdatePropertyStyle;
        }

        void GenerateInternalEditor()
        {
            if (m_InternalEditor == null)
            {
                UnityObject[] objs = new UnityObject[targets.Length];
                for (var index = 0; index < targets.Length; index++)
                {
                    var p = (Preset)targets[index];
                    if (p.GetReferenceObject() == null)
                    {
                        // fast exit on NULL targets as we do not support their inspector in Preset.
                        SetupUnsupportedInspector(p);
                        return;
                    }

                    ReferenceCount reference = null;
                    var referenceRegistered = s_References.TryGetValue(p.GetInstanceID(), out reference);
                    if (!referenceRegistered || reference.reference == null)
                    {
                        reference = new ReferenceCount()
                        {
                            count = 0,
                            reference = p.GetReferenceObject(),
                            presetHash = AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetOrScenePath(p))
                        };

                        reference.reference.name = p.name;
                        if (referenceRegistered)
                        {
                            s_References[p.GetInstanceID()] = reference;
                        }
                        else
                        {
                            s_References.Add(p.GetInstanceID(), reference);
                        }
                    }

                    m_PresetsInstanceIds.Add(p.GetInstanceID());
                    reference.count++;
                    objs[index] = reference.reference;
                }
                m_InternalEditor = CreateEditor(objs);
            }
            else
            {
                //Coming back from an assembly reload... our references are probably broken and we need to fix them.
                for (var index = 0; index < targets.Length; index++)
                {
                    var instanceID = targets[index].GetInstanceID();
                    ReferenceCount reference = null;
                    if (!s_References.TryGetValue(instanceID, out reference))
                    {
                        reference = new ReferenceCount()
                        {
                            count = 0,
                            reference = m_InternalEditor.targets[index],
                            presetHash = AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetOrScenePath(targets[index]))
                        };
                        s_References.Add(instanceID, reference);
                    }
                    reference.count++;
                }
            }

            m_InternalEditor.firstInspectedEditor = true;
            if(target is Preset preset && preset.IsCoupled())
            {
                m_CoupledEditor = m_InternalEditor as ICoupledEditor;
                if (m_CoupledEditor == null)
                {
                    Debug.LogError("CoupledComponent Editors have to implement ICoupledEditor interface.");
                    DestroyImmediate(m_InternalEditor);
                    m_InternalEditor = null;
                }
            }
        }

        void DestroyInternalEditor()
        {
            if (m_InternalEditor != null)
            {
                // Do not destroy anything if we are just reloading assemblies or re-importing.
                bool shouldDestroyEverything = Unsupported.IsDestroyScriptableObject(this);
                if (shouldDestroyEverything)
                    DestroyImmediate(m_InternalEditor);

                // On Destroy, look for instances id instead of target because they may already be null.
                for (var index = 0; index < m_PresetsInstanceIds.Count; index++)
                {
                    var instanceId = m_PresetsInstanceIds[index];
                    if (--s_References[instanceId].count == 0)
                    {
                        if (shouldDestroyEverything)
                        {
                            if (s_References[instanceId].reference is Component)
                            {
                                var go = ((Component)s_References[instanceId].reference).gameObject;
                                go.hideFlags = HideFlags.None; // make sure we remove the don't destroy flag before calling destroy
                                DestroyImmediate(go);
                            }
                            else
                            {
                                DestroyImmediate(s_References[instanceId].reference);
                            }
                        }
                        s_References.Remove(instanceId);
                    }
                }
            }
        }

        internal override void OnHeaderControlsGUI()
        {
            if (target is Preset preset)
            {
                using (new EditorGUI.DisabledScope(targets.Length != 1 || !preset.GetPresetType().IsValidDefault()))
                {
                    var defaultList = Preset.GetDefaultPresetsForType(preset.GetPresetType()).Where(d => d.preset == preset);
                    if (defaultList.Any())
                    {
                        if (GUILayout.Button(GUIContent.Temp(string.Format(Style.removeFromDefault.text, ObjectNames.NicifyVariableName(preset.GetTargetTypeName())), Style.removeFromDefault.tooltip), EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                        {
                            Undo.RecordObject(Resources.FindObjectsOfTypeAll<PresetManager>().First(), "Preset Manager");
                            Preset.RemoveFromDefault(preset);
                            Undo.FlushUndoRecordObjects();
                        }
                    }
                    else
                    {
                        if (GUILayout.Button(GUIContent.Temp(string.Format(Style.addToDefault.text, ObjectNames.NicifyVariableName(preset.GetTargetTypeName())), Style.addToDefault.tooltip), EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                        {
                            Undo.RecordObject(Resources.FindObjectsOfTypeAll<PresetManager>().First(), "Preset Manager");
                            var list = Preset.GetDefaultPresetsForType(preset.GetPresetType()).ToList();
                            list.Insert(0, new DefaultPreset(string.Empty, preset));
                            Preset.SetDefaultPresetsForType(preset.GetPresetType(), list.ToArray());
                            Undo.FlushUndoRecordObjects();
                        }
                    }
                }
            }
        }

        internal override void OnHeaderTitleGUI(Rect titleRect, string header)
        {
            base.OnHeaderTitleGUI(titleRect, m_HeaderTitle);
        }

        [Flags]
        enum PropertyState
        {
            None = 0,
            Included = 1,
            Excluded = 1 << 1,
            Both = Included | Excluded
        }

        PropertyState GetPropertyState(string propertyPath, bool checkChildren = false)
        {
            PropertyState state = PropertyState.None;
            // we can have an early exit when state reach PropertyState.Both
            for (var index = 0; index < targets.Length && state != PropertyState.Both; index++)
            {
                Preset preset = (Preset)targets[index];
                if (checkChildren && preset.excludedProperties.Any(p => p.StartsWith(propertyPath + ".", StringComparison.Ordinal)))
                    return PropertyState.Both;
                if (preset.excludedProperties.Any(p => p == propertyPath || propertyPath.StartsWith(p + ".", StringComparison.Ordinal)))
                {
                    state |= PropertyState.Excluded;
                }
                else
                {
                    state |= PropertyState.Included;
                }
            }

            return state;
        }

        static void UpdatePrefabOverrideBarStyle(VisualElement blueBar, VisualElement container)
        {
            var element = (VisualElement)((object[])blueBar.userData)[0];

            if (container == null)
                return;

            // Move the bar to where the control is in the container.
            var top = element.worldBound.y - container.worldBound.y;
            if (float.IsNaN(top))     // If this is run before the container has been layed out.
                return;

            var elementHeight = element.resolvedStyle.height;

            // This is needed so if you have 2 overridden fields their blue
            // bars touch (and it looks like one long bar). They normally wouldn't
            // because most fields have a small margin.
            var bottomOffset = element.resolvedStyle.marginBottom;

            blueBar.style.top = top;
            blueBar.style.height = elementHeight + bottomOffset;
            blueBar.style.left = 0.0f;
        }

        void UpdatePropertyStyle(VisualElement element, SerializedProperty property)
        {
            if (!IsPropertyTracked(property))
                return;

            var propertyPath = property.propertyPath;
            m_BoundElements[propertyPath] = element;

            var state = GetPropertyState(propertyPath);
            if ((state & PropertyState.Excluded) == PropertyState.Excluded)
            {
                if (!element.ClassListContains(Style.excludedUssClassName))
                {
                    var container = element.GetFirstAncestorOfType<InspectorElement>();


                    element.AddToClassList(Style.excludedUssClassName);

                    if (container != null)
                    {
                        var barContainer = container.Q(Style.excludedBarContainerName);
                        if (barContainer == null)
                        {
                            barContainer = new VisualElement() { name = Style.excludedBarContainerName };
                            barContainer.style.position = Position.Absolute;
                            container.Add(barContainer);
                        }
                        // Ideally, this blue bar would be a child of the field and just move
                        // outside the field in absolute offsets to hug the side of the field's
                        // container. However, right now we need to have overflow:hidden on
                        // fields because of case 1105567 (the inputs can grow beyond the field).
                        // Therefore, we have to add the blue bars as children of the container
                        // and move them down beside their respective field.

                        var prefabOverrideBar = new VisualElement();
                        prefabOverrideBar.name = Style.excludedBarName;
                        prefabOverrideBar.userData = new object[] {element, element.enabledSelf};
                        prefabOverrideBar.AddToClassList(Style.excludedBarUssClassName);
                        barContainer.Add(prefabOverrideBar);

                        element.SetProperty(Style.excludedBarName, prefabOverrideBar);
                        element.SetEnabled(false);

                        // We need to try and set the bar style right away, even if the container
                        // didn't compute its layout yet. This is for when the override is done after
                        // everything has been layed out.
                        UpdatePrefabOverrideBarStyle(prefabOverrideBar, container);

                        // We intentionally re-register this event on the container per element and
                        // never unregister.
                        container.RegisterCallback<GeometryChangedEvent>(UpdatePrefabOverrideBarStyleEvent);
                    }
                }
            }
            else if (element.ClassListContains(Style.excludedUssClassName))
            {
                element.RemoveFromClassList(Style.excludedUssClassName);

                var container = element.GetFirstAncestorOfType<InspectorElement>();
                if (container != null)
                {
                    if (element.GetProperty(Style.excludedBarName) is VisualElement prefabOverrideBar)
                    {
                        element.SetEnabled((bool)((object[])prefabOverrideBar.userData)[1]);
                        prefabOverrideBar.RemoveFromHierarchy();
                    }
                }
            }
        }

        private static void UpdatePrefabOverrideBarStyleEvent(GeometryChangedEvent evt)
        {
            var container = evt.target as InspectorElement;
            if (container == null)
                return;

            var barContainer = container.Q(Style.excludedBarContainerName);
            if (barContainer == null)
                return;

            foreach (var bar in barContainer.Children())
                UpdatePrefabOverrideBarStyle(bar, container);
        }

        void BeginProperty(Rect totalPosition, SerializedProperty property)
        {
            if (!IsPropertyTracked(property))
                return;

            var propertyPath = property.propertyPath;
            var state = GetPropertyState(propertyPath);
            if ((state & PropertyState.Excluded) == PropertyState.Excluded && Event.current.type == EventType.Repaint)
                EditorGUI.DrawMarginLineForRect(totalPosition, new Color(240f / 255f, 81f / 255f, 60f / 255f));

            GUI.enabled &= (state & PropertyState.Included) == PropertyState.Included;
        }

        private bool IsPropertyTracked(SerializedProperty property)
        {
            if ((m_InternalEditor == null || property.serializedObject != m_InternalEditor.serializedObject) &&
                (m_CoupledEditor == null || property.serializedObject != m_CoupledEditor.coupledComponent))
                return false;
            return true;
        }

        void AddExclusion(object path)
        {
            var propertyPath = (string)path;
            serializedObject.ApplyModifiedProperties();
            Undo.RecordObjects(targets, "Inspector");

            var toAdd = new[] { propertyPath };
            foreach (var preset in targets.OfType<Preset>())
            {
                // set excludedProperties to an empty array for PropertyModifications to return all properties.
                var excluded = preset.excludedProperties;
                preset.excludedProperties = new string[0];

                // We need to calculate children for each selected Presets
                // because the list may differ with polymorphic serialization.
                var childrenProperties = GetPresetProperties(preset)
                    .Where(p => p.StartsWith(propertyPath + ".", StringComparison.Ordinal));
                preset.excludedProperties = excluded
                    .Except(childrenProperties)
                    .Concat(toAdd)
                    .ToArray();
            }

            serializedObject.Update();
        }

        void RemoveExclusion(object path)
        {
            var propertyPath = (string)path;
            serializedObject.ApplyModifiedProperties();
            Undo.RecordObjects(targets, "Inspector");

            foreach (var preset in targets.OfType<Preset>())
            {
                // set excludedProperties to an empty array for PropertyModifications to return all properties.
                var excluded = preset.excludedProperties;
                preset.excludedProperties = new string[0];

                // We need to calculate children for each selected Presets
                // because the list may differs with polymorphic serialization.
                var properties = GetPresetProperties(preset);

                var childrenAndSelf = properties
                    .Where(p => p.StartsWith(propertyPath + ".", StringComparison.Ordinal) || p == propertyPath);

                var count = excluded.Length;
                var removed = excluded.Except(childrenAndSelf);
                if (removed.Count() != count)
                {
                    // we found something to remove for exclusion, lets stop here.
                    preset.excludedProperties = removed.ToArray();
                }
                else
                {
                    // we need the first excluded parent and then exclude all its direct children expect the selected path.
                    string propPath = propertyPath;
                    while (!removed.Contains(propPath))
                    {
                        var index = propPath.LastIndexOf('.');
                        if (index == -1)
                        {
                            break;
                        }
                        propPath = propPath.Remove(index);
                    }
                    // in the case of multi selection, we may have nothing to do on this specific Preset.
                    if (!removed.Contains(propPath))
                        continue;

                    var pathDepth = propPath.Count(c => c == '.');
                    var toExclude = properties
                        .Where(p => p.StartsWith(propPath + ".", StringComparison.Ordinal)
                        && !p.StartsWith(propertyPath + ".", StringComparison.Ordinal)
                        && p != propertyPath
                        && p.Count(c => c == '.') == pathDepth + 1);

                    preset.excludedProperties = removed
                        .Except(new[] {propPath})
                        .Concat(toExclude)
                        .ToArray();
                }
            }

            serializedObject.Update();
        }

        static IEnumerable<string> GetPresetProperties(Preset preset)
        {
            // We have to use PropertyModifications instead of directly a SerializedProperty
            // because some properties may be excluded from Preset and we don't want them in excludedProperties
            return preset.PropertyModifications
                .Select(pm => pm.propertyPath)
                .SelectMany(SplitPropertyPath)
                .Distinct();
        }

        static IEnumerable<string> SplitPropertyPath(string path)
        {
            yield return path;
            var index = path.LastIndexOf('.');
            while (index != -1)
            {
                path = path.Substring(0, index);
                yield return path;
                index = path.LastIndexOf('.');
            }
        }

        void DisableEnableProperty(GenericMenu menu, SerializedProperty property)
        {
            if (!IsPropertyTracked(property))
                return;

            var propertyPath = property.propertyPath;
            var state = GetPropertyState(propertyPath, true);
            if ((state & PropertyState.Included) == PropertyState.Included)
            {
                menu.AddItem(Style.disableProperty, false, AddExclusion, propertyPath);
            }
            if ((state & PropertyState.Excluded) == PropertyState.Excluded)
            {
                menu.AddItem(Style.enableProperty, false, RemoveExclusion, propertyPath);
            }
        }

        void SetupUnsupportedInspector(Preset p)
        {
            m_NotSupportedEditorName = p.GetTargetTypeName();
            m_UnsupportedIcon = EditorGUIUtility.LoadIcon(m_NotSupportedEditorName.Replace('.', '/') + " Icon");
            if (m_UnsupportedIcon == null)
            {
                m_UnsupportedIcon = AssetPreview.GetMiniTypeThumbnailFromType((serializedObject.FindProperty("m_TargetType.m_ManagedTypePPtr").objectReferenceValue as MonoScript)?.GetClass());
                if (m_UnsupportedIcon == null)
                {
                    m_UnsupportedIcon = AssetPreview.GetMiniTypeThumbnailFromClassID(serializedObject.FindProperty("m_TargetType.m_NativeTypeID").intValue);
                    if (m_UnsupportedIcon == null)
                    {
                        m_UnsupportedIcon = AssetPreview.GetMiniTypeThumbnail(typeof(UnityObject));
                    }
                }
            }
        }

        void DrawUnsupportedInspector()
        {
            GUILayout.BeginHorizontal(EditorStyles.inspectorBig);
            GUILayout.Space(38);
            GUILayout.BeginVertical();
            GUILayout.Space(k_HeaderHeight);
            GUILayout.BeginHorizontal();
            EditorGUILayout.GetControlRect();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            Rect r = GUILayoutUtility.GetLastRect();

            // Icon
            Rect iconRect = new Rect(r.x + 6, r.y + 6, 32, 32);
            GUI.Label(iconRect, m_UnsupportedIcon, Style.centerStyle);

            // Title
            Rect titleRect = new Rect(r.x + 44, r.y + 6, r.width - 86, 16);
            GUI.Label(titleRect, m_NotSupportedEditorName, EditorStyles.largeLabel);

            EditorGUILayout.HelpBox("Preset Inspectors are not supported for this type.", MessageType.Info);
        }

        static string GetRootProperty(string propertyPath)
        {
            var split = propertyPath.IndexOf('.');
            if (split != -1)
                return propertyPath.Substring(0, split);
            return propertyPath;
        }

        [MenuItem("CONTEXT/Preset/Exclude all properties")]
        static void ExcludeAll(MenuCommand mc)
        {
            Undo.RecordObject(mc.context, "Inspector");
            var preset = mc.context as Preset;
            if (preset != null)
            {
                preset.excludedProperties = new string[0];
                preset.excludedProperties =
                    preset.PropertyModifications
                        .Select(p => GetRootProperty(p.propertyPath))
                        .Distinct()
                        .ToArray();
            }
        }

        [MenuItem("CONTEXT/Preset/Include all properties")]
        static void IncludeAll(MenuCommand mc)
        {
            Undo.RecordObject(mc.context, "Inspector");
            var preset = mc.context as Preset;
            if (preset != null)
            {
                preset.excludedProperties = new string[0];
            }
        }
    }
}
