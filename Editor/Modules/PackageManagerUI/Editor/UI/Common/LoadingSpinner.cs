// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    [UxmlElement]
    internal partial class LoadingSpinner : VisualElement
    {
        public bool started { get; private set; }

        private const int k_RotationSpeed = 360; // Euler degrees per second
        private const double k_PaintInterval = 0.125f; // Time interval to repaint
        [NoAutoStaticsCleanup] // animation phase only; carrying it over just continues the rotation
        private static int s_Rotation;
        [NoAutoStaticsCleanup] // timestamp is re-seeded from timeSinceStartup on the next Start()
        private static double s_LastRotationTime;
        [NoAutoStaticsCleanup] // emptied by [OnCodeUnloading] ClearAllSpinners, so no stale spinner is carried over a code reload
        private static List<LoadingSpinner> s_CurrentSpinners = new List<LoadingSpinner>();
        public LoadingSpinner()
        {
            started = false;
            UIUtils.SetElementDisplay(this, false);

            // add child elements to set up centered spinner rotation
            var innerElement = new VisualElement();
            innerElement.AddToClassList("image");
            Add(innerElement);
            style.transformOrigin = new TransformOrigin(0, 0, 0);
        }

        private static void UpdateProgress()
        {
            var currentTime = EditorApplication.timeSinceStartup;
            var deltaTime = currentTime - s_LastRotationTime;
            if (deltaTime >= k_PaintInterval)
            {
                var q = Quaternion.Euler(0, 0, s_Rotation);
                foreach (var spinner in s_CurrentSpinners)
                    spinner.style.rotate = q;
                s_Rotation += (int)(k_RotationSpeed * deltaTime);
                s_Rotation %= 360;
                if (s_Rotation < 0) s_Rotation += 360;
                s_LastRotationTime = currentTime;
            }
        }

        public void Start()
        {
            if (started)
                return;

            s_Rotation = 0;

            // we remove k_PaintInterval from timeSinceStartup to make sure we start the animation the first time
            s_LastRotationTime = EditorApplication.timeSinceStartup - k_PaintInterval;

            started = true;
            UIUtils.SetElementDisplay(this, true);

            if (s_CurrentSpinners.Count == 0)
                EditorApplication.update += UpdateProgress;
            s_CurrentSpinners.Add(this);
        }

        public void Stop()
        {
            if (!started)
                return;

            started = false;
            UIUtils.SetElementDisplay(this, false);

            s_CurrentSpinners.Remove(this);
            if (s_CurrentSpinners.Count == 0)
                EditorApplication.update -= UpdateProgress;
        }

        // Also runs on code reload: this module is not reloaded, but the Package Manager UI tree owning
        // these elements is rebuilt and EditorApplication.update is cleared, so the spinners left behind
        // are detached and must be dropped for Start() to re-subscribe UpdateProgress.
        [OnCodeUnloading]
        public static void ClearAllSpinners()
        {
            if (s_CurrentSpinners.Count == 0)
                return;

            s_CurrentSpinners.Clear();
            EditorApplication.update -= UpdateProgress;
        }
    }
}
