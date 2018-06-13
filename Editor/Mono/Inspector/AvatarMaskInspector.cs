// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine.Profiling;

namespace UnityEditor
{
    internal class BodyMaskEditor
    {
        static class Styles
        {
            public static GUIContent UnityDude = EditorGUIUtility.IconContent("AvatarInspector/BodySIlhouette");
            public static GUIContent PickingTexture = EditorGUIUtility.IconContent("AvatarInspector/BodyPartPicker");

            public static GUIContent[] BodyPart =
            {
                EditorGUIUtility.IconContent("AvatarInspector/MaskEditor_Root"),
                EditorGUIUtility.IconContent("AvatarInspector/Torso"),

                EditorGUIUtility.IconContent("AvatarInspector/Head"),

                EditorGUIUtility.IconContent("AvatarInspector/LeftLeg"),
                EditorGUIUtility.IconContent("AvatarInspector/RightLeg"),

                EditorGUIUtility.IconContent("AvatarInspector/LeftArm"),
                EditorGUIUtility.IconContent("AvatarInspector/RightArm"),

                EditorGUIUtility.IconContent("AvatarInspector/LeftFingers"),
                EditorGUIUtility.IconContent("AvatarInspector/RightFingers"),

                EditorGUIUtility.IconContent("AvatarInspector/LeftFeetIk"),
                EditorGUIUtility.IconContent("AvatarInspector/RightFeetIk"),

                EditorGUIUtility.IconContent("AvatarInspector/LeftFingersIk"),
                EditorGUIUtility.IconContent("AvatarInspector/RightFingersIk"),
            };
        }

        protected static Color[] m_MaskBodyPartPicker =
        {
            new Color(255 / 255.0f,   144 / 255.0f,     0 / 255.0f), // root
            new Color(0 / 255.0f, 174 / 255.0f, 240 / 255.0f), // body
            new Color(171 / 255.0f, 160 / 255.0f,   0 / 255.0f), // head

            new Color(0 / 255.0f, 255 / 255.0f,     255 / 255.0f), // ll
            new Color(247 / 255.0f,   151 / 255.0f, 121 / 255.0f), // rl

            new Color(0 / 255.0f, 255 / 255.0f, 0 / 255.0f), // la
            new Color(86 / 255.0f, 116 / 255.0f, 185 / 255.0f), // ra

            new Color(255 / 255.0f,   255 / 255.0f,     0 / 255.0f), // lh
            new Color(130 / 255.0f,   202 / 255.0f, 156 / 255.0f), // rh

            new Color(82 / 255.0f,    82 / 255.0f,      82 / 255.0f), // lfi
            new Color(255 / 255.0f,   115 / 255.0f,     115 / 255.0f), // rfi
            new Color(159 / 255.0f,   159 / 255.0f,     159 / 255.0f), // lhi
            new Color(202 / 255.0f,   202 / 255.0f, 202 / 255.0f), // rhi

            new Color(101 / 255.0f,   101 / 255.0f, 101 / 255.0f), // hi
        };

        static string sAvatarBodyMaskStr = "AvatarMask";
        static int s_Hint = sAvatarBodyMaskStr.GetHashCode();

        public static void Show(SerializedProperty bodyMask, int count)
        {
            if (Styles.UnityDude.image)
            {
                Rect rect = GUILayoutUtility.GetRect(Styles.UnityDude, GUIStyle.none, GUILayout.MaxWidth(Styles.UnityDude.image.width));
                rect.x += (GUIView.current.position.width - rect.width) / 2;

                Color oldColor = GUI.color;

                GUI.color = bodyMask.GetArrayElementAtIndex(0).intValue == 1 ? Color.green : Color.red;

                if (Styles.BodyPart[0].image)
                    GUI.DrawTexture(rect, Styles.BodyPart[0].image);

                GUI.color = new Color(0.2f, 0.2f, 0.2f, 1.0f);
                GUI.DrawTexture(rect, Styles.UnityDude.image);

                for (int i = 1; i < count; i++)
                {
                    GUI.color = bodyMask.GetArrayElementAtIndex(i).intValue == 1 ? Color.green : Color.red;
                    if (Styles.BodyPart[i].image)
                        GUI.DrawTexture(rect, Styles.BodyPart[i].image);
                }
                GUI.color = oldColor;

                DoPicking(rect, bodyMask, count);
            }
        }

        protected static void DoPicking(Rect rect, SerializedProperty bodyMask, int count)
        {
            if (Styles.PickingTexture.image)
            {
                int id = GUIUtility.GetControlID(s_Hint, FocusType.Passive, rect);

                Event evt = Event.current;

                switch (evt.GetTypeForControl(id))
                {
                    case EventType.MouseDown:
                    {
                        if (rect.Contains(evt.mousePosition))
                        {
                            evt.Use();

                            // Texture coordinate start at 0,0 at bottom, left
                            // Screen coordinate start at 0,0 at top, left
                            // So we need to convert from screen coord to texture coord
                            int top = (int)evt.mousePosition.x - (int)rect.x;
                            int left = Styles.UnityDude.image.height - ((int)evt.mousePosition.y - (int)rect.y);

                            Texture2D pickTexture = Styles.PickingTexture.image as Texture2D;
                            Color color = pickTexture.GetPixel(top, left);

                            bool anyBodyPartPick = false;
                            for (int i = 0; i < count; i++)
                            {
                                if (m_MaskBodyPartPicker[i] == color)
                                {
                                    GUI.changed = true;
                                    bodyMask.GetArrayElementAtIndex(i).intValue = bodyMask.GetArrayElementAtIndex(i).intValue == 1 ? 0 : 1;
                                    anyBodyPartPick = true;
                                }
                            }

                            if (!anyBodyPartPick)
                            {
                                bool atLeastOneSelected = false;

                                for (int i = 0; i < count && !atLeastOneSelected; i++)
                                {
                                    atLeastOneSelected = bodyMask.GetArrayElementAtIndex(i).intValue == 1 ? true : false;
                                }

                                for (int i = 0; i < count; i++)
                                {
                                    bodyMask.GetArrayElementAtIndex(i).intValue = !atLeastOneSelected ? 1 : 0;
                                }
                                GUI.changed = true;
                            }
                        }
                        break;
                    }
                }
            }
        }
    }

    [CustomEditor(typeof(AvatarMask))]
    internal class AvatarMaskInspector : Editor
    {
        private static class Styles
        {
            // Model Importer related options
            public static GUIContent MaskDefinition = EditorGUIUtility.TrTextContent("Definition", "Choose between Create From This Model, Copy From Other Avatar. The first one create a Mask for this file and the second one use a Mask from another file to import animation.");
            public static GUIContent[] MaskDefinitionOpt =
            {
                EditorGUIUtility.TrTextContent("Create From This Model", "Create a Mask based on the model from this file. For Humanoid rig all the human transform are always imported and converted to muscle curve, thus they cannot be unchecked."),
                EditorGUIUtility.TrTextContent("Copy From Other Mask", "Copy a Mask from another file to import animation clip."),
                EditorGUIUtility.TrTextContent("None ", " Import Everything")
            };
            public static GUIContent CopyFromOtherSource = EditorGUIUtility.TrTextContent("Source", "Select from which AvatarMask the animation should take the mask information");
            public static GUIContent CreateMask = EditorGUIUtility.TrTextContent("Create Mask", "Create a new mask from this model avatar.");

            // Avatar mask options
            public static GUIContent SelectAvatarReference = EditorGUIUtility.TrTextContent("Use skeleton from", "The selected avatar is never linked here and only used to populate the list of transform.");
            public static GUIContent ImportAvatarReference = EditorGUIUtility.TrTextContent("Import skeleton", "Generates new transform data based on the selected avatar skeleton");

            // Avatar mask foldouts
            public static GUIContent BodyMask = EditorGUIUtility.TrTextContent("Humanoid", "Define which body part are active. Also define which animation curves will be imported for an Animation Clip.");
            public static GUIContent TransformMask = EditorGUIUtility.TrTextContent("Transform", "Define which transform are active. Also define which animation curves will be imported for an Animation Clip.");

            // TreeView columns
            public static GUIContent TransformName = EditorGUIUtility.TrTextContent("Node Name");
            public static GUIContent EnableName = EditorGUIUtility.TrTextContent("Use", "Maintain Alt/Option key to enable or disable all children");
        }

        // Body mask data
        private bool m_ShowBodyMask = true;
        private bool m_BodyMaskFoldout = false;
        private SerializedProperty m_BodyMask = null;

        // Transform data
        private SerializedProperty m_TransformMask = null;
        private string[] m_TransformPaths = null;

        [SerializeField] TreeViewState m_TreeViewState;
        [SerializeField] MultiColumnHeaderState m_ViewHeaderState;
        //The TreeView is not serializable, so it should be reconstructed from the tree data.
        AvatarMaskTreeView m_SimpleTreeView;

        // Importer specific data
        private bool m_CanImport = true;
        public bool canImport
        {
            get { return m_CanImport; }
            set { m_CanImport = value; }
        }

        private SerializedProperty m_AnimationType = null;
        private AnimationClipInfoProperties m_ClipInfo = null;
        public AnimationClipInfoProperties clipInfo
        {
            get { return m_ClipInfo;  }
            set
            {
                m_ClipInfo = value;
                if (m_ClipInfo != null)
                {
                    m_ClipInfo.MaskFromClip(target as AvatarMask);
                    SerializedObject so = m_ClipInfo.maskTypeProperty.serializedObject;
                    m_AnimationType = so.FindProperty("m_AnimationType");

                    ModelImporter modelImporter = (ModelImporter)so.targetObject;
                    m_TransformPaths = modelImporter.transformPaths;
                }
                else
                {
                    m_TransformPaths = null;
                    m_AnimationType = null;
                }

                InitializeSerializedProperties();
            }
        }

        private ModelImporterAnimationType animationType
        {
            get
            {
                if (m_AnimationType != null)
                    return (ModelImporterAnimationType)m_AnimationType.intValue;
                else
                    return ModelImporterAnimationType.None;
            }
        }

        private Avatar m_RefAvatar;
        private ModelImporter m_RefImporter;
        private bool m_TransformMaskFoldout = false;
        private string[] m_HumanTransform = null;

        private void OnEnable()
        {
            // The AvatarMaskInspector is instantiated in the ModelImporterClipEditor, which does not derive from the Editor class anymore.
            // All editors are added to a list so that OnEnable() and other methods can be called on them at certain times.
            // When going in game while having settings pending to be applied, at some point the target (ModelImporterClipEditor.m_Mask) becomes null,
            // since the parent becomes updated before this class.
            if (target == null)
            {
                return;
            }

            InitTreeView();
            InitializeSerializedProperties();
        }

        public void UpdateTransformInfos()
        {
            m_SimpleTreeView.Reload();
        }

        public bool showBody
        {
            get { return m_ShowBodyMask; }
            set { m_ShowBodyMask = value; }
        }

        public string[] humanTransforms
        {
            get
            {
                if (animationType == ModelImporterAnimationType.Human && clipInfo != null)
                {
                    if (m_HumanTransform == null)
                    {
                        SerializedObject so = clipInfo.maskTypeProperty.serializedObject;
                        ModelImporter modelImporter = so.targetObject as ModelImporter;

                        m_HumanTransform = AvatarMaskUtility.GetAvatarHumanTransform(so, modelImporter.transformPaths);
                    }
                }
                else
                    m_HumanTransform = null;
                return m_HumanTransform;
            }
        }

        private ClipAnimationMaskType IndexToMaskType(int index)
        {
            ClipAnimationMaskType ret;
            switch (index)
            {
                case 2:
                    ret = ClipAnimationMaskType.None;
                    break;
                default:
                    ret = (ClipAnimationMaskType)index;
                    break;
            }
            return ret;
        }

        private int MaskTypeToIndex(ClipAnimationMaskType maskType)
        {
            int ret;
            switch (maskType)
            {
                case ClipAnimationMaskType.None:
                    ret = 2;
                    break;
                default:
                    ret = (int)maskType;
                    break;
            }
            return ret;
        }

        public override void OnInspectorGUI()
        {
            Profiler.BeginSample("AvatarMaskInspector.OnInspectorGUI()");
            if (clipInfo != null)
                InspectorGUIWithClipInfo();
            else
                InspectorGUIWithNoClipInfo();
            Profiler.EndSample();
        }

        void InspectorGUIWithNoClipInfo()
        {
            serializedObject.Update();

            OnBodyInspectorGUI();
            OnTransformInspectorGUI();

            serializedObject.ApplyModifiedProperties();
        }

        void InspectorGUIWithClipInfo()
        {
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                int maskType = MaskTypeToIndex(clipInfo.maskType);
                EditorGUI.showMixedValue = clipInfo.maskTypeProperty.hasMultipleDifferentValues;
                maskType = EditorGUILayout.Popup(Styles.MaskDefinition, maskType, Styles.MaskDefinitionOpt);
                EditorGUI.showMixedValue = false;
                if (change.changed)
                {
                    clipInfo.maskType = IndexToMaskType(maskType);
                    UpdateMask(clipInfo.maskType);
                }
            }

            var showCopyFromOtherGUI = clipInfo.maskType == ClipAnimationMaskType.CopyFromOther;
            if (showCopyFromOtherGUI)
                CopyFromOtherGUI();

            using (new EditorGUI.DisabledScope(showCopyFromOtherGUI))
            {
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    OnBodyInspectorGUI();
                    OnTransformInspectorGUI();
                    if (change.changed)
                    {
                        AvatarMask mask = target as AvatarMask;
                        clipInfo.MaskFromClip(mask);
                    }
                }
            }
        }

        void CopyFromOtherGUI()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(clipInfo.maskSourceProperty, Styles.CopyFromOtherSource);
            AvatarMask maskSource = clipInfo.maskSourceProperty.objectReferenceValue as AvatarMask;
            if (EditorGUI.EndChangeCheck() && maskSource != null)
                UpdateMask(clipInfo.maskType);

            EditorGUILayout.EndHorizontal();
        }

        public bool IsMaskEmpty()
        {
            return !m_SimpleTreeView.rootNode.hasChildren;
        }

        public bool IsMaskUpToDate()
        {
            if (clipInfo == null)
                return false;

            if (m_SimpleTreeView.rootNode.DeepCount != m_TransformPaths.Length)
                return false;

            if (m_TransformPaths.Length > 0)
            {
                return CheckNodesPaths(m_SimpleTreeView.rootNode);
            }

            return true;
        }

        bool CheckNodesPaths(SerializedNodeInfo parent)
        {
            if (parent.children != null)
            {
                foreach (var treeViewItem in parent.children)
                {
                    var node = (SerializedNodeInfo)treeViewItem;
                    if (!CheckNodesPaths(node))
                        return false;
                }
            }

            if (parent.m_Path != null)
            {
                string path = parent.m_Path.stringValue;
                return ArrayUtility.FindIndex(m_TransformPaths, s => s == path) != -1;
            }

            return true;
        }

        private void UpdateMask(ClipAnimationMaskType maskType)
        {
            if (clipInfo == null)
                return;

            if (maskType == ClipAnimationMaskType.CreateFromThisModel)
            {
                SerializedObject so = clipInfo.maskTypeProperty.serializedObject;
                ModelImporter modelImporter = so.targetObject as ModelImporter;

                AvatarMaskUtility.UpdateTransformMask(m_TransformMask, modelImporter.transformPaths, humanTransforms);
                UpdateTransformInfos();
            }
            else if (maskType == ClipAnimationMaskType.CopyFromOther)
            {
                AvatarMask maskSource = clipInfo.maskSourceProperty.objectReferenceValue as AvatarMask;
                if (maskSource != null)
                {
                    AvatarMask maskDest = target as AvatarMask;
                    maskDest.Copy(maskSource);

                    // If this is a human clip make sure that all human transform path are set to true
                    if (humanTransforms != null)
                        AvatarMaskUtility.SetActiveHumanTransforms(maskDest, humanTransforms);

                    clipInfo.MaskToClip(maskDest);
                    UpdateTransformInfos();
                }
            }
            else if (maskType == ClipAnimationMaskType.None)
            {
                var emptyMask = new AvatarMask();
                ModelImporter.UpdateTransformMask(emptyMask, clipInfo.transformMaskProperty);
            }

            AvatarMask mask = target as AvatarMask;
            clipInfo.MaskFromClip(mask);
        }

        public void OnBodyInspectorGUI()
        {
            if (m_ShowBodyMask)
            {
                // Don't make toggling foldout cause GUI.changed to be true (shouldn't cause undoable action etc.)
                bool wasChanged = GUI.changed;
                m_BodyMaskFoldout = EditorGUILayout.Foldout(m_BodyMaskFoldout, Styles.BodyMask, true);
                GUI.changed = wasChanged;
                if (m_BodyMaskFoldout)
                    BodyMaskEditor.Show(m_BodyMask, (int)AvatarMaskBodyPart.LastBodyPart);
            }
        }

        public void OnTransformInspectorGUI()
        {
            // Don't make toggling foldout cause GUI.changed to be true (shouldn't cause undoable action etc.)
            bool wasChanged = GUI.changed;
            m_TransformMaskFoldout = EditorGUILayout.Foldout(m_TransformMaskFoldout, Styles.TransformMask, true);
            GUI.changed = wasChanged;
            if (m_TransformMaskFoldout)
            {
                if (canImport)
                    ImportAvatarReference();

                if (m_SimpleTreeView.rootNode == null || m_TransformMask.arraySize != m_SimpleTreeView.rootNode.DeepCount)
                    UpdateTransformInfos();

                if (IsMaskEmpty())
                {
                    GUILayout.BeginVertical();
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);
                    string message;
                    if (animationType == ModelImporterAnimationType.Generic)
                    {
                        message = "No transform mask defined, everything will be imported";
                    }
                    else if (animationType == ModelImporterAnimationType.Human)
                    {
                        message = "No transform mask defined, only human curves will be imported";
                    }
                    else
                    {
                        message = "No transform mask defined";
                    }

                    GUILayout.Label(message,
                        EditorStyles.wordWrappedMiniLabel);

                    GUILayout.EndHorizontal();

                    if (!canImport && clipInfo.maskType == ClipAnimationMaskType.CreateFromThisModel)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(Styles.CreateMask))
                            UpdateMask(clipInfo.maskType);
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }
                else
                {
                    m_SimpleTreeView.OnGUI(EditorGUILayout.GetControlRect(false, m_SimpleTreeView.totalHeightIncludingSearchBarAndBottomBar));
                }
            }
        }

        private void ImportAvatarReference()
        {
            EditorGUI.BeginChangeCheck();
            m_RefAvatar = EditorGUILayout.ObjectField(Styles.SelectAvatarReference, m_RefAvatar, typeof(Avatar), true) as Avatar;
            if (EditorGUI.EndChangeCheck())
                m_RefImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(m_RefAvatar)) as ModelImporter;

            using (new EditorGUI.DisabledScope(m_RefImporter == null))
            {
                if (GUILayout.Button(Styles.ImportAvatarReference))
                    AvatarMaskUtility.UpdateTransformMask(m_TransformMask, m_RefImporter.transformPaths, null);
            }
        }

        private void InitializeSerializedProperties()
        {
            if (clipInfo != null)
            {
                m_BodyMask = clipInfo.bodyMaskProperty;
                m_TransformMask = clipInfo.transformMaskProperty;
            }
            else
            {
                m_BodyMask = serializedObject.FindProperty("m_Mask");
                m_TransformMask = serializedObject.FindProperty("m_Elements");
            }

            UpdateTransformInfos();
        }

        private SerializedNodeInfo FillNodeInfos()
        {
            var rootNode = new SerializedNodeInfo() { depth = -1, displayName = "", id = 0, children = new List<TreeViewItem>(0) };
            if (m_TransformMask == null || m_TransformMask.arraySize == 0)
            {
                return rootNode;
            }

            var nodesCount = m_TransformMask.arraySize;
            var nodeInfos = new List<SerializedNodeInfo>(nodesCount);
            string[] paths = new string[nodesCount];
            SerializedProperty prop = m_TransformMask.GetArrayElementAtIndex(0);
            prop.Next(false);

            for (int i = 1; i < nodesCount; i++)
            {
                var newNode = new SerializedNodeInfo();
                newNode.id = i;
                newNode.m_Path = prop.FindPropertyRelative("m_Path");
                newNode.m_Weight = prop.FindPropertyRelative("m_Weight");

                paths[i] = newNode.m_Path.stringValue;
                string fullPath = paths[i];
                if (m_CanImport)
                {
                    // in avatar mask inspector UI,everything is enabled.
                    newNode.m_State = SerializedNodeInfo.State.Enabled;
                }
                else if (humanTransforms != null)
                {
                    //  Enable only transforms that are not human. Human transforms in this case are handled by muscle curves and cannot be imported.
                    if (ArrayUtility.FindIndex(humanTransforms, s => fullPath == s) == -1)
                    {
                        if (m_TransformPaths != null && ArrayUtility.FindIndex(m_TransformPaths, s => fullPath == s) == -1)
                            newNode.m_State = SerializedNodeInfo.State.Invalid;
                        else
                            newNode.m_State = SerializedNodeInfo.State.Enabled;
                    }
                    else
                    {
                        newNode.m_State = SerializedNodeInfo.State.Disabled;
                    }
                }
                else if (m_TransformPaths != null && ArrayUtility.FindIndex(m_TransformPaths, s => fullPath == s) == -1)
                {
                    // mask does not map to an existing hierarchy node. It's invalid.
                    newNode.m_State = SerializedNodeInfo.State.Invalid;
                }
                else
                {
                    newNode.m_State = SerializedNodeInfo.State.Enabled;
                }

                newNode.depth = i == 0 ? 0 : fullPath.Count(f => f == '/');

                int lastIndex = fullPath.LastIndexOf('/');
                lastIndex = lastIndex == -1 ? 0 : lastIndex + 1;
                newNode.displayName = fullPath.Substring(lastIndex);

                nodeInfos.Add(newNode);
                prop.Next(false);
            }

            TreeViewUtility.SetChildParentReferences(nodeInfos.Cast<TreeViewItem>().ToList(), rootNode);
            return rootNode;
        }

        private void InitTreeView()
        {
            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = Styles.EnableName,
                    headerTextAlignment = TextAlignment.Center,
                    canSort = false,
                    width = 31f, minWidth = 31f, maxWidth = 31f,
                    autoResize = true, allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = Styles.TransformName,
                    headerTextAlignment = TextAlignment.Left,
                    canSort = false,
                    autoResize = true, allowToggleVisibility = false,
                }
            };
            var newHeader = new MultiColumnHeaderState(columns);
            if (m_ViewHeaderState != null)
            {
                MultiColumnHeaderState.OverwriteSerializedFields(m_ViewHeaderState, newHeader);
            }
            m_ViewHeaderState = newHeader;
            var multiColumnHeader = new MultiColumnHeader(m_ViewHeaderState);
            multiColumnHeader.ResizeToFit();
            m_SimpleTreeView = new AvatarMaskTreeView(m_TreeViewState, multiColumnHeader, FillNodeInfos);
            if (m_SimpleTreeView.searchString == null)
                m_SimpleTreeView.searchString = string.Empty;
        }

        private class SerializedNodeInfo : ToggleTreeViewItem
        {
            public enum State { Disabled, Enabled, Invalid };
            public State m_State;

            // SerializedProperties
            public SerializedProperty m_Path;
            public SerializedProperty m_Weight;

            public override bool nodeState
            {
                get { return m_Weight.floatValue == 1f; }
                set
                {
                    if (m_State != State.Disabled)
                        m_Weight.floatValue = value ? 1f : 0f;
                }
            }

            public int DeepCount
            {
                get
                {
                    var count = 1;
                    if (hasChildren)
                        foreach (var treeViewItem in children)
                        {
                            var child = (SerializedNodeInfo)treeViewItem;
                            count += child.DeepCount;
                        }
                    return count;
                }
            }
        }

        private class AvatarMaskTreeView : ToggleTreeView<SerializedNodeInfo>
        {
            public AvatarMaskTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader, Func<SerializedNodeInfo> rebuildRoot)
                : base(state, multiColumnHeader, rebuildRoot) {}

            protected override void EnabledGUI(Rect cellRect, SerializedNodeInfo node, ref RowGUIArgs args)
            {
                var serializedNode = node;
                EditorGUI.BeginDisabled(serializedNode.m_State == SerializedNodeInfo.State.Disabled);
                base.EnabledGUI(cellRect, node, ref args);
                EditorGUI.EndDisabled();
            }

            protected override void NameGUI(Rect position, SerializedNodeInfo node, ref RowGUIArgs args)
            {
                var serializedNode = node;
                var color = GUI.contentColor;
                color = serializedNode.m_State == SerializedNodeInfo.State.Invalid ? new Color(1f, 0f, 0f, 0.66f) : color;
                base.NameGUI(position, node, ref args);
                GUI.contentColor = color;
            }

            protected override void ToggleAll()
            {
                bool value;
                GetFirstActiveValue((SerializedNodeInfo)rootItem, out value);
                PropagateValue((SerializedNodeInfo)rootItem, !value);
            }

            static bool GetFirstActiveValue(SerializedNodeInfo parent, out bool value)
            {
                foreach (var treeViewItem in parent.children)
                {
                    var child = (SerializedNodeInfo)treeViewItem;
                    if (child.m_State != SerializedNodeInfo.State.Disabled)
                    {
                        value = child.nodeState;
                        return true;
                    }
                    if (GetFirstActiveValue(child, out value))
                        return true;
                }
                value = false;
                return false;
            }

            public SerializedNodeInfo rootNode => (SerializedNodeInfo)rootItem;
        }
    }
}
