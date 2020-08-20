// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [CustomEditor(typeof(PlayableDirector))]
    [CanEditMultipleObjects]
    internal class DirectorEditor : Editor
    {
        private static class Styles
        {
            public static readonly GUIContent PlayableText = EditorGUIUtility.TrTextContent("Playable");
            public static readonly GUIContent InitialTimeContent = EditorGUIUtility.TrTextContent("Initial Time", "The time at which the Playable will begin playing");
            public static readonly GUIContent TimeContent = EditorGUIUtility.TrTextContent("Current Time", "The current Playable time");
            public static readonly GUIContent InitialStateContent = EditorGUIUtility.TrTextContent("Play On Awake", "Whether the Playable should be playing after it loads");
            public static readonly GUIContent UpdateMethod = EditorGUIUtility.TrTextContent("Update Method", "Controls how the Playable updates every frame");
            public static readonly GUIContent WrapModeContent = EditorGUIUtility.TrTextContent("Wrap Mode", "Controls the behaviour of evaluating the Playable outside its duration");
            public static readonly GUIContent NoBindingsContent = EditorGUIUtility.TrTextContent("This channel will not playback because it is not currently assigned");
            public static readonly GUIContent BindingsTitleContent = EditorGUIUtility.TrTextContent("Bindings");
            public static readonly GUIContent ClearUnused = EditorGUIUtility.TrTextContent("Show Unused", "A PlayableDirector may contain bindings to objects not referenced by the assigned Playable file.\nToggle this field to show them.\n It is recommended to remove unused bound objects if their Playable will be no longer used by this PlayableDirector.");
        }

        private static readonly int ObjectFieldControlID = "s_ObjectFieldHash".GetHashCode();
        private const int BindingHeaderPadding = 4;
        private const float UnusedItemBackGroundScale = 0.92f;
        private const float UnusedItemColorScale = 0.70f;


        private SerializedProperty m_PlayableAsset;
        private SerializedProperty m_InitialState;
        private SerializedProperty m_WrapMode;
        private SerializedProperty m_InitialTime;
        private SerializedProperty m_UpdateMethod;
        private SerializedProperty m_SceneBindings;

        private Texture    m_DefaultScriptContentTexture;

        private GUIContent m_BindingContent = new GUIContent();

        private struct BindingItem
        {
            public PlayableBinding binding;
            public SerializedProperty property;
            public string AssetPath;
            public bool IsMainAsset;
            public PlayableAsset masterAsset;
            public int propertyIndex;
        }

        private List<BindingItem> m_BindingItems = new List<BindingItem>();
        private PlayableBinding[] m_SynchedPlayableBindings = null;

        private ReorderableList m_BindingList;


        bool showUnused
        {
            get { return EditorPrefs.GetBool("PlayableDirector.ShowUnused", true); }
            set { EditorPrefs.SetBool("PlayableDirector.ShowUnused", value); }
        }

        bool hasUnused { get; set; }


        public void OnEnable()
        {
            m_PlayableAsset = serializedObject.FindProperty("m_PlayableAsset");
            m_InitialState = serializedObject.FindProperty("m_InitialState");
            m_WrapMode = serializedObject.FindProperty("m_WrapMode");
            m_UpdateMethod = serializedObject.FindProperty("m_DirectorUpdateMode");
            m_InitialTime = serializedObject.FindProperty("m_InitialTime");
            m_SceneBindings = serializedObject.FindProperty("m_SceneBindings");

            m_DefaultScriptContentTexture = EditorGUIUtility.FindTexture(typeof(ScriptableObject));

            m_BindingList = new ReorderableList(m_BindingItems, typeof(BindingItem), false, false, false, true);
            m_BindingList.drawElementCallback = BindingDrawCallback;
            m_BindingList.onCanRemoveCallback = BindingCanRemoveCallback;
            m_BindingList.onRemoveCallback = BindingOnRemove;
            m_BindingList.onSelectCallback = BindingOnSelect;
            m_BindingList.elementHeightCallback = BindingElementHeight;
        }

        public override void OnInspectorGUI()
        {
            if (PlayableAssetOutputsChanged() || m_BindingItems.Count != m_SceneBindings.arraySize)
                SynchronizeSceneBindings();

            serializedObject.Update();

            if (PropertyFieldAsObject(m_PlayableAsset, Styles.PlayableText, typeof(PlayableAsset)))
            {
                serializedObject.ApplyModifiedProperties();
                SynchronizeSceneBindings();

                // some editors (like Timeline) needs to repaint when the playable asset changes
                InternalEditorUtility.RepaintAllViews();
            }

            EditorGUILayout.PropertyField(m_UpdateMethod, Styles.UpdateMethod);

            var rect = EditorGUILayout.GetControlRect(true);
            var label = EditorGUI.BeginProperty(rect, Styles.InitialStateContent, m_InitialState);
            bool playOnAwake = m_InitialState.enumValueIndex != (int)PlayState.Paused;
            EditorGUI.BeginChangeCheck();
            playOnAwake = EditorGUI.Toggle(rect, label, playOnAwake);
            if (EditorGUI.EndChangeCheck())
            {
                m_InitialState.enumValueIndex = (int)(playOnAwake ? PlayState.Playing : PlayState.Paused);
            }
            EditorGUI.EndProperty();

            EditorGUILayout.PropertyField(m_WrapMode, Styles.WrapModeContent);

            PropertyFieldAsFloat(m_InitialTime, Styles.InitialTimeContent);

            if (Application.isPlaying)
            {
                // time field isn't functional in Editor, unless the sequencer window is open,
                // at which point it isn't required. In playmode it can provide valuable feedback,
                // and allow the user to scrub and debug without the sequencer window
                CurrentTimeField();
            }

            if (targets.Length == 1)
                DoDirectorBindingInspector();

            if (serializedObject.ApplyModifiedProperties())
                SynchronizeSceneBindings();
        }

        private bool PlayableAssetOutputsChanged()
        {
            var playableAsset = m_PlayableAsset.objectReferenceValue as PlayableAsset;

            if (m_SynchedPlayableBindings == null)
                return playableAsset != null;

            if (playableAsset == null)
                return true;

            if (playableAsset.outputs.Count() != m_SynchedPlayableBindings.Length)
                return true;

            return playableAsset.outputs.Where((t, i) => t.sourceObject != m_SynchedPlayableBindings[i].sourceObject).Any();
        }

        GUIContent GetContentForOutput(PlayableBinding binding, UnityEngine.Object source)
        {
            m_BindingContent.text = binding.streamName;
            m_BindingContent.tooltip = (source == null) ? Styles.NoBindingsContent.text : string.Empty;
            m_BindingContent.image = AssetPreview.GetMiniTypeThumbnail(binding.outputTargetType) ?? m_DefaultScriptContentTexture;
            return m_BindingContent;
        }

        private void DoDirectorBindingInspector()
        {
            EditorGUILayout.BeginHorizontal();

            var rect = GUILayoutUtility.GetRect(EditorGUIUtility.fieldWidth, EditorGUIUtility.fieldWidth, EditorGUI.kSingleLineHeight, EditorGUI.kSingleLineHeight,  EditorStyles.foldout);
            EditorGUI.BeginProperty(rect, GUIContent.none, m_SceneBindings);
            m_SceneBindings.isExpanded = EditorGUI.Foldout(rect, m_SceneBindings.isExpanded, Styles.BindingsTitleContent, true);
            EditorGUI.EndProperty();

            if (hasUnused)
            {
                const int rightEdgePad = 2;
                GUILayout.FlexibleSpace();
                EditorGUI.BeginChangeCheck();
                var size = EditorStyles.toggle.CalcSize(Styles.ClearUnused).x + rightEdgePad;
                showUnused = EditorGUILayout.ToggleLeft(Styles.ClearUnused, showUnused, GUILayout.Width(size));
                if (EditorGUI.EndChangeCheck())
                    SynchronizeSceneBindings();
            }
            EditorGUILayout.EndHorizontal();

            if (m_SceneBindings.isExpanded)
            {
                EditorGUI.indentLevel++;
                m_BindingList.displayRemove = showUnused;
                m_BindingList.DoLayoutList();
                EditorGUI.indentLevel--;
            }
        }

        PlayableBinding FindBinding(PlayableAsset source, UnityEngine.Object key)
        {
            if (source == null || key == null)
                return default(PlayableBinding);

            return source.outputs.FirstOrDefault(a => a.sourceObject == key);
        }

        void SynchronizeSceneBindings()
        {
            if (targets.Length > 1)
                return;

            var director = (PlayableDirector)target;
            var playableAsset = m_PlayableAsset.objectReferenceValue as PlayableAsset;

            hasUnused = false;
            m_BindingItems.Clear();
            UpdatePlayableBindingsIfRequired(playableAsset, director);

            var mainAssetPath = AssetDatabase.GetAssetPath(director.playableAsset);
            for (int i = 0; i < m_SceneBindings.arraySize; ++i)
            {
                var property = m_SceneBindings.GetArrayElementAtIndex(i);
                var keyObject = property.FindPropertyRelative("key").objectReferenceValue;

                // Don't show completely null keys.
                if (((object)keyObject) == null)
                    continue;

                var assetPath = AssetDatabase.GetAssetPath(keyObject);
                var cacheValue = new BindingItem()
                {
                    property = property,
                    AssetPath = assetPath,
                    IsMainAsset = !string.IsNullOrEmpty(assetPath) && mainAssetPath == assetPath,
                    masterAsset = !string.IsNullOrEmpty(assetPath) ? AssetDatabase.LoadMainAssetAtPath(assetPath) as PlayableAsset : null,
                    propertyIndex = i,
                };
                cacheValue.binding = FindBinding(cacheValue.masterAsset, keyObject);

                hasUnused |= !cacheValue.IsMainAsset;

                if (showUnused || cacheValue.IsMainAsset)
                    m_BindingItems.Add(cacheValue);
            }

            m_BindingItems.Sort((a, b) =>
            {
                if (a.IsMainAsset == b.IsMainAsset)
                    return -string.CompareOrdinal(a.AssetPath, b.AssetPath);
                if (a.IsMainAsset)
                    return -1;
                return 1;
            }
            );

            if (showUnused)
            {
                bool addHeader = false;
                for (int i = 0; i < m_BindingItems.Count - 1; i++)
                {
                    if (m_BindingItems[i].masterAsset != m_BindingItems[i + 1].masterAsset)
                    {
                        m_BindingItems.Insert(i + 1, new BindingItem()
                            { masterAsset = m_BindingItems[i + 1].masterAsset, propertyIndex = -1}
                        );
                        addHeader = true;
                    }
                }

                if (addHeader && m_BindingItems.Count > 0)
                {
                    m_BindingItems.Insert(0, new BindingItem()
                    {
                        masterAsset = m_BindingItems[0].masterAsset,
                        propertyIndex = -1,
                        IsMainAsset = m_BindingItems[0].masterAsset == director.playableAsset,
                    }
                    );
                }
            }
        }

        private void UpdatePlayableBindingsIfRequired(PlayableAsset playableAsset, PlayableDirector director)
        {
            m_SynchedPlayableBindings = new PlayableBinding[0];

            if (playableAsset != null)
            {
                var bindings = playableAsset.outputs;
                m_SynchedPlayableBindings = bindings.ToArray();
            }

            foreach (var binding in m_SynchedPlayableBindings)
            {
                // don't add bindings without a specific target type, clear previously bound objects
                // This can happen with timeline tracks that do not have a specific binding.
                if (binding.outputTargetType == null)
                    director.ClearGenericBinding(binding.sourceObject);
                else if (!director.HasGenericBinding(binding.sourceObject))
                    director.SetGenericBinding(binding.sourceObject, null);
            }

            serializedObject.Update();
        }

        // To show the current time field in play mode
        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        private static void PropertyFieldAsFloat(SerializedProperty property, GUIContent title)
        {
            Rect rect = EditorGUILayout.GetControlRect();
            title = EditorGUI.BeginProperty(rect, title, property);
            EditorGUI.BeginChangeCheck();
            float newValue = EditorGUI.FloatField(rect, title, (float)property.doubleValue);
            if (EditorGUI.EndChangeCheck())
            {
                property.doubleValue = newValue;
            }
            EditorGUI.EndProperty();
        }

        private static bool PropertyFieldAsObject(SerializedProperty property, GUIContent title, Type objType)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.ObjectField(property, objType, title);
            return EditorGUI.EndChangeCheck();
        }

        private static bool PropertyFieldAsObject(Rect rect, SerializedProperty property, GUIContent title, Type objType)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.ObjectField(rect, property, objType, title);
            return EditorGUI.EndChangeCheck();
        }

        // Does not use Properties because time is not a serialized property
        private void CurrentTimeField()
        {
            if (targets.Length == 1)
            {
                var director = (PlayableDirector)target;
                EditorGUI.BeginChangeCheck();
                float t = EditorGUILayout.FloatField(Styles.TimeContent, (float)director.time);
                if (EditorGUI.EndChangeCheck())
                {
                    director.time = t;
                }
            }
            else
            {
                EditorGUILayout.TextField(Styles.TimeContent, EditorGUI.mixedValueContent.text);
            }
        }

        private void BindingDrawCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            // apply a subtle darkening to unused items
            var guiColor = GUI.color;
            var backGroundColor = GUI.backgroundColor;

            if (!m_BindingItems[index].IsMainAsset)
            {
                if (EditorGUIUtility.isProSkin)
                    GUI.color = GUI.color.RGBMultiplied(UnusedItemColorScale);
                else
                    GUI.backgroundColor = GUI.backgroundColor.RGBMultiplied(UnusedItemBackGroundScale);
            }

            // header item
            if (m_BindingItems[index].property == null)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    rect.yMin += BindingHeaderPadding * 0.5f;
                    rect.height -= BindingHeaderPadding * 0.5f;

                    var content = EditorGUIUtility.ObjectContent(m_BindingItems[index].masterAsset, typeof(PlayableAsset));
                    EditorStyles.objectField.Draw(rect,
                        content,
                        ObjectFieldControlID,
                        false,
                        rect.Contains(Event.current.mousePosition)
                    );
                }
            }
            else
            {
                var binding = m_BindingItems[index].binding;
                var key = m_BindingItems[index].property.FindPropertyRelative("key");
                var value = m_BindingItems[index].property.FindPropertyRelative("value");
                var type = m_BindingItems[index].binding.outputTargetType;

                // don't permit assignment if we don't how to assign it
                using (new EditorGUI.DisabledScope(type == null))
                    PropertyFieldAsObject(ItemRect(rect), value, GetContentForOutput(binding, key.objectReferenceValue), type ?? typeof(Object));
            }

            GUI.backgroundColor = backGroundColor;
            GUI.color = guiColor;
        }

        private Rect ItemRect(Rect rect)
        {
            if (hasUnused && showUnused)
            {
                EditorGUI.indentLevel++;
                rect = EditorGUI.IndentedRect(rect);
                EditorGUI.indentLevel--;
            }

            return rect;
        }

        private bool BindingCanRemoveCallback(ReorderableList list)
        {
            if (!showUnused)
                return false;

            if (list.index < 0 || list.index >= m_BindingItems.Count)
                return false;

            return !m_BindingItems[list.index].IsMainAsset;
        }

        private void BindingOnRemove(ReorderableList list)
        {
            if (list.index >= 0 && list.index < m_BindingItems.Count)
            {
                var bindingProperty = m_BindingItems[list.index].property;
                // group header, remove all from this timeline
                if (bindingProperty == null)
                {
                    var path = AssetDatabase.GetAssetPath(m_BindingItems[list.index].masterAsset);
                    int size = m_SceneBindings.arraySize - 1;
                    for (int i = size; i >= 0; i--)
                    {
                        var prop = m_SceneBindings.GetArrayElementAtIndex(i).FindPropertyRelative("key").objectReferenceValue;
                        if (AssetDatabase.GetAssetPath(prop) == path)
                            m_SceneBindings.DeleteArrayElementAtIndex(i);
                    }
                }
                else
                {
                    m_SceneBindings.DeleteArrayElementAtIndex(m_BindingItems[list.index].propertyIndex);
                }
            }
        }

        private void BindingOnSelect(ReorderableList list)
        {
            if (list.index < 0 || list.index >= m_BindingItems.Count)
                return;

            if (m_BindingItems[list.index].masterAsset)
                EditorGUIUtility.PingObject(m_BindingItems[list.index].masterAsset);
            else
                EditorGUIUtility.PingObject(m_BindingItems[list.index].property.FindPropertyRelative("key").objectReferenceValue);
        }

        private float BindingElementHeight(int index)
        {
            if (m_BindingItems[index].property == null)
                return m_BindingList.elementHeight + DirectorEditor.BindingHeaderPadding;
            return m_BindingList.elementHeight;
        }
    }
}
