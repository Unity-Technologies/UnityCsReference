// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
using UnityEditor.IMGUI.Controls;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace UnityEditorInternal
{
    [System.Serializable]
    internal class AnimationWindowState : ScriptableObject, ICurveEditorState
    {
        public enum RefreshType
        {
            None = 0,
            CurvesOnly = 1,
            Everything = 2
        };

        public enum SnapMode
        {
            Disabled = 0,
            SnapToFrame = 1,
            SnapToClipFrame = 2
        };

        [SerializeField] public AnimationWindowHierarchyState hierarchyState; // Persistent state of treeview on the left side of window
        [SerializeField] public AnimEditor animEditor; // Reference to owner of this state. Used to trigger repaints.
        [SerializeField] public bool showCurveEditor; // Do we show dopesheet or curves
        [SerializeField] public bool linkedWithSequencer; // Toggle Sequencer selection mode.
        [SerializeField] private TimeArea m_TimeArea; // Either curveeditor or dopesheet depending on which is selected
        [SerializeField] private AnimationWindowSelection m_Selection; // Internal selection
        [SerializeField] private AnimationWindowKeySelection m_KeySelection; // What is selected. Hashes persist cache reload, because they are from keyframe time+value
        [SerializeField] private int m_ActiveKeyframeHash; // Which keyframe is active (selected key that user previously interacted with)
        [SerializeField] private float m_FrameRate = kDefaultFrameRate;
        [SerializeField] private TimeArea.TimeFormat m_TimeFormat = TimeArea.TimeFormat.TimeFrame;
        [SerializeField] private AnimationWindowControl m_ControlInterface;
        [SerializeField] private IAnimationWindowControl m_OverrideControlInterface;

        [NonSerialized] public Action onStartLiveEdit;
        [NonSerialized] public Action onEndLiveEdit;
        [NonSerialized] public Action<float> onFrameRateChange;

        private static List<AnimationWindowKeyframe> s_KeyframeClipboard; // For copy-pasting keyframes

        [NonSerialized] public AnimationWindowHierarchyDataSource hierarchyData;

        private List<AnimationWindowCurve> m_ActiveCurvesCache;
        private List<DopeLine> m_dopelinesCache;
        private List<AnimationWindowKeyframe> m_SelectedKeysCache;
        private Bounds? m_SelectionBoundsCache;
        private CurveWrapper[] m_ActiveCurveWrappersCache;
        private AnimationWindowKeyframe m_ActiveKeyframeCache;
        private HashSet<int> m_ModifiedCurves = new HashSet<int>();
        private EditorCurveBinding? m_lastAddedCurveBinding;

        // Hash of all the things that require animationWindow to refresh if they change
        private int m_PreviousRefreshHash;

        // Changing m_Refresh means you are ordering a refresh at the next OnGUI().
        // CurvesOnly means that there is no need to refresh the hierarchy, since only the keyframe data changed.
        private RefreshType m_Refresh = RefreshType.None;

        private struct LiveEditKeyframe
        {
            public AnimationWindowKeyframe keySnapshot;
            public AnimationWindowKeyframe key;
        }

        private class LiveEditCurve
        {
            public AnimationWindowCurve curve;
            public List<LiveEditKeyframe> selectedKeys = new List<LiveEditKeyframe>();
            public List<LiveEditKeyframe> unselectedKeys = new List<LiveEditKeyframe>();
        }

        private List<LiveEditCurve> m_LiveEditSnapshot;

        public const float kDefaultFrameRate = 60.0f;
        public const string kEditCurveUndoLabel = "Edit Curve";

        public AnimationWindowSelection selection
        {
            get
            {
                if (m_Selection == null)
                    m_Selection = new AnimationWindowSelection();

                return m_Selection;
            }
        }

        public AnimationWindowSelectionItem selectedItem
        {
            get
            {
                if ((m_Selection != null) && (m_Selection.count > 0))
                {
                    return m_Selection.First();
                }

                return null;
            }

            set
            {
                if (m_Selection == null)
                    m_Selection = new AnimationWindowSelection();

                if (value == null)
                {
                    m_Selection.Clear();
                }
                else
                {
                    m_Selection.Set(value);
                }
            }
        }

        // AnimationClip we are currently editing
        public AnimationClip activeAnimationClip
        {
            get
            {
                if (selectedItem != null)
                {
                    return selectedItem.animationClip;
                }

                return null;
            }
        }


        // Previously or currently selected gameobject is considered as the active gameobject
        public GameObject activeGameObject
        {
            get
            {
                if (selectedItem != null)
                {
                    return selectedItem.gameObject;
                }

                return null;
            }
        }

        // Closes parent to activeGameObject that has Animator component
        public GameObject activeRootGameObject
        {
            get
            {
                if (selectedItem != null)
                {
                    return selectedItem.rootGameObject;
                }

                return null;
            }
        }

        public Component activeAnimationPlayer
        {
            get
            {
                if (selectedItem != null)
                {
                    return selectedItem.animationPlayer;
                }

                return null;
            }
        }

        // Is the hierarchy in animator optimized
        public bool animatorIsOptimized
        {
            get
            {
                if (selectedItem != null)
                {
                    return selectedItem.objectIsOptimized;
                }

                return false;
            }
        }

        public bool disabled
        {
            get { return selection.disabled; }
        }

        public IAnimationWindowControl controlInterface
        {
            get
            {
                if (m_OverrideControlInterface != null)
                    return m_OverrideControlInterface;

                return m_ControlInterface;
            }
        }

        public IAnimationWindowControl overrideControlInterface
        {
            get
            {
                return m_OverrideControlInterface;
            }

            set
            {
                if (m_OverrideControlInterface != null)
                    Object.DestroyImmediate(m_OverrideControlInterface);

                m_OverrideControlInterface = value;
            }
        }

        public void OnGUI()
        {
            RefreshHashCheck();
            Refresh();
        }

        private void RefreshHashCheck()
        {
            int newRefreshHash = GetRefreshHash();
            if (m_PreviousRefreshHash != newRefreshHash)
            {
                refresh = RefreshType.Everything;
                m_PreviousRefreshHash = newRefreshHash;
            }
        }

        private void Refresh()
        {
            selection.Synchronize();

            if (refresh == RefreshType.Everything)
            {
                selection.Refresh();

                m_ActiveKeyframeCache = null;
                m_ActiveCurvesCache = null;
                m_dopelinesCache = null;
                m_SelectedKeysCache = null;
                m_SelectionBoundsCache = null;

                ClearCurveWrapperCache();

                if (hierarchyData != null)
                    hierarchyData.UpdateData();

                // If there was new curve added, set it as active selection
                if (m_lastAddedCurveBinding != null)
                    OnNewCurveAdded((EditorCurveBinding)m_lastAddedCurveBinding);

                // select top dopeline if there is no selection available
                if (activeCurves.Count == 0 && dopelines.Count > 0)
                    SelectHierarchyItem(dopelines[0], false, false);

                m_Refresh = RefreshType.None;
            }
            else if (refresh == RefreshType.CurvesOnly)
            {
                m_ActiveKeyframeCache = null;
                m_SelectedKeysCache = null;
                m_SelectionBoundsCache = null;

                ReloadModifiedAnimationCurveCache();
                ReloadModifiedDopelineCache();
                ReloadModifiedCurveWrapperCache();

                m_Refresh = RefreshType.None;
                m_ModifiedCurves.Clear();
            }
        }

        // Hash for checking if any of these things is changed
        private int GetRefreshHash()
        {
            return
                selection.GetRefreshHash() ^
                (hierarchyState != null ? hierarchyState.expandedIDs.Count : 0) ^
                (hierarchyState != null ? hierarchyState.GetTallInstancesCount() : 0) ^
                (showCurveEditor ? 1 : 0);
        }

        public void ForceRefresh()
        {
            refresh = RefreshType.Everything;
        }

        public void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
            AnimationUtility.onCurveWasModified += CurveWasModified;
            Undo.undoRedoPerformed += UndoRedoPerformed;

            // NoOps...
            onStartLiveEdit += () => {};
            onEndLiveEdit += () => {};
            onFrameRateChange += (float frameRate) => {};

            if (m_ControlInterface == null)
                m_ControlInterface = CreateInstance(typeof(AnimationWindowControl)) as AnimationWindowControl;

            m_ControlInterface.state = this;
        }

        public void OnDisable()
        {
            AnimationUtility.onCurveWasModified -= CurveWasModified;
            Undo.undoRedoPerformed -= UndoRedoPerformed;

            m_ControlInterface.OnDisable();
        }

        public void OnDestroy()
        {
            if (m_Selection != null)
            {
                m_Selection.Clear();
            }

            Object.DestroyImmediate(m_KeySelection);
            Object.DestroyImmediate(m_ControlInterface);
            Object.DestroyImmediate(m_OverrideControlInterface);
        }

        public void OnSelectionChanged()
        {
            onFrameRateChange(frameRate);

            // reset back time at 0 upon selection change.
            controlInterface.OnSelectionChanged();
        }

        // Set this property to ask for refresh at the next OnGUI.
        public RefreshType refresh
        {
            get { return m_Refresh; }
            // Make sure that if full refresh is already ordered, nobody gets to f*** with it
            set
            {
                if ((int)m_Refresh < (int)value)
                    m_Refresh = value;
            }
        }

        public void UndoRedoPerformed()
        {
            refresh = RefreshType.Everything;
            controlInterface.ResampleAnimation();
        }

        // When curve is modified, we never trigger refresh right away. We order a refresh at later time by setting refresh to appropriate value.
        void CurveWasModified(AnimationClip clip, EditorCurveBinding binding, AnimationUtility.CurveModifiedType type)
        {
            // AnimationWindow doesn't care if some other clip somewhere changed
            AnimationWindowSelectionItem[] selectedItems = Array.FindAll(selection.ToArray(), item => item.animationClip == clip);
            if (selectedItems.Length == 0)
                return;

            // Refresh curves that already exist.
            if (type == AnimationUtility.CurveModifiedType.CurveModified)
            {
                bool didFind = false;
                bool hadPhantom = false;
                int hashCode = binding.GetHashCode();
                for (int i = 0; i < selectedItems.Length; ++i)
                {
                    List<AnimationWindowCurve> curves = selectedItems[i].curves;
                    for (int j = 0; j < curves.Count; ++j)
                    {
                        AnimationWindowCurve curve = curves[j];
                        int curveHash = curve.GetBindingHashCode();
                        if (curveHash == hashCode)
                        {
                            m_ModifiedCurves.Add(curve.GetHashCode());
                            didFind = true;
                            hadPhantom |= curve.binding.isPhantom;
                        }
                    }
                }

                if (didFind && !hadPhantom)
                    refresh = RefreshType.CurvesOnly;
                else
                {
                    // New curve was added, so let's save binding and make it active selection when Refresh is called next time
                    m_lastAddedCurveBinding = binding;
                    refresh = RefreshType.Everything;
                }
            }
            else
            {
                // Otherwise do a full reload
                refresh = RefreshType.Everything;
            }
        }

        public void SaveKeySelection(string undoLabel)
        {
            if (m_KeySelection != null)
                Undo.RegisterCompleteObjectUndo(m_KeySelection, undoLabel);
        }

        public void SaveCurve(AnimationWindowCurve curve)
        {
            SaveCurve(curve, kEditCurveUndoLabel);
        }

        public void SaveCurve(AnimationWindowCurve curve, string undoLabel)
        {
            if (!curve.animationIsEditable)
                Debug.LogError("Curve is not editable and shouldn't be saved.");

            Undo.RegisterCompleteObjectUndo(curve.clip, undoLabel);
            AnimationRecording.SaveModifiedCurve(curve, curve.clip);
            Repaint();
        }

        private void SaveSelectedKeys(string undoLabel)
        {
            List<AnimationWindowCurve> saveCurves = new List<AnimationWindowCurve>();

            // Find all curves that need saving
            foreach (LiveEditCurve snapshot in m_LiveEditSnapshot)
            {
                if (!snapshot.curve.animationIsEditable)
                    continue;

                if (!saveCurves.Contains(snapshot.curve))
                    saveCurves.Add(snapshot.curve);

                List<AnimationWindowKeyframe> toBeDeleted = new List<AnimationWindowKeyframe>();

                // If selected keys are dragged over non-selected keyframe at exact same time, then delete the unselected ones underneath
                foreach (AnimationWindowKeyframe other in snapshot.curve.m_Keyframes)
                {
                    // Keyframe is in selection, skip.
                    if (snapshot.selectedKeys.Exists(liveEditKey => liveEditKey.key == other))
                        continue;

                    // There is already a selected keyframe at that time, delete non-selected keyframe.
                    if (!snapshot.selectedKeys.Exists(liveEditKey => AnimationKeyTime.Time(liveEditKey.key.time, frameRate).frame == AnimationKeyTime.Time(other.time, frameRate).frame))
                        continue;

                    toBeDeleted.Add(other);
                }

                foreach (AnimationWindowKeyframe deletedKey in toBeDeleted)
                {
                    snapshot.curve.m_Keyframes.Remove(deletedKey);
                }
            }

            foreach (AnimationWindowCurve curve in saveCurves)
                SaveCurve(curve, undoLabel);
        }

        public void RemoveCurve(AnimationWindowCurve curve, string undoLabel)
        {
            if (!curve.animationIsEditable)
                return;

            Undo.RegisterCompleteObjectUndo(curve.clip, undoLabel);

            if (curve.isPPtrCurve)
                AnimationUtility.SetObjectReferenceCurve(curve.clip, curve.binding, null);
            else
                AnimationUtility.SetEditorCurve(curve.clip, curve.binding, null);
        }

        public bool previewing { get { return controlInterface.previewing; } }

        public bool canPreview { get { return controlInterface.canPreview; } }

        public void StartPreview()
        {
            controlInterface.StartPreview();
            controlInterface.ResampleAnimation();
        }

        public void StopPreview()
        {
            controlInterface.StopPreview();
        }

        public bool recording { get { return controlInterface.recording; } }

        public bool canRecord { get { return controlInterface.canRecord; } }

        public void StartRecording()
        {
            if (selectedItem != null)
            {
                controlInterface.StartRecording(selectedItem.sourceObject);
                controlInterface.ResampleAnimation();
            }
        }

        public void StopRecording()
        {
            controlInterface.StopRecording();
        }

        public bool playing { get { return controlInterface.playing; } }

        public void StartPlayback()
        {
            controlInterface.StartPlayback();
        }

        public void StopPlayback()
        {
            controlInterface.StopPlayback();
        }

        public void ResampleAnimation()
        {
            controlInterface.ResampleAnimation();
        }

        public List<AnimationWindowCurve> allCurves
        {
            get { return selection.curves; }
        }

        public List<AnimationWindowCurve> activeCurves
        {
            get
            {
                if (m_ActiveCurvesCache == null)
                {
                    m_ActiveCurvesCache = new List<AnimationWindowCurve>();
                    if (hierarchyState != null && hierarchyData != null)
                    {
                        foreach (int id in hierarchyState.selectedIDs)
                        {
                            TreeViewItem node = hierarchyData.FindItem(id);
                            AnimationWindowHierarchyNode hierarchyNode = node as AnimationWindowHierarchyNode;

                            if (hierarchyNode == null)
                                continue;

                            AnimationWindowCurve[] curves = hierarchyNode.curves;
                            if (curves == null)
                                continue;

                            foreach (AnimationWindowCurve curve in curves)
                                if (!m_ActiveCurvesCache.Contains(curve))
                                    m_ActiveCurvesCache.Add(curve);
                        }

                        m_ActiveCurvesCache.Sort();
                    }
                }

                return m_ActiveCurvesCache;
            }
        }

        public CurveWrapper[] activeCurveWrappers
        {
            get
            {
                if (m_ActiveCurveWrappersCache == null || m_ActiveCurvesCache == null)
                {
                    List<CurveWrapper> activeCurveWrappers = new List<CurveWrapper>();
                    foreach (AnimationWindowCurve curve in activeCurves)
                        if (!curve.isDiscreteCurve)
                            activeCurveWrappers.Add(AnimationWindowUtility.GetCurveWrapper(curve, curve.clip));

                    // If there are no active curves, we would end up with empty curve editor so we just give all curves insteads
                    if (!activeCurveWrappers.Any())
                        foreach (AnimationWindowCurve curve in allCurves)
                            if (!curve.isDiscreteCurve)
                                activeCurveWrappers.Add(AnimationWindowUtility.GetCurveWrapper(curve, curve.clip));

                    m_ActiveCurveWrappersCache = activeCurveWrappers.ToArray();
                }

                return m_ActiveCurveWrappersCache;
            }
        }

        public List<DopeLine> dopelines
        {
            get
            {
                if (m_dopelinesCache == null)
                {
                    m_dopelinesCache = new List<DopeLine>();

                    if (hierarchyData != null)
                    {
                        foreach (TreeViewItem node in hierarchyData.GetRows())
                        {
                            AnimationWindowHierarchyNode hierarchyNode = node as AnimationWindowHierarchyNode;

                            if (hierarchyNode == null || hierarchyNode is AnimationWindowHierarchyAddButtonNode)
                                continue;

                            AnimationWindowCurve[] curves = hierarchyNode.curves;
                            if (curves == null)
                                continue;

                            DopeLine dopeLine = new DopeLine(node.id, curves);
                            dopeLine.tallMode = hierarchyState.GetTallMode(hierarchyNode);
                            dopeLine.objectType = hierarchyNode.animatableObjectType;
                            dopeLine.hasChildren = !(hierarchyNode is AnimationWindowHierarchyPropertyNode);
                            dopeLine.isMasterDopeline = node is AnimationWindowHierarchyMasterNode;
                            m_dopelinesCache.Add(dopeLine);
                        }
                    }
                }
                return m_dopelinesCache;
            }
        }

        public List<AnimationWindowHierarchyNode> selectedHierarchyNodes
        {
            get
            {
                List<AnimationWindowHierarchyNode> selectedHierarchyNodes = new List<AnimationWindowHierarchyNode>();

                if (activeAnimationClip != null && hierarchyData != null)
                {
                    foreach (int id in hierarchyState.selectedIDs)
                    {
                        AnimationWindowHierarchyNode hierarchyNode = (AnimationWindowHierarchyNode)hierarchyData.FindItem(id);

                        if (hierarchyNode == null || hierarchyNode is AnimationWindowHierarchyAddButtonNode)
                            continue;

                        selectedHierarchyNodes.Add(hierarchyNode);
                    }
                }

                return selectedHierarchyNodes;
            }
        }

        public AnimationWindowKeyframe activeKeyframe
        {
            get
            {
                if (m_ActiveKeyframeCache == null)
                {
                    foreach (AnimationWindowCurve curve in allCurves)
                    {
                        foreach (AnimationWindowKeyframe keyframe in curve.m_Keyframes)
                        {
                            if (keyframe.GetHash() == m_ActiveKeyframeHash)
                                m_ActiveKeyframeCache = keyframe;
                        }
                    }
                }
                return m_ActiveKeyframeCache;
            }
            set
            {
                m_ActiveKeyframeCache = null;
                m_ActiveKeyframeHash = value != null ? value.GetHash() : 0;
            }
        }

        public List<AnimationWindowKeyframe> selectedKeys
        {
            get
            {
                if (m_SelectedKeysCache == null)
                {
                    m_SelectedKeysCache = new List<AnimationWindowKeyframe>();
                    foreach (AnimationWindowCurve curve in allCurves)
                    {
                        foreach (AnimationWindowKeyframe keyframe in curve.m_Keyframes)
                        {
                            if (KeyIsSelected(keyframe))
                            {
                                m_SelectedKeysCache.Add(keyframe);
                            }
                        }
                    }
                }
                return m_SelectedKeysCache;
            }
        }

        public Bounds selectionBounds
        {
            get
            {
                if (m_SelectionBoundsCache == null)
                {
                    List<AnimationWindowKeyframe> keys = selectedKeys;
                    if (keys.Count > 0)
                    {
                        AnimationWindowKeyframe key = keys[0];
                        float time = key.time + key.curve.timeOffset;
                        float val = key.isPPtrCurve ? 0.0f : (float)key.value;

                        Bounds bounds = new Bounds(new Vector2(time, val), Vector2.zero);

                        for (int i = 1; i < keys.Count; ++i)
                        {
                            key = keys[i];

                            time = key.time + key.curve.timeOffset;
                            val = key.isPPtrCurve ? 0.0f : (float)key.value;

                            bounds.Encapsulate(new Vector2(time, val));
                        }

                        m_SelectionBoundsCache = bounds;
                    }
                    else
                    {
                        m_SelectionBoundsCache = new Bounds(Vector2.zero, Vector2.zero);
                    }
                }

                return m_SelectionBoundsCache.Value;
            }
        }

        private HashSet<int> selectedKeyHashes
        {
            get
            {
                if (m_KeySelection == null)
                {
                    m_KeySelection = CreateInstance<AnimationWindowKeySelection>();
                    m_KeySelection.hideFlags = HideFlags.HideAndDontSave;
                }

                return m_KeySelection.selectedKeyHashes;
            }
            set
            {
                if (m_KeySelection == null)
                {
                    m_KeySelection = CreateInstance<AnimationWindowKeySelection>();
                    m_KeySelection.hideFlags = HideFlags.HideAndDontSave;
                }

                m_KeySelection.selectedKeyHashes = value;
            }
        }

        public bool AnyKeyIsSelected(DopeLine dopeline)
        {
            foreach (AnimationWindowKeyframe keyframe in dopeline.keys)
                if (KeyIsSelected(keyframe))
                    return true;

            return false;
        }

        public bool KeyIsSelected(AnimationWindowKeyframe keyframe)
        {
            return selectedKeyHashes.Contains(keyframe.GetHash());
        }

        public void SelectKey(AnimationWindowKeyframe keyframe)
        {
            int hash = keyframe.GetHash();
            if (!selectedKeyHashes.Contains(hash))
                selectedKeyHashes.Add(hash);

            m_SelectedKeysCache = null;
            m_SelectionBoundsCache = null;
        }

        public void SelectKeysFromDopeline(DopeLine dopeline)
        {
            if (dopeline == null)
                return;

            foreach (var key in dopeline.keys)
                SelectKey(key);
        }

        public void UnselectKey(AnimationWindowKeyframe keyframe)
        {
            int hash = keyframe.GetHash();
            if (selectedKeyHashes.Contains(hash))
                selectedKeyHashes.Remove(hash);

            m_SelectedKeysCache = null;
            m_SelectionBoundsCache = null;
        }

        public void UnselectKeysFromDopeline(DopeLine dopeline)
        {
            if (dopeline == null)
                return;

            foreach (var key in dopeline.keys)
                UnselectKey(key);
        }

        public void DeleteSelectedKeys()
        {
            if (selectedKeys.Count == 0)
                return;

            DeleteKeys(selectedKeys);
        }

        public void DeleteKeys(List<AnimationWindowKeyframe> keys)
        {
            SaveKeySelection(kEditCurveUndoLabel);

            foreach (AnimationWindowKeyframe keyframe in keys)
            {
                if (!keyframe.curve.animationIsEditable)
                    continue;

                UnselectKey(keyframe);
                keyframe.curve.m_Keyframes.Remove(keyframe);

                // TODO: optimize by not saving curve for each keyframe
                SaveCurve(keyframe.curve, kEditCurveUndoLabel);
            }

            ResampleAnimation();
        }

        public void StartLiveEdit()
        {
            if (onStartLiveEdit != null)
                onStartLiveEdit();

            m_LiveEditSnapshot = new List<LiveEditCurve>();

            SaveKeySelection(kEditCurveUndoLabel);

            foreach (AnimationWindowKeyframe selectedKey in selectedKeys)
            {
                if (!m_LiveEditSnapshot.Exists(snapshot => snapshot.curve == selectedKey.curve))
                {
                    LiveEditCurve snapshot = new LiveEditCurve();
                    snapshot.curve = selectedKey.curve;
                    foreach (AnimationWindowKeyframe key in selectedKey.curve.m_Keyframes)
                    {
                        LiveEditKeyframe liveEditKey = new LiveEditKeyframe();
                        liveEditKey.keySnapshot = new AnimationWindowKeyframe(key);
                        liveEditKey.key = key;

                        if (KeyIsSelected(key))
                            snapshot.selectedKeys.Add(liveEditKey);
                        else
                            snapshot.unselectedKeys.Add(liveEditKey);
                    }

                    m_LiveEditSnapshot.Add(snapshot);
                }
            }
        }

        public void EndLiveEdit()
        {
            SaveSelectedKeys(kEditCurveUndoLabel);

            m_LiveEditSnapshot = null;

            if (onEndLiveEdit != null)
                onEndLiveEdit();
        }

        public bool InLiveEdit()
        {
            return m_LiveEditSnapshot != null;
        }

        public void MoveSelectedKeys(float deltaTime, bool snapToFrame)
        {
            bool inLiveEdit = InLiveEdit();
            if (!inLiveEdit)
                StartLiveEdit();

            // Clear selections since all hashes are now different
            ClearKeySelections();

            foreach (LiveEditCurve snapshot in m_LiveEditSnapshot)
            {
                foreach (LiveEditKeyframe liveEditKey in snapshot.selectedKeys)
                {
                    if (snapshot.curve.animationIsEditable)
                    {
                        liveEditKey.key.time = Mathf.Max(liveEditKey.keySnapshot.time + deltaTime, 0f);

                        if (snapToFrame)
                            liveEditKey.key.time = SnapToFrame(liveEditKey.key.time, snapshot.curve.clip.frameRate);
                    }

                    SelectKey(liveEditKey.key);
                }
            }

            if (!inLiveEdit)
                EndLiveEdit();
        }

        public void TransformSelectedKeys(Matrix4x4 matrix, bool flipX, bool flipY, bool snapToFrame)
        {
            bool inLiveEdit = InLiveEdit();
            if (!inLiveEdit)
                StartLiveEdit();

            // Clear selections since all hashes are now different
            ClearKeySelections();

            foreach (LiveEditCurve snapshot in m_LiveEditSnapshot)
            {
                foreach (LiveEditKeyframe liveEditKey in snapshot.selectedKeys)
                {
                    if (snapshot.curve.animationIsEditable)
                    {
                        // Transform time value.
                        Vector3 v = new Vector3(liveEditKey.keySnapshot.time, liveEditKey.keySnapshot.isPPtrCurve ? 0f : (float)liveEditKey.keySnapshot.value, 0f);
                        v = matrix.MultiplyPoint3x4(v);

                        liveEditKey.key.time = Mathf.Max((snapToFrame) ? SnapToFrame(v.x, snapshot.curve.clip.frameRate) : v.x, 0f);

                        if (flipX)
                        {
                            liveEditKey.key.inTangent = (liveEditKey.keySnapshot.outTangent != Mathf.Infinity) ? -liveEditKey.keySnapshot.outTangent : Mathf.Infinity;
                            liveEditKey.key.outTangent = (liveEditKey.keySnapshot.inTangent != Mathf.Infinity) ? -liveEditKey.keySnapshot.inTangent : Mathf.Infinity;
                        }

                        if (!liveEditKey.key.isPPtrCurve)
                        {
                            liveEditKey.key.value = v.y;

                            if (flipY)
                            {
                                liveEditKey.key.inTangent = (liveEditKey.key.inTangent != Mathf.Infinity) ? -liveEditKey.key.inTangent : Mathf.Infinity;
                                liveEditKey.key.outTangent = (liveEditKey.key.outTangent != Mathf.Infinity) ? -liveEditKey.key.outTangent : Mathf.Infinity;
                            }
                        }
                    }

                    SelectKey(liveEditKey.key);
                }
            }

            if (!inLiveEdit)
                EndLiveEdit();
        }

        public void TransformRippleKeys(Matrix4x4 matrix, float t1, float t2, bool flipX, bool snapToFrame)
        {
            bool inLiveEdit = InLiveEdit();
            if (!inLiveEdit)
                StartLiveEdit();

            // Clear selections since all hashes are now different
            ClearKeySelections();

            foreach (LiveEditCurve snapshot in m_LiveEditSnapshot)
            {
                foreach (LiveEditKeyframe liveEditKey in snapshot.selectedKeys)
                {
                    if (snapshot.curve.animationIsEditable)
                    {
                        Vector3 v = new Vector3(liveEditKey.keySnapshot.time, 0f, 0f);
                        v = matrix.MultiplyPoint3x4(v);

                        liveEditKey.key.time = Mathf.Max((snapToFrame) ? SnapToFrame(v.x, snapshot.curve.clip.frameRate) : v.x, 0f);

                        if (flipX)
                        {
                            liveEditKey.key.inTangent = (liveEditKey.keySnapshot.outTangent != Mathf.Infinity) ? -liveEditKey.keySnapshot.outTangent : Mathf.Infinity;
                            liveEditKey.key.outTangent = (liveEditKey.keySnapshot.inTangent != Mathf.Infinity) ? -liveEditKey.keySnapshot.inTangent : Mathf.Infinity;
                        }
                    }

                    SelectKey(liveEditKey.key);
                }

                if (!snapshot.curve.animationIsEditable)
                    continue;

                foreach (LiveEditKeyframe liveEditKey in snapshot.unselectedKeys)
                {
                    if (liveEditKey.keySnapshot.time > t2)
                    {
                        Vector3 v = new Vector3(flipX ? t1 : t2, 0f, 0f);
                        v = matrix.MultiplyPoint3x4(v);

                        float dt = v.x - t2;
                        if (dt > 0f)
                        {
                            float newTime = liveEditKey.keySnapshot.time + dt;
                            liveEditKey.key.time = Mathf.Max((snapToFrame) ? SnapToFrame(newTime, snapshot.curve.clip.frameRate) : newTime, 0f);
                        }
                        else
                        {
                            liveEditKey.key.time = liveEditKey.keySnapshot.time;
                        }
                    }
                    else if (liveEditKey.keySnapshot.time < t1)
                    {
                        Vector3 v = new Vector3(flipX ? t2 : t1, 0f, 0f);
                        v = matrix.MultiplyPoint3x4(v);

                        float dt = v.x - t1;
                        if (dt < 0f)
                        {
                            float newTime = liveEditKey.keySnapshot.time + dt;
                            liveEditKey.key.time = Mathf.Max((snapToFrame) ? SnapToFrame(newTime, snapshot.curve.clip.frameRate) : newTime, 0f);
                        }
                        else
                        {
                            liveEditKey.key.time = liveEditKey.keySnapshot.time;
                        }
                    }
                }
            }

            if (!inLiveEdit)
                EndLiveEdit();
        }

        public void CopyKeys()
        {
            if (s_KeyframeClipboard == null)
                s_KeyframeClipboard = new List<AnimationWindowKeyframe>();

            float smallestTime = float.MaxValue;
            s_KeyframeClipboard.Clear();
            foreach (AnimationWindowKeyframe keyframe in selectedKeys)
            {
                s_KeyframeClipboard.Add(new AnimationWindowKeyframe(keyframe));
                float candidate = keyframe.time + keyframe.curve.timeOffset;
                if (candidate < smallestTime)
                    smallestTime = candidate;
            }
            if (s_KeyframeClipboard.Count > 0) // copying selected keys
            {
                foreach (AnimationWindowKeyframe keyframe in s_KeyframeClipboard)
                {
                    keyframe.time -= smallestTime - keyframe.curve.timeOffset;
                }
            }
            else // No selected keys, lets copy entire curves
            {
                CopyAllActiveCurves();
            }
        }

        public void CopyAllActiveCurves()
        {
            foreach (AnimationWindowCurve curve in activeCurves)
            {
                foreach (AnimationWindowKeyframe keyframe in curve.m_Keyframes)
                {
                    s_KeyframeClipboard.Add(new AnimationWindowKeyframe(keyframe));
                }
            }
        }

        public void PasteKeys()
        {
            if (s_KeyframeClipboard == null)
                s_KeyframeClipboard = new List<AnimationWindowKeyframe>();

            SaveKeySelection(kEditCurveUndoLabel);

            HashSet<int> oldSelection = new HashSet<int>(selectedKeyHashes);
            ClearKeySelections();

            AnimationWindowCurve lastTargetCurve = null;
            AnimationWindowCurve lastSourceCurve = null;
            float lastTime = 0f;

            List<AnimationWindowCurve> clipboardCurves = new List<AnimationWindowCurve>();
            foreach (AnimationWindowKeyframe keyframe in s_KeyframeClipboard)
                if (!clipboardCurves.Any() || clipboardCurves.Last() != keyframe.curve)
                    clipboardCurves.Add(keyframe.curve);

            // If we have equal number of target and source curves, then match by index. If not, then try to match with AnimationWindowUtility.BestMatchForPaste.
            bool matchCurveByIndex = clipboardCurves.Count() == activeCurves.Count();
            int targetIndex = 0;

            foreach (AnimationWindowKeyframe keyframe in s_KeyframeClipboard)
            {
                if (lastSourceCurve != null && keyframe.curve != lastSourceCurve)
                    targetIndex++;

                AnimationWindowKeyframe newKeyframe = new AnimationWindowKeyframe(keyframe);

                if (matchCurveByIndex)
                    newKeyframe.curve = activeCurves[targetIndex];
                else
                    newKeyframe.curve = AnimationWindowUtility.BestMatchForPaste(newKeyframe.curve.binding, clipboardCurves, activeCurves);

                if (newKeyframe.curve == null) // Paste as new curve
                {
                    // Curves are selected in the animation window hierarchy.  Since we couldn't find a proper match,
                    // create a new curve in first selected clip in active curves.
                    if (activeCurves.Count > 0)
                    {
                        AnimationWindowCurve firstCurve = activeCurves[0];
                        if (firstCurve.animationIsEditable)
                        {
                            newKeyframe.curve = new AnimationWindowCurve(firstCurve.clip, keyframe.curve.binding, keyframe.curve.valueType);
                            newKeyframe.curve.selectionBinding = firstCurve.selectionBinding;
                            newKeyframe.time = keyframe.time;
                        }
                    }
                    // If nothing is selected, create a new curve in first selected clip.
                    else
                    {
                        AnimationWindowSelectionItem targetSelection = selection.First();
                        if (targetSelection.animationIsEditable)
                        {
                            newKeyframe.curve = new AnimationWindowCurve(targetSelection.animationClip, keyframe.curve.binding, keyframe.curve.valueType);
                            newKeyframe.curve.selectionBinding = targetSelection;
                            newKeyframe.time = keyframe.time;
                        }
                    }
                }

                if (newKeyframe.curve == null || !newKeyframe.curve.animationIsEditable)
                    continue;

                newKeyframe.time += time.time - newKeyframe.curve.timeOffset;

                //  Only allow pasting of key frame from numerical curves to numerical curves or from pptr curves to pptr curves.
                if ((newKeyframe.time >= 0.0f) && (newKeyframe.curve != null) && (newKeyframe.curve.isPPtrCurve == keyframe.curve.isPPtrCurve))
                {
                    if (newKeyframe.curve.HasKeyframe(AnimationKeyTime.Time(newKeyframe.time, newKeyframe.curve.clip.frameRate)))
                        newKeyframe.curve.RemoveKeyframe(AnimationKeyTime.Time(newKeyframe.time, newKeyframe.curve.clip.frameRate));

                    // When copy-pasting multiple keyframes (curve), its a continous thing. This is why we delete the existing keyframes in the pasted range.
                    if (lastTargetCurve == newKeyframe.curve)
                        newKeyframe.curve.RemoveKeysAtRange(lastTime, newKeyframe.time);

                    newKeyframe.curve.m_Keyframes.Add(newKeyframe);
                    SelectKey(newKeyframe);
                    // TODO: Optimize to only save curve once instead once per keyframe
                    SaveCurve(newKeyframe.curve, kEditCurveUndoLabel);

                    lastTargetCurve = newKeyframe.curve;
                    lastTime = newKeyframe.time;
                }

                lastSourceCurve = keyframe.curve;
            }

            // If nothing is pasted, then we revert to old selection
            if (selectedKeyHashes.Count == 0)
                selectedKeyHashes = oldSelection;
            else
                ResampleAnimation();
        }

        public void ClearSelections()
        {
            ClearKeySelections();
            ClearHierarchySelection();
        }

        public void ClearKeySelections()
        {
            selectedKeyHashes.Clear();
            m_SelectedKeysCache = null;
            m_SelectionBoundsCache = null;
        }

        public void ClearHierarchySelection()
        {
            hierarchyState.selectedIDs.Clear();
            m_ActiveCurvesCache = null;
        }

        private void ClearCurveWrapperCache()
        {
            if (m_ActiveCurveWrappersCache == null)
                return;

            for (int i = 0; i < m_ActiveCurveWrappersCache.Length; ++i)
            {
                CurveWrapper curveWrapper = m_ActiveCurveWrappersCache[i];
                if (curveWrapper.renderer != null)
                    curveWrapper.renderer.FlushCache();
            }

            m_ActiveCurveWrappersCache = null;
        }

        private void ReloadModifiedDopelineCache()
        {
            if (m_dopelinesCache == null)
                return;

            for (int i = 0; i < m_dopelinesCache.Count; ++i)
            {
                DopeLine dopeLine = m_dopelinesCache[i];
                AnimationWindowCurve[] curves = dopeLine.curves;
                for (int j = 0; j < curves.Length; ++j)
                {
                    if (m_ModifiedCurves.Contains(curves[j].GetHashCode()))
                    {
                        dopeLine.InvalidateKeyframes();
                        break;
                    }
                }
            }
        }

        private void ReloadModifiedCurveWrapperCache()
        {
            if (m_ActiveCurveWrappersCache == null)
                return;

            Dictionary<int, AnimationWindowCurve> updateList = new Dictionary<int, AnimationWindowCurve>();

            for (int i = 0; i < m_ActiveCurveWrappersCache.Length; ++i)
            {
                CurveWrapper curveWrapper = m_ActiveCurveWrappersCache[i];

                if (m_ModifiedCurves.Contains(curveWrapper.id))
                {
                    AnimationWindowCurve curve = allCurves.Find(c => c.GetHashCode() == curveWrapper.id);
                    if (curve != null)
                    {
                        //  Boundaries have changed, invalidate all curves
                        if (curve.clip.startTime != curveWrapper.renderer.RangeStart() ||
                            curve.clip.stopTime != curveWrapper.renderer.RangeEnd())
                        {
                            ClearCurveWrapperCache();
                            return;
                        }
                        else
                        {
                            updateList[i] = curve;
                        }
                    }
                }
            }

            //  Only update curve wrappers that were modified.
            for (int i = 0; i < updateList.Count; ++i)
            {
                var entry = updateList.ElementAt(i);

                CurveWrapper curveWrapper = m_ActiveCurveWrappersCache[entry.Key];
                if (curveWrapper.renderer != null)
                    curveWrapper.renderer.FlushCache();

                // Recreate curve wrapper only if curve has been modified.
                m_ActiveCurveWrappersCache[entry.Key] = AnimationWindowUtility.GetCurveWrapper(entry.Value, entry.Value.clip);
            }
        }

        private void ReloadModifiedAnimationCurveCache()
        {
            for (int i = 0; i < allCurves.Count; ++i)
            {
                AnimationWindowCurve curve = allCurves[i];
                if (m_ModifiedCurves.Contains(curve.GetHashCode()))
                    curve.LoadKeyframes(curve.clip);
            }
        }

        // This is called when there is a new curve, but after the data refresh.
        // This means that hierarchynodes and dopeline(s) for new curve are already available.
        private void OnNewCurveAdded(EditorCurveBinding newCurve)
        {
            //  Retrieve group property name.
            //  For example if we got "position.z" as our newCurve,
            //  the property will be "position" with three child child nodes x,y,z
            string propertyName = newCurve.propertyName;
            string groupPropertyName = AnimationWindowUtility.GetPropertyGroupName(newCurve.propertyName);

            if (hierarchyData == null)
                return;

            if (HasHierarchySelection())
            {
                // Update hierarchy selection with newly created curve.
                foreach (AnimationWindowHierarchyNode node in hierarchyData.GetRows())
                {
                    if (node.path != newCurve.path ||
                        node.animatableObjectType != newCurve.type ||
                        (node.propertyName != propertyName && node.propertyName != groupPropertyName))
                        continue;

                    SelectHierarchyItem(node.id, true, false);

                    // We want the pptr curves to be in tall mode by default
                    if (newCurve.isPPtrCurve)
                        hierarchyState.AddTallInstance(node.id);
                }
            }

            //  Values do not change whenever a new curve is added, so we force an inspector update here.
            controlInterface.ResampleAnimation();

            m_lastAddedCurveBinding = null;
        }

        public void Repaint()
        {
            if (animEditor != null)
                animEditor.Repaint();
        }

        public List<AnimationWindowKeyframe> GetAggregateKeys(AnimationWindowHierarchyNode hierarchyNode)
        {
            DopeLine dopeline = dopelines.FirstOrDefault(e => e.hierarchyNodeID == hierarchyNode.id);
            if (dopeline == null)
                return null;
            return dopeline.keys;
        }

        public void OnHierarchySelectionChanged(int[] selectedInstanceIDs)
        {
            HandleHierarchySelectionChanged(selectedInstanceIDs, true);
        }

        public void HandleHierarchySelectionChanged(int[] selectedInstanceIDs, bool triggerSceneSelectionSync)
        {
            m_ActiveCurvesCache = null;

            if (triggerSceneSelectionSync)
                SyncSceneSelection(selectedInstanceIDs);
        }

        public void SelectHierarchyItem(DopeLine dopeline, bool additive)
        {
            SelectHierarchyItem(dopeline.hierarchyNodeID, additive, true);
        }

        public void SelectHierarchyItem(DopeLine dopeline, bool additive, bool triggerSceneSelectionSync)
        {
            SelectHierarchyItem(dopeline.hierarchyNodeID, additive, triggerSceneSelectionSync);
        }

        public void SelectHierarchyItem(int hierarchyNodeID, bool additive, bool triggerSceneSelectionSync)
        {
            if (!additive)
                ClearHierarchySelection();

            hierarchyState.selectedIDs.Add(hierarchyNodeID);

            int[] selectedInstanceIDs = hierarchyState.selectedIDs.ToArray();

            // We need to manually trigger this event, because injecting data to m_SelectedInstanceIDs directly doesn't trigger one via TreeView
            HandleHierarchySelectionChanged(selectedInstanceIDs, triggerSceneSelectionSync);
        }

        public void UnSelectHierarchyItem(DopeLine dopeline)
        {
            UnSelectHierarchyItem(dopeline.hierarchyNodeID);
        }

        public void UnSelectHierarchyItem(int hierarchyNodeID)
        {
            hierarchyState.selectedIDs.Remove(hierarchyNodeID);
        }

        public bool HasHierarchySelection()
        {
            if (hierarchyState.selectedIDs.Count == 0)
                return false;

            if (hierarchyState.selectedIDs.Count == 1)
                return (hierarchyState.selectedIDs[0] != 0);

            return true;
        }

        public List<int> GetAffectedHierarchyIDs(List<AnimationWindowKeyframe> keyframes)
        {
            List<int> hierarchyIDs = new List<int>();

            foreach (DopeLine dopeline in GetAffectedDopelines(keyframes))
                if (!hierarchyIDs.Contains(dopeline.hierarchyNodeID))
                    hierarchyIDs.Add(dopeline.hierarchyNodeID);

            return hierarchyIDs;
        }

        public List<DopeLine> GetAffectedDopelines(List<AnimationWindowKeyframe> keyframes)
        {
            List<DopeLine> affectedDopelines = new List<DopeLine>();

            foreach (AnimationWindowCurve curve in GetAffectedCurves(keyframes))
                foreach (DopeLine dopeline in dopelines)
                    if (!affectedDopelines.Contains(dopeline) && dopeline.curves.Contains(curve))
                        affectedDopelines.Add(dopeline);

            return affectedDopelines;
        }

        public List<AnimationWindowCurve> GetAffectedCurves(List<AnimationWindowKeyframe> keyframes)
        {
            List<AnimationWindowCurve> affectedCurves = new List<AnimationWindowCurve>();

            foreach (AnimationWindowKeyframe keyframe in keyframes)
                if (!affectedCurves.Contains(keyframe.curve))
                    affectedCurves.Add(keyframe.curve);

            return affectedCurves;
        }

        public DopeLine GetDopeline(int selectedInstanceID)
        {
            foreach (var dopeline in dopelines)
            {
                if (dopeline.hierarchyNodeID == selectedInstanceID)
                    return dopeline;
            }

            return null;
        }

        // Set scene active go to be the same as the one selected from hierarchy
        private void SyncSceneSelection(int[] selectedNodeIDs)
        {
            if (selectedItem == null || !selectedItem.canSyncSceneSelection)
                return;

            GameObject rootGameObject = selectedItem.rootGameObject;
            if (rootGameObject == null)
                return;

            List<int> selectedGameObjectIDs = new List<int>();
            foreach (var selectedNodeID in selectedNodeIDs)
            {
                AnimationWindowHierarchyNode node = hierarchyData.FindItem(selectedNodeID) as AnimationWindowHierarchyNode;

                if (node == null)
                    continue;

                if (node is AnimationWindowHierarchyMasterNode)
                    continue;

                Transform t = rootGameObject.transform.Find(node.path);

                // In the case of nested animation component, we don't want to sync the scene selection (case 569506)
                // When selection changes, animation window will always pick nearest animator component in terms of hierarchy depth
                // Automatically syncinc scene selection in nested scenarios would cause unintuitive clip & animation change for animation window so we check for it and deny sync if necessary

                if (t != null && rootGameObject != null && activeAnimationPlayer == AnimationWindowUtility.GetClosestAnimationPlayerComponentInParents(t))
                    selectedGameObjectIDs.Add(t.gameObject.GetInstanceID());
            }

            if (selectedGameObjectIDs.Count > 0)
                UnityEditor.Selection.instanceIDs = selectedGameObjectIDs.ToArray();
            else
                UnityEditor.Selection.activeGameObject = rootGameObject;
        }

        public float clipFrameRate
        {
            get
            {
                if (activeAnimationClip == null)
                    return 60.0f;
                return activeAnimationClip.frameRate;
            }
            set
            {
                // @TODO: Changing the clip in AnimationWindowState.frame rate feels a bit intrusive
                // Should probably be done explicitly from the UI and not go through AnimationWindowState...
                if (activeAnimationClip != null && value > 0 && value <= 10000)
                {
                    // Clear selection and save empty selection snapshot for undo consistency.
                    ClearKeySelections();
                    SaveKeySelection(kEditCurveUndoLabel);

                    // Reposition all keyframes to match the new sampling rate
                    foreach (var curve in allCurves)
                    {
                        foreach (var key in curve.m_Keyframes)
                        {
                            int frame = AnimationKeyTime.Time(key.time, clipFrameRate).frame;
                            key.time = AnimationKeyTime.Frame(frame, value).time;
                        }
                        SaveCurve(curve, kEditCurveUndoLabel);
                    }

                    AnimationEvent[] events = AnimationUtility.GetAnimationEvents(activeAnimationClip);
                    foreach (AnimationEvent ev in events)
                    {
                        int frame = AnimationKeyTime.Time(ev.time, clipFrameRate).frame;
                        ev.time = AnimationKeyTime.Frame(frame, value).time;
                    }
                    AnimationUtility.SetAnimationEvents(activeAnimationClip, events);

                    activeAnimationClip.frameRate = value;
                }
            }
        }

        public float frameRate
        {
            get
            {
                return m_FrameRate;
            }
            set
            {
                if (m_FrameRate != value)
                {
                    m_FrameRate = value;
                    onFrameRateChange(m_FrameRate);
                }
            }
        }

        public AnimationKeyTime time { get { return controlInterface.time; } }
        public int currentFrame { get { return time.frame; } set { controlInterface.GoToFrame(value); } }
        public float currentTime { get { return time.time; } set { controlInterface.GoToTime(value); } }

        public TimeArea.TimeFormat timeFormat { get { return m_TimeFormat; } set { m_TimeFormat = value; } }

        public TimeArea timeArea
        {
            get { return m_TimeArea; }
            set { m_TimeArea = value; }
        }

        // Pixel to time ratio (used for time-pixel conversions)
        public float pixelPerSecond
        {
            get { return timeArea.m_Scale.x; }
        }

        // The GUI x-coordinate, where time==0 (used for time-pixel conversions)
        public float zeroTimePixel
        {
            get { return timeArea.shownArea.xMin * timeArea.m_Scale.x * -1f; }
        }

        public float PixelToTime(float pixel)
        {
            return PixelToTime(pixel, SnapMode.Disabled);
        }

        public float PixelToTime(float pixel, SnapMode snap)
        {
            float time = pixel - zeroTimePixel;
            return SnapToFrame(time / pixelPerSecond, snap);
        }

        public float TimeToPixel(float time)
        {
            return TimeToPixel(time, SnapMode.Disabled);
        }

        public float TimeToPixel(float time, SnapMode snap)
        {
            return SnapToFrame(time, snap) * pixelPerSecond + zeroTimePixel;
        }

        //@TODO: Move to animatkeytime??
        public float SnapToFrame(float time, SnapMode snap)
        {
            if (snap == SnapMode.Disabled)
                return time;

            float fps = (snap == SnapMode.SnapToFrame) ? frameRate : clipFrameRate;
            return SnapToFrame(time, fps);
        }

        public float SnapToFrame(float time, float fps)
        {
            float snapTime = Mathf.Round(time * fps) / fps;
            return snapTime;
        }

        public float minVisibleTime { get { return m_TimeArea.shownArea.xMin; } }
        public float maxVisibleTime { get { return m_TimeArea.shownArea.xMax; } }
        public float visibleTimeSpan { get { return maxVisibleTime - minVisibleTime; } }
        public float minVisibleFrame { get { return minVisibleTime * frameRate; } }
        public float maxVisibleFrame { get { return maxVisibleTime * frameRate; } }
        public float visibleFrameSpan { get { return visibleTimeSpan * frameRate; } }
        public float minTime { get { return timeRange.x; } }
        public float maxTime { get { return timeRange.y; } }

        public Vector2 timeRange
        {
            get
            {
                float minTime = 0.0f;
                float maxTime = 0.0f;

                if (selection.count > 0)
                {
                    minTime = float.MaxValue;
                    maxTime = float.MinValue;

                    foreach (var selectedItem in selection.ToArray())
                    {
                        minTime = Mathf.Min(minTime, selectedItem.animationClip.startTime + selectedItem.timeOffset);
                        maxTime = Mathf.Max(maxTime, selectedItem.animationClip.stopTime + selectedItem.timeOffset);
                    }
                }

                return new Vector2(minTime, maxTime);
            }
        }

        public string FormatFrame(int frame, int frameDigits)
        {
            return (frame / (int)frameRate).ToString() + ":" + (frame % frameRate).ToString().PadLeft(frameDigits, '0');
        }

        //@TODO: Remove. Replace with animationkeytime
        public float TimeToFrame(float time)
        {
            return time * frameRate;
        }

        //@TODO: Remove. Replace with animationkeytime
        public float FrameToTime(float frame)
        {
            return frame / frameRate;
        }

        public int TimeToFrameFloor(float time)
        {
            return Mathf.FloorToInt(TimeToFrame(time));
        }

        public int TimeToFrameRound(float time)
        {
            return Mathf.RoundToInt(TimeToFrame(time));
        }

        public float FrameToPixel(float i, Rect rect)
        {
            return (i - minVisibleFrame) * rect.width / visibleFrameSpan;
        }

        public float FrameDeltaToPixel(Rect rect)
        {
            return rect.width / visibleFrameSpan;
        }

        public float TimeToPixel(float time, Rect rect)
        {
            return FrameToPixel(time * frameRate, rect);
        }

        public float PixelToTime(float pixelX, Rect rect)
        {
            return (pixelX * visibleTimeSpan / rect.width + minVisibleTime);
        }

        public float PixelDeltaToTime(Rect rect)
        {
            return visibleTimeSpan / rect.width;
        }
    }
}
