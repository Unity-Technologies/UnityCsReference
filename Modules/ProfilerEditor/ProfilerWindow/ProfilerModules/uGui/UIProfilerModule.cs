// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Profiling;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    internal class UIProfilerModule : ProfilerModuleBase
    {
        protected static WeakReference instance;
        [SerializeField]
        UISystemProfiler m_UISystemProfiler;

        static UISystemProfiler sharedUISystemProfiler
        {
            get
            {
                return instance.IsAlive ? (instance.Target as UIProfilerModule)?.m_UISystemProfiler : null;
            }
        }

        public override void OnEnable(IProfilerWindowController profilerWindow)
        {
            if (this.GetType() == typeof(UIProfilerModule))
            {
                instance = new WeakReference(this);
            }

            base.OnEnable(profilerWindow);

            if (m_UISystemProfiler == null)
                m_UISystemProfiler = new UISystemProfiler();
        }

        public override void OnDisable()
        {
            base.OnDisable();
            sharedUISystemProfiler?.CurrentAreaChanged(null);
        }

        public override void DrawToolbar(Rect position)
        {
            // This module still needs to be broken apart into Toolbar and View.
        }

        public override void DrawView(Rect position)
        {
            sharedUISystemProfiler?.DrawUIPane(m_ProfilerWindow);
        }
    }
}
