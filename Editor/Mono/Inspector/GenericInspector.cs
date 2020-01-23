// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    internal class GenericInspector : Editor
    {
        private enum OptimizedBlockState
        {
            CheckOptimizedBlock,
            HasOptimizedBlock,
            NoOptimizedBlock
        }

        private float m_LastHeight;
        private Rect m_LastVisibleRect;
        private OptimizedBlockState m_OptimizedBlockState = OptimizedBlockState.CheckOptimizedBlock;

        internal override bool GetOptimizedGUIBlock(bool isDirty, bool isVisible, out float height)
        {
            height = -1;

            // Don't use optimizedGUI for audio filters
            var behaviour = target as MonoBehaviour;
            if (behaviour != null && AudioUtil.HasAudioCallback(behaviour) && AudioUtil.GetCustomFilterChannelCount(behaviour) > 0)
                return false;

            if (ObjectIsMonoBehaviourOrScriptableObject(target))
                return false;

            if (isDirty)
                ResetOptimizedBlock();

            if (!isVisible)
            {
                height = 0;
                return true;
            }

            // Return cached result if any.
            if (m_OptimizedBlockState != OptimizedBlockState.CheckOptimizedBlock)
            {
                if (m_OptimizedBlockState == OptimizedBlockState.NoOptimizedBlock)
                    return false;
                height = m_LastHeight;
                return true;
            }

            // Update serialized object representation
            if (m_SerializedObject == null)
                m_SerializedObject = new SerializedObject(targets, m_Context) {inspectorMode = inspectorMode};
            else
            {
                m_SerializedObject.Update();
                m_SerializedObject.inspectorMode = inspectorMode;
            }

            height = 0;
            SerializedProperty property = m_SerializedObject.GetIterator();
            bool childrenAreExpanded = true;
            while (property.NextVisible(childrenAreExpanded))
            {
                var handler = ScriptAttributeUtility.GetHandler(property);
                var hasPropertyDrawer = handler.propertyDrawer != null;
                height += handler.GetHeight(property, null, hasPropertyDrawer) + EditorGUI.kControlVerticalSpacing;
                childrenAreExpanded = !hasPropertyDrawer && property.isExpanded && EditorGUI.HasVisibleChildFields(property);
            }

            m_LastHeight = height;
            m_OptimizedBlockState = OptimizedBlockState.HasOptimizedBlock;

            return true;
        }

        internal override bool OnOptimizedInspectorGUI(Rect contentRect)
        {
            m_SerializedObject.UpdateIfRequiredOrScript();

            bool childrenAreExpanded = true;
            bool wasEnabled = GUI.enabled;
            var visibleRect = GUIClip.visibleRect;
            var contentOffset = contentRect.y;
            if (Event.current.type != EventType.Repaint)
                visibleRect = m_LastVisibleRect;

            // Release keyboard focus before scrolling so that the virtual scrolling focus wrong control.
            if (Event.current.type == EventType.ScrollWheel)
                GUIUtility.keyboardControl = 0;

            var behaviour = target as MonoBehaviour;
            var property = m_SerializedObject.GetIterator();
            var isInspectorModeNormal = inspectorMode == InspectorMode.Normal;

            using (new LocalizationGroup(behaviour))
            {
                while (property.NextVisible(childrenAreExpanded))
                {
                    var handler = ScriptAttributeUtility.GetHandler(property);
                    var hasPropertyDrawer = handler.propertyDrawer != null;
                    childrenAreExpanded = !hasPropertyDrawer && property.isExpanded && EditorGUI.HasVisibleChildFields(property);
                    contentRect.height = handler.GetHeight(property, null, hasPropertyDrawer);

                    if (contentRect.Overlaps(visibleRect))
                    {
                        EditorGUI.indentLevel = property.depth;
                        using (new EditorGUI.DisabledScope(isInspectorModeNormal && "m_Script" == property.propertyPath))
                            childrenAreExpanded &= handler.OnGUI(contentRect, property, null, false, visibleRect);
                    }

                    contentRect.y += contentRect.height + EditorGUI.kControlVerticalSpacing;
                }
            }

            // Fix new height
            if (Event.current.type == EventType.Repaint)
            {
                m_LastVisibleRect = visibleRect;
                var newHeight = contentRect.y - contentOffset;
                if (newHeight != m_LastHeight)
                {
                    m_LastHeight = contentRect.y - contentOffset;
                    Repaint();
                }
            }

            GUI.enabled = wasEnabled;
            return m_SerializedObject.ApplyModifiedProperties();
        }

        public bool MissingMonoBehaviourGUI()
        {
            serializedObject.Update();
            SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
            if (scriptProperty == null)
                return false;

            bool scriptLoaded = CheckIfScriptLoaded(scriptProperty);
            bool oldGUIEnabled = GUI.enabled;
            if (!GUI.enabled && !scriptLoaded)
            {
                GUI.enabled = true;
            }

            EditorGUILayout.PropertyField(scriptProperty);

            if (!scriptLoaded)
            {
                ShowScriptNotLoadedWarning();
            }

            GUI.enabled = oldGUIEnabled;

            if (serializedObject.ApplyModifiedProperties())
                EditorUtility.ForceRebuildInspectors();

            return true;
        }

        private static bool CheckIfScriptLoaded(SerializedProperty scriptProperty)
        {
            MonoScript targetScript = scriptProperty?.objectReferenceValue as MonoScript;
            return targetScript != null && targetScript.GetScriptTypeWasJustCreatedFromComponentMenu();
        }

        private static void ShowScriptNotLoadedWarning()
        {
            var text = L10n.Tr(
                "The associated script can not be loaded.\nPlease fix any compile errors\nand assign a valid script.");
            EditorGUILayout.HelpBox(text, MessageType.Warning, true);
        }

        internal static void ShowScriptNotLoadedWarning(SerializedProperty scriptProperty)
        {
            bool scriptLoaded = CheckIfScriptLoaded(scriptProperty);
            if (!scriptLoaded)
            {
                ShowScriptNotLoadedWarning();
            }
        }

        private void ResetOptimizedBlock(OptimizedBlockState resetState = OptimizedBlockState.CheckOptimizedBlock)
        {
            m_LastHeight = -1;
            m_OptimizedBlockState = resetState;
        }

        internal void OnDisableINTERNAL()
        {
            ResetOptimizedBlock();
            CleanupPropertyEditor();
        }

        internal static bool ObjectIsMonoBehaviourOrScriptableObject(Object obj)
        {
            if (obj) // This test for native reference state first.
            {
                return obj.GetType() == typeof(MonoBehaviour) || obj.GetType() == typeof(ScriptableObject);
            }
            return obj is MonoBehaviour || obj is ScriptableObject;
        }

        public override void OnInspectorGUI()
        {
            if (ObjectIsMonoBehaviourOrScriptableObject(target) && MissingMonoBehaviourGUI())
                return;

            base.OnInspectorGUI();
        }
    }
}
