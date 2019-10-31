// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace TreeEditor
{
    [CustomEditor(typeof(Tree))]
    internal class TreeEditor : Editor
    {
        private enum PropertyType
        {
            Normal = 0,
            FullUndo = 1,
            FullUpdate = 2,
            FullUndoUpdate = 3
        }

        public enum EditMode
        {
            None = -1,
            MoveNode = 0,
            RotateNode = 1,
            Freehand = 2,
            Parameter = 3,
            Everything = 4,
            Delete = 5,
            CreateGroup = 6,
            Duplicate = 7
        }

        private static Vector3 s_StartPosition;
        private static int s_SelectedPoint = -1;
        private static TreeNode s_SelectedNode;
        private static TreeGroup s_SelectedGroup;

        private static string[] geometryModeOptions =
        {
            L10n.Tr("Plane|"),
            L10n.Tr("Cross|"),
            L10n.Tr("TriCross|"),
            L10n.Tr("Billboard|"),
            L10n.Tr("Mesh|")
        };

        private static string createNewTreeString = L10n.Tr("Create New Tree");

        private static string createWindZoneString = L10n.Tr("Create Wind Zone");

        private static string[] toolbarIconStringsBranch =
        {
            "TreeEditor.BranchTranslate",
            "TreeEditor.BranchRotate",
            "TreeEditor.BranchFreeHand",
        };

        private static string[] toolbarContentStringsBranch =
        {
            L10n.Tr("Move Branch|Select a branch spline point and drag to move it."),
            L10n.Tr("Rotate Branch|Select a branch spline point and drag to rotate it."),
            L10n.Tr("Free Hand|Click on a branch spline node and drag to draw a new shape.")
        };

        private static string[] toolbarIconStringsLeaf =
        {
            "TreeEditor.LeafTranslate",
            "TreeEditor.LeafRotate",
        };

        private static string[] toolbarContentStringsLeaf =
        {
            L10n.Tr("Move Leaf|Select a leaf spline point and drag to move it."),
            L10n.Tr("Rotate Leaf|Select a leaf spline point and drag to rotate it.")
        };

        private static string[] inspectorDistributionPopupOptions =
        {
            L10n.Tr("Random|"),
            L10n.Tr("Alternate|"),
            L10n.Tr("Opposite|"),
            L10n.Tr("Whorled|")
        };

        private static string treeSeedString = L10n.Tr("Tree Seed|The global seed that affects the entire tree. Use it to randomize your tree, while keeping the general structure of it.");

        private static string areaSpreadString = L10n.Tr("Area Spread|Adjusts the spread of trunk nodes. Has an effect only if you have more than one trunk.");

        private static string groundOffsetString = L10n.Tr("Ground Offset|Adjusts the offset of trunk nodes on Y axis.");

        private static string lodQualityString = L10n.Tr("LOD Quality|Defines the level-of-detail for the entire tree. A low value will make the tree less complex, a high value will make the tree more complex. Check the statistics in the hierarchy view to see the current complexity of the mesh.");

        private static string ambientOcclusionString = L10n.Tr("Ambient Occlusion|Toggles ambient occlusion on or off.");

        private static string aoDensityString = L10n.Tr("AO Density|Adjusts the density of ambient occlusion.");

        private static string translucencyColorString = L10n.Tr("Translucency Color|The color that will be multiplied in when the leaves are backlit.");

        private static string transViewDepString = L10n.Tr("Trans. View Dep.|Translucency view dependency. Fully view dependent translucency is relative to the angle between the view direction and the light direction. View independent is relative to the angle between the leaf's normal vector and the light direction.");

        private static string alphaCutoffString = L10n.Tr("Alpha Cutoff|Alpha values from the base texture smaller than the alpha cutoff are clipped creating a cutout.");

        private static string shadowStrengthString = L10n.Tr("Shadow Strength|Makes the shadows on the leaves less harsh. Since it scales all the shadowing that the leaves receive, it should be used with care for trees that are e.g. in a shadow of a mountain.");

        private static string shadowOffsetString = L10n.Tr("Shadow Offset|Scales the values from the Shadow Offset texture set in the source material. It is used to offset the position of the leaves when collecting the shadows, so that the leaves appear as if they were not placed on one quad. It's especially important for billboarded leaves and they should have brighter values in the center of the texture and darker ones at the border. Start out with a black texture and add different shades of gray per leaf.");

        private static string[] leafMaterialOptions =
        {
            L10n.Tr("Full Res|"),
            L10n.Tr("Half Res|"),
            L10n.Tr("Quarter Res|"),
            L10n.Tr("One-8th Res|"),
            L10n.Tr("One-16th Res|")
        };

        private static string shadowCasterResString = L10n.Tr("Shadow Caster Res.|Defines the resolution of the texture atlas containing alpha values from source diffuse textures. The atlas is used when the leaves are rendered as shadow casters. Using lower resolution might increase performance.");

        private static string lodMultiplierString = L10n.Tr("LOD Multiplier|Adjusts the quality of this group relative to tree's LOD Quality, so that it is of either higher or lower quality than the rest of the tree.");

        private static string[] categoryNamesOptions =
        {
            L10n.Tr("Branch Only|"),
            L10n.Tr("Branch + Fronds|"),
            L10n.Tr("Fronds Only|")
        };

        private static string geometryModeString = L10n.Tr("Geometry Mode|Type of geometry for this branch group.");
        private static string branchMaterialString = L10n.Tr("Branch Material|The primary material for the branches.");

        private static string breakMaterialString = L10n.Tr("Break Material|Material for capping broken branches.");

        private static string frondMaterialString = L10n.Tr("Frond Material|Material for the fronds.");

        private static string lengthString = L10n.Tr("Length|Adjusts the length of the branches.");

        private static string relativeLengthString = L10n.Tr("Relative Length|Determines whether the radius of a branch is affected by its length.");

        private static string radiusString = L10n.Tr("Radius|Adjusts the radius of the branches, use the curve to fine-tune the radius along the length of the branches.");

        private static string capSmoothingString = L10n.Tr("Cap Smoothing|Defines the roundness of the cap/tip of the branches. Useful for cacti.");

        private static string crinklynessString = L10n.Tr("Crinkliness|Adjusts how crinkly/crooked the branches are, use the curve to fine-tune.");

        private static string seekSunString = L10n.Tr("Seek Sun|Use the curve to adjust how the branches are bent upwards/downwards and the slider to change the scale.");

        private static string noiseString = L10n.Tr("Noise|Overall noise factor, use the curve to fine-tune.");

        private static string noiseScaleUString = L10n.Tr("Noise Scale U|Scale of the noise around the branch, lower values will give a more wobbly look, while higher values gives a more stochastic look.");

        private static string noiseScaleVString = L10n.Tr("Noise Scale V|Scale of the noise along the branch, lower values will give a more wobbly look, while higher values gives a more stochastic look.");

        private static string flareRadiusString = L10n.Tr("Flare Radius|The radius of the flares, this is added to the main radius, so a zero value means no flares.");

        private static string flareHeightString = L10n.Tr("Flare Height|Defines how far up the trunk the flares start.");

        private static string flareNoiseString = L10n.Tr("Flare Noise|Defines the noise of the flares, lower values will give a more wobbly look, while higher values gives a more stochastic look.");

        private static string weldLengthString = L10n.Tr("Weld Length|Defines how far up the branch the weld spread starts.");

        private static string spreadTopString = L10n.Tr("Spread Top|Weld's spread factor on the top-side of the branch, relative to its parent branch. Zero means no spread.");

        private static string spreadBottomString = L10n.Tr("Spread Bottom|Weld's spread factor on the bottom-side of the branch, relative to its parent branch. Zero means no spread.");

        private static string breakChanceString = L10n.Tr("Break Chance|Chance of a branch breaking, i.e. 0 = no branches are broken, 0.5 = half of the branches are broken, 1.0 = all the branches are broken.");

        private static string breakLocationString = L10n.Tr("Break Location|This range defines where the branches will be broken. Relative to the length of the branch.");

        private static string frondCountString = L10n.Tr("Frond Count|Defines the number of fronds per branch. Fronds are always evenly spaced around the branch.");

        private static string frondWidthString = L10n.Tr("Frond Width|The width of the fronds, use the curve to adjust the specific shape along the length of the branch.");

        private static string frondRangeString = L10n.Tr("Frond Range|Defines the starting and ending point of the fronds.");

        private static string frondRotationString = L10n.Tr("Frond Rotation|Defines rotation around the parent branch.");

        private static string frondCreaseString = L10n.Tr("Frond Crease|Adjust to crease / fold the fronds.");

        private static string geometryModeLeafString = L10n.Tr("Geometry Mode|The type of geometry created. You can use a custom mesh, by selecting the Mesh option, ideal for flowers, fruits, etc.");

        private static string materialLeafString = L10n.Tr("Material|Material used for the leaves.");

        private static string meshString = L10n.Tr("Mesh|Mesh used for the leaves.");

        private static string sizeString = L10n.Tr("Size|Adjusts the size of the leaves, use the range to adjust the minimum and the maximum size.");

        private static string perpendicularAlignString = L10n.Tr("Perpendicular Align|Adjusts whether the leaves are aligned perpendicular to the parent branch.");

        private static string horizontalAlignString = L10n.Tr("Horizontal Align|Adjusts whether the leaves are aligned horizontally.");

        public static EditMode editMode
        {
            get
            {
                switch (Tools.current)
                {
                    case Tool.View:
                        s_EditMode = EditMode.None;
                        break;
                    case Tool.Move:
                        s_EditMode = EditMode.MoveNode;
                        break;
                    case Tool.Rotate:
                        s_EditMode = EditMode.RotateNode;
                        break;
                    case Tool.Scale:
                        s_EditMode = EditMode.None;
                        break;
                    default:
                        break;
                }

                return s_EditMode;
            }
            set
            {
                switch (value)
                {
                    case EditMode.None:
                        break;
                    case EditMode.MoveNode:
                        Tools.current = Tool.Move;
                        break;
                    case EditMode.RotateNode:
                        Tools.current = Tool.Rotate;
                        break;
                    default:
                        Tools.current = Tool.None;
                        break;
                }
                s_EditMode = value;
            }
        }
        private static EditMode s_EditMode = EditMode.MoveNode;

        private static string s_SavedSourceMaterialsHash;
        private static float s_CutoutMaterialHashBeforeUndo;

        private bool m_WantCompleteUpdate = false;
        private bool m_WantedCompleteUpdateInPreviousFrame;

        private const float kSectionSpace = 10.0f;
        private const float kCurveSpace = 50.0f;
        private bool m_SectionHasCurves = true;

        private static int s_ShowCategory = -1;
        private readonly Rect m_CurveRangesA = new Rect(0, 0, 1, 1); // Range 0..1
        private readonly Rect m_CurveRangesB = new Rect(0, -1, 1, 2); // Range -1..1

        private static readonly Color s_GroupColor = new Color(1, 0, 1, 1);
        private static readonly Color s_NormalColor = new Color(1, 1, 0, 1);

        private readonly TreeEditorHelper m_TreeEditorHelper = new TreeEditorHelper();

        // Section animation state
        private readonly AnimBool[] m_SectionAnimators = new AnimBool[6];

        private Vector3 m_LockedWorldPos = Vector3.zero;
        private Matrix4x4 m_StartMatrix = Matrix4x4.identity;
        private Quaternion m_StartPointRotation = Quaternion.identity;
        private bool m_StartPointRotationDirty = false;
        private Quaternion m_GlobalToolRotation = Quaternion.identity;
        private TreeSpline m_TempSpline;



        [MenuItem("GameObject/3D Object/Tree", false, 3001)]
        static void CreateNewTree(MenuCommand menuCommand)
        {
            // Assets
            Mesh meshAsset = new Mesh();
            meshAsset.name = "Mesh";
            Material materialSolidAsset = new Material(TreeEditorHelper.DefaultOptimizedBarkShader);
            materialSolidAsset.name = "Optimized Bark Material";
            materialSolidAsset.hideFlags = HideFlags.NotEditable | HideFlags.HideInInspector;

            Material materialCutoutAsset = new Material(TreeEditorHelper.DefaultOptimizedLeafShader);
            materialCutoutAsset.name = "Optimized Leaf Material";
            materialCutoutAsset.hideFlags = HideFlags.NotEditable | HideFlags.HideInInspector;

            // Create optimized tree mesh prefab the user can instantiate in the scene
            GameObject prefabInstance = new GameObject("OptimizedTree", typeof(Tree), typeof(MeshFilter), typeof(MeshRenderer));

            // Create a tree prefab
            string path = "Assets/Tree.prefab";
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAssetAndConnect(prefabInstance, path, InteractionMode.AutomatedAction); // saving the unfinished prefab here allows assets to be added

            prefabInstance.GetComponent<MeshFilter>().sharedMesh = meshAsset;

            // Add mesh, material and textures to the prefab

            TreeData data = ScriptableObject.CreateInstance<TreeData>();
            data.name = "Tree Data";
            data.Initialize();
            data.optimizedSolidMaterial = materialSolidAsset;
            data.optimizedCutoutMaterial = materialCutoutAsset;
            data.mesh = meshAsset;

            prefabInstance.GetComponent<Tree>().data = data;

            AssetDatabase.AddObjectToAsset(meshAsset, prefabAsset);
            AssetDatabase.AddObjectToAsset(materialSolidAsset, prefabAsset);
            AssetDatabase.AddObjectToAsset(materialCutoutAsset, prefabAsset);
            AssetDatabase.AddObjectToAsset(data, prefabAsset);

            GameObjectUtility.SetParentAndAlign(prefabInstance, menuCommand.context as GameObject);

            // Store Creation undo
            // @TODO: THIS DOESN"T UNDO ASSET CREATION!
            Undo.RegisterCreatedObjectUndo(prefabInstance, createNewTreeString);

            Material[] materials;
            data.UpdateMesh(prefabInstance.transform.worldToLocalMatrix, out materials);

            AssignMaterials(prefabInstance.GetComponent<Renderer>(), materials);
            PrefabUtility.ApplyPrefabInstance(prefabInstance);

            StageUtility.PlaceGameObjectInCurrentStage(prefabInstance);
            Selection.activeObject = prefabInstance;
            Undo.RegisterCreatedObjectUndo(prefabInstance, "Create Tree");
        }

        static TreeData GetTreeData(Tree tree)
        {
            if (tree == null)
                return null;

            return tree.data as TreeData;
        }

        static void PreviewMesh(Tree tree)
        {
            PreviewMesh(tree, true);
        }

        static void PreviewMesh(Tree tree, bool callExitGUI)
        {
            TreeData treeData = GetTreeData(tree);
            if (treeData == null)
                return;

            Profiler.BeginSample("TreeEditor.PreviewMesh");

            Material[] materials;
            treeData.PreviewMesh(tree.transform.worldToLocalMatrix, out materials);

            AssignMaterials(tree.GetComponent<Renderer>(), materials);

            Profiler.EndSample(); // TreeEditor.PreviewMesh

            if (callExitGUI)
            {
                GUIUtility.ExitGUI();
            }
        }

        static void UpdateMesh(Tree tree)
        {
            UpdateMesh(tree, true);
        }

        static void AssignMaterials(Renderer renderer, Material[] materials)
        {
            if (renderer != null)
            {
                if (materials == null)
                    materials = new Material[0];

                renderer.sharedMaterials = materials;
            }
        }

        static void UpdateMesh(Tree tree, bool callExitGUI)
        {
            TreeData treeData = GetTreeData(tree);
            if (treeData == null)
                return;

            Profiler.BeginSample("TreeEditor.UpdateMesh");

            Material[] materials;
            treeData.UpdateMesh(tree.transform.worldToLocalMatrix, out materials);

            AssignMaterials(tree.GetComponent<Renderer>(), materials);

            // If a Tree is opened in Prefab Mode the Tree should be saved by the Prefab Mode logic (manual- or auto-save)
            // Note: When opened in Prefab Mode the Tree will be unpacked (have no connection to its Prefab).
            // so we have to dirty its scene instead.
            if (PrefabUtility.IsPartOfNonAssetPrefabInstance(tree.gameObject))
            {
                PrefabUtility.ApplyPrefabInstance(tree.gameObject);
            }
            else
            {
                if (tree.gameObject.scene.IsValid())
                    EditorSceneManager.MarkSceneDirty(tree.gameObject.scene);
            }

            s_SavedSourceMaterialsHash = treeData.materialHash;

            Profiler.EndSample(); // TreeEditor.UpdateMesh

            if (callExitGUI)
            {
                GUIUtility.ExitGUI();
            }
        }

        [MenuItem("GameObject/3D Object/Wind Zone", false, 3002)]
        static void CreateWindZone(MenuCommand menuCommand)
        {
            // Create a wind zone
            GameObject wind = CreateDefaultWindZone();
            GameObjectUtility.SetParentAndAlign(wind, menuCommand.context as GameObject);
            StageUtility.PlaceGameObjectInCurrentStage(wind);
            Selection.activeObject = wind;
            Undo.RegisterCreatedObjectUndo(wind, "Create Wind Zone");
        }

        static GameObject CreateDefaultWindZone()
        {
            // Create a wind zone
            GameObject wind = ObjectFactory.CreateGameObject("WindZone", typeof(WindZone));
            Undo.SetCurrentGroupName(createWindZoneString);
            return wind;
        }

        private float FindClosestOffset(TreeData data, Matrix4x4 objMatrix, TreeNode node, Ray mouseRay, ref float rotation)
        {
            TreeGroup group = data.GetGroup(node.groupID);
            if (group == null) return 0.0f;
            if (group.GetType() != typeof(TreeGroupBranch)) return 0.0f;

            // Must fetch data directly
            data.ValidateReferences();

            Matrix4x4 matrix = objMatrix * node.matrix;

            float step = 1.0f / (node.spline.GetNodeCount() * 10.0f);

            float closestOffset = 0.0f;
            float closestDist = 10000000.0f;

            Vector3 closestSegment = Vector3.zero;
            Vector3 closestRay = Vector3.zero;
            Vector3 pointA = matrix.MultiplyPoint(node.spline.GetPositionAtTime(0.0f));
            for (float t = step; t <= 1.0f; t += step)
            {
                Vector3 pointB = matrix.MultiplyPoint(node.spline.GetPositionAtTime(t));
                float dist = 0.0f;
                float s = 0.0f;
                closestSegment = MathUtils.ClosestPtSegmentRay(pointA, pointB, mouseRay, out dist, out s, out closestRay);
                if (dist < closestDist)
                {
                    closestOffset = t - step + (step * s);
                    closestDist = dist;

                    //
                    // Compute local rotation around spline
                    //
                    // 1: Closest point on ray compared to sphere positioned at spline position..
                    // 2: Transform to local space of the spline (oriented to the spline)
                    // 3: Compute angle in Degrees..
                    //
                    float radius = node.GetRadiusAtTime(closestOffset);
                    float tt = 0.0f;
                    if (MathUtils.ClosestPtRaySphere(mouseRay, closestSegment, radius, ref tt, ref closestRay))
                    {
                        Matrix4x4 invMatrix = (matrix * node.GetLocalMatrixAtTime(closestOffset)).inverse;
                        Vector3 raySegmentVector = closestRay - closestSegment;
                        raySegmentVector = invMatrix.MultiplyVector(raySegmentVector);
                        rotation = Mathf.Atan2(raySegmentVector.x, raySegmentVector.z) * Mathf.Rad2Deg;
                    }
                }
                pointA = pointB;
            }

            // Clear directly referenced stuff
            data.ClearReferences();

            return closestOffset;
        }

        void SelectGroup(TreeGroup group)
        {
            if (group == null)
                Debug.Log("GROUP SELECTION IS NULL!");

            if (m_TreeEditorHelper.NodeHasWrongMaterial(group))
            {
                // automatically show the Geometry tab if the selected node has a wrong shader
                s_ShowCategory = 1;
            }

            s_SelectedGroup = group;
            s_SelectedNode = null;
            s_SelectedPoint = -1;

            Tree tree = target as Tree;
            if (tree == null) return;
            Renderer treeRenderer = tree.GetComponent<Renderer>();
            var hidden = !(s_SelectedGroup is TreeGroupRoot);
            EditorUtility.SetSelectedRenderState(
                treeRenderer,
                hidden
                ? EditorSelectedRenderState.Hidden
                : EditorSelectedRenderState.Wireframe | EditorSelectedRenderState.Highlight);
        }

        void SelectNode(TreeNode node, TreeData treeData)
        {
            SelectGroup(node == null ? treeData.root : treeData.GetGroup(node.groupID));
            s_SelectedNode = node;
            s_SelectedPoint = -1;
        }

        void DuplicateSelected(TreeData treeData)
        {
            // Store complete undo
            UndoStoreSelected(EditMode.Duplicate);

            if (s_SelectedNode != null)
            {
                // duplicate node
                s_SelectedNode = treeData.DuplicateNode(s_SelectedNode);
                s_SelectedGroup.Lock();
            }
            else
            {
                // duplicate group
                SelectGroup(treeData.DuplicateGroup(s_SelectedGroup));
            }

            m_WantCompleteUpdate = true;
            UpdateMesh(target as Tree);
            m_WantCompleteUpdate = false;
        }

        void DeleteSelected(TreeData treeData)
        {
            // Store complete undo
            UndoStoreSelected(EditMode.Delete);

            //
            if (s_SelectedNode != null)
            {
                if (s_SelectedPoint >= 1)
                {
                    if (s_SelectedNode.spline.nodes.Length > 2)
                    {
                        //Undo.RegisterSceneUndo("Delete");
                        if (s_SelectedGroup.lockFlags == 0)
                        {
                            s_SelectedGroup.Lock();
                        }
                        s_SelectedNode.spline.RemoveNode(s_SelectedPoint);
                        s_SelectedPoint = Mathf.Max(s_SelectedPoint - 1, 0);
                    }
                }
                else
                {
                    if ((s_SelectedGroup != null) && (s_SelectedGroup.nodeIDs.Length == 1))
                    {
                        s_SelectedNode = null;
                        DeleteSelected(treeData);
                        return;
                    }

                    treeData.DeleteNode(s_SelectedNode);
                    s_SelectedGroup.Lock();
                    SelectGroup(s_SelectedGroup);
                }
            }
            else if (s_SelectedGroup != null)
            {
                TreeGroup parentGroup = treeData.GetGroup(s_SelectedGroup.parentGroupID);
                if (parentGroup == null)
                {
                    return;
                }
                treeData.DeleteGroup(s_SelectedGroup);
                SelectGroup(parentGroup);
            }

            m_WantCompleteUpdate = true;
            UpdateMesh(target as Tree);
            m_WantCompleteUpdate = false;
        }

        void VerifySelection(TreeData treeData)
        {
            TreeGroup groupToSelect = s_SelectedGroup;
            TreeNode nodeToSelect = s_SelectedNode;

            if (groupToSelect != null)
            {
                groupToSelect = treeData.GetGroup(groupToSelect.uniqueID);
            }
            if (nodeToSelect != null)
            {
                nodeToSelect = treeData.GetNode(nodeToSelect.uniqueID);
            }

            // on first start, we need to verify the selected part actually belongs to the current tree);
            if (groupToSelect != treeData.root && groupToSelect != null)
            {
                // Make sure this group is part of this tree..
                if (!treeData.IsAncestor(treeData.root, groupToSelect))
                {
                    // not part of this tree..
                    groupToSelect = null;
                    nodeToSelect = null;
                }
            }

            // Clear selected node if it's been deleted
            if ((nodeToSelect != null) && (treeData.GetGroup(nodeToSelect.groupID) != groupToSelect))
            {
                nodeToSelect = null;
            }

            if (groupToSelect == null)
                groupToSelect = treeData.root;

            if (s_SelectedGroup != null && groupToSelect == s_SelectedGroup) return;

            SelectGroup(groupToSelect);

            if (nodeToSelect != null)
                SelectNode(nodeToSelect, treeData);
        }

        //
        // Check for hot key events..
        //
        bool OnCheckHotkeys(TreeData treeData, bool checkFrameSelected)
        {
            switch (Event.current.type)
            {
                case EventType.ValidateCommand:

                    if (Event.current.commandName == "SoftDelete" || Event.current.commandName == "Delete")
                    {
                        if ((s_SelectedGroup != null) && (s_SelectedGroup != treeData.root))
                        {
                            Event.current.Use();
                        }
                    }
                    if (Event.current.commandName == "FrameSelected")
                    {
                        if (checkFrameSelected)
                        {
                            Event.current.Use();
                        }
                    }
                    if (Event.current.commandName == "UndoRedoPerformed")
                    {
                        Event.current.Use();
                    }
                    break;

                case EventType.ExecuteCommand:
                    //Debug.Log("Execute command " + Event.current.commandName);
                    if (Event.current.commandName == "SoftDelete" || Event.current.commandName == "Delete")
                    {
                        if ((s_SelectedGroup != null) && (s_SelectedGroup != treeData.root))
                        {
                            DeleteSelected(treeData);
                            Event.current.Use();
                        }
                    }
                    if (Event.current.commandName == "FrameSelected")
                    {
                        if (checkFrameSelected)
                        {
                            FrameSelected(target as Tree);
                            Event.current.Use();
                        }
                    }
                    if (Event.current.commandName == "UndoRedoPerformed")
                    {
                        float materialHash = GenerateMaterialHash(treeData.optimizedCutoutMaterial);
                        if (s_CutoutMaterialHashBeforeUndo != materialHash)
                        {
                            // Cutout material's hash has changed, so we're undoing property changes on the
                            // generated material only, no need to update the whole tree.
                            s_CutoutMaterialHashBeforeUndo = materialHash;
                        }
                        else
                        {
                            // After undo: restore the material hash of the materials that were used to create
                            // texture atlases currently stored on disk. That will make sure that performing undo
                            // will regenerate atlases when necessary.
                            treeData.materialHash = s_SavedSourceMaterialsHash;

                            m_StartPointRotationDirty = true;

                            UpdateMesh(target as Tree);
                        }
                        Event.current.Use();

                        // Must exit to update properly
                        return true;
                    }
                    if (Event.current.commandName == "CurveChangeCompleted")
                    {
                        UpdateMesh(target as Tree);
                        Event.current.Use();
                        return true;
                    }
                    break;
            }

            return false;
        }

        Bounds CalcBounds(TreeData treeData, Matrix4x4 objMatrix, TreeNode node)
        {
            Matrix4x4 nodeMatrix = objMatrix * node.matrix;
            Bounds bounds;

            if (treeData.GetGroup(node.groupID).GetType() == typeof(TreeGroupBranch) && node.spline != null && node.spline.nodes.Length > 0)
            {
                // Has a spline
                bounds = new Bounds(nodeMatrix.MultiplyPoint(node.spline.nodes[0].point), Vector3.zero);
                for (int i = 1; i < node.spline.nodes.Length; i++)
                {
                    bounds.Encapsulate(nodeMatrix.MultiplyPoint(node.spline.nodes[i].point));
                }
            }
            else
            {
                // no spline
                bounds = new Bounds(nodeMatrix.MultiplyPoint(Vector3.zero), Vector3.zero);
            }

            return bounds;
        }

        void FrameSelected(Tree tree)
        {
            TreeData treeData = GetTreeData(tree);

            Matrix4x4 objMatrix = tree.transform.localToWorldMatrix;

            Bounds bounds = new Bounds(objMatrix.MultiplyPoint(Vector3.zero), Vector3.zero);

            // Compute bounds for selection
            if (s_SelectedGroup != null)
            {
                if (s_SelectedGroup.GetType() == typeof(TreeGroupRoot))
                {
                    MeshFilter meshFilter = tree.GetComponent<MeshFilter>();
                    if ((meshFilter == null) || (s_SelectedGroup.childGroupIDs.Length == 0))
                    {
                        float rad = s_SelectedGroup.GetRootSpread();
                        bounds = new Bounds(objMatrix.MultiplyPoint(Vector3.zero), objMatrix.MultiplyVector(new Vector3(rad, rad, rad)));
                    }
                    else
                    {
                        bounds = new Bounds(objMatrix.MultiplyPoint(meshFilter.sharedMesh.bounds.center), objMatrix.MultiplyVector(meshFilter.sharedMesh.bounds.size));
                    }
                }
                else if (s_SelectedNode != null)
                {
                    if (s_SelectedGroup.GetType() == typeof(TreeGroupLeaf) && s_SelectedPoint >= 0)
                    {
                        // Focus on selected point
                        Matrix4x4 nodeMatrix = objMatrix * s_SelectedNode.matrix;
                        bounds = new Bounds(nodeMatrix.MultiplyPoint(s_SelectedNode.spline.nodes[s_SelectedPoint].point), Vector3.zero);
                    }
                    else
                    {
                        bounds = CalcBounds(treeData, objMatrix, s_SelectedNode);
                    }
                }
                else
                {
                    // Focus on selected group
                    for (int i = 0; i < s_SelectedGroup.nodeIDs.Length; i++)
                    {
                        Bounds temp = CalcBounds(treeData, objMatrix, treeData.GetNode(s_SelectedGroup.nodeIDs[i]));
                        if (i == 0)
                        {
                            bounds = temp;
                        }
                        else
                        {
                            bounds.Encapsulate(temp);
                        }
                    }
                }
            }

            Vector3 center = bounds.center;
            float size = bounds.size.magnitude + 1.0f;

            SceneView currentSceneView = SceneView.lastActiveSceneView;
            if (currentSceneView)
            {
                currentSceneView.LookAt(center, currentSceneView.rotation, size);
            }
        }

        void UndoStoreSelected(EditMode mode)
        {
            //
            // Store undo
            //
            TreeData treeData = GetTreeData(target as Tree);
            if (!treeData)
                return;

            //
            //
            Object[] objects;

            objects = new Object[1];
            objects[0] = treeData;

            EditorUtility.SetDirty(treeData);

            switch (mode)
            {
                case EditMode.Freehand:
                    Undo.RegisterCompleteObjectUndo(objects, "Freehand Drawing");
                    break;
                case EditMode.RotateNode:
                    Undo.RegisterCompleteObjectUndo(objects, "Rotate");
                    break;
                case EditMode.MoveNode:
                    Undo.RegisterCompleteObjectUndo(objects, "Move");
                    break;
                case EditMode.Parameter:
                    Undo.RegisterCompleteObjectUndo(objects, "Parameter Change");
                    break;
                case EditMode.Everything:
                    Undo.RegisterCompleteObjectUndo(objects, "Parameter Change");
                    break;
                case EditMode.Delete:
                    Undo.RegisterCompleteObjectUndo(objects, "Delete");
                    break;
                case EditMode.CreateGroup:
                    Undo.RegisterCompleteObjectUndo(objects, "Create Group");
                    break;
                case EditMode.Duplicate:
                    Undo.RegisterCompleteObjectUndo(objects, "Duplicate");
                    break;
            }
        }

        void RepaintGUIView()
        {
            GUIView.current.Repaint();
        }

        void OnEnable()
        {
            Tree tree = target as Tree;
            if (tree == null) return;

            TreeData treeData = GetTreeData(tree);
            if (treeData == null) return;

            m_TreeEditorHelper.OnEnable(treeData);
            m_TreeEditorHelper.SetAnimsCallback(RepaintGUIView);

            for (int i = 0; i < m_SectionAnimators.Length; i++)
                m_SectionAnimators[i] = new AnimBool(s_ShowCategory == i, Repaint);

            Renderer treeRenderer = tree.GetComponent<Renderer>();
            var hidden = !(s_SelectedGroup is TreeGroupRoot);

            EditorUtility.SetSelectedRenderState(
                treeRenderer,
                hidden
                ? EditorSelectedRenderState.Hidden
                : EditorSelectedRenderState.Wireframe | EditorSelectedRenderState.Highlight);
        }

        void OnDisable()
        {
            Tools.s_Hidden = false;

            Tree tree = target as Tree;
            if (tree == null) return;

            Renderer treeRenderer = tree.GetComponent<Renderer>();
            EditorUtility.SetSelectedRenderState(treeRenderer, EditorSelectedRenderState.Wireframe | EditorSelectedRenderState.Highlight);
        }

        void OnSceneGUI()
        {
            // make sure it's a tree
            Tree tree = target as Tree;
            TreeData treeData = GetTreeData(tree);
            if (!treeData)
                return;

            // make sure selection is ok
            VerifySelection(treeData);
            if (s_SelectedGroup == null)
            {
                return;
            }

            // Check for hotkey event
            OnCheckHotkeys(treeData, true);

            Transform treeTransform = tree.transform;
            Matrix4x4 treeMatrix = tree.transform.localToWorldMatrix;

            Event evt = Event.current;

            #region Do Root Handles
            if (s_SelectedGroup.GetType() == typeof(TreeGroupRoot))
            {
                Tools.s_Hidden = false;

                Handles.color = s_NormalColor;
                Handles.DrawWireDisc(treeTransform.position, treeTransform.up, treeData.root.rootSpread);
            }
            else
            {
                Tools.s_Hidden = true;

                Handles.color = Handles.secondaryColor;
                Handles.DrawWireDisc(treeTransform.position, treeTransform.up, treeData.root.rootSpread);
            }
            #endregion

            #region Do Branch Handles
            if (s_SelectedGroup != null && s_SelectedGroup.GetType() == typeof(TreeGroupBranch))
            {
                EventType oldEventType = evt.type;

                // we want ignored mouse up events to check for dragging off of scene view
                if (evt.type == EventType.Ignore && evt.rawType == EventType.MouseUp)
                    oldEventType = evt.rawType;

                // Draw all splines in a single GL.Begin / GL.End
                Handles.DrawLine(Vector3.zero, Vector3.zero);
                HandleUtility.ApplyWireMaterial();
                GL.Begin(GL.LINES);
                for (int nodeIndex = 0; nodeIndex < s_SelectedGroup.nodeIDs.Length; nodeIndex++)
                {
                    TreeNode branch = treeData.GetNode(s_SelectedGroup.nodeIDs[nodeIndex]);
                    TreeSpline spline = branch.spline;
                    if (spline == null) continue;

                    Handles.color = (branch == s_SelectedNode) ? s_NormalColor : s_GroupColor;

                    Matrix4x4 branchMatrix = treeMatrix * branch.matrix;

                    // Draw Spline
                    Vector3 prevPos = branchMatrix.MultiplyPoint(spline.GetPositionAtTime(0.0f));

                    GL.Color(Handles.color);
                    for (float t = 0.01f; t <= 1.0f; t += 0.01f)
                    {
                        Vector3 currPos = branchMatrix.MultiplyPoint(spline.GetPositionAtTime(t));
                        //Handles.DrawLine(prevPos, currPos);

                        GL.Vertex(prevPos);
                        GL.Vertex(currPos);

                        prevPos = currPos;
                    }
                }
                GL.End();

                // Draw all handles
                for (int nodeIndex = 0; nodeIndex < s_SelectedGroup.nodeIDs.Length; nodeIndex++)
                {
                    TreeNode branch = treeData.GetNode(s_SelectedGroup.nodeIDs[nodeIndex]);
                    TreeSpline spline = branch.spline;
                    if (spline == null) continue;

                    Handles.color = (branch == s_SelectedNode) ? s_NormalColor : s_GroupColor;

                    Matrix4x4 branchMatrix = treeMatrix * branch.matrix;


                    // Draw Points
                    for (int pointIndex = 0; pointIndex < spline.nodes.Length; pointIndex++)
                    {
                        SplineNode point = spline.nodes[pointIndex];
                        Vector3 worldPos = branchMatrix.MultiplyPoint(point.point);
                        float handleSize = HandleUtility.GetHandleSize(worldPos) * 0.08f;

                        Handles.color = Handles.centerColor;

                        int oldKeyboardControl = GUIUtility.keyboardControl;

                        switch (editMode)
                        {
                            case EditMode.MoveNode:
                                if (pointIndex == 0)
                                    worldPos = Handles.FreeMoveHandle(worldPos, Quaternion.identity, handleSize, Vector3.zero, Handles.CircleHandleCap);
                                else
                                    worldPos = Handles.FreeMoveHandle(worldPos, Quaternion.identity, handleSize, Vector3.zero, Handles.RectangleHandleCap);

                                // check if point was just selected
                                if (oldEventType == EventType.MouseDown && evt.type == EventType.Used && oldKeyboardControl != GUIUtility.keyboardControl)
                                {
                                    SelectNode(branch, treeData);
                                    s_SelectedPoint = pointIndex;
                                    m_StartPointRotation = MathUtils.QuaternionFromMatrix(branchMatrix) * point.rot;
                                }

                                if ((oldEventType == EventType.MouseDown || oldEventType == EventType.MouseUp) && evt.type == EventType.Used)
                                {
                                    m_StartPointRotation = MathUtils.QuaternionFromMatrix(branchMatrix) * point.rot;
                                }

                                // check if we're done dragging handle (so we can change ids)
                                if (oldEventType == EventType.MouseUp && evt.type == EventType.Used)
                                {
                                    if (treeData.isInPreviewMode)
                                    {
                                        // We need a complete rebuild..
                                        UpdateMesh(tree);
                                    }
                                }

                                if (GUI.changed)
                                {
                                    Undo.RegisterCompleteObjectUndo(treeData, "Move");

                                    s_SelectedGroup.Lock();

                                    // Snap root branch to parent (overrides position received from handles, uses mouse raycasts instead)
                                    float angle = branch.baseAngle;
                                    if (pointIndex == 0)
                                    {
                                        TreeNode parentNode = treeData.GetNode(s_SelectedNode.parentID);

                                        Ray mouseRay = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
                                        float hitDist = 0f;

                                        if (parentNode != null)
                                        {
                                            TreeGroup parentGroup = treeData.GetGroup(s_SelectedGroup.parentGroupID);
                                            if (parentGroup.GetType() == typeof(TreeGroupBranch))
                                            {
                                                // Snap to parent branch
                                                s_SelectedNode.offset = FindClosestOffset(treeData, treeMatrix, parentNode,
                                                    mouseRay, ref angle);
                                                worldPos = branchMatrix.MultiplyPoint(Vector3.zero);
                                            }
                                            else if (parentGroup.GetType() == typeof(TreeGroupRoot))
                                            {
                                                // Snap to ground
                                                Vector3 mid = treeMatrix.MultiplyPoint(Vector3.zero);
                                                Plane p = new Plane(treeMatrix.MultiplyVector(Vector3.up), mid);
                                                if (p.Raycast(mouseRay, out hitDist))
                                                {
                                                    worldPos = mouseRay.origin + mouseRay.direction * hitDist;
                                                    Vector3 delta = worldPos - mid;
                                                    delta = treeMatrix.inverse.MultiplyVector(delta);
                                                    s_SelectedNode.offset =
                                                        Mathf.Clamp01(delta.magnitude / treeData.root.rootSpread);
                                                    angle = Mathf.Atan2(delta.z, delta.x) * Mathf.Rad2Deg;
                                                    worldPos = branchMatrix.MultiplyPoint(Vector3.zero);
                                                }
                                                else
                                                {
                                                    worldPos = branchMatrix.MultiplyPoint(point.point);
                                                }
                                            }
                                        }
                                    }

                                    branch.baseAngle = angle;
                                    point.point = branchMatrix.inverse.MultiplyPoint(worldPos);

                                    spline.UpdateTime();
                                    spline.UpdateRotations();

                                    PreviewMesh(tree);

                                    GUI.changed = false;
                                }

                                break;
                            case EditMode.RotateNode:
                                Handles.FreeMoveHandle(worldPos, Quaternion.identity, handleSize, Vector3.zero, Handles.CircleHandleCap);

                                // check if point was just selected
                                if (oldEventType == EventType.MouseDown && evt.type == EventType.Used && oldKeyboardControl != GUIUtility.keyboardControl)
                                {
                                    SelectNode(branch, treeData);
                                    s_SelectedPoint = pointIndex;
                                    m_GlobalToolRotation = Quaternion.identity;
                                    m_TempSpline = new TreeSpline(branch.spline);
                                }

                                GUI.changed = false;
                                break;
                            case EditMode.Freehand:
                                Handles.FreeMoveHandle(worldPos, Quaternion.identity, handleSize, Vector3.zero, Handles.CircleHandleCap);

                                // check if point was just selected
                                if (oldEventType == EventType.MouseDown && evt.type == EventType.Used && oldKeyboardControl != GUIUtility.keyboardControl)
                                {
                                    Undo.RegisterCompleteObjectUndo(treeData, "Free Hand");

                                    SelectNode(branch, treeData);
                                    s_SelectedPoint = pointIndex;
                                    s_StartPosition = worldPos;

                                    int cutCount = Mathf.Max(2, s_SelectedPoint + 1);
                                    branch.spline.SetNodeCount(cutCount);

                                    evt.Use();
                                }

                                if (s_SelectedPoint == pointIndex && s_SelectedNode == branch && oldEventType == EventType.MouseDrag)
                                {
                                    Ray mouseRay = HandleUtility.GUIPointToWorldRay(evt.mousePosition);

                                    // In draw mode.. move current spline node to mouse position
                                    // trace ray on a plane placed at the original position of the selected node and aligned to the camera..
                                    Vector3 camFront = Camera.current.transform.forward;
                                    Plane p = new Plane(camFront, s_StartPosition);
                                    float hitDist = 0.0f;

                                    if (p.Raycast(mouseRay, out hitDist))
                                    {
                                        Vector3 hitPos = mouseRay.origin + hitDist * mouseRay.direction;

                                        if (s_SelectedPoint == 0)
                                        {
                                            s_SelectedPoint = 1;
                                        }

                                        // lock shape
                                        s_SelectedGroup.Lock();

                                        s_SelectedNode.spline.nodes[s_SelectedPoint].point = (branchMatrix.inverse).MultiplyPoint(hitPos);

                                        Vector3 delta = s_SelectedNode.spline.nodes[s_SelectedPoint].point -
                                            s_SelectedNode.spline.nodes[s_SelectedPoint - 1].point;
                                        if (delta.magnitude > 1.0f)
                                        {
                                            s_SelectedNode.spline.nodes[s_SelectedPoint].point =
                                                s_SelectedNode.spline.nodes[s_SelectedPoint - 1].point + delta;
                                            // move on to the next node
                                            s_SelectedPoint++;
                                            if (s_SelectedPoint >= s_SelectedNode.spline.nodes.Length)
                                            {
                                                s_SelectedNode.spline.AddPoint(branchMatrix.inverse.MultiplyPoint(hitPos), 1.1f);
                                            }
                                        }

                                        s_SelectedNode.spline.UpdateTime();
                                        s_SelectedNode.spline.UpdateRotations();

                                        // Make sure changes are saved
                                        // EditorUtility.SetDirty( selectedGroup );
                                        evt.Use();
                                        PreviewMesh(tree);
                                    }
                                }
                                break;
                            default:
                                break;
                        }

                        // Handle undo
                        if (s_SelectedPoint == pointIndex && s_SelectedNode == branch && m_StartPointRotationDirty)
                        {
                            spline.UpdateTime();
                            spline.UpdateRotations();

                            m_StartPointRotation = MathUtils.QuaternionFromMatrix(branchMatrix) * point.rot;
                            m_GlobalToolRotation = Quaternion.identity;

                            m_StartPointRotationDirty = false;
                        }
                    }
                }

                if (oldEventType == EventType.MouseUp && editMode == EditMode.Freehand)
                {
                    s_SelectedPoint = -1;

                    if (treeData.isInPreviewMode)
                    {
                        // We need a complete rebuild..
                        UpdateMesh(tree);
                    }
                }

                // Draw Position Handles for Selected Point
                if (s_SelectedPoint > 0 && editMode == EditMode.MoveNode && s_SelectedNode != null)
                {
                    TreeNode branch = s_SelectedNode;
                    SplineNode point = branch.spline.nodes[s_SelectedPoint];
                    Matrix4x4 branchMatrix = treeMatrix * branch.matrix;

                    Vector3 worldPos = branchMatrix.MultiplyPoint(point.point);
                    Quaternion toolRotation = Quaternion.identity;
                    if (Tools.pivotRotation == PivotRotation.Local)
                    {
                        if (oldEventType == EventType.MouseUp || oldEventType == EventType.MouseDown)
                            m_StartPointRotation = MathUtils.QuaternionFromMatrix(branchMatrix) * point.rot;
                        toolRotation = m_StartPointRotation;
                    }
                    worldPos = DoPositionHandle(worldPos, toolRotation, false);

                    if (GUI.changed)
                    {
                        Undo.RegisterCompleteObjectUndo(treeData, "Move");

                        s_SelectedGroup.Lock();

                        point.point = branchMatrix.inverse.MultiplyPoint(worldPos);

                        branch.spline.UpdateTime();
                        branch.spline.UpdateRotations();

                        PreviewMesh(tree);
                    }

                    // check if we're done dragging handle (so we can change ids)
                    if (oldEventType == EventType.MouseUp && evt.type == EventType.Used)
                    {
                        if (treeData.isInPreviewMode)
                        {
                            // We need a complete rebuild..
                            UpdateMesh(tree);
                        }
                    }
                }

                // Draw Rotation Handles for selected Point
                if (s_SelectedPoint >= 0 && editMode == EditMode.RotateNode && s_SelectedNode != null)
                {
                    TreeNode branch = s_SelectedNode;
                    SplineNode point = branch.spline.nodes[s_SelectedPoint];
                    Matrix4x4 branchMatrix = treeMatrix * branch.matrix;

                    if (m_TempSpline == null)
                    {
                        m_TempSpline = new TreeSpline(branch.spline);
                    }

                    Vector3 worldPos = branchMatrix.MultiplyPoint(point.point);
                    Quaternion rotation = Quaternion.identity;
                    m_GlobalToolRotation = Handles.RotationHandle(m_GlobalToolRotation, worldPos);
                    rotation = m_GlobalToolRotation;

                    if (GUI.changed)
                    {
                        Undo.RegisterCompleteObjectUndo(treeData, "Move");

                        s_SelectedGroup.Lock();

                        for (int i = s_SelectedPoint + 1; i < m_TempSpline.nodes.Length; i++)
                        {
                            Vector3 pointVector = (m_TempSpline.nodes[i].point - point.point);
                            pointVector = branchMatrix.MultiplyVector(pointVector);
                            pointVector = rotation * pointVector;
                            pointVector = branchMatrix.inverse.MultiplyVector(pointVector);
                            Vector3 newPos = point.point + pointVector;
                            s_SelectedNode.spline.nodes[i].point = newPos;
                        }

                        branch.spline.UpdateTime();
                        branch.spline.UpdateRotations();

                        PreviewMesh(tree);
                    }

                    // check if we're done dragging handle (so we can change ids)
                    if (oldEventType == EventType.MouseUp && evt.type == EventType.Used)
                    {
                        if (treeData.isInPreviewMode)
                        {
                            // We need a complete rebuild..
                            UpdateMesh(tree);
                        }
                    }
                }
            }
            #endregion

            #region Do Leaf Handles
            if (s_SelectedGroup != null && s_SelectedGroup.GetType() == typeof(TreeGroupLeaf))
            {
                for (int nodeIndex = 0; nodeIndex < s_SelectedGroup.nodeIDs.Length; nodeIndex++)
                {
                    TreeNode leaf = treeData.GetNode(s_SelectedGroup.nodeIDs[nodeIndex]);
                    Matrix4x4 leafMatrix = treeMatrix * leaf.matrix;
                    Vector3 worldPos = leafMatrix.MultiplyPoint(Vector3.zero);
                    float handleSize = HandleUtility.GetHandleSize(worldPos) * 0.08f;

                    Handles.color = Handles.centerColor;

                    EventType oldEventType = evt.type;
                    int oldKeyboardControl = GUIUtility.keyboardControl;

                    switch (editMode)
                    {
                        case EditMode.MoveNode:
                            Handles.FreeMoveHandle(worldPos, Quaternion.identity, handleSize, Vector3.zero, Handles.CircleHandleCap);

                            // check if point was just selected
                            if (oldEventType == EventType.MouseDown && evt.type == EventType.Used && oldKeyboardControl != GUIUtility.keyboardControl)
                            {
                                SelectNode(leaf, treeData);
                                m_GlobalToolRotation = MathUtils.QuaternionFromMatrix(leafMatrix);
                                m_StartMatrix = leafMatrix;
                                m_StartPointRotation = leaf.rotation;
                                m_LockedWorldPos = new Vector3(m_StartMatrix.m03, m_StartMatrix.m13, m_StartMatrix.m23);
                            }

                            // check if we're done dragging handle (so we can change ids)
                            if (oldEventType == EventType.MouseUp && evt.type == EventType.Used)
                            {
                                if (treeData.isInPreviewMode)
                                {
                                    // We need a complete rebuild..
                                    UpdateMesh(tree);
                                }
                            }

                            if (GUI.changed)
                            {
                                s_SelectedGroup.Lock();

                                TreeNode parentNode = treeData.GetNode(leaf.parentID);
                                TreeGroup parentGroup = treeData.GetGroup(s_SelectedGroup.parentGroupID);

                                Ray mouseRay = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
                                float hitDist = 0f;
                                float angle = leaf.baseAngle;

                                if (parentGroup.GetType() == typeof(TreeGroupBranch))
                                {
                                    // Snap to branch
                                    leaf.offset = FindClosestOffset(treeData, treeMatrix, parentNode, mouseRay, ref angle);
                                    leaf.baseAngle = angle;

                                    PreviewMesh(tree);
                                }
                                else if (parentGroup.GetType() == typeof(TreeGroupRoot))
                                {
                                    // Snap to ground
                                    Vector3 mid = treeMatrix.MultiplyPoint(Vector3.zero);
                                    Plane p = new Plane(treeMatrix.MultiplyVector(Vector3.up), mid);
                                    if (p.Raycast(mouseRay, out hitDist))
                                    {
                                        worldPos = mouseRay.origin + mouseRay.direction * hitDist;
                                        Vector3 delta = worldPos - mid;
                                        delta = treeMatrix.inverse.MultiplyVector(delta);
                                        leaf.offset =
                                            Mathf.Clamp01(delta.magnitude / treeData.root.rootSpread);
                                        angle = Mathf.Atan2(delta.z, delta.x) * Mathf.Rad2Deg;
                                    }
                                    leaf.baseAngle = angle;

                                    PreviewMesh(tree);
                                }
                            }
                            break;

                        case EditMode.RotateNode:
                        {
                            Handles.FreeMoveHandle(worldPos, Quaternion.identity, handleSize, Vector3.zero, Handles.CircleHandleCap);

                            // check if point was just selected
                            if (oldEventType == EventType.MouseDown && evt.type == EventType.Used && oldKeyboardControl != GUIUtility.keyboardControl)
                            {
                                SelectNode(leaf, treeData);
                                m_GlobalToolRotation = MathUtils.QuaternionFromMatrix(leafMatrix);
                                m_StartMatrix = leafMatrix;
                                m_StartPointRotation = leaf.rotation;
                                m_LockedWorldPos = new Vector3(leafMatrix.m03, leafMatrix.m13, leafMatrix.m23);
                            }

                            // Rotation handle for selected leaf
                            if (s_SelectedNode == leaf)
                            {
                                oldEventType = evt.GetTypeForControl(GUIUtility.hotControl);
                                m_GlobalToolRotation = Handles.RotationHandle(m_GlobalToolRotation, m_LockedWorldPos);

                                // check if we're done dragging handle (so we can change ids)
                                if (oldEventType == EventType.MouseUp && evt.type == EventType.Used)
                                {
                                    // update position of gizmo
                                    m_LockedWorldPos = new Vector3(leafMatrix.m03, leafMatrix.m13, leafMatrix.m23);

                                    if (treeData.isInPreviewMode)
                                    {
                                        // We need a complete rebuild..
                                        UpdateMesh(tree);
                                    }
                                }

                                if (GUI.changed)
                                {
                                    s_SelectedGroup.Lock();
                                    Quaternion invStart = Quaternion.Inverse(MathUtils.QuaternionFromMatrix(m_StartMatrix));
                                    leaf.rotation = m_StartPointRotation * (invStart * m_GlobalToolRotation);
                                    MathUtils.QuaternionNormalize(ref leaf.rotation);
                                    PreviewMesh(tree);
                                }
                            }
                        }
                        break;

                        default:
                            break;
                    }
                }
            }
            #endregion
        }

        private Vector3 DoPositionHandle(Vector3 position, Quaternion rotation, bool hide)
        {
            Color temp = Handles.color;

            Handles.color = Handles.xAxisColor; position = Handles.Slider(position, rotation * Vector3.right);
            Handles.color = Handles.yAxisColor; position = Handles.Slider(position, rotation * Vector3.up);
            Handles.color = Handles.zAxisColor; position = Handles.Slider(position, rotation * Vector3.forward);

            Handles.color = temp;

            return position;
        }

        private Rect GUIPropBegin()
        {
            Rect r = EditorGUILayout.BeginHorizontal();
            return r;
        }

        private void GUIPropEnd()
        {
            GUIPropEnd(true);
        }

        private void GUIPropEnd(bool addSpace)
        {
            if (addSpace)
            {
                GUILayout.Space(m_SectionHasCurves ? kCurveSpace + 4 : 0);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void GUIHandlePropertyChange(PropertyType prop)
        {
            switch (prop)
            {
                case PropertyType.Normal:
                    UndoStoreSelected(EditMode.Parameter);
                    break;

                case PropertyType.FullUpdate:
                    UndoStoreSelected(EditMode.Parameter);
                    m_WantCompleteUpdate = true;
                    break;

                case PropertyType.FullUndo:
                    UndoStoreSelected(EditMode.Everything);
                    break;

                case PropertyType.FullUndoUpdate:
                    UndoStoreSelected(EditMode.Everything);
                    m_WantCompleteUpdate = true;
                    break;
            }
        }

        // GUI Helpers..
        private float GUISlider(PropertyType prop, string contentID, float value, float minimum, float maximum, bool hasCurve)
        {
            GUIPropBegin();
            float newValue = EditorGUILayout.Slider(TreeEditorHelper.GetGUIContent(contentID), value, minimum, maximum);
            if (newValue != value)
            {
                GUIHandlePropertyChange(prop);
            }
            if (!hasCurve) GUIPropEnd();

            return newValue;
        }

        private int GUIIntSlider(PropertyType prop, string contentID, int value, int minimum, int maximum, bool hasCurve)
        {
            GUIPropBegin();
            int newValue = EditorGUILayout.IntSlider(TreeEditorHelper.GetGUIContent(contentID), value, minimum, maximum);
            if (newValue != value)
            {
                GUIHandlePropertyChange(prop);
            }
            if (!hasCurve) GUIPropEnd();

            return newValue;
        }

        private bool GUIToggle(PropertyType prop, string contentID, bool value, bool hasCurve)
        {
            GUIPropBegin();
            bool newValue = EditorGUILayout.Toggle(TreeEditorHelper.GetGUIContent(contentID), value);
            if (newValue != value)
            {
                GUIHandlePropertyChange(prop);
                m_WantCompleteUpdate = true;
            }
            if (!hasCurve) GUIPropEnd();

            return newValue;
        }

        private int GUIPopup(PropertyType prop, string contentID, string[] optionIDs, int value, bool hasCurve)
        {
            GUIPropBegin();

            GUIContent[] options = new GUIContent[optionIDs.Length];
            for (int i = 0; i < optionIDs.Length; i++)
            {
                options[i] = TreeEditorHelper.GetGUIContent(optionIDs[i]);
            }

            int newValue = EditorGUILayout.Popup(TreeEditorHelper.GetGUIContent(contentID), value, options);
            if (newValue != value)
            {
                GUIHandlePropertyChange(prop);
                m_WantCompleteUpdate = true;
            }
            if (!hasCurve) GUIPropEnd();

            return newValue;
        }

        private Material GUIMaterialField(PropertyType prop, int uniqueNodeID, string contentID, Material value, TreeEditorHelper.NodeType nodeType)
        {
            string uniqueID = uniqueNodeID + "_" + contentID;

            GUIPropBegin();
            Material newValue = EditorGUILayout.ObjectField(TreeEditorHelper.GetGUIContent(contentID), value, typeof(Material), false) as Material;
            GUIPropEnd();

            bool shaderFixed = m_TreeEditorHelper.GUIWrongShader(uniqueID, newValue, nodeType);

            if (newValue != value || shaderFixed)
            {
                GUIHandlePropertyChange(prop);
                m_WantCompleteUpdate = true;
            }

            return newValue;
        }

        private Object GUIObjectField(PropertyType prop, string contentID, Object value, System.Type type, bool hasCurve)
        {
            GUIPropBegin();
            Object newValue = EditorGUILayout.ObjectField(TreeEditorHelper.GetGUIContent(contentID), value, type, false);
            if (newValue != value)
            {
                GUIHandlePropertyChange(prop);
                m_WantCompleteUpdate = true;
            }
            if (!hasCurve) GUIPropEnd();

            return newValue;
        }

        private bool GUICurve(PropertyType prop, AnimationCurve curve, Rect ranges)
        {
            bool preChange = GUI.changed;
            EditorGUILayout.CurveField(curve, Color.green, ranges, GUILayout.Width(kCurveSpace));
            GUIPropEnd(false);

            if (preChange != GUI.changed)
            {
                if (GUIUtility.hotControl == 0)
                    m_WantCompleteUpdate = true;
                GUIHandlePropertyChange(prop);
                return true;
            }
            return false;
        }

        private Vector2 GUIMinMaxSlider(PropertyType prop, string contentID, Vector2 value, float minimum, float maximum, bool hasCurve)
        {
            GUIPropBegin();
            Vector2 newValue = new Vector2(Mathf.Min(value.x, value.y), Mathf.Max(value.x, value.y));
            GUIContent content = TreeEditorHelper.GetGUIContent(contentID);
            bool preChange = GUI.changed;
            Rect tempRect = GUILayoutUtility.GetRect(content, "Button");
            EditorGUI.MinMaxSlider(tempRect, content, ref newValue.x, ref newValue.y, minimum, maximum);
            if (preChange != GUI.changed)
            {
                GUIHandlePropertyChange(prop);
            }
            if (!hasCurve) GUIPropEnd();
            return newValue;
        }

        public void InspectorHierachy(TreeData treeData, Renderer renderer)
        {
            if (s_SelectedGroup == null)
            {
                Debug.Log("NO GROUP SELECTED!");
                return;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.BeginVertical();

            bool preChange = GUI.changed;
            Rect sizeRect = GUIPropBegin();
            DrawHierachy(treeData, renderer, sizeRect);
            if (GUI.changed != preChange) m_WantCompleteUpdate = true;

            GUIPropEnd(false);

            GUIPropBegin();

            int sel = -1;
            GUILayout.BeginHorizontal(styles.toolbar);

            if (GUILayout.Button(styles.iconRefresh, styles.toolbarButton))
            {
                TreeGroupLeaf.s_TextureHullsDirty = true;
                UpdateMesh(target as Tree);
            }

            GUILayout.FlexibleSpace();

            // Only enable if this group can have sub groups
            GUI.enabled = s_SelectedGroup.CanHaveSubGroups();
            if (GUILayout.Button(styles.iconAddLeaves, styles.toolbarButton)) sel = 0;
            if (GUILayout.Button(styles.iconAddBranches, styles.toolbarButton)) sel = 1;
            GUI.enabled = true;

            if (s_SelectedGroup == treeData.root)
            {
                GUI.enabled = false;
            }
            if (GUILayout.Button(styles.iconDuplicate, styles.toolbarButton)) sel = 3;
            if (GUILayout.Button(styles.iconTrash, styles.toolbarButton)) sel = 2;
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            switch (sel)
            {
                case 0:
                {
                    UndoStoreSelected(EditMode.CreateGroup);
                    TreeGroup g = treeData.AddGroup(s_SelectedGroup, typeof(TreeGroupLeaf));
                    SelectGroup(g);
                    m_WantCompleteUpdate = true;
                    Event.current.Use();
                }
                break;
                case 1:
                {
                    UndoStoreSelected(EditMode.CreateGroup);
                    TreeGroup g = treeData.AddGroup(s_SelectedGroup, typeof(TreeGroupBranch));
                    SelectGroup(g);
                    m_WantCompleteUpdate = true;
                    Event.current.Use();
                }
                break;
                case 2:
                {
                    DeleteSelected(treeData);
                    Event.current.Use();
                }
                break;
                case 3:
                {
                    DuplicateSelected(treeData);
                    Event.current.Use();
                }
                break;
            }
            GUIPropEnd(false);

            GUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }

        public void InspectorEditTools(Tree obj)
        {
            // early out if we don't have a scene representation
            if (EditorUtility.IsPersistent(obj)) return;

            string[] toolbarIconStrings;
            string[] toolbarContentStrings;
            if (s_SelectedGroup is TreeGroupBranch)
            {
                toolbarIconStrings = toolbarIconStringsBranch;
                toolbarContentStrings = toolbarContentStringsBranch;
            }
            else
            {
                toolbarIconStrings = toolbarIconStringsLeaf;
                toolbarContentStrings = toolbarContentStringsLeaf;

                // check if user selected a leaf group when in branch free hand mode
                if (editMode == EditMode.Freehand)
                    editMode = EditMode.None;
            }

            EditMode oldEditMode = editMode;
            editMode = (EditMode)GUItoolbar((int)editMode, BuildToolbarContent(toolbarIconStrings, toolbarContentStrings, (int)editMode));
            if (oldEditMode != editMode)
            {
                // Edit mode changed.. repaint scene view!
                SceneView.RepaintAll();
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (editMode == EditMode.None)
            {
                GUILayout.Label("No Tool Selected");
                GUILayout.Label("Please select a tool", EditorStyles.wordWrappedMiniLabel);
            }
            else
            {
                string uiString = toolbarContentStrings[(int)editMode];
                GUILayout.Label(TreeEditorHelper.ExtractLabel(uiString));
                GUILayout.Label(TreeEditorHelper.ExtractTooltip(uiString), EditorStyles.wordWrappedMiniLabel);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
        }

        private static GUIContent[] BuildToolbarContent(string[] iconStrings, string[] contentStrings, int selection)
        {
            GUIContent[] contents = new GUIContent[contentStrings.Length];
            for (int i = 0; i < contentStrings.Length; i++)
            {
                string iconSuffix = selection == i ? " On" : "";
                string tooltip = TreeEditorHelper.ExtractLabel(contentStrings[i]);
                contents[i] = EditorGUIUtility.IconContent(iconStrings[i] + iconSuffix, tooltip);
            }

            return contents;
        }

        //
        // Distribution properties inspector (generic)
        //
        public void InspectorDistribution(TreeData treeData, TreeGroup group)
        {
            if (group == null) return;

            PrepareSpacing(true);

            bool enableGUI = true;
            if (group.lockFlags != 0)
            {
                enableGUI = false;
            }

            // Seed
            GUI.enabled = enableGUI;
            int pre = group.seed;
            group.seed = GUIIntSlider(PropertyType.Normal, group.GroupSeedString, group.seed, 0, 999999, false);
            if (group.seed != pre)
            {
                treeData.UpdateSeed(group.uniqueID);
            }

            // Frequency
            pre = group.distributionFrequency;
            group.distributionFrequency = GUIIntSlider(PropertyType.FullUndo, group.FrequencyString, group.distributionFrequency, 1, 100, false);
            if (group.distributionFrequency != pre)
            {
                treeData.UpdateFrequency(group.uniqueID);
            }

            // Distribution Mode
            pre = (int)group.distributionMode;
            group.distributionMode = (TreeGroup.DistributionMode)GUIPopup(PropertyType.Normal, group.DistributionModeString, inspectorDistributionPopupOptions, (int)group.distributionMode, true);
            if ((int)group.distributionMode != pre)
            {
                treeData.UpdateDistribution(group.uniqueID);
            }

            // Distribution Curve
            AnimationCurve tempCurve = group.distributionCurve;
            if (GUICurve(PropertyType.Normal, tempCurve, m_CurveRangesA))
            {
                group.distributionCurve = tempCurve;
                treeData.UpdateDistribution(group.uniqueID);
            }

            // Twirl
            if (group.distributionMode != TreeGroup.DistributionMode.Random)
            {
                float pref = group.distributionTwirl;
                group.distributionTwirl = GUISlider(PropertyType.Normal, group.TwirlString, group.distributionTwirl, -1.0f, 1.0f,
                    false);
                if (group.distributionTwirl != pref)
                {
                    treeData.UpdateDistribution(group.uniqueID);
                }
            }

            // Nodes per arrangement
            if (group.distributionMode == TreeGroup.DistributionMode.Whorled)
            {
                pre = group.distributionNodes;
                group.distributionNodes = GUIIntSlider(PropertyType.Normal, group.WhorledStepString, group.distributionNodes, 1, 21,
                    false);
                if (group.distributionNodes != pre)
                {
                    treeData.UpdateDistribution(group.uniqueID);
                }
            }

            // growth scale
            group.distributionScale = GUISlider(PropertyType.Normal, group.GrowthScaleString, group.distributionScale, 0.0f, 1.0f, true);

            // growth scale curve
            tempCurve = group.distributionScaleCurve;
            if (GUICurve(PropertyType.Normal, tempCurve, m_CurveRangesA))
            {
                group.distributionScaleCurve = tempCurve;
            }

            // growth scale
            group.distributionPitch = GUISlider(PropertyType.Normal, group.GrowthAngleString, group.distributionPitch, 0.0f, 1.0f, true);

            // growth scale curve
            tempCurve = group.distributionPitchCurve;
            if (GUICurve(PropertyType.Normal, tempCurve, m_CurveRangesB))
            {
                group.distributionPitchCurve = tempCurve;
            }

            GUI.enabled = true;

            EditorGUILayout.Space();
        }

        //
        // Animation properties inspector (generic)
        //
        public void InspectorAnimation(TreeData treeData, TreeGroup group)
        {
            if (group == null) return;

            PrepareSpacing(false);

            group.animationPrimary = GUISlider(PropertyType.Normal, group.MainWindString, group.animationPrimary, 0.0f, 1.0f, false);

            // not for trunks
            if (treeData.GetGroup(group.parentGroupID) != treeData.root)
                group.animationSecondary = GUISlider(PropertyType.Normal, group.MainTurbulenceString, group.animationSecondary, 0.0f, 1.0f, false);

            // branches must have fronds
            GUI.enabled = true;
            if (!(group is TreeGroupBranch && ((group as TreeGroupBranch).geometryMode == (int)TreeGroupBranch.GeometryMode.Branch)))
            {
                group.animationEdge = GUISlider(PropertyType.Normal, group.EdgeTurbulenceString, group.animationEdge, 0.0f, 1.0f, false);
            }

            // Create default wind button
            GUIPropBegin();
            if (GUILayout.Button(TreeEditorHelper.GetGUIContent("Create Wind Zone|Creates a default wind zone, which is required for animating trees while playing the game.")))
            {
                CreateDefaultWindZone();
            }
            GUIPropEnd();
        }

        private int GUItoolbar(int selection, GUIContent[] names)
        {
            GUI.enabled = true;

            bool preChange = GUI.changed;

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            for (int i = 0; i < names.Length; i++)
            {
                GUIStyle buttonStyle = "ButtonMid";
                if (i == 0) buttonStyle = "ButtonLeft";
                if (i == names.Length - 1) buttonStyle = "ButtonRight";

                if (names[i] != null)
                {
                    if (GUILayout.Toggle(selection == i, names[i], buttonStyle)) selection = i;
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUI.changed = preChange;

            return selection;
        }

        private void GUIunlockbox(TreeData treeData)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);

            GUIContent labelContent = TreeEditorHelper.GetGUIContent("This group has been edited by hand. Some parameters may not be available.|");
            labelContent.image = styles.warningIcon.image;
            GUILayout.Label(labelContent, EditorStyles.wordWrappedMiniLabel);

            GUIStyle helpButtonStyle = "wordwrapminibutton";

            GUIContent buttonContent = TreeEditorHelper.GetGUIContent("Convert to procedural group. All hand editing will be lost!|");
            if (GUILayout.Button(buttonContent, helpButtonStyle))
            {
                treeData.UnlockGroup(s_SelectedGroup);
                m_WantCompleteUpdate = true;
            }

            GUILayout.EndVertical();
        }

        void PrepareSpacing(bool hasCurves)
        {
            m_SectionHasCurves = hasCurves;
            EditorGUIUtility.labelWidth = hasCurves ? 100 : 120;
        }

        private bool GUIMaterialColor(Material material, string propertyID, string contentID)
        {
            bool changed = false;
            GUIPropBegin();
            Color value = material.GetColor(propertyID);
            Color newValue = EditorGUILayout.ColorField(TreeEditorHelper.GetGUIContent(contentID), value);
            if (newValue != value)
            {
                Undo.RegisterCompleteObjectUndo(material, "Material");
                material.SetColor(propertyID, newValue);
                changed = true;
            }
            GUIPropEnd();
            return changed;
        }

        private bool GUIMaterialSlider(Material material, string propertyID, string contentID)
        {
            bool changed = false;
            GUIPropBegin();
            float value = material.GetFloat(propertyID);
            float newValue = EditorGUILayout.Slider(TreeEditorHelper.GetGUIContent(contentID), value, 0, 1);
            if (newValue != value)
            {
                Undo.RegisterCompleteObjectUndo(material, "Material");
                material.SetFloat(propertyID, newValue);
                changed = true;
            }
            GUIPropEnd();
            return changed;
        }

        private bool GUIMaterialFloatField(Material material, string propertyID, string contentID)
        {
            bool success;
            float value = GetMaterialFloat(material, propertyID, out success);
            if (!success)
                return false;

            bool changed = false;
            GUIPropBegin();
            float newValue = EditorGUILayout.FloatField(TreeEditorHelper.GetGUIContent(contentID), value);
            if (newValue != value)
            {
                Undo.RegisterCompleteObjectUndo(material, "Material");
                material.SetFloat(propertyID, newValue);
                changed = true;
            }
            GUIPropEnd();
            return changed;
        }

        private static float GetMaterialFloat(Material material, string propertyID, out bool success)
        {
            success = false;
            if (!material.HasProperty(propertyID))
                return 0.0f;

            success = true;
            return material.GetFloat(propertyID);
        }

        private static float GetMaterialFloat(Material material, string propertyID)
        {
            bool success;
            return GetMaterialFloat(material, propertyID, out success);
        }

        private static float GenerateMaterialHash(Material material)
        {
            float hash = 0;
            Color color = material.GetColor("_TranslucencyColor");
            hash += color.r + color.g + color.b + color.a;

            hash += GetMaterialFloat(material, "_Cutoff");
            hash += GetMaterialFloat(material, "_TranslucencyViewDependency");
            hash += GetMaterialFloat(material, "_ShadowStrength");
            hash += GetMaterialFloat(material, "_ShadowOffsetScale");

            return hash;
        }

        public void InspectorRoot(TreeData treeData, TreeGroupRoot group)
        {
            GUIContent[] categoryNames = { TreeEditorHelper.GetGUIContent(L10n.Tr("Distribution|")),
                                           TreeEditorHelper.GetGUIContent(L10n.Tr("Geometry|")),
                                           TreeEditorHelper.GetGUIContent(L10n.Tr("Material|Controls global material properties of the tree.")),
                                           null,
                                           null,
                                           null};

            bool guiEnabled = GUI.enabled;


            //
            // Distribution props
            //
            BeginSettingsSection(0, categoryNames);
            PrepareSpacing(false);
            int pre = group.seed;
            group.seed = GUIIntSlider(PropertyType.Normal, treeSeedString, group.seed, 0, 9999999, false);
            if (group.seed != pre)
            {
                treeData.UpdateSeed(group.uniqueID);
            }

            group.rootSpread = GUISlider(PropertyType.Normal, areaSpreadString, group.rootSpread, 0.0f, 10.0f, false);
            group.groundOffset = GUISlider(PropertyType.Normal, groundOffsetString, group.groundOffset, 0.0f, 10.0f, false);
            EndSettingsSection();


            //
            // Geometry props
            //
            BeginSettingsSection(1, categoryNames);
            PrepareSpacing(false);
            group.adaptiveLODQuality = GUISlider(PropertyType.FullUndo, lodQualityString, group.adaptiveLODQuality, 0.0f, 1.0f, false);

            group.enableAmbientOcclusion = GUIToggle(PropertyType.FullUndo, ambientOcclusionString, group.enableAmbientOcclusion, false);

            GUI.enabled = group.enableAmbientOcclusion;
            group.aoDensity = GUISlider(PropertyType.Normal, aoDensityString, group.aoDensity, 0.0f, 1.0f, false);
            GUI.enabled = true;
            EndSettingsSection();


            //
            // Material props
            //
            Material leafMaterial = treeData.optimizedCutoutMaterial;
            if (leafMaterial != null)
            {
                BeginSettingsSection(2, categoryNames);
                PrepareSpacing(false);

                bool guiChanged = GUI.changed;

                bool changed = GUIMaterialColor(leafMaterial, "_TranslucencyColor", translucencyColorString);

                changed |= GUIMaterialSlider(leafMaterial, "_TranslucencyViewDependency", transViewDepString);
                changed |= GUIMaterialSlider(leafMaterial, "_Cutoff", alphaCutoffString);
                changed |= GUIMaterialSlider(leafMaterial, "_ShadowStrength", shadowStrengthString);

                changed |= GUIMaterialFloatField(leafMaterial, "_ShadowOffsetScale", shadowOffsetString);

                if (changed)
                    s_CutoutMaterialHashBeforeUndo = GenerateMaterialHash(treeData.optimizedCutoutMaterial);

                group.shadowTextureQuality = GUIPopup(PropertyType.FullUpdate, shadowCasterResString, leafMaterialOptions, group.shadowTextureQuality, false);

                GUI.changed = guiChanged;
                EndSettingsSection();
            }

            GUI.enabled = guiEnabled;

            EditorGUILayout.Space();
        }

        public void InspectorBranch(TreeData treeData, TreeGroupBranch group)
        {
            InspectorEditTools(target as Tree);

            GUIContent[] categoryNames =
            {
                TreeEditorHelper.GetGUIContent(L10n.Tr("Distribution|Adjusts the count and placement of branches in the group. Use the curves to fine tune position, rotation and scale. The curves are relative to the parent branch or to the area spread in case of a trunk.")),
                TreeEditorHelper.GetGUIContent(L10n.Tr("Geometry|Select what type of geometry is generated for this branch group and which materials are applied. LOD Multiplier allows you to adjust the quality of this group relative to tree's LOD Quality.")),
                TreeEditorHelper.GetGUIContent(L10n.Tr("Shape|Adjusts the shape and growth of the branches. Use the curves to fine tune the shape, all curves are relative to the branch itself.")),
                TreeEditorHelper.GetGUIContent(L10n.Tr("Fronds|")),
                TreeEditorHelper.GetGUIContent(L10n.Tr("Wind|Adjusts the parameters used for animating this group of branches. The wind zones are only active in Play Mode."))
            };

            bool guiEnabled = GUI.enabled;

            // Locked group warning
            if (s_SelectedGroup.lockFlags != 0)
            {
                GUIunlockbox(treeData);
            }

            AnimationCurve tempCurve;

            // Distribution (Standard)
            BeginSettingsSection(0, categoryNames);
            InspectorDistribution(treeData, group);
            EndSettingsSection();

            // Geometry
            BeginSettingsSection(1, categoryNames);
            PrepareSpacing(false);
            group.lodQualityMultiplier = GUISlider(PropertyType.Normal, lodMultiplierString, group.lodQualityMultiplier, 0.0f, 2.0f, false);

            group.geometryMode = (TreeGroupBranch.GeometryMode)GUIPopup(PropertyType.FullUpdate, geometryModeString, categoryNamesOptions, (int)group.geometryMode, false);

            if (group.geometryMode != TreeGroupBranch.GeometryMode.Frond)
                group.materialBranch = GUIMaterialField(PropertyType.FullUpdate, group.uniqueID, branchMaterialString, group.materialBranch, TreeEditorHelper.NodeType.BarkNode);

            group.materialBreak = GUIMaterialField(PropertyType.FullUpdate, group.uniqueID, breakMaterialString, group.materialBreak, TreeEditorHelper.NodeType.BarkNode);

            if (group.geometryMode != TreeGroupBranch.GeometryMode.Branch)
                group.materialFrond = GUIMaterialField(PropertyType.FullUpdate, group.uniqueID, frondMaterialString, group.materialFrond, TreeEditorHelper.NodeType.BarkNode);
            EndSettingsSection();

            BeginSettingsSection(2, categoryNames);
            PrepareSpacing(true);

            //
            // General Shape
            //

            GUI.enabled = (group.lockFlags == 0);
            group.height = GUIMinMaxSlider(PropertyType.Normal, lengthString, group.height, 0.1f, 50.0f, false);

            GUI.enabled = (group.geometryMode != TreeGroupBranch.GeometryMode.Frond);
            group.radiusMode = GUIToggle(PropertyType.Normal, relativeLengthString, group.radiusMode, false);

            GUI.enabled = (group.geometryMode != TreeGroupBranch.GeometryMode.Frond);
            group.radius = GUISlider(PropertyType.Normal, radiusString, group.radius, 0.1f, 5.0f, true);
            tempCurve = group.radiusCurve;
            if (GUICurve(PropertyType.Normal, tempCurve, m_CurveRangesA))
            {
                group.radiusCurve = tempCurve;
            }

            GUI.enabled = (group.geometryMode != TreeGroupBranch.GeometryMode.Frond);
            group.capSmoothing = GUISlider(PropertyType.Normal, capSmoothingString, group.capSmoothing, 0.0f, 1.0f, false);

            GUI.enabled = true;
            EditorGUILayout.Space();

            //
            // Growth
            //

            GUI.enabled = (group.lockFlags == 0);
            group.crinklyness = GUISlider(PropertyType.Normal, crinklynessString, group.crinklyness, 0.0f, 1.0f, true);
            tempCurve = group.crinkCurve;
            if (GUICurve(PropertyType.Normal, tempCurve, m_CurveRangesA))
            {
                group.crinkCurve = tempCurve;
            }

            GUI.enabled = (group.lockFlags == 0);
            group.seekBlend = GUISlider(PropertyType.Normal, seekSunString, group.seekBlend, 0.0f, 1.0f, true);
            tempCurve = group.seekCurve;
            if (GUICurve(PropertyType.Normal, tempCurve, m_CurveRangesB))
            {
                group.seekCurve = tempCurve;
            }
            GUI.enabled = true;
            EditorGUILayout.Space();

            //
            // Surface Noise
            //
            // Only enabled if there is branch geometry
            GUI.enabled = (group.geometryMode != TreeGroupBranch.GeometryMode.Frond);

            group.noise = GUISlider(PropertyType.Normal, noiseString, group.noise, 0.0f, 1.0f, true);
            tempCurve = group.noiseCurve;
            if (GUICurve(PropertyType.Normal, tempCurve, m_CurveRangesA))
            {
                group.noiseCurve = tempCurve;
            }

            group.noiseScaleU = GUISlider(PropertyType.Normal, noiseScaleUString, group.noiseScaleU, 0.0f, 1.0f, false);
            group.noiseScaleV = GUISlider(PropertyType.Normal, noiseScaleVString, group.noiseScaleV, 0.0f, 1.0f, false);

            EditorGUILayout.Space();

            // Only enabled if there is branch geometry
            GUI.enabled = (group.geometryMode != TreeGroupBranch.GeometryMode.Frond);

            if (treeData.GetGroup(group.parentGroupID) == treeData.root)
            {
                //
                // Flares
                //
                group.flareSize = GUISlider(PropertyType.Normal, flareRadiusString, group.flareSize, 0.0f, 5.0f, false);
                group.flareHeight = GUISlider(PropertyType.Normal, flareHeightString, group.flareHeight, 0.0f, 1.0f, false);
                group.flareNoise = GUISlider(PropertyType.Normal, flareNoiseString, group.flareNoise, 0.0f, 1.0f, false);
            }
            else
            {
                //
                // Welding
                //
                group.weldHeight = GUISlider(PropertyType.Normal, weldLengthString, group.weldHeight, 0.01f, 1.0f, false);
                group.weldSpreadTop = GUISlider(PropertyType.Normal, spreadTopString, group.weldSpreadTop, 0.0f, 1.0f, false);
                group.weldSpreadBottom = GUISlider(PropertyType.Normal, spreadBottomString, group.weldSpreadBottom, 0.0f, 1.0f, false);
            }

            EditorGUILayout.Space();

            // Breaking
            group.breakingChance = GUISlider(PropertyType.Normal, breakChanceString, group.breakingChance, 0.0f, 1.0f, false);
            group.breakingSpot = GUIMinMaxSlider(PropertyType.Normal, breakLocationString, group.breakingSpot, 0.0f, 1.0f, false);
            EndSettingsSection();

            // Fronds
            if (group.geometryMode != (int)(TreeGroupBranch.GeometryMode.Branch))
            {
                BeginSettingsSection(3, categoryNames);
                PrepareSpacing(true);

                group.frondCount = GUIIntSlider(PropertyType.Normal, frondCountString, group.frondCount, 1, 16, false);
                group.frondWidth = GUISlider(PropertyType.Normal, frondWidthString, group.frondWidth, 0.1f, 10.0f, true);
                tempCurve = group.frondCurve;
                if (GUICurve(PropertyType.Normal, tempCurve, m_CurveRangesA))
                {
                    group.frondCurve = tempCurve;
                }
                group.frondRange = GUIMinMaxSlider(PropertyType.Normal, frondRangeString, group.frondRange, 0.0f, 1.0f, false);
                group.frondRotation = GUISlider(PropertyType.Normal, frondRotationString, group.frondRotation, 0.0f, 1.0f, false);
                group.frondCrease = GUISlider(PropertyType.Normal, frondCreaseString, group.frondCrease, -1.0f, 1.0f, false);

                GUI.enabled = true;
                EndSettingsSection();
            }

            BeginSettingsSection(4, categoryNames);
            // Animation
            InspectorAnimation(treeData, group);
            EndSettingsSection();

            GUI.enabled = guiEnabled;

            EditorGUILayout.Space();
        }

        public void InspectorLeaf(TreeData treeData, TreeGroupLeaf group)
        {
            InspectorEditTools(target as Tree);

            GUIContent[] categoryNames =
            {
                TreeEditorHelper.GetGUIContent("Distribution|Adjusts the count and placement of leaves in the group. Use the curves to fine tune position, rotation and scale. The curves are relative to the parent branch."),
                TreeEditorHelper.GetGUIContent("Geometry|Select what type of geometry is generated for this leaf group and which materials are applied. If you use a custom mesh, its materials will be used."),
                TreeEditorHelper.GetGUIContent("Shape|Adjusts the shape and growth of the leaves."),
                null,
                null,
                TreeEditorHelper.GetGUIContent("Wind|Adjusts the parameters used for animating this group of leaves. Wind zones are only active in Play Mode. If you select too high values for Main Wind and Main Turbulence the leaves may float away from the branches.")
            };

            bool guiEnabled = GUI.enabled;

            // Locked group warning
            if (s_SelectedGroup.lockFlags != 0)
            {
                GUIunlockbox(treeData);
            }

            BeginSettingsSection(0, categoryNames);
            // Distribution
            InspectorDistribution(treeData, group);
            EndSettingsSection();

            BeginSettingsSection(1, categoryNames);
            PrepareSpacing(false);

            // Geometry
            group.geometryMode = GUIPopup(PropertyType.FullUpdate, geometryModeLeafString, geometryModeOptions, group.geometryMode, false);

            if (group.geometryMode != (int)TreeGroupLeaf.GeometryMode.MESH)
                group.materialLeaf = GUIMaterialField(PropertyType.FullUpdate, group.uniqueID, materialLeafString, group.materialLeaf, TreeEditorHelper.NodeType.LeafNode);

            if (group.geometryMode == (int)TreeGroupLeaf.GeometryMode.MESH)
                group.instanceMesh = GUIObjectField(PropertyType.FullUpdate, meshString, group.instanceMesh, typeof(GameObject), false) as GameObject;
            EndSettingsSection();

            BeginSettingsSection(2, categoryNames);
            PrepareSpacing(false);

            // Shape
            group.size = GUIMinMaxSlider(PropertyType.Normal, sizeString, group.size, 0.1f, 2.0f, false);

            group.perpendicularAlign = GUISlider(PropertyType.Normal, perpendicularAlignString, group.perpendicularAlign, 0.0f, 1.0f, false);
            group.horizontalAlign = GUISlider(PropertyType.Normal, horizontalAlignString, group.horizontalAlign, 0.0f, 1.0f, false);
            EndSettingsSection();

            BeginSettingsSection(5, categoryNames);
            PrepareSpacing(false);

            // Animation
            InspectorAnimation(treeData, group);
            EndSettingsSection();

            GUI.enabled = guiEnabled;

            EditorGUILayout.Space();
        }

        public override bool UseDefaultMargins() { return false; }

        public override void OnInspectorGUI()
        {
            // make sure it's a tree
            Tree obj = target as Tree;
            TreeData treeData = GetTreeData(obj);
            if (!treeData)
                return;

            Renderer renderer = obj.GetComponent<Renderer>();

            // make sure selection is ok
            VerifySelection(treeData);
            if (s_SelectedGroup == null)
            {
                return;
            }

            // reset complete update flag
            m_WantCompleteUpdate = false;

            // We just want events in this window - checking on Event.current.type won't work
            EventType hotControlType = Event.current.GetTypeForControl(GUIUtility.hotControl);
            // MouseUp works for most controls, need to check Return and OnLostFocus for controls with text fields
            if (hotControlType == EventType.MouseUp ||
                (hotControlType == EventType.KeyUp && Event.current.keyCode == KeyCode.Return) ||
                (hotControlType == EventType.ExecuteCommand && Event.current.commandName == "OnLostFocus"))
            {
                if (treeData.isInPreviewMode)
                {
                    // We need a complete rebuild..
                    m_WantCompleteUpdate = true;
                }
            }

            // Check for hotkey event
            if (OnCheckHotkeys(treeData, true))
                return;

            // Refresh all tree shaders to validate whether we don't have too many
            // and in case a node is not using a tree shader - recommend one already used.
            m_TreeEditorHelper.RefreshAllTreeShaders();

            if (m_TreeEditorHelper.GUITooManyShaders())
                m_WantCompleteUpdate = true;

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical(EditorStyles.inspectorFullWidthMargins);

            InspectorHierachy(treeData, renderer);

            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);

            if (s_SelectedGroup != null)
            {
                if (s_SelectedGroup.GetType() == typeof(TreeGroupBranch))
                {
                    InspectorBranch(treeData, (TreeGroupBranch)s_SelectedGroup);
                }
                else if (s_SelectedGroup.GetType() == typeof(TreeGroupLeaf))
                {
                    InspectorLeaf(treeData, (TreeGroupLeaf)s_SelectedGroup);
                }
                else if (s_SelectedGroup.GetType() == typeof(TreeGroupRoot))
                {
                    InspectorRoot(treeData, (TreeGroupRoot)s_SelectedGroup);
                }
            }

            EditorGUILayout.EndVertical();

            if (m_WantedCompleteUpdateInPreviousFrame)
                m_WantCompleteUpdate = true;

            m_WantedCompleteUpdateInPreviousFrame = false;

            // HACKY!
            if (m_WantCompleteUpdate)
            {
                GUI.changed = true;
            }

            if (GUI.changed)
            {
                if (!m_TreeEditorHelper.AreShadersCorrect())
                {
                    m_WantedCompleteUpdateInPreviousFrame = m_WantCompleteUpdate;
                    return;
                }

                if (m_WantCompleteUpdate)
                {
                    // full update
                    UpdateMesh(obj);
                    m_WantCompleteUpdate = false;
                }
                else
                {
                    // preview update
                    PreviewMesh(obj);
                }
            }
        }

        public class Styles
        {
            public GUIContent iconAddLeaves =   EditorGUIUtility.TrIconContent("TreeEditor.AddLeaves", "Add Leaf Group");
            public GUIContent iconAddBranches = EditorGUIUtility.TrIconContent("TreeEditor.AddBranches", "Add Branch Group");
            public GUIContent iconTrash =       EditorGUIUtility.TrIconContent("TreeEditor.Trash", "Delete Selected Group");
            public GUIContent iconDuplicate =   EditorGUIUtility.TrIconContent("TreeEditor.Duplicate", "Duplicate Selected Group");
            public GUIContent iconRefresh =     EditorGUIUtility.TrIconContent("TreeEditor.Refresh", "Recompute Tree");
            public GUIStyle toolbar = "TE Toolbar";
            public GUIStyle toolbarButton = "TE toolbarbutton";

            public GUIStyle nodeBackground = "TE NodeBackground";
            public GUIStyle[] nodeBoxes =
            {
                "TE NodeBox",
                "TE NodeBoxSelected"
            };

            public GUIContent warningIcon = EditorGUIUtility.IconContent("editicon.sml");

            public GUIContent[] nodeIcons =
            {
                EditorGUIUtility.IconContent("tree_icon_branch_frond"),
                EditorGUIUtility.IconContent("tree_icon_branch"),
                EditorGUIUtility.IconContent("tree_icon_frond"),
                EditorGUIUtility.IconContent("tree_icon_leaf"),
                EditorGUIUtility.IconContent("tree_icon")
            };

            public GUIContent[] visibilityIcons =
            {
                EditorGUIUtility.IconContent("animationvisibilitytoggleon"),
                EditorGUIUtility.IconContent("animationvisibilitytoggleoff")
            };

            public GUIStyle nodeLabelTop = "TE NodeLabelTop";
            public GUIStyle nodeLabelBot = "TE NodeLabelBot";
            public GUIStyle pinLabel = "TE PinLabel";
        }
        public static Styles styles;

        Vector2 hierachyScroll = new Vector2();
        Vector2 hierachyNodeSize = new Vector2(40.0f, 48.0f);
        Vector2 hierachyNodeSpace = new Vector2(16.0f, 16.0f);
        Vector2 hierachySpread = new Vector2(32.0f, 32.0f);
        Rect hierachyView = new Rect(0, 0, 0, 0);
        Rect hierachyRect = new Rect(0, 0, 0, 0);
        Rect hierachyDisplayRect = new Rect(0, 0, 0, 0);

        HierachyNode dragNode = null;
        HierachyNode dropNode = null;

        bool isDragging = false;
        Vector2 dragClickPos;

        internal class HierachyNode
        {
            internal Vector3 pos;
            internal TreeGroup group;
            internal Rect rect;
        }

        void DrawHierachy(TreeData treeData, Renderer renderer, Rect sizeRect)
        {
            if (styles == null)
                styles = new Styles();

            hierachySpread = hierachyNodeSize + hierachyNodeSpace;

            hierachyView = sizeRect;
            Event cloneEvent = new Event(Event.current);

            List<HierachyNode> nodes = new List<HierachyNode>();
            BuildHierachyNodes(treeData, nodes, treeData.root, 0);
            LayoutHierachyNodes(nodes, sizeRect);

            // begin scroll view
            float oversize = 16.0f;
            Vector2 offset = Vector2.zero;
            if (sizeRect.width < hierachyRect.width)
            {
                offset.y -= 16.0f;
            }
            bool clear = GUI.changed;

            hierachyDisplayRect = GUILayoutUtility.GetRect(sizeRect.width, hierachyRect.height + oversize);
            hierachyDisplayRect.width = sizeRect.width;

            // background
            GUI.Box(hierachyDisplayRect, GUIContent.none, styles.nodeBackground);

            hierachyScroll = GUI.BeginScrollView(hierachyDisplayRect, hierachyScroll, hierachyRect, false, false);
            GUI.changed = clear;

            // handle dragging
            HandleDragHierachyNodes(treeData, nodes);

            // draw nodes
            DrawHierachyNodes(treeData, nodes, treeData.root, offset / 2, 1.0f, 1.0f);

            // draw drag nodes
            if (dragNode != null && isDragging)
            {
                Vector2 dragOffset = Event.current.mousePosition - dragClickPos;
                DrawHierachyNodes(treeData, nodes, dragNode.group, dragOffset + offset / 2, 0.5f, 0.5f);
            }

            // end scroll view
            // EditorGUILayout.EndScrollView();
            GUI.EndScrollView();

            // draw stats
            if (renderer)
            {
                MeshFilter m = renderer.GetComponent<MeshFilter>();
                if (m && m.sharedMesh && renderer)
                {
                    int vs = m.sharedMesh.vertices.Length;
                    int ts = m.sharedMesh.triangles.Length / 3;
                    int ms = renderer.sharedMaterials.Length;
                    Rect labelrect = new Rect(hierachyDisplayRect.xMax - 80 - 4, hierachyDisplayRect.yMax + offset.y - 40 - 4, 80, 40);

                    string text = TreeEditorHelper.GetGUIContent("Hierarchy Stats").text;
                    text = text.Replace("[v]", vs.ToString());
                    text = text.Replace("[t]", ts.ToString());
                    text = text.Replace("[m]", ms.ToString());
                    text = text.Replace(" / ", "\n");

                    GUI.Label(labelrect, text, EditorStyles.helpBox);
                }
            }


            // Pass scroll wheel event through..
            if ((cloneEvent.type == EventType.ScrollWheel) && (Event.current.type == EventType.Used))
            {
                Event.current = cloneEvent;
            }
        }

        void BuildHierachyNodes(TreeData treeData, List<HierachyNode> nodes, TreeGroup group, int depth)
        {
            HierachyNode node = new HierachyNode();
            node.group = group;
            node.pos = new Vector3(0, depth * hierachySpread.y, 0);

            nodes.Add(node);

            for (int i = 0; i < group.childGroupIDs.Length; i++)
            {
                TreeGroup childGroup = treeData.GetGroup(group.childGroupIDs[i]);
                BuildHierachyNodes(treeData, nodes, childGroup, depth - 1);
            }
        }

        void LayoutHierachyNodes(List<HierachyNode> nodes, Rect sizeRect)
        {
            Bounds bounds = new Bounds();
            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = i + 1; j < nodes.Count; j++)
                {
                    if (nodes[i].pos.y == nodes[j].pos.y)
                    {
                        nodes[i].pos.x -= hierachySpread.x * 0.5f;
                        nodes[j].pos.x = nodes[i].pos.x + hierachySpread.x;
                    }
                }

                bounds.Encapsulate(nodes[i].pos);
            }
            bounds.Expand(hierachySpread);

            hierachyRect = new Rect(0, 0, bounds.size.x, bounds.size.y);
            //hierachyRect.height = Mathf.Max(hierachyRect.height, hierachyView.height);
            hierachyRect.width = Mathf.Max(hierachyRect.width, hierachyView.width);
            Vector3 cen = new Vector3((hierachyRect.xMax + hierachyRect.xMin) * 0.5f, (hierachyRect.yMax + hierachyRect.yMin) * 0.5f, 0.0f);
            cen.y += 8.0f;

            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].pos -= bounds.center;

                nodes[i].pos.x += cen.x;
                nodes[i].pos.y += cen.y;

                nodes[i].rect = new Rect(nodes[i].pos.x - hierachyNodeSize.x * 0.5f, nodes[i].pos.y - hierachyNodeSize.y * 0.5f, hierachyNodeSize.x, hierachyNodeSize.y);
            }
        }

        void HandleDragHierachyNodes(TreeData treeData, List<HierachyNode> nodes)
        {
            if (dragNode == null)
            {
                isDragging = false;
                dropNode = null;
            }

            int handleID = GUIUtility.GetControlID(FocusType.Passive);
            EventType eventType = Event.current.GetTypeForControl(handleID);

            // on left mouse button down
            if (eventType == EventType.MouseDown && Event.current.button == 0)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    // continue if not in the node's rect
                    if (!nodes[i].rect.Contains(Event.current.mousePosition))
                        continue;

                    // don't drag from visibility checkbox rect
                    if (GetHierachyNodeVisRect(nodes[i].rect).Contains(Event.current.mousePosition))
                        continue;

                    if (nodes[i].group is TreeGroupRoot)
                        continue;

                    // start dragging
                    dragClickPos = Event.current.mousePosition;
                    dragNode = nodes[i];
                    GUIUtility.hotControl = handleID;
                    Event.current.Use();
                    break;
                }
            }

            if (dragNode != null)
            {
                // find a drop node
                dropNode = null;

                for (int i = 0; i < nodes.Count; i++)
                {
                    // Are we over this node?
                    if (nodes[i].rect.Contains(Event.current.mousePosition))
                    {
                        //
                        // Verify drop-target is valid
                        //
                        TreeGroup sourceGroup = dragNode.group;
                        TreeGroup targetGroup = nodes[i].group;

                        if (targetGroup == sourceGroup)
                        {
                            // Dropping on itself.. do nothing
                        }
                        else if (!targetGroup.CanHaveSubGroups())
                        {
                            // Drop target cannot have a sub group
                            // Debug.Log("Drop target cannot have subGroups");
                        }
                        else if (treeData.GetGroup(sourceGroup.parentGroupID) == targetGroup)
                        {
                            // No need to do anything
                            // Debug.Log("Drop target already parent of Drag node .. IGNORING");
                        }
                        else if (treeData.IsAncestor(sourceGroup, targetGroup))
                        {
                            // Would create cyclic-dependency
                            // Debug.Log("Drop target is a subGroup of Drag node");
                        }
                        else
                        {
                            // Drop target is ok!
                            dropNode = nodes[i];
                            break;
                        }
                    }
                }

                if (eventType == EventType.MouseMove || eventType == EventType.MouseDrag)
                {
                    Vector2 delta = dragClickPos - Event.current.mousePosition;
                    if (delta.magnitude > 10.0f)
                    {
                        isDragging = true;
                    }
                    Event.current.Use();
                }
                else if (eventType == EventType.MouseUp)
                {
                    if (GUIUtility.hotControl == handleID)
                    {
                        // finish dragging
                        if (dropNode != null)
                        {
                            // Store Complete Undo..
                            UndoStoreSelected(EditMode.Everything);

                            // Relink
                            TreeGroup sourceGroup = dragNode.group;
                            TreeGroup targetGroup = dropNode.group;
                            treeData.SetGroupParent(sourceGroup, targetGroup);

                            // Tell editor to do full mesh update
                            m_WantCompleteUpdate = true;
                        }
                        else
                        {
                            // the nodes have not been dropped on a drop node so e.g.
                            // they landed outside of the hierarchy viewask for a repaint
                            Repaint();
                        }

                        // clear and exit
                        dragNode = null;
                        dropNode = null;

                        // cleanup drag
                        GUIUtility.hotControl = 0;
                        Event.current.Use();
                    }
                }
            }
        }

        Rect GetHierachyNodeVisRect(Rect rect)
        {
            return new Rect(rect.x + rect.width - 13.0f - 1.0f, rect.y + 11, 13.0f, 11.0f);
        }

        void DrawHierachyNodes(TreeData treeData, List<HierachyNode> nodes, TreeGroup group, Vector2 offset, float alpha, float fade)
        {
            // fade
            if ((dragNode != null) && isDragging)
            {
                if (dragNode.group == group)
                {
                    alpha = 0.5f;
                    fade = 0.75f;
                }
            }

            Vector3 delta = new Vector3(0, hierachyNodeSize.y * 0.5f, 0);
            Vector3 offset3 = new Vector3(offset.x, offset.y);
            Handles.color = new Color(0.0f, 0.0f, 0.0f, 0.5f * alpha);
            if (EditorGUIUtility.isProSkin)
                Handles.color = new Color(0.4f, 0.4f, 0.4f, 0.5f * alpha);

            // find node for this group..
            HierachyNode node = null;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (group == nodes[i].group)
                {
                    node = nodes[i];
                    break;
                }
            }
            if (node == null)
            {
                return;
            }

            for (int j = 0; j < group.childGroupIDs.Length; j++)
            {
                TreeGroup childGroup = treeData.GetGroup(group.childGroupIDs[j]);
                for (int k = 0; k < nodes.Count; k++)
                {
                    if (nodes[k].group == childGroup)
                    {
                        Handles.DrawLine(node.pos + offset3 - delta, nodes[k].pos + offset3 + delta);
                    }
                }
            }

            Rect rect = node.rect;
            rect.x += offset.x;
            rect.y += offset.y;

            int nodeBoxIndex = 0;

            if (node == dropNode)
            {
                // hovering over this node which is a valid drop-target
                nodeBoxIndex = 1;
            }
            else if (s_SelectedGroup == node.group)
            {
                if (s_SelectedNode != null)
                {
                    // node selected
                    nodeBoxIndex = 1;
                }
                else
                {
                    // only the group is selected
                    nodeBoxIndex = 1;
                }
            }

            GUI.backgroundColor = new Color(1, 1, 1, alpha);
            GUI.contentColor = new Color(1, 1, 1, alpha);
            GUI.Label(rect, GUIContent.none, styles.nodeBoxes[nodeBoxIndex]);
            //  GUI.Label(rect, styles.nodeBoxes[nodeBoxIndex]);

            Rect pinRectTop = new Rect(rect.x + rect.width / 2f - 4.0f, rect.y - 2.0f, 0f, 0f);
            Rect pinRectBot = new Rect(rect.x + rect.width / 2f - 4.0f, rect.y + rect.height - 2.0f, 0f, 0f);
            Rect iconRect = new Rect(rect.x + 1.0f, rect.yMax - 36f, 32.0f , 32.0f);
            Rect editRect = new Rect(rect.xMax - 18.0f, rect.yMax - 18.0f, 16.0f, 16.0f);
            Rect labelRect = new Rect(rect.x, rect.y, rect.width - 2.0f, 16.0f);

            // Select correct icon
            bool showExtras = true;
            int iconIndex = 0;

            GUIContent buttonContent = new GUIContent();

            System.Type typ = group.GetType();
            if (typ == typeof(TreeGroupBranch))
            {
                buttonContent = TreeEditorHelper.GetGUIContent("|Branch Group");

                TreeGroupBranch br = (TreeGroupBranch)group;
                switch (br.geometryMode)
                {
                    case TreeGroupBranch.GeometryMode.BranchFrond:
                        iconIndex = 0;
                        break;
                    case TreeGroupBranch.GeometryMode.Branch:
                        iconIndex = 1;
                        break;
                    case TreeGroupBranch.GeometryMode.Frond:
                        iconIndex = 2;
                        break;
                }
            }
            else if (typ == typeof(TreeGroupLeaf))
            {
                buttonContent = TreeEditorHelper.GetGUIContent("|Leaf Group");
                iconIndex = 3;
            }
            else if (typ == typeof(TreeGroupRoot))
            {
                buttonContent = TreeEditorHelper.GetGUIContent("|Tree Root Node");
                iconIndex = 4;
                showExtras = false;
            }

            // anything but the root
            if (showExtras)
            {
                // visibility
                // Rect rect = HierachyNodeRect(nodes, i);
                Rect visRect = GetHierachyNodeVisRect(rect);

                GUIContent visContent = TreeEditorHelper.GetGUIContent("|Show / Hide Group");
                visContent.image = styles.visibilityIcons[group.visible ? 0 : 1].image;

                GUI.contentColor = new Color(1, 1, 1, 0.7f);
                if (GUI.Button(visRect, visContent, GUIStyle.none))
                {
                    group.visible = !group.visible;
                    GUI.changed = true;
                }
                GUI.contentColor = Color.white;
            }

            // Icon, click to select..
            buttonContent.image = styles.nodeIcons[iconIndex].image;
            GUI.contentColor = new Color(1, 1, 1, group.visible ? 1 : 0.5f);
            if (GUI.Button(iconRect, buttonContent, GUIStyle.none) || (dragNode == node))
            {
                TreeGroup preSelect = s_SelectedGroup;
                SelectGroup(group);
                if (preSelect == s_SelectedGroup)
                {
                    Tree tree = target as Tree;
                    FrameSelected(tree);
                }
            }
            GUI.contentColor = Color.white;

            // only show top attachement pin if needed
            if (group.CanHaveSubGroups())
            {
                GUI.Label(pinRectTop, GUIContent.none, styles.pinLabel);
            }

            // anything but the root
            if (showExtras)
            {
                GUIContent nodeContent = TreeEditorHelper.GetGUIContent("|Node Count");
                nodeContent.text = group.nodeIDs.Length.ToString();
                GUI.Label(labelRect, nodeContent, styles.nodeLabelTop);

                // one of the node's material has a wrong shader
                if (m_TreeEditorHelper.NodeHasWrongMaterial(group))
                {
                    GUI.DrawTexture(editRect, ConsoleWindow.iconErrorSmall);
                }
                // edited by hand
                else if (group.lockFlags != 0)
                {
                    GUI.DrawTexture(editRect, styles.warningIcon.image);
                }

                GUI.Label(pinRectBot, GUIContent.none, styles.pinLabel);
            }

            // Draw sub groups..
            for (int j = 0; j < group.childGroupIDs.Length; j++)
            {
                TreeGroup childGroup = treeData.GetGroup(group.childGroupIDs[j]);
                DrawHierachyNodes(treeData, nodes, childGroup, offset, alpha * fade, fade);
            }

            GUI.backgroundColor = Color.white;
            GUI.contentColor = Color.white;
        }

        private static void BeginSettingsSection(int nr, GUIContent[] names)
        {
            GUILayout.Space(kSectionSpace / 2.0f);

            GUILayout.Label(names[nr].text, EditorStyles.boldLabel);
        }

        private static void EndSettingsSection()
        {
            GUI.enabled = true;
            GUILayout.Space(kSectionSpace / 2.0f);
        }
    }
}
