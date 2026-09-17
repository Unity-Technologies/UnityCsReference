// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.U2D.PhysicsCore2D.Profiler.UI;
using UnityEngine;

using UnityEngine.UIElements;

namespace UnityEditor.U2D.PhysicsCore2D.Profiler
{
    class PhysicsCore2DProfilerView : VisualElement
    {
        const string k_UXML = "PhysicsCore2D/Profiler/PhysicsCore2DProfilerView/PhysicsCore2DProfilerView.uxml";
        PhysicsCore2DModuleView m_PhysicsCore2DModuleView;
        PhysicsCore2DStatisticView m_PhysicsCore2DStatisticView;

        // Raised when the Live Update toggle changes. See PhysicsCore2DStatisticView.liveUpdateChanged.
        public event Action liveUpdateChanged
        {
            add => m_PhysicsCore2DStatisticView.liveUpdateChanged += value;
            remove => m_PhysicsCore2DStatisticView.liveUpdateChanged -= value;
        }

        public PhysicsCore2DProfilerView()
        {
            VisualTreeAsset visualTree = EditorGUIUtility.Load(k_UXML) as VisualTreeAsset;
            visualTree.CloneTree(this);
            TwoPaneSplitView splitView = this.Q<TwoPaneSplitView>();
            splitView.fixedPaneInitialDimension = 300;
            splitView.fixedPaneIndex = 0;

            m_PhysicsCore2DModuleView = new PhysicsCore2DModuleView();
            m_PhysicsCore2DStatisticView = new PhysicsCore2DStatisticView();
            splitView.Add(m_PhysicsCore2DStatisticView);
            splitView.Add(m_PhysicsCore2DModuleView);
            this.Q<Label>("noProfiling").style.display = DisplayStyle.None;
            splitView.style.display = DisplayStyle.Flex;
        }

        public void SetCapturedFrameData(PhysicsCore2DFrameData[] frameData, float[] profilerMarkerValues)
        {
            m_PhysicsCore2DModuleView.SetData(frameData, profilerMarkerValues);
        }

        public void SetStatistic(Unity.U2D.Physics.PhysicsWorld.WorldCounters counter)
        {
            m_PhysicsCore2DStatisticView.SetStatistic(counter);
        }

        public bool IsLiveUpdateEnabled()
        {
            return m_PhysicsCore2DStatisticView.IsLiveUpdateEnabled();
        }
    }
}
