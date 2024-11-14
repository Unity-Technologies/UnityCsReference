// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using Object = UnityEngine.Object;

namespace UnityEditorInternal
{
    [System.Serializable]
    class AnimationWindowState : ICurveEditorState
    {
        public enum RefreshType
        {
            None = 0,
            CurvesOnly = 1,
            Everything = 2
        }

        public enum SnapMode
        {
            Disabled = 0,
            SnapToFrame = 1,
            [Obsolete("SnapToClipFrame has been made redundant with SnapToFrame, SnapToFrame will behave the same.")]
            SnapToClipFrame = 2
        }

        [SerializeField] public AnimEditor animEditor; // Reference to owner of this state. Used to trigger repaints.
        [SerializeField] public AnimationWindowHierarchyState hierarchyState = new(); // Persistent state of treeview on the left side of window
        [NonSerialized] public AnimationWindowHierarchyDataSource hierarchyData;

        [SerializeReference] private TimeArea m_TimeArea; // Either curveeditor or dopesheet depending on which is selected
        [SerializeReference] private AnimationWindowControl m_ControlInterface;
        [SerializeReference] private IAnimationWindowController m_OverrideControlInterface;

        [SerializeReference] private AnimationWindowSelectionItem m_EmptySelection;
        [SerializeReference] private AnimationWindowSelectionItem m_Selection; // Internal selection
        [SerializeField] private AnimationWindowKeySelection m_KeySelection; // What is selected. Hashes persist cache reload, because they are from keyframe time+value

        [SerializeField] public bool showCurveEditor; // Do we show dopesheet or curves
        [SerializeField] public bool linkedWithSequencer; // Toggle Sequencer selection mode.
        [SerializeField] private bool m_RippleTime; // Toggle ripple time option for curve editor and dopesheet.
        private bool m_RippleTimeClutch; // Toggle ripple time option for curve editor and dopesheet.
        [SerializeField] private int m_ActiveKeyframeHash; // Which keyframe is active (selected key that user previously interacted with)
        [SerializeField] private float m_FrameRate = kDefaultFrameRate;
        [SerializeField] private int[] m_SelectionFilter;

        [NonSerialized] public Action onStartLiveEdit;
        [NonSerialized] public Action onEndLiveEdit;
        [NonSerialized] public Action<float> onFrameRateChange;

        private static List<AnimationWindowKeyframe> s_KeyframeClipboard; // For copy-pasting keyframes

        private bool m_AllCurvesCacheDirty = true;
        private bool m_FilteredCurvesCacheDirty = true;
        private bool m_ActiveCurvesCacheDirty = true;
        private List<AnimationWindowCurve> m_AllCurvesCache = new ();
        private List<AnimationWindowCurve> m_FilteredCurvesCache = new ();
        private List<AnimationWindowCurve> m_ActiveCurvesCache = new ();

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

        public AnimationWindowSelectionItem selection
        {
            get
            {
                if (m_Selection != null)
                    return m_Selection;

                if (m_EmptySelection == null)
                    m_EmptySelection = AnimationClipSelectionItem.Create(null, null);

                return m_EmptySelection;
            }

            set
            {
                m_Selection = value;
                OnSelectionChanged();
            }
        }

        // AnimationClip we are currently editing
        public AnimationClip activeAnimationClip
        {
            get
            {
                return selection.animationClip;
            }
            set
            {
                if (selection.canChangeAnimationClip)
                {
                    selection.animationClip = value;
                    OnSelectionChanged();
                }
            }
        }


        // Previously or currently selected gameobject is considered as the active gameobject
        public GameObject activeGameObject
        {
            get
            {
                return selection.gameObject;
            }
        }

        // Closes parent to activeGameObject that has Animator component
        public GameObject activeRootGameObject
        {
            get
            {
                return selection.rootGameObject;
            }
        }

        public Component activeAnimationPlayer
        {
            get
            {
                return selection.animationPlayer;
            }
        }

        public ScriptableObject activeScriptableObject
        {
            get
            {
                return selection.scriptableObject;
            }
        }

        // Is the hierarchy in animator optimized
        public bool animatorIsOptimized
        {
            get
            {
                return selection.objectIsOptimized;
            }
        }

        public bool disabled
        {
            get
            {
                return selection.disabled;
            }
        }

        public IAnimationWindowController controlInterface => m_OverrideControlInterface ?? m_ControlInterface;

        public IAnimationWindowController overrideControlInterface
        {
            get => m_OverrideControlInterface;
            set
            {
                if (m_OverrideControlInterface != null)
                    m_OverrideControlInterface.OnDestroy();

                m_OverrideControlInterface = value;
            }
        }

        public bool filterBySelection
        {
            get
            {
                return AnimationWindowOptions.filterBySelection;
            }
            set
            {
                AnimationWindowOptions.filterBySelection = value;
                UpdateSelectionFilter();

                // Refresh everything.
                refresh = RefreshType.Everything;
            }
        }

        public bool showReadOnly
        {
            get
            {
                return AnimationWindowOptions.showReadOnly;
            }
            set
            {
                AnimationWindowOptions.showReadOnly = value;

                // Refresh everything.
                refresh = RefreshType.Everything;
            }
        }

        public bool rippleTime
        {
            get
            {
                return m_RippleTime || m_RippleTimeClutch;
            }
            set
            {
                m_RippleTime = value;
            }
        }

        public bool rippleTimeClutch { get { return m_RippleTimeClutch; } set { m_RippleTimeClutch = value; } }

        public bool showFrameRate { get { return AnimationWindowOptions.showFrameRate; } set { AnimationWindowOptions.showFrameRate = value; } }

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
                m_AllCurvesCacheDirty = true;
                m_FilteredCurvesCacheDirty = true;
                m_ActiveCurvesCacheDirty = true;

                m_ActiveKeyframeCache = null;
                m_dopelinesCache = null;
                m_SelectedKeysCache = null;
                m_SelectionBoundsCache = null;

                if (animEditor != null && animEditor.curveEditor != null)
                    animEditor.curveEditor.InvalidateSelectionBounds();

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

                if (animEditor != null && animEditor.curveEditor != null)
                    animEditor.curveEditor.InvalidateSelectionBounds();

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

        private void PurgeSelection()
        {
            linkedWithSequencer = false;
            m_OverrideControlInterface = null;
            m_Selection = null;
        }

        public void OnEnable()
        {
            AnimationUtility.onCurveWasModified += CurveWasModified;
            Undo.undoRedoEvent += UndoRedoPerformed;
            AssemblyReloadEvents.beforeAssemblyReload += PurgeSelection;

            // NoOps...
            onStartLiveEdit += () => {};
            onEndLiveEdit += () => {};

            m_ControlInterface = new AnimationWindowControl
            {
                state = this
            };
            m_ControlInterface.OnEnable();

            m_AllCurvesCacheDirty = true;
            m_FilteredCurvesCacheDirty = true;
            m_ActiveCurvesCacheDirty = true;
        }

        public void OnDisable()
        {
            AnimationUtility.onCurveWasModified -= CurveWasModified;
            Undo.undoRedoEvent -= UndoRedoPerformed;
            AssemblyReloadEvents.beforeAssemblyReload -= PurgeSelection;

            m_ControlInterface.OnDisable();
            previewing = false;
        }

        public void OnDestroy()
        {
            m_ControlInterface.OnDestroy();
            m_ControlInterface = null;

            if (m_OverrideControlInterface != null)
            {
                m_OverrideControlInterface.OnDestroy();
                m_OverrideControlInterface = null;
            }

            Object.DestroyImmediate(m_KeySelection);
        }

        public void OnSelectionChanged()
        {
            if (onFrameRateChange != null)
                onFrameRateChange(frameRate);

            UpdateSelectionFilter();

            // reset back time at 0 upon selection change.
            controlInterface.OnSelectionChanged();

            if (animEditor != null)
                animEditor.OnSelectionChanged();
        }

        public void OnSelectionUpdated()
        {
            UpdateSelectionFilter();
            if (filterBySelection)
            {
                // Refresh everything.
                refresh = RefreshType.Everything;
            }
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

        public void UndoRedoPerformed(in UndoRedoInfo info)
        {
            refresh = RefreshType.Everything;
            ResampleAnimation();
        }

        // When curve is modified, we never trigger refresh right away. We order a refresh at later time by setting refresh to appropriate value.
        void CurveWasModified(AnimationClip clip, EditorCurveBinding binding, AnimationUtility.CurveModifiedType type)
        {
            // AnimationWindow doesn't care if some other clip somewhere changed
            if (activeAnimationClip != clip)
                return;

            // Refresh curves that already exist.
            if (type == AnimationUtility.CurveModifiedType.CurveModified)
            {
                bool didFind = false;
                bool hadPhantom = false;
                int hashCode = binding.GetHashCode();

                var curves = filteredCurves;
                for (int j = 0; j < curves.Count; ++j)
                {
                    var curve = curves[j];
                    int curveHash = curve.GetBindingHashCode();
                    if (curveHash == hashCode)
                    {
                        m_ModifiedCurves.Add(curve.GetHashCode());
                        didFind = true;
                        hadPhantom |= curve.binding.isPhantom;
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
            // Force repaint to display live animation curve changes from other editor window (like timeline).
            Repaint();
        }

        public void SaveKeySelection(string undoLabel)
        {
            if (m_KeySelection != null)
                Undo.RegisterCompleteObjectUndo(m_KeySelection, undoLabel);
        }

        public void SaveCurve(AnimationClip clip, AnimationWindowCurve curve)
        {
            SaveCurve(clip, curve, kEditCurveUndoLabel);
        }

        public void SaveCurve(AnimationClip clip, AnimationWindowCurve curve, string undoLabel)
        {
            if (!curve.animationIsEditable)
                Debug.LogError("Curve is not editable and shouldn't be saved.");

            Undo.RegisterCompleteObjectUndo(clip, undoLabel);
            AnimationWindowUtility.SaveCurve(clip, curve);
            Repaint();
        }

        public void SaveCurves(AnimationClip clip, ICollection<AnimationWindowCurve> curves, string undoLabel = kEditCurveUndoLabel)
        {
            if (curves.Count == 0)
                return;

            Undo.RegisterCompleteObjectUndo(clip, undoLabel);
            AnimationWindowUtility.SaveCurves(clip, curves);
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
                foreach (AnimationWindowKeyframe other in snapshot.curve.keyframes)
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
                    snapshot.curve.RemoveKeyframe(deletedKey);
                }
            }

            SaveCurves(activeAnimationClip, saveCurves, undoLabel);
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

        public bool previewing
        {
            get => controlInterface.previewing;
            set
            {
                if (controlInterface.previewing == value)
                    return;

                if (value)
                {
                    if (canPreview)
                        controlInterface.previewing = true;
                }
                else
                {
                    // Automatically stop recording and playback when stopping preview.
                    controlInterface.playing = false;
                    controlInterface.recording = false;
                    controlInterface.previewing = false;
                }
            }
        }

        public bool canPreview => controlInterface.canPreview;

        public void UpdateCurvesDisplayName()
        {
            if (hierarchyData != null)
                hierarchyData.UpdateSerializeReferenceCurvesArrayNiceDisplayName();
        }

        public bool recording
        {
            get => controlInterface.recording;
            set
            {
                if (controlInterface.recording == value)
                    return;

                if (value)
                {
                    if (canRecord)
                    {
                        // Auto-Preview when starting recording
                        controlInterface.previewing = true;

                        if (controlInterface.previewing)
                            controlInterface.recording = true;
                    }
                }
                else
                    controlInterface.recording = false;
            }
        }

        public bool canRecord => controlInterface.canRecord;

        public bool playing
        {
            get => controlInterface.playing;
            set
            {
                if (controlInterface.playing == value)
                    return;

                if (value)
                {
                    if (canPlay)
                    {
                        // Auto-Preview when starting playback.
                        controlInterface.previewing = true;

                        if (controlInterface.previewing)
                            controlInterface.playing = true;
                    }
                }
                else
                    controlInterface.playing = false;
            }
        }

        public bool canPlay => controlInterface.canPlay;

        public void ResampleAnimation()
        {
            if (disabled)
                return;

            if (controlInterface.previewing == false)
                return;
            if (controlInterface.canPreview == false)
                return;

            controlInterface.ResampleAnimation();
        }

        public bool PlaybackUpdate()
        {
            if (disabled)
                return false;

            if (!controlInterface.playing)
                return false;

            return controlInterface.PlaybackUpdate();
        }

        public void ClearCandidates() => controlInterface.ClearCandidates();
        public void ProcessCandidates() => controlInterface.ProcessCandidates();

        public bool ShouldShowCurve(AnimationWindowCurve curve)
        {
            if (filterBySelection && activeRootGameObject != null)
            {
                if (m_SelectionFilter != null)
                {
                    Transform t = activeRootGameObject.transform.Find(curve.path);
                    if (t != null)
                    {
                        if (!m_SelectionFilter.Contains(t.gameObject.GetInstanceID()))
                            return false;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void UpdateSelectionFilter()
        {
            m_SelectionFilter = (filterBySelection) ? (int[])Selection.instanceIDs.Clone() : null;
        }

        void RebuildAllCurvesCacheIfNecessary()
        {
            if (m_AllCurvesCacheDirty == false && m_AllCurvesCache != null)
                return;

            if (m_AllCurvesCache == null)
                m_AllCurvesCache = new List<AnimationWindowCurve>();
            else
                m_AllCurvesCache.Clear();

            var animationClip = activeAnimationClip;
            if (animationClip == null || (!selection.animationIsEditable && !showReadOnly))
                return;

            EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(animationClip);
            EditorCurveBinding[] objectCurveBindings = AnimationUtility.GetObjectReferenceCurveBindings(animationClip);

            List<AnimationWindowCurve> transformCurves = new List<AnimationWindowCurve>();

            foreach (EditorCurveBinding curveBinding in curveBindings)
            {
                if (AnimationWindowUtility.ShouldShowAnimationWindowCurve(curveBinding))
                {
                    AnimationWindowCurve curve = new AnimationWindowCurve(animationClip, curveBinding, controlInterface.GetValueType(curveBinding));
                    curve.selectionBinding = selection;

                    m_AllCurvesCache.Add(curve);

                    if (AnimationWindowUtility.IsActualTransformCurve(curveBinding))
                    {
                        transformCurves.Add(curve);
                    }
                }
            }

            foreach (EditorCurveBinding curveBinding in objectCurveBindings)
            {
                AnimationWindowCurve curve = new AnimationWindowCurve(animationClip, curveBinding, controlInterface.GetValueType(curveBinding));
                curve.selectionBinding = selection;

                m_AllCurvesCache.Add(curve);
            }

            transformCurves.Sort();
            if (transformCurves.Count > 0)
            {
                FillInMissingTransformCurves(animationClip, transformCurves, ref m_AllCurvesCache);
            }

            // Curves need to be sorted with path/type/property name so it's possible to construct hierarchy from them
            // Sorting logic in AnimationWindowCurve.CompareTo()
            m_AllCurvesCache.Sort();

            m_AllCurvesCacheDirty = false;
        }

        private void RebuildFilteredCurvesCacheIfNecessary()
        {
            if (m_FilteredCurvesCacheDirty == false && m_FilteredCurvesCache != null)
                return;

            if (m_FilteredCurvesCache == null)
                m_FilteredCurvesCache = new List<AnimationWindowCurve>();
            else
                m_FilteredCurvesCache.Clear();

            for (int i = 0; i < m_AllCurvesCache.Count; ++i)
            {
                if (ShouldShowCurve(m_AllCurvesCache[i]))
                    m_FilteredCurvesCache.Add(m_AllCurvesCache[i]);
            }

            m_FilteredCurvesCacheDirty = false;
        }

        private void RebuildActiveCurvesCacheIfNecessary()
        {
            if (m_ActiveCurvesCacheDirty == false && m_ActiveCurvesCache != null)
                return;

            if (m_ActiveCurvesCache == null)
                m_ActiveCurvesCache = new List<AnimationWindowCurve>();
            else
                m_ActiveCurvesCache.Clear();

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

            m_ActiveCurvesCacheDirty = false;
        }

        private void FillInMissingTransformCurves(AnimationClip animationClip, List<AnimationWindowCurve> transformCurves, ref List<AnimationWindowCurve> curvesCache)
        {
            EditorCurveBinding lastBinding = transformCurves[0].binding;
            var propertyGroup = new EditorCurveBinding ? [3];
            string propertyGroupName;
            foreach (var transformCurve in transformCurves)
            {
                var transformBinding = transformCurve.binding;
                //if it's a new property group
                if (transformBinding.path != lastBinding.path
                    || AnimationWindowUtility.GetPropertyGroupName(transformBinding.propertyName) != AnimationWindowUtility.GetPropertyGroupName(lastBinding.propertyName))
                {
                    propertyGroupName = AnimationWindowUtility.GetPropertyGroupName(lastBinding.propertyName);

                    FillPropertyGroup(animationClip, ref propertyGroup, lastBinding, propertyGroupName, ref curvesCache);

                    lastBinding = transformBinding;

                    propertyGroup = new EditorCurveBinding ? [3];
                }

                AssignBindingToRightSlot(transformBinding, ref propertyGroup);
            }
            FillPropertyGroup(animationClip, ref propertyGroup, lastBinding, AnimationWindowUtility.GetPropertyGroupName(lastBinding.propertyName), ref curvesCache);
        }

        private void FillPropertyGroup(AnimationClip animationClip, ref EditorCurveBinding?[] propertyGroup, EditorCurveBinding lastBinding, string propertyGroupName, ref List<AnimationWindowCurve> curvesCache)
        {
            var newBinding = lastBinding;
            newBinding.isPhantom = true;
            if (!propertyGroup[0].HasValue)
            {
                newBinding.propertyName = propertyGroupName + ".x";
                AnimationWindowCurve curve = new AnimationWindowCurve(animationClip, newBinding, controlInterface.GetValueType(newBinding));
                curve.selectionBinding = selection;
                curvesCache.Add(curve);
            }

            if (!propertyGroup[1].HasValue)
            {
                newBinding.propertyName = propertyGroupName + ".y";
                AnimationWindowCurve curve = new AnimationWindowCurve(animationClip, newBinding, controlInterface.GetValueType(newBinding));
                curve.selectionBinding = selection;
                curvesCache.Add(curve);
            }

            if (!propertyGroup[2].HasValue)
            {
                newBinding.propertyName = propertyGroupName + ".z";
                AnimationWindowCurve curve = new AnimationWindowCurve(animationClip, newBinding, controlInterface.GetValueType(newBinding));
                curve.selectionBinding = selection;
                curvesCache.Add(curve);
            }
        }

        private void AssignBindingToRightSlot(EditorCurveBinding transformBinding, ref EditorCurveBinding?[] propertyGroup)
        {
            if (transformBinding.propertyName.EndsWith(".x"))
            {
                propertyGroup[0] = transformBinding;
            }
            else if (transformBinding.propertyName.EndsWith(".y"))
            {
                propertyGroup[1] = transformBinding;
            }
            else if (transformBinding.propertyName.EndsWith(".z"))
            {
                propertyGroup[2] = transformBinding;
            }
        }

        public List<AnimationWindowCurve> allCurves
        {
            get
            {
                RebuildAllCurvesCacheIfNecessary();
                return m_AllCurvesCache;
            }
        }

        public List<AnimationWindowCurve> filteredCurves
        {
            get
            {
                RebuildAllCurvesCacheIfNecessary();
                RebuildFilteredCurvesCacheIfNecessary();
                return m_FilteredCurvesCache;
            }
        }

        public List<AnimationWindowCurve> activeCurves
        {
            get
            {
                RebuildActiveCurvesCacheIfNecessary();
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
                        foreach (AnimationWindowCurve curve in filteredCurves)
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
                    foreach (AnimationWindowCurve curve in filteredCurves)
                    {
                        foreach (AnimationWindowKeyframe keyframe in curve.keyframes)
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
                    foreach (AnimationWindowCurve curve in filteredCurves)
                    {
                        foreach (AnimationWindowKeyframe keyframe in curve.keyframes)
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
                        float time = key.time;
                        float val = key.isPPtrCurve || key.isDiscreteCurve ? 0.0f : (float)key.value;

                        Bounds bounds = new Bounds(new Vector2(time, val), Vector2.zero);

                        for (int i = 1; i < keys.Count; ++i)
                        {
                            key = keys[i];

                            time = key.time;
                            val = key.isPPtrCurve || key.isDiscreteCurve ? 0.0f : (float)key.value;

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
                    m_KeySelection = ScriptableObject.CreateInstance<AnimationWindowKeySelection>();
                    m_KeySelection.hideFlags = HideFlags.HideAndDontSave;
                }

                return m_KeySelection.selectedKeyHashes;
            }
            set
            {
                if (m_KeySelection == null)
                {
                    m_KeySelection = ScriptableObject.CreateInstance<AnimationWindowKeySelection>();
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

        public void UnselectKey(AnimationWindowKeyframe keyframe)
        {
            int hash = keyframe.GetHash();
            if (selectedKeyHashes.Contains(hash))
                selectedKeyHashes.Remove(hash);

            m_SelectedKeysCache = null;
            m_SelectionBoundsCache = null;
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

            HashSet<AnimationWindowCurve> curves = new HashSet<AnimationWindowCurve>();

            foreach (AnimationWindowKeyframe keyframe in keys)
            {
                if (!keyframe.curve.animationIsEditable)
                    continue;

                curves.Add(keyframe.curve);

                UnselectKey(keyframe);
                keyframe.curve.RemoveKeyframe(keyframe);
            }

            SaveCurves(activeAnimationClip, curves, kEditCurveUndoLabel);

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
                    foreach (AnimationWindowKeyframe key in selectedKey.curve.keyframes)
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
                        Vector3 v = new Vector3(liveEditKey.keySnapshot.time, liveEditKey.keySnapshot.isPPtrCurve || liveEditKey.keySnapshot.isDiscreteCurve ? 0f : (float)liveEditKey.keySnapshot.value, 0f);
                        v = matrix.MultiplyPoint3x4(v);

                        liveEditKey.key.time = Mathf.Max((snapToFrame) ? SnapToFrame(v.x, snapshot.curve.clip.frameRate) : v.x, 0f);

                        if (flipX)
                        {
                            liveEditKey.key.inTangent = (liveEditKey.keySnapshot.outTangent != Mathf.Infinity) ? -liveEditKey.keySnapshot.outTangent : Mathf.Infinity;
                            liveEditKey.key.outTangent = (liveEditKey.keySnapshot.inTangent != Mathf.Infinity) ? -liveEditKey.keySnapshot.inTangent : Mathf.Infinity;

                            if (liveEditKey.keySnapshot.weightedMode == WeightedMode.In)
                                liveEditKey.key.weightedMode = WeightedMode.Out;
                            else if (liveEditKey.keySnapshot.weightedMode == WeightedMode.Out)
                                liveEditKey.key.weightedMode = WeightedMode.In;
                            else
                                liveEditKey.key.weightedMode = liveEditKey.keySnapshot.weightedMode;

                            liveEditKey.key.inWeight = liveEditKey.keySnapshot.outWeight;
                            liveEditKey.key.outWeight = liveEditKey.keySnapshot.inWeight;
                        }

                        if (!liveEditKey.key.isPPtrCurve && !liveEditKey.key.isDiscreteCurve)
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

        internal static bool CanPasteKeys()
        {
            return s_KeyframeClipboard != null && s_KeyframeClipboard.Count > 0;
        }

        internal static void ClearKeyframeClipboard()
        {
            s_KeyframeClipboard?.Clear();
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
                if (keyframe.time < smallestTime)
                    smallestTime = keyframe.time;
            }
            if (s_KeyframeClipboard.Count > 0) // copying selected keys
            {
                foreach (AnimationWindowKeyframe keyframe in s_KeyframeClipboard)
                {
                    keyframe.time -= smallestTime;
                }
            }
            else // No selected keys, lets copy entire curves
            {
                CopyAllActiveCurves();
            }

            // Animation keyframes right now do not go through regular clipboard machinery,
            // so when copying keyframes, make sure regular clipboard is cleared, or things
            // get confusing.
            if (s_KeyframeClipboard.Count > 0)
                Clipboard.stringValue = string.Empty;
        }

        public void CopyAllActiveCurves()
        {
            foreach (AnimationWindowCurve curve in activeCurves)
            {
                foreach (AnimationWindowKeyframe keyframe in curve.keyframes)
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
            bool matchCurveByIndex = clipboardCurves.Count == activeCurves.Count;
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
                        if (selection.animationIsEditable)
                        {
                            newKeyframe.curve = new AnimationWindowCurve(selection.animationClip, keyframe.curve.binding, keyframe.curve.valueType);
                            newKeyframe.curve.selectionBinding = selection;
                            newKeyframe.time = keyframe.time;
                        }
                    }
                }

                if (newKeyframe.curve == null || !newKeyframe.curve.animationIsEditable)
                    continue;

                newKeyframe.time = AnimationKeyTime.Time(newKeyframe.time + currentTime, newKeyframe.curve.clip.frameRate).timeRound;

                //  Only allow pasting of key frame from numerical curves to numerical curves or from pptr curves to pptr curves.
                if ((newKeyframe.time >= 0.0f) && (newKeyframe.curve != null) && (newKeyframe.curve.isPPtrCurve == keyframe.curve.isPPtrCurve))
                {
                    var keyTime = AnimationKeyTime.Time(newKeyframe.time, newKeyframe.curve.clip.frameRate);

                    if (newKeyframe.curve.HasKeyframe(keyTime))
                        newKeyframe.curve.RemoveKeyframe(keyTime);

                    // When copy-pasting multiple keyframes (curve), its a continous thing. This is why we delete the existing keyframes in the pasted range.
                    if (lastTargetCurve == newKeyframe.curve)
                        newKeyframe.curve.RemoveKeysAtRange(lastTime, newKeyframe.time);

                    newKeyframe.curve.AddKeyframe(newKeyframe, keyTime);
                    SelectKey(newKeyframe);
                    // TODO: Optimize to only save curve once instead once per keyframe
                    SaveCurve(newKeyframe.curve.clip, newKeyframe.curve, kEditCurveUndoLabel);

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
                    AnimationWindowCurve curve = filteredCurves.Find(c => c.GetHashCode() == curveWrapper.id);
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
            for (int i = 0; i < filteredCurves.Count; ++i)
            {
                AnimationWindowCurve curve = filteredCurves[i];
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
            ResampleAnimation();

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

        public void SelectHierarchyItems(IEnumerable<int> hierarchyNodeIDs, bool additive, bool triggerSceneSelectionSync)
        {
            if (!additive)
                ClearHierarchySelection();

            hierarchyState.selectedIDs.AddRange(hierarchyNodeIDs);

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

        public HashSet<int> GetAffectedHierarchyIDs(List<AnimationWindowKeyframe> keyframes)
        {
            HashSet<int> hierarchyIDs = new HashSet<int>();

            foreach (AnimationWindowKeyframe keyframe in keyframes)
            {
                var curve = keyframe.curve;

                int hierarchyID = AnimationWindowUtility.GetPropertyNodeID(0, curve.path, curve.type, curve.propertyName);
                if (hierarchyIDs.Add(hierarchyID))
                {
                    string propertyGroupName = AnimationWindowUtility.GetPropertyGroupName(curve.propertyName);
                    hierarchyIDs.Add(AnimationWindowUtility.GetPropertyNodeID(0, curve.path, curve.type, propertyGroupName));
                }
            }

            return hierarchyIDs;
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
            if (filterBySelection)
                return;

            if (!selection.canSyncSceneSelection)
                return;

            GameObject rootGameObject = selection.rootGameObject;
            if (rootGameObject == null)
                return;

            List<int> selectedGameObjectIDs = new List<int>(selectedNodeIDs.Length);
            foreach (var selectedNodeID in selectedNodeIDs)
            {
                // Skip nodes without associated curves.
                if (selectedNodeID == 0)
                    continue;

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
                        foreach (var key in curve.keyframes)
                        {
                            int frame = AnimationKeyTime.Time(key.time, clipFrameRate).frame;
                            key.time = AnimationKeyTime.Frame(frame, value).time;
                        }
                    }

                    SaveCurves(activeAnimationClip, allCurves, kEditCurveUndoLabel);

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
                    if (onFrameRateChange != null)
                        onFrameRateChange(m_FrameRate);
                }
            }
        }

        public AnimationKeyTime time => AnimationKeyTime.Time(controlInterface.time, frameRate);

        public int currentFrame
        {
            get => controlInterface.frame;
            set => controlInterface.frame = value;
        }

        public float currentTime
        {
            get => controlInterface.time;
            set => controlInterface.time = value;
        }

        public TimeArea.TimeFormat timeFormat { get { return AnimationWindowOptions.timeFormat; } set { AnimationWindowOptions.timeFormat = value; } }

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
                if (activeAnimationClip != null)
                    return new Vector2(activeAnimationClip.startTime, activeAnimationClip.stopTime);

                return Vector2.zero;
            }
        }

        public string FormatFrame(int frame, int frameDigits)
        {
            return (frame / (int)frameRate) + ":" + (frame % frameRate).ToString().PadLeft(frameDigits, '0');
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

        public void GoToPreviousFrame()
        {
            controlInterface.frame -= 1;
        }

        public void GoToNextFrame()
        {
            controlInterface.frame += 1;
        }

        public void GoToPreviousKeyframe()
        {
            List<AnimationWindowCurve> curves = (showCurveEditor && activeCurves.Count > 0) ? activeCurves : filteredCurves;

            float newTime = AnimationWindowUtility.GetPreviousKeyframeTime(curves.ToArray(), controlInterface.time, clipFrameRate);
            controlInterface.time = SnapToFrame(newTime, SnapMode.SnapToFrame);
        }

        public void GoToNextKeyframe()
        {
            List<AnimationWindowCurve> curves = (showCurveEditor && activeCurves.Count > 0) ? activeCurves : filteredCurves;

            float newTime = AnimationWindowUtility.GetNextKeyframeTime(curves.ToArray(), controlInterface.time, clipFrameRate);
            controlInterface.time = SnapToFrame(newTime, AnimationWindowState.SnapMode.SnapToFrame);
        }

        public void GoToFirstKeyframe()
        {
            if (activeAnimationClip)
                controlInterface.time = activeAnimationClip.startTime;
        }

        public void GoToLastKeyframe()
        {
            if (activeAnimationClip)
                controlInterface.time = activeAnimationClip.stopTime;
        }

    }
}
