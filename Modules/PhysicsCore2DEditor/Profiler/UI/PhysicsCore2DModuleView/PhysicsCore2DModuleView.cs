// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.U2D.PhysicsCore2D.Profiler.UI
{
    [UxmlElement]
    partial class PhysicsCore2DModuleView : VisualElement
    {
        const string k_UXML = "PhysicsCore2D/Profiler/PhysicsCore2DModuleView/PhysicsCore2DModuleView.uxml";
        enum Grouping { ByWorld, ByModule, ByMarker }

        [NoAutoStaticsCleanup]
        static readonly (string GroupName, string[] markers)[] k_MarkerGroups =
        {
            ("Transform", new[] { "PhysicsCore2D.TransformChanged", "PhysicsCore2D.TransformParentHierarchyChanged" }),
            ("PhysicsWorld", new[] { "PhysicsCore2D.PhysicsWorld.Create", "PhysicsCore2D.PhysicsWorld.Destroy", "PhysicsCore2D.PhysicsWorld.WriteDefinition", "PhysicsCore2D.PhysicsWorld.ReadDefinition", "PhysicsCore2D.PhysicsWorld.Simulate", "PhysicsCore2D.PhysicsWorld.SimulateBatch", "PhysicsCore2D.PhysicsWorld.Simulate.StepWorlds", "PhysicsCore2D.PhysicsWorld.PreSimulate", "PhysicsCore2D.PhysicsWorld.PostSimulate", "PhysicsCore2D.PhysicsWorld.ProcessTransformWriteTweens", "PhysicsCore2D.PhysicsWorld.SyncInterpolation", "PhysicsCore2D.PhysicsWorld.SetTransformWriteTweens", "PhysicsCore2D.PhysicsWorld.SetTransform", "PhysicsCore2D.PhysicsWorld.SetTransformAccess", "PhysicsCore2D.PhysicsWorld.WriteTransforms", "PhysicsCore2D.PhysicsWorld.WriteTransforms.CalculateWorldTransformWrite", "PhysicsCore2D.PhysicsWorld.WriteTransforms.SortTransformWriteTeens", "PhysicsCore2D.PhysicsWorld.WriteTransforms.SetTransformAccessArray", "PhysicsCore2D.PhysicsWorld.WriteTransformTweens", "PhysicsCore2D.PhysicsWorld.WriteTransformTweensSync", "PhysicsCore2D.PhysicsWorld.TestOverlapAABB", "PhysicsCore2D.PhysicsWorld.TestOverlapShape", "PhysicsCore2D.PhysicsWorld.TestOverlapShapeProxy", "PhysicsCore2D.PhysicsWorld.TestOverlapPoint", "PhysicsCore2D.PhysicsWorld.TestOverlapCircleGeometry", "PhysicsCore2D.PhysicsWorld.TestOverlapCapsuleGeometry", "PhysicsCore2D.PhysicsWorld.TestOverlapPolygonGeometry", "PhysicsCore2D.PhysicsWorld.TestOverlapSegmentGeometry", "PhysicsCore2D.PhysicsWorld.TestOverlapChainSegmentGeometry", "PhysicsCore2D.PhysicsWorld.OverlapAABB", "PhysicsCore2D.PhysicsWorld.OverlapShape", "PhysicsCore2D.PhysicsWorld.OverlapShapeProxy", "PhysicsCore2D.PhysicsWorld.OverlapPoint", "PhysicsCore2D.PhysicsWorld.OverlapCircleGeometry", "PhysicsCore2D.PhysicsWorld.OverlapCapsuleGeometry", "PhysicsCore2D.PhysicsWorld.OverlapPolygonGeometry", "PhysicsCore2D.PhysicsWorld.OverlapSegmentGeometry", "PhysicsCore2D.PhysicsWorld.OverlapChainSegmentGeometry", "PhysicsCore2D.PhysicsWorld.CastRay", "PhysicsCore2D.PhysicsWorld.CastShape", "PhysicsCore2D.PhysicsWorld.CastShapeProxy", "PhysicsCore2D.PhysicsWorld.CastMover", "PhysicsCore2D.PhysicsWorld.GetBodyUpdateUserData", "PhysicsCore2D.PhysicsWorld.GetBodyUpdateCallbackTargets", "PhysicsCore2D.PhysicsWorld.GetTriggerCallbackTargets", "PhysicsCore2D.PhysicsWorld.GetContactCallbackTargets", "PhysicsCore2D.PhysicsWorld.GetJointThresholdCallbackTargets", "PhysicsCore2D.PhysicsWorld.AutoBodyUpdateCallbacks", "PhysicsCore2D.PhysicsWorld.AutoContactCallbacks", "PhysicsCore2D.PhysicsWorld.AutoTriggerCallbacks", "PhysicsCore2D.PhysicsWorld.AutoJointThresholdCallbacks", "PhysicsCore2D.PhysicsWorld.QueueTask", "PhysicsCore2D.PhysicsWorld.FinishTask", "PhysicsCore2D.PhysicsWorld.Explode" }),
            ("PhysicsBody", new[] { "PhysicsCore2D.PhysicsBody.Create", "PhysicsCore2D.PhysicsBody.CreateBatch", "PhysicsCore2D.PhysicsBody.Destroy", "PhysicsCore2D.PhysicsBody.DestroyBatch", "PhysicsCore2D.PhysicsBody.WriteDefinition", "PhysicsCore2D.PhysicsBody.ReadDefinition", "PhysicsCore2D.PhysicsBody.SetPosition", "PhysicsCore2D.PhysicsBody.SetRotation", "PhysicsCore2D.PhysicsBody.SetTransform", "PhysicsCore2D.PhysicsBody.SetTransformTarget", "PhysicsCore2D.PhysicsBody.SetBatchTransform", "PhysicsCore2D.PhysicsBody.GetBatchTransform", "PhysicsCore2D.PhysicsBody.GetBatchTransformObjects", "PhysicsCore2D.PhysicsBody.SetLinearVelocity", "PhysicsCore2D.PhysicsBody.SetAngularVelocity", "PhysicsCore2D.PhysicsBody.ApplyForce", "PhysicsCore2D.PhysicsBody.ApplyForceToCenter", "PhysicsCore2D.PhysicsBody.ApplyTorque", "PhysicsCore2D.PhysicsBody.ApplyLinearImpulse", "PhysicsCore2D.PhysicsBody.ApplyLinearImpulseToCenter", "PhysicsCore2D.PhysicsBody.ApplyAngularImpulse", "PhysicsCore2D.PhysicsBody.ApplyBuoyancy", "PhysicsCore2D.PhysicsBody.ApplyBuoyancyOverlap", "PhysicsCore2D.PhysicsBody.ApplyWind", "PhysicsCore2D.PhysicsBody.ApplyWindOverlap", "PhysicsCore2D.PhysicsBody.SetBatchVelocity", "PhysicsCore2D.PhysicsBody.SetBatchForce", "PhysicsCore2D.PhysicsBody.SetBatchImpulse", "PhysicsCore2D.PhysicsBody.GetBatchVelocity", "PhysicsCore2D.PhysicsBody.SetBodyType", "PhysicsCore2D.PhysicsBody.SetMassConfiguration", "PhysicsCore2D.PhysicsBody.ApplyMassFromShapes", "PhysicsCore2D.PhysicsBody.MassOverride", "PhysicsCore2D.PhysicsBody.SetAwake", "PhysicsCore2D.PhysicsBody.SetEnabled", "PhysicsCore2D.PhysicsBody.SetFastRotationAllowed", "PhysicsCore2D.PhysicsBody.SetFastCollisionsAllowed", "PhysicsCore2D.PhysicsBody.SetContactEvents", "PhysicsCore2D.PhysicsBody.SetHitEvents", "PhysicsCore2D.PhysicsBody.GetShapes", "PhysicsCore2D.PhysicsBody.GetJoints", "PhysicsCore2D.PhysicsBody.GetContacts", "PhysicsCore2D.PhysicsBody.userData", "PhysicsCore2D.PhysicsBody.ownerUserData", "PhysicsCore2D.PhysicsBody.transformObject", "PhysicsCore2D.PhysicsBody.transformWriteMode" }),
            ("Geometry", new[] { "PhysicsCore2D.PolygonGeometry.CreateBox", "PhysicsCore2D.PolygonGeometry.Create", "PhysicsCore2D.PolygonGeometry.CreatePolygons", "PhysicsCore2D.ChainSegmentGeometry.CreateSegments", "PhysicsCore2D.ChainSegmentGeometry.UpdateSegments" }),
            ("PhysicsShape", new[] { "PhysicsCore2D.PhysicsShape.CreateCircle", "PhysicsCore2D.PhysicsShape.CreateCapsule", "PhysicsCore2D.PhysicsShape.CreatePolygon", "PhysicsCore2D.PhysicsShape.CreateSegment", "PhysicsCore2D.PhysicsShape.CreateChainSegment", "PhysicsCore2D.PhysicsShape.CreateShapeBatch", "PhysicsCore2D.PhysicsShape.Destroy", "PhysicsCore2D.PhysicsShape.DestroyBatch", "PhysicsCore2D.PhysicsShape.WriteDefinition", "PhysicsCore2D.PhysicsShape.ReadDefinition", "PhysicsCore2D.PhysicsShape.SetDensity", "PhysicsCore2D.PhysicsShape.SetContactFilter", "PhysicsCore2D.PhysicsShape.SetCircleGeometry", "PhysicsCore2D.PhysicsShape.SetCapsuleGeometry", "PhysicsCore2D.PhysicsShape.SetPolygonGeometry", "PhysicsCore2D.PhysicsShape.SetSegmentGeometry", "PhysicsCore2D.PhysicsShape.SetChainSegmentGeometry", "PhysicsCore2D.PhysicsShape.SetBatchCircleGeometry", "PhysicsCore2D.PhysicsShape.SetBatchCapsuleGeometry", "PhysicsCore2D.PhysicsShape.SetBatchPolygonGeometry", "PhysicsCore2D.PhysicsShape.SetBatchSegmentGeometry", "PhysicsCore2D.PhysicsShape.SetBatchChainSegmentGeometry", "PhysicsCore2D.PhysicsShape.OverlapPoint", "PhysicsCore2D.PhysicsShape.ClosestPoint", "PhysicsCore2D.PhysicsShape.CastRay", "PhysicsCore2D.PhysicsShape.CastShape", "PhysicsCore2D.PhysicsShape.GetContacts", "PhysicsCore2D.PhysicsShape.GetTriggerVisitors", "PhysicsCore2D.PhysicsShape.ApplyBuoyancy", "PhysicsCore2D.PhysicsShape.ApplyWind", "PhysicsCore2D.PhysicsShape.FilterContactCallback", "PhysicsCore2D.PhysicsShape.PreSolveCallback", "PhysicsCore2D.PhysicsShape.userData", "PhysicsCore2D.PhysicsShape.ownerUserData" }),
            ("PhysicsChain", new[] { "PhysicsCore2D.PhysicsChain.Create", "PhysicsCore2D.PhysicsChain.Destroy", "PhysicsCore2D.PhysicsChain.UpdateVertices", "PhysicsCore2D.PhysicsChain.userData", "PhysicsCore2D.PhysicsChain.ownerUserData" }),
            ("PhysicsJoint", new[] { "PhysicsCore2D.PhysicsJoint.Destroy", "PhysicsCore2D.PhysicsJoint.DestroyBatch", "PhysicsCore2D.PhysicsJoint.userData", "PhysicsCore2D.PhysicsJoint.ownerUserData", "PhysicsCore2D.PhysicsDistanceJoint.Create", "PhysicsCore2D.PhysicsDistanceJoint.WriteDefinition", "PhysicsCore2D.PhysicsDistanceJoint.ReadDefinition", "PhysicsCore2D.PhysicsRelativeJoint.Create", "PhysicsCore2D.PhysicsRelativeJoint.WriteDefinition", "PhysicsCore2D.PhysicsRelativeJoint.ReadDefinition", "PhysicsCore2D.PhysicsIgnoreJoint.Create", "PhysicsCore2D.PhysicsIgnoreJoint.WriteDefinition", "PhysicsCore2D.PhysicsIgnoreJoint.ReadDefinition", "PhysicsCore2D.PhysicsSliderJoint.Create", "PhysicsCore2D.PhysicsSliderJoint.WriteDefinition", "PhysicsCore2D.PhysicsSliderJoint.ReadDefinition", "PhysicsCore2D.PhysicsHingeJoint.Create", "PhysicsCore2D.PhysicsHingeJoint.WriteDefinition", "PhysicsCore2D.PhysicsHingeJoint.ReadDefinition", "PhysicsCore2D.PhysicsFixedJoint.Create", "PhysicsCore2D.PhysicsFixedJoint.WriteDefinition", "PhysicsCore2D.PhysicsFixedJoint.ReadDefinition", "PhysicsCore2D.PhysicsWheelJoint.Create", "PhysicsCore2D.PhysicsWheelJoint.WriteDefinition", "PhysicsCore2D.PhysicsWheelJoint.ReadDefinition" }),
            ("PhysicsComposer", new[] { "PhysicsCore2D.PhysicsComposer.Create", "PhysicsCore2D.PhysicsComposer.Destroy", "PhysicsCore2D.PhysicsComposer.DestroyAll", "PhysicsCore2D.PhysicsComposer.AddLayer", "PhysicsCore2D.PhysicsComposer.RemoveLayer", "PhysicsCore2D.PhysicsComposer.ClearLayers", "PhysicsCore2D.PhysicsComposer.GetLayerCount", "PhysicsCore2D.PhysicsComposer.GetLayerHandles", "PhysicsCore2D.PhysicsComposer.GetComposers", "PhysicsCore2D.PhysicsComposer.CreatePolygons", "PhysicsCore2D.PhysicsComposer.CreateConvexHulls", "PhysicsCore2D.PhysicsComposer.CreateChains", "PhysicsCore2D.PhysicsComposer.GetGeometryIslands", "PhysicsCore2D.PhysicsComposer.AddVertexGeometryPath", "PhysicsCore2D.PhysicsComposer.AddCircleGeometryPath", "PhysicsCore2D.PhysicsComposer.AddCapsuleGeometryPath", "PhysicsCore2D.PhysicsComposer.AddPolygonGeometryPath", "PhysicsCore2D.PhysicsComposer.AddPolygonGeometryPathWithRadius", "PhysicsCore2D.PhysicsComposer.AddRawGeometryPath", "PhysicsCore2D.PhysicsComposer.AddShapeGeometryPath", "PhysicsCore2D.PhysicsComposer.CreateLayerPaths", "PhysicsCore2D.PhysicsComposer.CreateGeometryPaths", "PhysicsCore2D.PhysicsComposer.TessellationPaths", "PhysicsCore2D.PhysicsComposer.Tessellation", "PhysicsCore2D.PhysicsComposer.Tessellation.ConstructPolygons" }),
            ("Draw", new[] { "PhysicsCore2D.PhysicsWorld.DrawCircleGeometry", "PhysicsCore2D.PhysicsWorld.DrawCapsuleGeometry", "PhysicsCore2D.PhysicsWorld.DrawPolygonGeometry", "PhysicsCore2D.PhysicsWorld.DrawSegmentGeometry", "PhysicsCore2D.PhysicsWorld.DrawQueryCastRay", "PhysicsCore2D.PhysicsWorld.DrawQueryShapeProxy", "PhysicsCore2D.PhysicsWorld.DrawQueryCastResult", "PhysicsCore2D.PhysicsWorld.DrawQueryWorldCastResult", "PhysicsCore2D.PhysicsWorld.DrawShapeProxy", "PhysicsCore2D.PhysicsWorld.DrawBox", "PhysicsCore2D.PhysicsWorld.DrawCircle", "PhysicsCore2D.PhysicsWorld.DrawCapsule", "PhysicsCore2D.PhysicsWorld.DrawPoint", "PhysicsCore2D.PhysicsWorld.DrawLine", "PhysicsCore2D.PhysicsWorld.DrawLineStrip", "PhysicsCore2D.PhysicsWorld.DrawTransformAxis", "PhysicsCore2D.DrawWorld", "PhysicsCore2D.DrawWorld.UpdateLifetimes", "PhysicsCore2D.DrawWorld.CompileDrawResults", "PhysicsCore2D.PhysicsWorld.DrawShapes", "PhysicsCore2D.PhysicsBody.Draw", "PhysicsCore2D.PhysicsShape.Draw", "PhysicsCore2D.PhysicsJoint.Draw" }),
            ("PhysicsDestructor", new[] { "PhysicsCore2D.PhysicsDestructor.Fragment", "PhysicsCore2D.PhysicsDestructor.FragmentMasked", "PhysicsCore2D.PhysicsDestructor.Slice" }),
            ("PhysicsSpace", new[] { "PhysicsCore2D.PhysicsSpace.Create", "PhysicsCore2D.PhysicsSpace.Destroy", "PhysicsCore2D.PhysicsSpace.DestroyAll", "PhysicsCore2D.PhysicsSpace.GetSpaces", "PhysicsCore2D.PhysicsSpace.GetSourceWorld", "PhysicsCore2D.PhysicsSpace.Clone", "PhysicsCore2D.PhysicsSpace.CreateProxy", "PhysicsCore2D.PhysicsSpace.CreateProxyShapes", "PhysicsCore2D.PhysicsSpace.DestroyProxy", "PhysicsCore2D.PhysicsSpace.ClearProxies", "PhysicsCore2D.PhysicsSpace.GetProxyAABB", "PhysicsCore2D.PhysicsSpace.SetBatchProxyAABB", "PhysicsCore2D.PhysicsSpace.GetBatchProxyAABB", "PhysicsCore2D.PhysicsSpace.GetProxyCategories", "PhysicsCore2D.PhysicsSpace.GetBatchProxyCategories", "PhysicsCore2D.PhysicsSpace.SetBatchProxyCategories", "PhysicsCore2D.PhysicsSpace.GetProxyUserHandle", "PhysicsCore2D.PhysicsSpace.SetProxyUserHandle", "PhysicsCore2D.PhysicsSpace.GetBatchProxyUserHandle", "PhysicsCore2D.PhysicsSpace.CastRay", "PhysicsCore2D.PhysicsSpace.CastShape", "PhysicsCore2D.PhysicsSpace.OverlapAABB", "PhysicsCore2D.PhysicsSpace.SyncShapes" }),
        };

        // Maps each marker name to its index in PhysicsCore2DProfilerMarkers.k_MarkerNames (and the per-frame timing array).
        [NoAutoStaticsCleanup]
        static readonly Dictionary<string, int> k_MarkerNameToIndex = BuildMarkerNameToIndex();

        static Dictionary<string, int> BuildMarkerNameToIndex()
        {
            var names = PhysicsCore2DProfilerMarkers.k_MarkerNames;
            var map = new Dictionary<string, int>(names.Length);
            for (int i = 0; i < names.Length; ++i)
                map[names[i]] = i;
            return map;
        }

        MultiColumnTreeView m_WorldTreeView;
        Label m_NoDataLabel;
        VisualElement m_ContentContainer;
        DropdownField m_GroupingDropdown;
        List<TreeViewItemData<TreeNodeData>> m_Data = new();
        Dictionary<int, string> m_IdToKey = new();
        HashSet<string> m_ByWorldExpandedKeys = new();
        HashSet<string> m_ByModuleExpandedKeys = new();
        HashSet<string> m_ByMarkerExpandedKeys = new();
        PhysicsCore2DFrameData[] m_FrameData;
        float[] m_ProfileMarkers;
        int m_NextId;

        public PhysicsCore2DModuleView()
        {
            VisualTreeAsset visualTree = EditorGUIUtility.Load(k_UXML) as VisualTreeAsset;
            visualTree.CloneTree(this);

            m_WorldTreeView = this.Q<MultiColumnTreeView>("WorldTreeView");
            m_NoDataLabel = this.Q<Label>("noDataLabel");
            m_ContentContainer = this.Q<VisualElement>("contentContainer");
            m_GroupingDropdown = this.Q<DropdownField>("groupingDropdown");

            m_GroupingDropdown.choices = new List<string> { "Physics World", "Physics Module", "Physics Profiler Timing" };
            m_GroupingDropdown.index = 0;
            m_GroupingDropdown.RegisterValueChangedCallback(_ => Rebuild());

            SetupTree();
            ShowTree();
        }

        void SetupTree()
        {
            if (EditorGUIUtility.isProSkin)
                m_WorldTreeView.AddToClassList("dark");
            else
                m_WorldTreeView.AddToClassList("light");

            m_WorldTreeView.sortingMode = ColumnSortingMode.Custom;
            m_WorldTreeView.columnSortingChanged += OnColumnSortingChanged;

            foreach (var column in m_WorldTreeView.columns)
            {
                column.makeCell = () =>
                {
                    var label = new Label();
                    label.AddToClassList("cell-label");
                    return label;
                };

                column.bindCell = (element, index) =>
                {
                    var label = element as Label;
                    var data = m_WorldTreeView.GetItemDataForIndex<TreeNodeData>(index);
                    if (data == null)
                        return;

                    label.text = column.name == "Name" ? data.name : data.value;
                };

                column.comparison = (a, b) =>
                {
                    var dataA = m_WorldTreeView.GetItemDataForIndex<TreeNodeData>(a);
                    var dataB = m_WorldTreeView.GetItemDataForIndex<TreeNodeData>(b);
                    if (dataA == null || dataB == null)
                        return 0;
                    return column.name == "Name"
                        ? string.Compare(dataA.name, dataB.name, StringComparison.Ordinal)
                        : dataA.timeMs.CompareTo(dataB.timeMs);
                };
            }
        }

        // Rebuilding from the frame data restores the natural order when sorting is cleared, which re-sorting the already-sorted items cannot.
        void OnColumnSortingChanged() => Rebuild();

        void ApplySorting(List<TreeViewItemData<TreeNodeData>> items)
        {
            // With no sorted column every comparison ties, and an unstable sort would still shuffle the build order, so leave the items untouched.
            if (!HasSortedColumns())
                return;

            SortItems(items);
        }

        bool HasSortedColumns()
        {
            if (m_WorldTreeView.sortedColumns == null)
                return false;

            foreach (var _ in m_WorldTreeView.sortedColumns)
                return true;

            return false;
        }

        void SortItems(List<TreeViewItemData<TreeNodeData>> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item.children != null)
                {
                    var children = new List<TreeViewItemData<TreeNodeData>>(item.children);
                    SortItems(children);
                    items[i] = new TreeViewItemData<TreeNodeData>(item.id, item.data, children);
                }
            }

            items.Sort(CompareItems);
        }

        int CompareItems(TreeViewItemData<TreeNodeData> a, TreeViewItemData<TreeNodeData> b)
        {
            foreach (var sort in m_WorldTreeView.sortedColumns)
            {
                int result = sort.column.name == "Name"
                    ? string.Compare(a.data.name, b.data.name, StringComparison.Ordinal)
                    : a.data.timeMs.CompareTo(b.data.timeMs);
                if (result != 0)
                    return sort.direction == SortDirection.Ascending ? result : -result;
            }
            return 0;
        }

        public void SetData(PhysicsCore2DFrameData[] frameData, float[] profileMarkers)
        {
            m_FrameData = frameData;
            if (m_FrameData != null)
                Array.Sort(m_FrameData, (a, b) => a.worldIndex.CompareTo(b.worldIndex));
            m_ProfileMarkers = (profileMarkers != null && profileMarkers.Length == PhysicsCore2DProfilerMarkers.k_MarkerNames.Length)
                ? profileMarkers : null;
            Rebuild();
        }

        void Rebuild()
        {
            var grouping = m_GroupingDropdown.index switch
            {
                0 => Grouping.ByWorld,
                1 => Grouping.ByModule,
                _ => Grouping.ByMarker,
            };
            var expandedKeys = grouping == Grouping.ByWorld ? m_ByWorldExpandedKeys
                             : grouping == Grouping.ByModule ? m_ByModuleExpandedKeys
                             : m_ByMarkerExpandedKeys;

            foreach (var (id, key) in m_IdToKey)
            {
                if (m_WorldTreeView.IsExpanded(id))
                    expandedKeys.Add(key);
                else
                    expandedKeys.Remove(key);
            }

            m_NextId = 0;
            m_Data.Clear();
            m_IdToKey.Clear();

            if (grouping == Grouping.ByMarker)
                BuildByMarker();
            else if (m_FrameData != null)
            {
                if (grouping == Grouping.ByWorld)
                    BuildByWorld();
                else
                    BuildByModule();
            }

            ApplySorting(m_Data);
            m_WorldTreeView.SetRootItems(m_Data);
            m_WorldTreeView.Rebuild();

            foreach (var (id, key) in m_IdToKey)
            {
                if (expandedKeys.Contains(key))
                    m_WorldTreeView.ExpandItem(id, false);
            }

            ShowTree();
        }

        // Allocate the id for an expandable node and record its expansion key, which is the node's path within the tree so it survives a rebuild.
        // Leaf rows have nothing to expand, so they take a plain id without a key.
        int RegisterNode(string expansionKey)
        {
            int id = m_NextId++;
            m_IdToKey[id] = expansionKey;
            return id;
        }

        void BuildByWorld()
        {
            for (int i = 0; i < m_FrameData.Length; i++)
            {
                var worldData = new PhysicsWorldTreeData(m_FrameData[i]);
                int worldId = RegisterNode($"World:{worldData.name}");
                var worldNode = TreeNodeData.CreateWorldNode(worldId, worldData);

                var moduleItems = new List<TreeViewItemData<TreeNodeData>>();
                foreach (var module in worldData.modules)
                {
                    int moduleId = RegisterNode($"World:{worldData.name}/{module.name}");
                    var moduleNode = TreeNodeData.CreateModuleNode(moduleId, module);

                    var entryItems = new List<TreeViewItemData<TreeNodeData>>();
                    foreach (var entry in module.entries)
                    {
                        int entryId = m_NextId++;
                        entryItems.Add(new TreeViewItemData<TreeNodeData>(entryId, TreeNodeData.CreateTimingEntryNode(entryId, entry)));
                    }

                    moduleItems.Add(new TreeViewItemData<TreeNodeData>(moduleId, moduleNode, entryItems));
                }

                m_Data.Add(new TreeViewItemData<TreeNodeData>(worldId, worldNode, moduleItems));
            }
        }

        void BuildByModule()
        {
            // Collect all worlds' data first.
            var worlds = new List<PhysicsWorldTreeData>();
            for (int i = 0; i < m_FrameData.Length; i++)
                worlds.Add(new PhysicsWorldTreeData(m_FrameData[i]));

            if (worlds.Count == 0)
                return;

            // Use module order from the first world; all worlds share the same module types.
            foreach (var templateModule in worlds[0].modules)
            {
                float moduleTotalTime = 0f;
                foreach (var world in worlds)
                {
                    var match = world.modules.Find(m => m.moduleType == templateModule.moduleType);
                    if (match != null)
                        moduleTotalTime += match.totalTime;
                }

                int moduleId = RegisterNode($"Module:{templateModule.name}");
                var moduleNode = new TreeNodeData(moduleId, TreeNodeType.Module,
                    templateModule.name, $"{moduleTotalTime:F3}ms", moduleTotalTime);

                // Children: per timing entry, showing each world's contribution.
                var entryGroupItems = new List<TreeViewItemData<TreeNodeData>>();
                foreach (var templateEntry in templateModule.entries)
                {
                    float entryTotalTime = 0f;
                    foreach (var world in worlds)
                    {
                        var match = world.modules.Find(m => m.moduleType == templateModule.moduleType);
                        var entry = match?.entries.Find(e => e.name == templateEntry.name);
                        if (entry != null)
                            entryTotalTime += entry.timeMs;
                    }

                    int entryId = RegisterNode($"Module:{templateModule.name}/{templateEntry.name}");
                    var entryNode = new TreeNodeData(entryId, TreeNodeType.TimingEntry,
                        templateEntry.name, $"{entryTotalTime:F3}ms", entryTotalTime);

                    var worldItems = new List<TreeViewItemData<TreeNodeData>>();
                    foreach (var world in worlds)
                    {
                        var match = world.modules.Find(m => m.moduleType == templateModule.moduleType);
                        var entry = match?.entries.Find(e => e.name == templateEntry.name);
                        if (entry == null)
                            continue;

                        int worldId = m_NextId++;
                        var worldNode = new TreeNodeData(worldId, TreeNodeType.World,
                            world.name, $"{entry.timeMs:F3}ms", entry.timeMs);
                        worldItems.Add(new TreeViewItemData<TreeNodeData>(worldId, worldNode));
                    }

                    entryGroupItems.Add(new TreeViewItemData<TreeNodeData>(entryId, entryNode, worldItems));
                }

                m_Data.Add(new TreeViewItemData<TreeNodeData>(moduleId, moduleNode, entryGroupItems));
            }
        }

        void BuildByMarker()
        {
            if (m_ProfileMarkers == null)
                return;

            foreach (var (groupName, markers) in k_MarkerGroups)
            {
                float groupTotal = 0f;
                foreach (var marker in markers)
                    groupTotal += m_ProfileMarkers[k_MarkerNameToIndex[marker]];

                int groupId = RegisterNode($"Marker:{groupName}");
                var groupNode = new TreeNodeData(groupId, TreeNodeType.Module,
                    groupName, $"{groupTotal:F3}ms", groupTotal);

                var markerItems = new List<TreeViewItemData<TreeNodeData>>();
                foreach (var marker in markers)
                {
                    int markerIndex = k_MarkerNameToIndex[marker];
                    float time = m_ProfileMarkers[markerIndex];

                    int markerId = m_NextId++;
                    var markerNode = new TreeNodeData(markerId, TreeNodeType.TimingEntry,
                        PhysicsCore2DProfilerMarkers.k_MarkerNames[markerIndex], $"{time:F3}ms", time);
                    markerItems.Add(new TreeViewItemData<TreeNodeData>(markerId, markerNode));
                }

                m_Data.Add(new TreeViewItemData<TreeNodeData>(groupId, groupNode, markerItems));
            }
        }

        void ShowTree()
        {
            bool hasData = m_Data.Count > 0;
            m_ContentContainer.style.display = hasData ? DisplayStyle.Flex : DisplayStyle.None;
            m_NoDataLabel.style.display = hasData ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
