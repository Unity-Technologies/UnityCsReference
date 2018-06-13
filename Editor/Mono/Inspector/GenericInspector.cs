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

        private AudioFilterGUI m_AudioFilterGUI;
        private OptimizedGUIBlock m_OptimizedBlock;

        private float m_LastHeight;
        private OptimizedGUIBlock m_LastOptimizedBlock;
        private OptimizedBlockState m_OptimizedBlockState = OptimizedBlockState.CheckOptimizedBlock;

        internal override bool GetOptimizedGUIBlock(bool isDirty, bool isVisible, out OptimizedGUIBlock block, out float height)
        {
            block = null; height = -1;

            // Don't use optimizedGUI for audio filters
            var behaviour = target as MonoBehaviour;
            if (behaviour != null && AudioUtil.HasAudioCallback(behaviour) && AudioUtil.GetCustomFilterChannelCount(behaviour) > 0)
                return false;

            if (IsMissingMonoBehaviourTarget())
                return false;

            if (isDirty && m_OptimizedBlock != null)
                ResetOptimizedBlock();

            if (!isVisible)
            {
                if (m_OptimizedBlock == null)
                    m_OptimizedBlock = new OptimizedGUIBlock();

                height = 0;
                block = m_OptimizedBlock;
                return true;
            }

            // Return cached result if any.
            if (m_OptimizedBlockState != OptimizedBlockState.CheckOptimizedBlock)
            {
                if (m_OptimizedBlockState == OptimizedBlockState.NoOptimizedBlock)
                    return false;
                height = m_LastHeight;
                block = m_LastOptimizedBlock;
                return true;
            }

            // Update serialized object representation
            if (m_SerializedObject == null)
                m_SerializedObject = new SerializedObject(targets, m_Context) {inspectorMode = m_InspectorMode};
            else
            {
                m_SerializedObject.Update();
                m_SerializedObject.inspectorMode = m_InspectorMode;
            }

            height = 0;
            SerializedProperty property = m_SerializedObject.GetIterator();
            while (property.NextVisible(height <= 0))
            {
                var handler = ScriptAttributeUtility.GetHandler(property);
                if (!handler.CanCacheInspectorGUI(property))
                    return ResetOptimizedBlock(OptimizedBlockState.NoOptimizedBlock);

                // Allocate height for control plus spacing below it
                height += handler.GetHeight(property, null, true) + EditorGUI.kControlVerticalSpacing;
            }

            // Allocate height for spacing above first control
            if (height > 0)
                height += EditorGUI.kControlVerticalSpacing;

            if (m_OptimizedBlock == null)
                m_OptimizedBlock = new OptimizedGUIBlock();

            m_LastHeight = height;
            m_LastOptimizedBlock = block = m_OptimizedBlock;
            m_OptimizedBlockState = OptimizedBlockState.HasOptimizedBlock;
            return true;
        }

        internal override bool OnOptimizedInspectorGUI(Rect contentRect)
        {
            bool childrenAreExpanded = true;
            bool wasEnabled = GUI.enabled;
            var visibleRect = GUIClip.visibleRect;
            var contentOffset = contentRect.y;

            contentRect.xMin += InspectorWindow.kInspectorPaddingLeft;
            contentRect.xMax -= InspectorWindow.kInspectorPaddingRight;
            contentRect.y += EditorGUI.kControlVerticalSpacing;

            var property = m_SerializedObject.GetIterator();
            var isInspectorModeNormal = m_InspectorMode == InspectorMode.Normal;
            while (property.NextVisible(childrenAreExpanded))
            {
                var handler = ScriptAttributeUtility.GetHandler(property);
                contentRect.height = handler.GetHeight(property, null, false);

                if (contentRect.Overlaps(visibleRect))
                {
                    EditorGUI.indentLevel = property.depth;
                    using (new EditorGUI.DisabledScope(isInspectorModeNormal && "m_Script" == property.propertyPath))
                        childrenAreExpanded = handler.OnGUI(contentRect, property, null, false, visibleRect);
                }
                else
                    childrenAreExpanded = property.isExpanded;

                contentRect.y += contentRect.height + EditorGUI.kControlVerticalSpacing;
            }

            m_LastHeight = contentRect.y - contentOffset;
            GUI.enabled = wasEnabled;
            return m_SerializedObject.ApplyModifiedProperties();
        }

        public bool MissingMonoBehaviourGUI()
        {
            serializedObject.Update();
            SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
            if (scriptProperty == null)
                return false;

            EditorGUILayout.PropertyField(scriptProperty);

            MonoScript targetScript = scriptProperty.objectReferenceValue as MonoScript;
            bool showScriptWarning = targetScript == null || !targetScript.GetScriptTypeWasJustCreatedFromComponentMenu();
            if (showScriptWarning)
            {
                var text = L10n.Tr("The associated script can not be loaded.\nPlease fix any compile errors\nand assign a valid script.");
                EditorGUILayout.HelpBox(text, MessageType.Warning, true);
            }

            if (serializedObject.ApplyModifiedProperties())
                EditorUtility.ForceRebuildInspectors();

            return true;
        }

        private bool ResetOptimizedBlock(OptimizedBlockState resetState = OptimizedBlockState.CheckOptimizedBlock)
        {
            if (m_OptimizedBlock != null)
            {
                m_OptimizedBlock.Dispose();
                m_OptimizedBlock = null;
            }

            m_LastHeight = -1;
            m_LastOptimizedBlock = null;
            m_OptimizedBlockState = resetState;
            return m_OptimizedBlockState == OptimizedBlockState.HasOptimizedBlock;
        }

        internal void OnDisableINTERNAL()
        {
            ResetOptimizedBlock();
            CleanupPropertyEditor();
        }

        internal bool IsMissingMonoBehaviourTarget()
        {
            if (target) // This test for native reference state first.
                return target.GetType() == typeof(MonoBehaviour) || target.GetType() == typeof(ScriptableObject);
            return target is MonoBehaviour || target is ScriptableObject;
        }

        public override void OnInspectorGUI()
        {
            if (IsMissingMonoBehaviourTarget() && MissingMonoBehaviourGUI())
                return;

            base.OnInspectorGUI();

            var behaviour = target as MonoBehaviour;
            if (behaviour != null)
            {
                // Does this have a AudioRead callback?
                if (AudioUtil.HasAudioCallback(behaviour) && AudioUtil.GetCustomFilterChannelCount(behaviour) > 0)
                {
                    if (m_AudioFilterGUI == null)
                        m_AudioFilterGUI = new AudioFilterGUI();
                    m_AudioFilterGUI.DrawAudioFilterGUI(behaviour);
                }
            }
        }
    }
}
