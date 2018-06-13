// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Events;

namespace TreeEditor
{
    public class TreeEditorHelper
    {
        public enum NodeType { BarkNode, LeafNode }

        private const string kDefaultBarkShaderName = "Nature/Tree Creator Bark";
        private const string kDefaultLeafShaderName = "Nature/Tree Creator Leaves Fast";
        private const string kDefaultOptimizedBarkShaderName = "Hidden/Nature/Tree Creator Bark Optimized";
        private const string kDefaultOptimizedLeafShaderName = "Hidden/Nature/Tree Creator Leaves Optimized";

        private const string kOptimizedShaderDependency = "OptimizedShader";

        private readonly Dictionary<string, AnimBool> m_AnimBools = new Dictionary<string, AnimBool>();

        private readonly List<string> m_BarkShaders = new List<string>();
        private readonly List<string> m_LeafShaders = new List<string>();
        private readonly HashSet<string> m_WrongShaders = new HashSet<string>();

        private readonly Dictionary<string, int> m_SelectedShader = new Dictionary<string, int>();

        private TreeData m_TreeData;

        internal static Shader DefaultOptimizedBarkShader { get { return Shader.Find(kDefaultOptimizedBarkShaderName); } }
        internal static Shader DefaultOptimizedLeafShader { get { return Shader.Find(kDefaultOptimizedLeafShaderName); } }

        static readonly Dictionary<string, GUIContent> s_Dictionary = new Dictionary<string, GUIContent>();

        internal void SetAnimsCallback(UnityAction callback)
        {
            foreach (var animBool in m_AnimBools)
                animBool.Value.valueChanged.AddListener(callback);
        }

        public void OnEnable(TreeData treeData)
        {
            m_TreeData = treeData;
        }

        public bool AreShadersCorrect()
        {
            // we can either have 0, 1 or two shaders of a particular type and
            // at most two shaders of both types together
            bool tooManyShaders = (m_BarkShaders.Count + m_LeafShaders.Count > 2);

            return m_WrongShaders.Count == 0 && !tooManyShaders;
        }

        public static string GetOptimizedShaderName(Shader shader)
        {
            if (shader)
                return ShaderUtil.GetDependency(shader, kOptimizedShaderDependency);

            return null;
        }

        private static bool IsTreeShader(Shader shader)
        {
            return IsTreeBarkShader(shader) || IsTreeLeafShader(shader);
        }

        public static bool IsTreeLeafShader(Shader shader)
        {
            return HasOptimizedShaderAndNameContains(shader, "leaves");
        }

        public static bool IsTreeBarkShader(Shader shader)
        {
            return HasOptimizedShaderAndNameContains(shader, "bark");
        }

        public bool GUITooManyShaders()
        {
            bool shadersFixed = GUITooManyShaders(NodeType.BarkNode);
            shadersFixed |= GUITooManyShaders(NodeType.LeafNode);

            if (shadersFixed)
                RefreshAllTreeShaders();

            return shadersFixed;
        }

        private bool GUITooManyShaders(NodeType nodeType)
        {
            string uniqueID = nodeType.ToString();

            if (CheckForTooManyShaders(nodeType))
                SetAnimBool(uniqueID, true, true);

            List<string> shaders = GetShadersListForNodeType(nodeType);
            GUIContent message = nodeType == NodeType.BarkNode ? GetGUIContent("This tree uses multiple bark shaders but only one bark shader can be used on a tree. Select which bark shader to apply to all the bark materials used by this tree.|") :
                GetGUIContent("This tree uses multiple leaf shaders but only one leaf shader can be used on a tree. Select which leaf shader to apply to all the leaf materials used by this tree.|");
            GUIContent button = GetGUIContent("Apply|Will change the shader in all the materials on that node type to the one selected.");

            int selectedIndex = GUIShowError(uniqueID, shaders, message, button, ConsoleWindow.iconError);

            if (selectedIndex >= 0)
            {
                Shader shader = Shader.Find(shaders[selectedIndex]);
                ChangeShaderOnMaterials(m_TreeData, shader, m_TreeData.root, nodeType);
                DisableAnimBool(uniqueID);
                RemoveSelectedIndex(uniqueID);
                return true;
            }

            return false;
        }

        public bool GUIWrongShader(string uniqueID, Material value, NodeType nodeType)
        {
            GUIContent message = GetGUIContent("This material does not use a tree shader. A tree shader has to contain the word 'leaves' or 'bark' in the name and the line: Dependency \"OptimizedShader\" = \"OPTIMIZED_SHADER_NAME\" in the code.|");
            GUIContent button = GetGUIContent("Apply|Will change the shader in the material to the one selected.");

            if (IsMaterialCorrect(value))
                return false;

            List<string> recommendedShaders = GetRecommendedShaders(nodeType);

            m_WrongShaders.Add(uniqueID);
            SetAnimBool(uniqueID, true, true);

            int selectedIndex = GUIShowError(uniqueID, recommendedShaders, message, button, ConsoleWindow.iconError);

            if (selectedIndex >= 0)
            {
                // shader in the material has been changed to one of the
                // recommended ones
                value.shader = Shader.Find(recommendedShaders[selectedIndex]);
                m_WrongShaders.Remove(uniqueID);
                DisableAnimBool(uniqueID);
                RemoveSelectedIndex(uniqueID);
                return true;
            }

            return false;
        }

        private int GUIShowError(string uniqueID, List<string> list, GUIContent message, GUIContent button, Texture2D icon)
        {
            int result = -1;
            if (m_AnimBools.ContainsKey(uniqueID))
            {
                if (EditorGUILayout.BeginFadeGroup(m_AnimBools[uniqueID].faded))
                {
                    GUILayout.BeginVertical(EditorStyles.helpBox);

                    GUIContent labelContent = message;
                    labelContent.image = icon;

                    GUILayout.Label(labelContent, EditorStyles.wordWrappedMiniLabel);

                    GUILayout.BeginHorizontal();

                    int selectedIndex = EditorGUILayout.Popup(GetSelectedIndex(uniqueID), list.ToArray());

                    SetSelectedIndex(uniqueID, selectedIndex);

                    if (GUILayout.Button(button, EditorStyles.miniButton))
                    {
                        // return the selected index only when the user approved the choice
                        result = selectedIndex;
                    }

                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                }
                EditorGUILayout.EndFadeGroup();
            }

            return result;
        }

        public void RefreshAllTreeShaders()
        {
            m_BarkShaders.Clear();
            m_LeafShaders.Clear();

            GetAllTreeShaders(m_TreeData, m_BarkShaders, m_LeafShaders, m_TreeData.root);
        }

        private static void GetAllTreeShaders(TreeData treeData, List<string> barkShaders, List<string> leafShaders, TreeGroup group)
        {
            if (group is TreeGroupBranch)
            {
                TreeGroupBranch tgb = group as TreeGroupBranch;
                AddShaderFromMaterial(tgb.materialBranch, barkShaders, leafShaders);
                AddShaderFromMaterial(tgb.materialBreak, barkShaders, leafShaders);
                AddShaderFromMaterial(tgb.materialFrond, barkShaders, leafShaders);
            }
            else if (group is TreeGroupLeaf)
            {
                TreeGroupLeaf tgl = group as TreeGroupLeaf;
                AddShaderFromMaterial(tgl.materialLeaf, barkShaders, leafShaders);
            }

            foreach (int id in group.childGroupIDs)
            {
                TreeGroup childGroup = treeData.GetGroup(id);
                GetAllTreeShaders(treeData, barkShaders, leafShaders, childGroup);
            }
        }

        public bool NodeHasWrongMaterial(TreeGroup group)
        {
            bool result = false;
            if (group is TreeGroupBranch)
            {
                TreeGroupBranch tgb = group as TreeGroupBranch;
                result |= !IsMaterialCorrect(tgb.materialBranch);
                result |= !IsMaterialCorrect(tgb.materialBreak);
                result |= !IsMaterialCorrect(tgb.materialFrond);
            }
            else if (group is TreeGroupLeaf)
            {
                TreeGroupLeaf tgl = group as TreeGroupLeaf;
                result |= !IsMaterialCorrect(tgl.materialLeaf);
            }

            return result;
        }

        private static bool IsMaterialCorrect(Material material)
        {
            // material is correct when it uses a tree shader
            // or is null
            if (material && !IsTreeShader(material.shader))
                return false;

            return true;
        }

        private List<string> GetShadersListForNodeType(NodeType nodeType)
        {
            if (nodeType == NodeType.BarkNode)
                return m_BarkShaders;

            return m_LeafShaders;
        }

        private List<string> GetShadersListOppositeToNodeType(NodeType nodeType)
        {
            if (nodeType == NodeType.BarkNode)
                return GetShadersListForNodeType(NodeType.LeafNode);

            return GetShadersListForNodeType(NodeType.BarkNode);
        }

        private static string GetDefaultShader(NodeType nodeType)
        {
            if (nodeType == NodeType.BarkNode)
                return kDefaultBarkShaderName;

            return kDefaultLeafShaderName;
        }

        private List<string> GetRecommendedShaders(NodeType nodeType)
        {
            List<string> recommendedShaders = new List<string>(3);

            List<string> shaders = GetShadersListForNodeType(nodeType);
            List<string> oppositeShaders = GetShadersListOppositeToNodeType(nodeType);

            if (shaders.Count == 1 || (shaders.Count == 2 && oppositeShaders.Count == 0))
            {
                foreach (string shader in shaders)
                {
                    recommendedShaders.Add(shader);
                }
            }
            if (shaders.Count == 0)
                recommendedShaders.Add(GetDefaultShader(nodeType));

            return recommendedShaders;
        }

        private bool CheckForTooManyShaders(NodeType nodeType)
        {
            List<string> shaders = GetShadersListForNodeType(nodeType);
            List<string> oppositeShaders = GetShadersListOppositeToNodeType(nodeType);

            if (shaders.Count > 2 || (shaders.Count == 2 && oppositeShaders.Count > 0))
                return true;

            return false;
        }

        private static bool HasOptimizedShaderAndNameContains(Shader shader, string name)
        {
            if (GetOptimizedShaderName(shader) != null)
            {
                if (shader.name.ToLower().Contains(name))
                    return true;
            }

            return false;
        }

        private static void AddShaderFromMaterial(Material material, List<string> barkShaders, List<string> leafShaders)
        {
            if (material && material.shader)
            {
                Shader shader = material.shader;
                if (IsTreeBarkShader(shader) && !barkShaders.Contains(shader.name))
                    barkShaders.Add(shader.name);
                else if (IsTreeLeafShader(material.shader) && !leafShaders.Contains(shader.name))
                    leafShaders.Add(shader.name);
            }
        }

        private static void ChangeShaderOnMaterial(Material material, Shader shader)
        {
            if (material && shader)
                material.shader = shader;
        }

        private static void ChangeShaderOnMaterials(TreeData treeData, Shader shader, TreeGroup group, NodeType nodeType)
        {
            if (group is TreeGroupBranch && nodeType == NodeType.BarkNode)
            {
                TreeGroupBranch tgb = group as TreeGroupBranch;
                ChangeShaderOnMaterial(tgb.materialBranch, shader);
                ChangeShaderOnMaterial(tgb.materialBreak, shader);
                ChangeShaderOnMaterial(tgb.materialFrond, shader);
            }
            else if (group is TreeGroupLeaf && nodeType == NodeType.LeafNode)
            {
                TreeGroupLeaf tgl = group as TreeGroupLeaf;
                ChangeShaderOnMaterial(tgl.materialLeaf, shader);
            }

            foreach (int id in group.childGroupIDs)
            {
                TreeGroup childGroup = treeData.GetGroup(id);
                ChangeShaderOnMaterials(treeData, shader, childGroup, nodeType);
            }
        }

        private void RemoveSelectedIndex(string contentID)
        {
            m_SelectedShader.Remove(contentID);
        }

        private void SetSelectedIndex(string contentID, int value)
        {
            if (m_SelectedShader.ContainsKey(contentID))
                m_SelectedShader[contentID] = value;
            else
                m_SelectedShader.Add(contentID, value);
        }

        private int GetSelectedIndex(string contentID)
        {
            if (!m_SelectedShader.ContainsKey(contentID))
                m_SelectedShader.Add(contentID, 0);

            return m_SelectedShader[contentID];
        }

        private void SetAnimBool(string contentID, bool target, bool value)
        {
            SetAnimBool(contentID, target);
            m_AnimBools[contentID].value = value;
        }

        private void SetAnimBool(string contentID, bool target)
        {
            AnimBool animBool;
            if (!m_AnimBools.ContainsKey(contentID))
            {
                animBool = new AnimBool();
                m_AnimBools.Add(contentID, animBool);
            }
            else
            {
                animBool = m_AnimBools[contentID];
            }
            animBool.target = target;
        }

        private void DisableAnimBool(string contentID)
        {
            if (m_AnimBools.ContainsKey(contentID))
            {
                m_AnimBools[contentID].target = false;
            }
        }

        static public GUIContent GetGUIContent(string id)
        {
            // Already stored?
            if (s_Dictionary.ContainsKey(id))
                return s_Dictionary[id];

            // Fetch it..
            string uiString = id;
            if (uiString == null) return new GUIContent(id, "");

            GUIContent content = new GUIContent(ExtractLabel(uiString), ExtractTooltip(uiString));

            s_Dictionary.Add(id, content);

            return content;
        }

        static public string ExtractLabel(string uiString)
        {
            string[] parts = uiString.Split('|');
            return parts[0].Trim();
        }

        static public string ExtractTooltip(string uiString)
        {
            string[] parts = uiString.Split('|');
            if (parts.Length > 1)
                return parts[1].Trim();
            return string.Empty;
        }
    }
}
