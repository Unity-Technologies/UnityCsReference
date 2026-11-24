// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    internal interface IBuildAnalysisTabView
    {
        VisualElement Root { get; }
        void Initialize();
        void Apply(BuildAnalysisView view);
        void OnTabVisibilityChanged(bool isVisible);
        void OnInspectorVisibilityChanged(bool isOpen);
        
        bool DoesSupportInspector()
        {
            return false;
        }
        
    }
}
