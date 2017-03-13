// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
using Object = UnityEngine.Object;

namespace UnityEditorInternal
{
    [System.Serializable]
    internal class AnimationWindowSelection
    {
        [NonSerialized] public Action onSelectionChanged;

        [SerializeField] private List<AnimationWindowSelectionItem> m_Selection = new List<AnimationWindowSelectionItem>();

        private bool m_BatchOperations = false;
        private bool m_SelectionChanged = false;

        private List<AnimationWindowCurve> m_CurvesCache = null;

        public int count
        {
            get
            {
                return m_Selection.Count;
            }
        }

        public List<AnimationWindowCurve> curves
        {
            get
            {
                if (m_CurvesCache == null)
                {
                    m_CurvesCache = new List<AnimationWindowCurve>();

                    foreach (var item in m_Selection)
                    {
                        m_CurvesCache.AddRange(item.curves);
                    }
                }

                return m_CurvesCache;
            }
        }

        public bool disabled
        {
            get
            {
                if (m_Selection.Count > 0)
                {
                    foreach (var item in m_Selection)
                    {
                        if (item.animationClip != null)
                            return false;
                    }
                }

                return true;
            }
        }

        public bool canPreview
        {
            get
            {
                if (m_Selection.Count > 0)
                    return !m_Selection.Any(item => !item.canPreview);

                return false;
            }
        }

        public bool canRecord
        {
            get
            {
                if (m_Selection.Count > 0)
                    return !m_Selection.Any(item => !item.canRecord);

                return false;
            }
        }

        public bool canAddCurves
        {
            get
            {
                if (m_Selection.Count > 0)
                    return !m_Selection.Any(item => !item.canAddCurves);

                return false;
            }
        }

        public AnimationWindowSelection()
        {
            //  noOp...
            onSelectionChanged += () => {};
        }

        public void BeginOperations()
        {
            if (m_BatchOperations)
            {
                Debug.LogWarning("AnimationWindowSelection: Already inside a BeginOperations/EndOperations block");
                return;
            }


            m_BatchOperations = true;
            m_SelectionChanged = false;
        }

        public void EndOperations()
        {
            if (m_BatchOperations)
            {
                if (m_SelectionChanged)
                {
                    onSelectionChanged();
                }

                m_SelectionChanged = false;
                m_BatchOperations = false;
            }
        }

        public void Notify()
        {
            if (m_BatchOperations)
            {
                m_SelectionChanged = true;
            }
            else
            {
                onSelectionChanged();
            }
        }

        public void Set(AnimationWindowSelectionItem newItem)
        {
            BeginOperations();
            Clear();
            Add(newItem);
            EndOperations();
        }

        public void Add(AnimationWindowSelectionItem newItem)
        {
            if (!m_Selection.Contains(newItem))
            {
                m_Selection.Add(newItem);
                Notify();
            }
        }

        public void RangeAdd(AnimationWindowSelectionItem[] newItemArray)
        {
            bool selectionChanged = false;
            foreach (var newItem in newItemArray)
            {
                if (!m_Selection.Contains(newItem))
                {
                    m_Selection.Add(newItem);
                    selectionChanged = true;
                }
            }

            if (selectionChanged)
            {
                Notify();
            }
        }

        public void UpdateClip(AnimationWindowSelectionItem itemToUpdate, AnimationClip newClip)
        {
            if (m_Selection.Contains(itemToUpdate))
            {
                itemToUpdate.animationClip = newClip;
                Notify();
            }
        }

        public void UpdateTimeOffset(AnimationWindowSelectionItem itemToUpdate, float timeOffset)
        {
            if (m_Selection.Contains(itemToUpdate))
            {
                itemToUpdate.timeOffset = timeOffset;
            }
        }

        public bool Exists(AnimationWindowSelectionItem itemToFind)
        {
            return m_Selection.Contains(itemToFind);
        }

        public bool Exists(Predicate<AnimationWindowSelectionItem> predicate)
        {
            return m_Selection.Exists(predicate);
        }

        public AnimationWindowSelectionItem Find(Predicate<AnimationWindowSelectionItem> predicate)
        {
            return m_Selection.Find(predicate);
        }

        public AnimationWindowSelectionItem First()
        {
            return m_Selection.First();
        }

        public int GetRefreshHash()
        {
            int hashCode = 0;
            foreach (var selectedItem in m_Selection)
            {
                hashCode = hashCode ^ selectedItem.GetRefreshHash();
            }

            return hashCode;
        }

        public void Refresh()
        {
            ClearCache();
            foreach (var selectedItem in m_Selection)
            {
                selectedItem.ClearCache();
            }
        }

        public AnimationWindowSelectionItem[] ToArray()
        {
            return m_Selection.ToArray();
        }

        public void Clear()
        {
            if (m_Selection.Count > 0)
            {
                foreach (var selectedItem in m_Selection)
                {
                    Object.DestroyImmediate(selectedItem);
                }

                m_Selection.Clear();
                Notify();
            }
        }

        public void ClearCache()
        {
            m_CurvesCache = null;
        }

        public void Synchronize()
        {
            if (m_Selection.Count > 0)
            {
                foreach (var selectedItem in m_Selection)
                {
                    selectedItem.Synchronize();
                }
            }
        }
    }
}
