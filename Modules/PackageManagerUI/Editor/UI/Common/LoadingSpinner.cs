// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class LoadingSpinner : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<LoadingSpinner> {}

        public bool started { get; private set; }

        private const int k_RotationSpeed = 360; // Euler degrees per second
        private int m_Rotation;
        private double m_LastRotationTime;
        private static readonly List<LoadingSpinner> s_CurrentSpinners = new List<LoadingSpinner>();

        public LoadingSpinner()
        {
            started = false;
            UIUtils.SetElementDisplay(this, false);

            // add child elements to set up centered spinner rotation
            var innerElement = new VisualElement();
            innerElement.AddToClassList("image");
            Add(innerElement);
        }

        private static void UpdateProgress()
        {
            foreach (var spinner in s_CurrentSpinners)
            {
                var currentTime = EditorApplication.timeSinceStartup;
                var deltaTime = currentTime - spinner.m_LastRotationTime;

                spinner.style.transformOrigin = new TransformOrigin(0, 0, 0);
                spinner.transform.rotation = Quaternion.Euler(0, 0, spinner.m_Rotation);
                spinner.m_Rotation += (int)(k_RotationSpeed * deltaTime);
                spinner.m_Rotation %= 360;
                if (spinner.m_Rotation < 0) spinner.m_Rotation += 360;

                spinner.m_LastRotationTime = currentTime;
            }
        }

        public void Start()
        {
            if (started)
                return;

            m_Rotation = 0;
            m_LastRotationTime = EditorApplication.timeSinceStartup;

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
    }
}
