// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class LoadingSpinner : VisualElement
    {
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new LoadingSpinner();
        }

        public bool started { get; private set; }

        private const int k_RotationSpeed = 360; // Euler degrees per second
        private const double k_PaintInterval = 0.125f; // Time interval to repaint
        static private int s_Rotation;
        static private double s_LastRotationTime;
        private static readonly List<LoadingSpinner> s_CurrentSpinners = new List<LoadingSpinner>();
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
                    spinner.transform.rotation = q;
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

            if (!s_CurrentSpinners.Any())
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
            if (!s_CurrentSpinners.Any())
                EditorApplication.update -= UpdateProgress;
        }

        public static void ClearAllSpinners()
        {
            if (!s_CurrentSpinners.Any())
                return;

            s_CurrentSpinners.Clear();
            EditorApplication.update -= UpdateProgress;
        }
    }
}
