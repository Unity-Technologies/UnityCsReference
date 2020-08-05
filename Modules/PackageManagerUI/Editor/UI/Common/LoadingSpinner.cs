// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class LoadingSpinner : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<LoadingSpinner> {}

        public bool started { get; private set; }

        private const int k_RotationSpeed = 360; // Euler degrees per second
        private int m_Rotation;
        private double m_LastRotationTime;

        public LoadingSpinner()
        {
            started = false;
            UIUtils.SetElementDisplay(this, false);

            // add child elements to set up centered spinner rotation
            var innerElement = new VisualElement();
            innerElement.AddToClassList("image");
            Add(innerElement);
        }

        private void UpdateProgress()
        {
            var currentTime = EditorApplication.timeSinceStartup;
            var deltaTime = currentTime - m_LastRotationTime;

            transform.rotation = Quaternion.Euler(0, 0, m_Rotation);
            m_Rotation += (int)(k_RotationSpeed * deltaTime);
            m_Rotation = m_Rotation % 360;
            if (m_Rotation < 0) m_Rotation += 360;

            m_LastRotationTime = currentTime;
        }

        public void Start()
        {
            if (started)
                return;

            m_Rotation = 0;
            m_LastRotationTime = EditorApplication.timeSinceStartup;

            EditorApplication.update += UpdateProgress;

            started = true;
            UIUtils.SetElementDisplay(this, true);
        }

        public void Stop()
        {
            if (!started)
                return;

            EditorApplication.update -= UpdateProgress;

            started = false;
            UIUtils.SetElementDisplay(this, false);
        }
    }
}
