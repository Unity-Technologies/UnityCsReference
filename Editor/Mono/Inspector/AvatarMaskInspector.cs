// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Profiling;

namespace UnityEditor
{
    internal class BodyMaskEditor
    {
        class Styles
        {
            public GUIContent UnityDude = EditorGUIUtility.IconContent("AvatarInspector/BodySIlhouette");
            public GUIContent PickingTexture = EditorGUIUtility.IconContent("AvatarInspector/BodyPartPicker");

            public GUIContent[] BodyPart =
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

        static Styles styles = new Styles();


        static protected Color[] m_MaskBodyPartPicker =
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

        static public void Show(SerializedProperty bodyMask, int count)
        {
            if (styles.UnityDude.image)
            {
                Rect rect = GUILayoutUtility.GetRect(styles.UnityDude, GUIStyle.none, GUILayout.MaxWidth(styles.UnityDude.image.width));
                rect.x += (GUIView.current.position.width - rect.width) / 2;

                Color oldColor = GUI.color;

                GUI.color = bodyMask.GetArrayElementAtIndex(0).intValue == 1 ? Color.green : Color.red;

                if (styles.BodyPart[0].image)
                    GUI.DrawTexture(rect, styles.BodyPart[0].image);

                GUI.color = new Color(0.2f, 0.2f, 0.2f, 1.0f);
                GUI.DrawTexture(rect, styles.UnityDude.image);

                for (int i = 1; i < count; i++)
                {
                    GUI.color = bodyMask.GetArrayElementAtIndex(i).intValue == 1 ? Color.green : Color.red;
                    if (styles.BodyPart[i].image)
                        GUI.DrawTexture(rect, styles.BodyPart[i].image);
                }
                GUI.color = oldColor;

                DoPicking(rect, bodyMask, count);
            }
        }

        static protected void DoPicking(Rect rect, SerializedProperty bodyMask, int count)
        {
            if (styles.PickingTexture.image)
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
                            int left = styles.UnityDude.image.height - ((int)evt.mousePosition.y - (int)rect.y);

                            Texture2D pickTexture = styles.PickingTexture.image as Texture2D;
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
            public static GUIContent MaskDefinition = EditorGUIUtility.TextContent("Definition|Choose between Create From This Model, Copy From Other Avatar. The first one create a Mask for this file and the second one use a Mask from another file to import animation.");

            public static GUIContent[] MaskDefinitionOpt =
            {
                EditorGUIUtility.TextContent("Create From This Model|Create a Mask based on the model from this file. For Humanoid rig all the human transform are always imported and converted to muscle curve, thus they cannot be unchecked."),
                EditorGUIUtility.TextContent("Copy From Other Mask|Copy a Mask from another file to import animation clip."),
                EditorGUIUtility.TextContent("None | Import Everything")
            };

            public static GUIContent BodyMask = EditorGUIUtility.TextContent("Humanoid|Define which body part are active. Also define which animation curves will be imported for an Animation Clip.");
            public static GUIContent TransformMask = EditorGUIUtility.TextContent("Transform|Define which transform are active. Also define which animation curves will be imported for an Animation Clip.");

            public static GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout);
            public static GUIStyle labelStyle = new GUIStyle(EditorStyles.label);

            static Styles()
            {
                foldoutStyle.richText = labelStyle.richText = true;
            }
        }


        // Body mask data
        private bool m_ShowBodyMask = true;
        private bool m_BodyMaskFoldout = false;


        /// Transform mask data
        /// <summary>
        ///  Accelaration scructure
        /// </summary>
        struct NodeInfo
        {
            public enum State { disabled, enabled, invalid };

            public bool m_Expanded;
            public bool m_Show;
            public State m_State;
            public int m_ParentIndex;
            public List<int> m_ChildIndices;
            public int m_Depth;
            public SerializedProperty m_Path;
            public SerializedProperty m_Weight;
            public string m_Name;
        };


        private bool m_CanImport = true;
        public bool canImport
        {
            get { return m_CanImport; }
            set { m_CanImport = value; }
        }

        private SerializedProperty m_BodyMask = null;
        private SerializedProperty m_TransformMask = null;
        private SerializedProperty m_AnimationType = null;
        private AnimationClipInfoProperties m_ClipInfo = null;
        private string[] m_TransformPaths = null;
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

                    ModelImporter modelImporter = so.targetObject as ModelImporter;
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

        private NodeInfo[] m_NodeInfos;

        private Avatar m_RefAvatar;
        private ModelImporter m_RefImporter;
        private bool m_TransformMaskFoldout = false;
        private string[] m_HumanTransform = null;

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

            FillNodeInfos();
        }

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

            InitializeSerializedProperties();
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
            if (clipInfo == null)
                serializedObject.Update();

            bool showCopyFromOtherGUI = false;

            if (clipInfo != null)
            {
                EditorGUI.BeginChangeCheck();
                int maskType = MaskTypeToIndex(clipInfo.maskType);
                EditorGUI.showMixedValue = clipInfo.maskTypeProperty.hasMultipleDifferentValues;
                maskType = EditorGUILayout.Popup(Styles.MaskDefinition, maskType, Styles.MaskDefinitionOpt);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    clipInfo.maskType = IndexToMaskType(maskType);
                    UpdateMask(clipInfo.maskType);
                }

                showCopyFromOtherGUI = clipInfo.maskType == ClipAnimationMaskType.CopyFromOther;
            }

            if (showCopyFromOtherGUI)
                CopyFromOtherGUI();

            bool wasEnabled = GUI.enabled;
            GUI.enabled = !showCopyFromOtherGUI;

            EditorGUI.BeginChangeCheck();
            OnBodyInspectorGUI();
            OnTransformInspectorGUI();
            if (clipInfo != null && EditorGUI.EndChangeCheck())
            {
                AvatarMask mask = target as AvatarMask;
                clipInfo.MaskFromClip(mask);
            }

            GUI.enabled = wasEnabled;

            if (clipInfo == null)
                serializedObject.ApplyModifiedProperties();

            Profiler.EndSample();
        }

        protected void CopyFromOtherGUI()
        {
            if (clipInfo == null)
                return;

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(clipInfo.maskSourceProperty, GUIContent.Temp("Source"));
            AvatarMask maskSource = clipInfo.maskSourceProperty.objectReferenceValue as AvatarMask;
            if (EditorGUI.EndChangeCheck() && maskSource != null)
                UpdateMask(clipInfo.maskType);

            EditorGUILayout.EndHorizontal();
        }

        public bool IsMaskEmpty()
        {
            return m_NodeInfos.Length == 0;
        }

        public bool IsMaskUpToDate()
        {
            if (clipInfo == null)
                return false;

            if (m_NodeInfos.Length != m_TransformPaths.Length)
                return false;

            if (m_TransformMask.arraySize > 0)
            {
                SerializedProperty prop = m_TransformMask.GetArrayElementAtIndex(0);

                for (int i = 1; i < m_NodeInfos.Length; i++)
                {
                    string path = m_NodeInfos[i].m_Path.stringValue;
                    int index = ArrayUtility.FindIndex(m_TransformPaths, s => s == path);
                    if (index == -1)
                        return false;

                    prop.Next(false);
                }
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
                FillNodeInfos();
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
                    FillNodeInfos();
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
            float left = 0 , top = 0 , right = 0, bottom = 0;

            // Don't make toggling foldout cause GUI.changed to be true (shouldn't cause undoable action etc.)
            bool wasChanged = GUI.changed;
            m_TransformMaskFoldout = EditorGUILayout.Foldout(m_TransformMaskFoldout, Styles.TransformMask, true);
            GUI.changed = wasChanged;
            if (m_TransformMaskFoldout)
            {
                if (canImport)
                    ImportAvatarReference();

                if (m_NodeInfos == null || m_TransformMask.arraySize != m_NodeInfos.Length)
                    FillNodeInfos();

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
                        string fixMaskButtonLabel = "Create Mask";
                        if (GUILayout.Button(fixMaskButtonLabel))
                            UpdateMask(clipInfo.maskType);
                        GUILayout.EndHorizontal();
                    }


                    GUILayout.EndVertical();
                }
                else
                {
                    ComputeShownElements();

                    GUILayout.Space(1);
                    int prevIndent = EditorGUI.indentLevel;
                    int size = m_TransformMask.arraySize;
                    for (int i = 1; i < size; i++)
                    {
                        if (m_NodeInfos[i].m_Show)
                        {
                            GUILayout.BeginHorizontal();
                            EditorGUI.indentLevel = m_NodeInfos[i].m_Depth + 1;

                            EditorGUI.BeginChangeCheck();
                            Rect toggleRect = GUILayoutUtility.GetRect(15, 15, GUILayout.ExpandWidth(false));
                            GUILayoutUtility.GetRect(10, 15, GUILayout.ExpandWidth(false));
                            toggleRect.x += 15;

                            EditorGUI.BeginDisabledGroup(m_NodeInfos[i].m_State == NodeInfo.State.disabled || m_NodeInfos[i].m_State == NodeInfo.State.invalid);
                            bool rightClick = Event.current.button == 1;

                            bool isActive = m_NodeInfos[i].m_Weight.floatValue > 0.0f;
                            isActive = GUI.Toggle(toggleRect, isActive, "");

                            if (EditorGUI.EndChangeCheck())
                            {
                                m_NodeInfos[i].m_Weight.floatValue = isActive ? 1.0f : 0.0f;
                                if (!rightClick)
                                    CheckChildren(i, isActive);
                            }

                            string textValue;
                            if (m_NodeInfos[i].m_State == NodeInfo.State.invalid)
                                textValue = "<color=#FF0000AA>" + m_NodeInfos[i].m_Name + "</color>";
                            else
                                textValue = m_NodeInfos[i].m_Name;

                            if (m_NodeInfos[i].m_ChildIndices.Count > 0)
                                m_NodeInfos[i].m_Expanded = EditorGUILayout.Foldout(m_NodeInfos[i].m_Expanded, textValue, true, Styles.foldoutStyle);
                            else
                                EditorGUILayout.LabelField(textValue, Styles.labelStyle);

                            EditorGUI.EndDisabledGroup();
                            if (i == 1)
                            {
                                top = toggleRect.yMin;
                                left = toggleRect.xMin;
                            }
                            else if (i == size - 1)
                            {
                                bottom = toggleRect.yMax;
                            }

                            right = Mathf.Max(right, GUILayoutUtility.GetLastRect().xMax);

                            GUILayout.EndHorizontal();
                        }
                    }

                    EditorGUI.indentLevel = prevIndent;
                }
            }

            Rect bounds  = Rect.MinMaxRect(left, top, right, bottom);

            if (Event.current != null && Event.current.type == EventType.MouseUp && Event.current.button == 1 && bounds.Contains(Event.current.mousePosition))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Select all"), false, SelectAll);
                menu.AddItem(new GUIContent("Deselect all"), false, DeselectAll);
                menu.ShowAsContext();
                Event.current.Use();
            }
        }

        private void SetAllTransformActive(bool active)
        {
            for (int i = 0; i < m_NodeInfos.Length; i++)
                if (m_NodeInfos[i].m_State == NodeInfo.State.enabled)
                    m_NodeInfos[i].m_Weight.floatValue = active ? 1.0f : 0.0f;
        }

        private void SelectAll()
        {
            SetAllTransformActive(true);
        }

        private void DeselectAll()
        {
            SetAllTransformActive(false);
        }

        private void ImportAvatarReference()
        {
            EditorGUI.BeginChangeCheck();
            m_RefAvatar = EditorGUILayout.ObjectField("Use skeleton from", m_RefAvatar, typeof(Avatar), true) as Avatar;
            if (EditorGUI.EndChangeCheck())
                m_RefImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(m_RefAvatar)) as ModelImporter;

            if (m_RefImporter != null && GUILayout.Button("Import skeleton"))
                AvatarMaskUtility.UpdateTransformMask(m_TransformMask, m_RefImporter.transformPaths, null);
        }

        public void FillNodeInfos()
        {
            m_NodeInfos = new NodeInfo[m_TransformMask.arraySize];
            if (m_TransformMask.arraySize == 0)
                return;

            string[] paths = new string[m_TransformMask.arraySize];

            SerializedProperty prop = m_TransformMask.GetArrayElementAtIndex(0);
            prop.Next(false);

            for (int i = 1; i < m_NodeInfos.Length; i++)
            {
                m_NodeInfos[i].m_Path = prop.FindPropertyRelative("m_Path");
                m_NodeInfos[i].m_Weight = prop.FindPropertyRelative("m_Weight");

                paths[i] = m_NodeInfos[i].m_Path.stringValue;
                string fullPath = paths[i];
                if (m_CanImport)
                {
                    // in avatar mask inspector UI,everything is enabled.
                    m_NodeInfos[i].m_State = NodeInfo.State.enabled;
                }
                else if (humanTransforms != null)
                {
                    //  Enable only transforms that are not human. Human transforms in this case are handled by muscle curves and cannot be imported.
                    if (ArrayUtility.FindIndex(humanTransforms, s => fullPath == s) == -1)
                    {
                        if (m_TransformPaths != null && ArrayUtility.FindIndex(m_TransformPaths, s => fullPath == s) == -1)
                            m_NodeInfos[i].m_State = NodeInfo.State.invalid;
                        else
                            m_NodeInfos[i].m_State = NodeInfo.State.enabled;
                    }
                    else
                    {
                        m_NodeInfos[i].m_State = NodeInfo.State.disabled;
                    }
                }
                else if (m_TransformPaths != null && ArrayUtility.FindIndex(m_TransformPaths, s => fullPath == s) == -1)
                {
                    // mask does not map to an existing hierarchy node. It's invalid.
                    m_NodeInfos[i].m_State = NodeInfo.State.invalid;
                }
                else
                {
                    m_NodeInfos[i].m_State = NodeInfo.State.enabled;
                }


                m_NodeInfos[i].m_Expanded = true;
                m_NodeInfos[i].m_ParentIndex = -1;
                m_NodeInfos[i].m_ChildIndices = new List<int>();

                m_NodeInfos[i].m_Depth = i == 0 ? 0 : fullPath.Count(f => f == '/');

                string parentPath = "";
                int lastIndex = fullPath.LastIndexOf('/');
                if (lastIndex > 0)
                    parentPath = fullPath.Substring(0, lastIndex);

                lastIndex = lastIndex == -1 ? 0 : lastIndex + 1;
                m_NodeInfos[i].m_Name = fullPath.Substring(lastIndex);

                for (int j = 1; j < i; j++) // parents are already processed
                {
                    string otherPath = paths[j];
                    if (parentPath != "" && otherPath == parentPath)
                    {
                        m_NodeInfos[i].m_ParentIndex = j;
                        m_NodeInfos[j].m_ChildIndices.Add(i);
                    }
                }

                prop.Next(false);
            }
        }

        private void ComputeShownElements()
        {
            for (int i = 0; i < m_NodeInfos.Length; i++)
            {
                if (m_NodeInfos[i].m_ParentIndex == -1)
                    ComputeShownElements(i, true);
            }
        }

        private void ComputeShownElements(int currentIndex, bool show)
        {
            m_NodeInfos[currentIndex].m_Show = show;
            bool showChilds = show && m_NodeInfos[currentIndex].m_Expanded;
            foreach (int index in m_NodeInfos[currentIndex].m_ChildIndices)
            {
                ComputeShownElements(index, showChilds);
            }
        }

        private void CheckChildren(int index, bool value)
        {
            foreach (int childIndex in m_NodeInfos[index].m_ChildIndices)
            {
                if (m_NodeInfos[childIndex].m_State == NodeInfo.State.enabled)
                    m_NodeInfos[childIndex].m_Weight.floatValue = value ? 1.0f : 0.0f;
                CheckChildren(childIndex, value);
            }
        }
    }
}
