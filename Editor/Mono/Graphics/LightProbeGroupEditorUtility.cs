// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEngine;

namespace UnityEditor.Rendering
{
    public static class LightProbeGroupEditorUtility
    {
        static LightProbeGroupTool lightProbeTool => EditorToolManager.activeTool as LightProbeGroupTool;

        private static bool IsLightProbeSelectedInHierarchy()
        {
            var selection = Selection.activeGameObject;
            if (!selection)
                return false;
            var lightProbeGroup = selection.GetComponent<LightProbeGroup>();
            return lightProbeGroup != null;
        }

        public static void MarkProbePositionsDirty()
        {
            if (!IsLightProbeSelectedInHierarchy())
                return;
            lightProbeTool?.MarkProbePositionsDirty();
        }

        public static ReadOnlyCollection<int> GetSelectedLightProbes()
        {
            if (!IsLightProbeSelectedInHierarchy())
                return new List<int>().AsReadOnly();
            if (lightProbeTool == null)
                return new List<int>().AsReadOnly();
            return lightProbeTool.GetSelected();
        }

        public static bool probesAreBeingEdited => lightProbeTool != null;

        public static void SelectLightProbe(int lightProbeIndex)
        {
            if (!IsLightProbeSelectedInHierarchy())
                return;
            lightProbeTool?.SelectLightProbe(lightProbeIndex);
        }

        public static void UnselectLightProbe(int lightProbeIndex)
        {
            if (!IsLightProbeSelectedInHierarchy())
                return;
            lightProbeTool?.UnselectLightProbe(lightProbeIndex);
        }
    }
}
