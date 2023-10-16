// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

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

        static class Styles
        {
            public static string missingScriptMessage = L10n.Tr("The associated script can not be loaded.\nPlease fix any compile errors\nand assign a valid script.");
            public static string missingSerializeReferenceInstanceMessage = L10n.Tr("This object contains SerializeReference types which are missing.\nFor more information see SerializationUtility.HasManagedReferencesWithMissingTypes.");
            public static string missingScriptMessageForPrefabInstance = L10n.Tr("The associated script can not be loaded.\nPlease fix any compile errors\nand open Prefab Mode and assign a valid script to the Prefab Asset.");
        }

        internal static string GetMissingSerializeRefererenceMessageContainer()
        {
            return Styles.missingSerializeReferenceInstanceMessage;
        }

        internal override bool GetOptimizedGUIBlock(bool isDirty, bool isVisible, out float height)
        {
            height = -1;

            // Don't use optimizedGUI for audio filters
            var behaviour = target as MonoBehaviour;
            if (behaviour != null && AudioUtil.HasAudioCallback(behaviour) && AudioUtil.GetCustomFilterChannelCount(behaviour) > 0)
                return false;

            if (ObjectIsMonoBehaviourOrScriptableObjectWithoutScript(target))
                return false;

            var scriptableObject = target as ScriptableObject;
            if ((behaviour != null || scriptableObject != null) && SerializationUtility.HasManagedReferencesWithMissingTypes(target))
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
            {
                m_SerializedObject = new SerializedObject(targets, m_Context)
                {
                    inspectorMode = inspectorMode,
                    inspectorDataMode = dataMode
                };
            }
            else
            {
                m_SerializedObject.Update();
                m_SerializedObject.inspectorMode = inspectorMode;
                if (m_SerializedObject.inspectorDataMode != dataMode)
                    m_SerializedObject.inspectorDataMode = dataMode;
            }

            height = 0;
            SerializedProperty property = m_SerializedObject.GetIterator();
            bool childrenAreExpanded = true;
            while (property.NextVisible(childrenAreExpanded))
            {
                var handler = ScriptAttributeUtility.GetHandler(property);
                var hasPropertyDrawer = handler.propertyDrawer != null;
                var propertyHeight = handler.GetHeight(property, null, hasPropertyDrawer || PropertyHandler.UseReorderabelListControl(property));
                if (propertyHeight > 0)
                    height += propertyHeight + EditorGUI.kControlVerticalSpacing;
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

            // In some specific cases (e.g. when the inspector field has a dropdown behavior - case 1335344) we need to
            // apply the padding values so it behaves properly. By checking that xMin is zero when we do the assignments,
            // we avoid applying the padding more than once (because this is called more than once in some cases and
            // can lead to wrong indentation - case 1114055).
            if (contentRect.xMin == 0)
            {
                contentRect.xMin = EditorStyles.kInspectorPaddingLeft;
                contentRect.xMax -= EditorStyles.kInspectorPaddingRight;
            }

            if (Event.current.type != EventType.Repaint)
                visibleRect = m_LastVisibleRect;

            // Release keyboard focus before scrolling so that the virtual scrolling focus wrong control.
            if (Event.current.type == EventType.ScrollWheel)
                GUIUtility.keyboardControl = 0;

            var behaviour = target as MonoBehaviour;
            var property = m_SerializedObject.GetIterator();
            bool isInspectorModeNormal = inspectorMode == InspectorMode.Normal;
            bool isInPrefabInstance = PrefabUtility.GetPrefabInstanceHandle(behaviour) != null;
            bool isMultiSelection = m_SerializedObject.targetObjectsCount > 1;

            using (new LocalizationGroup(behaviour))
            {
                while (property.NextVisible(childrenAreExpanded))
                {
                    if (GUI.isInsideList && property.depth <= EditorGUI.GetInsideListDepth())
                        EditorGUI.EndIsInsideList();

                    if (property.isArray)
                        EditorGUI.BeginIsInsideList(property.depth);

                    var handler = ScriptAttributeUtility.GetHandler(property);
                    var hasPropertyDrawer = handler.propertyDrawer != null;
                    childrenAreExpanded = !hasPropertyDrawer && property.isExpanded && EditorGUI.HasVisibleChildFields(property);
                    contentRect.height = handler.GetHeight(property, null, hasPropertyDrawer || PropertyHandler.UseReorderabelListControl(property));

                    if (contentRect.Overlaps(visibleRect))
                    {
                        EditorGUI.indentLevel = property.depth;
                        using (new EditorGUI.DisabledScope((isInspectorModeNormal || isInPrefabInstance || isMultiSelection) && string.Equals("m_Script", property.propertyPath, System.StringComparison.Ordinal)))
                            childrenAreExpanded &= handler.OnGUI(contentRect, property, GetPropertyLabel(property), PropertyHandler.UseReorderabelListControl(property), visibleRect);
                    }

                    if (contentRect.height > 0)
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

        GUIContent GetPropertyLabel(SerializedProperty property)
        {
            var isInspectorModeNormal = inspectorMode == InspectorMode.Normal;
            if (isInspectorModeNormal)
                return null;

            return GUIContent.Temp(property.displayName, $"{property.tooltip}\n{property.propertyPath} ({property.propertyType})".Trim());
        }

        internal static bool IsAnyMonoBehaviourTargetPartOfPrefabInstance(Editor editor)
        {
            if ((editor.target is MonoBehaviour) == false)
                return false;

            foreach (var t in editor.targets)
            {
                var instanceID = t.GetInstanceID();
                if (PrefabUtility.IsInstanceIDPartOfNonAssetPrefabInstance(instanceID))
                    return true;
            }

            return false;
        }

        public bool MissingMonoBehaviourGUI()
        {
            serializedObject.Update();
            SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
            if (scriptProperty == null)
                return false;

            bool scriptLoaded = CheckIfScriptLoaded(scriptProperty);

            bool oldGUIEnabled = GUI.enabled;

            if (!scriptLoaded)
            {
                GUI.enabled = IsAnyMonoBehaviourTargetPartOfPrefabInstance(this) == false; // We don't support changing script as an override on Prefab Instances (case 1255454)
            }

            EditorGUILayout.PropertyField(scriptProperty);
            if (!scriptLoaded)
            {
                GUI.enabled = true;
                // if script is not loaded, this might also be because
                // the asset is tied to an EditorClassIdentifier rather than a MonoScript
                // (it happens when multiple types are defined in a C# script)
                switch (ShowTypeFixup())
                {
                    case ShowTypeFixupResult.CantFindCandidate:
                        ShowScriptNotLoadedWarning(IsAnyMonoBehaviourTargetPartOfPrefabInstance(this));
                        break;
                    case ShowTypeFixupResult.SelectedCandidate:
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.RequestScriptReload();
                        return true;
                }
            }

            GUI.enabled = oldGUIEnabled;
            if (serializedObject.ApplyModifiedProperties())
            {
                EditorUtility.ForceRebuildInspectors();
            }
            return true;
        }

        internal static bool MissingSerializeReference(Object unityTarget)
        {
            var monoBehaviour = unityTarget as MonoBehaviour;
            var scriptableObject = unityTarget as ScriptableObject;
            if ((monoBehaviour != null || scriptableObject != null) && SerializationUtility.HasManagedReferencesWithMissingTypes(unityTarget))
            {
                return true;
            }
            return false;
        }

        internal static bool ShowMissingSerializeReferenceWarningBoxIfRequired(Object unityTarget)
        {
            if (MissingSerializeReference(unityTarget))
            {
                EditorGUILayout.HelpBox(Styles.missingSerializeReferenceInstanceMessage, MessageType.Warning, true);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool ShowMissingSerializeReferenceWarningBoxIfRequired()
        {
            return ShowMissingSerializeReferenceWarningBoxIfRequired(target);
        }

        static GUIContent s_fixupTypeContent = new GUIContent("Fix underlying type");
        enum ShowTypeFixupResult
        {
            CantFindCandidate,
            DisplayedCandidates,
            SelectedCandidate
        }
        private ShowTypeFixupResult ShowTypeFixup()
        {
            var originalClassIdentifier = serializedObject.FindProperty("m_EditorClassIdentifier");
            if (originalClassIdentifier == null)
            {
                return ShowTypeFixupResult.CantFindCandidate;
            }
            var assemblySepartor = originalClassIdentifier.stringValue?.IndexOf("::");
            if (assemblySepartor == null || assemblySepartor == -1)
            {
                return ShowTypeFixupResult.CantFindCandidate;
            }
            var withoutAssembly = originalClassIdentifier.stringValue.Substring(assemblySepartor.Value + 2);
            var potentialMatches = TypeCache.GetTypesDerivedFrom<ScriptableObject>().Where(c => c.FullName == withoutAssembly)
                .Concat(TypeCache.GetTypesDerivedFrom<MonoBehaviour>().Where(c => c.FullName == withoutAssembly))
                .Select(c => $"{c.Assembly.GetName().Name}::{c.FullName}")
                .Where(c => c != originalClassIdentifier.stringValue)
                .ToList();

            if (potentialMatches.Count == 0)
            {
                return ShowTypeFixupResult.CantFindCandidate;
            }

            var buttons = new string[potentialMatches.Count + 1];
            buttons[0] = "-";
            for (int i = 0; i < potentialMatches.Count; i++)
            {
                buttons[i + 1] = potentialMatches[i];
            }

            EditorGUILayout.HelpBox("It seems that the underlying type has been moved in a different assembly. Please select the correct object type.", MessageType.Warning);
            EditorGUI.BeginChangeCheck();
            var value = EditorGUILayout.Popup(s_fixupTypeContent, 0, buttons);
            if (EditorGUI.EndChangeCheck())
            {
                originalClassIdentifier.stringValue = buttons[value];
                return ShowTypeFixupResult.SelectedCandidate;
            }
            return ShowTypeFixupResult.DisplayedCandidates;
        }

        private static bool CheckIfScriptLoaded(SerializedProperty scriptProperty)
        {
            MonoScript targetScript = scriptProperty?.objectReferenceValue as MonoScript;
            return targetScript != null && targetScript.GetScriptTypeWasJustCreatedFromComponentMenu();
        }

        private static void ShowScriptNotLoadedWarning(bool missingScriptIsOnPrefabInstance)
        {
            var message = missingScriptIsOnPrefabInstance ? Styles.missingScriptMessageForPrefabInstance : Styles.missingScriptMessage;
            EditorGUILayout.HelpBox(message, MessageType.Warning, true);
        }

        internal static void ShowScriptNotLoadedWarning(SerializedProperty scriptProperty, bool isPartOfPrefabInstance)
        {
            bool scriptLoaded = CheckIfScriptLoaded(scriptProperty);
            if (!scriptLoaded)
            {
                ShowScriptNotLoadedWarning(isPartOfPrefabInstance);
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
            propertyHandlerCache.Dispose();

            if (m_DummyPreview != null && m_DummyPreview is not Editor)
                m_DummyPreview.Cleanup();
        }

        internal static bool ObjectIsMonoBehaviourOrScriptableObjectWithoutScript(Object obj)
        {
            if (obj)
            {
                // When script is present the type will be a derived class instead
                return obj.GetType() == typeof(MonoBehaviour) || obj.GetType() == typeof(ScriptableObject);
            }
            return obj is MonoBehaviour || obj is ScriptableObject;
        }

        public override void OnInspectorGUI()
        {
            if (ObjectIsMonoBehaviourOrScriptableObjectWithoutScript(target))
            {
                if (MissingMonoBehaviourGUI())
                    return;
            }
            else
            {
                ShowMissingSerializeReferenceWarningBoxIfRequired();
            }

            base.OnInspectorGUI();
        }

        public override VisualElement CreateInspectorGUI()
        {
            if (serializedObject == null)
                return null;

            var root = new VisualElement();

            if (MissingSerializeReference(target))
            {
                root.Add(new HelpBox(GetMissingSerializeRefererenceMessageContainer(), HelpBoxMessageType.Warning));
            }

            UIElements.InspectorElement.FillDefaultInspector(root, serializedObject, this);

            return root;
        }
    }
}
