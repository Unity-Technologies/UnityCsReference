// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditorInternal;
using Object = UnityEngine.Object;
using UnityEditor.Build;

namespace UnityEditor
{
    [Serializable]
    internal struct SceneViewInfo
    {
        public int total_scene_views;
        public int num_of_2d_views;
        public bool is_default_2d_mode;
    }

    internal class BuildEventsHandlerPostProcess : IPostprocessBuild
    {
        private static bool s_EventSent = false;
        private static int s_NumOfSceneViews = 0;
        private static int s_NumOf2dSceneViews = 0;

        public int callbackOrder {get { return 0; }}
        public void OnPostprocessBuild(BuildTarget target, string path)
        {
            Object[] views = Resources.FindObjectsOfTypeAll(typeof(SceneView));
            int numOf2dSceneViews = 0;
            foreach (SceneView view in views)
            {
                if (view.in2DMode)
                    numOf2dSceneViews++;
            }
            if ((s_NumOfSceneViews != views.Length) || (s_NumOf2dSceneViews != numOf2dSceneViews) || !s_EventSent)
            {
                s_EventSent = true;
                s_NumOfSceneViews = views.Length;
                s_NumOf2dSceneViews = numOf2dSceneViews;
                EditorAnalytics.SendEventSceneViewInfo(new SceneViewInfo()
                {
                    total_scene_views = s_NumOfSceneViews, num_of_2d_views = s_NumOf2dSceneViews,
                    is_default_2d_mode = EditorSettings.defaultBehaviorMode == EditorBehaviorMode.Mode2D
                });
            }
        }
    }
} // namespace
