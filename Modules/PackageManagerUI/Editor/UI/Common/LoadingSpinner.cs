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

        private int m_Rotation;

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
            transform.rotation = Quaternion.Euler(0, 0, m_Rotation);
            m_Rotation += 3;
            if (m_Rotation > 360)
                m_Rotation -= 360;
        }

        public void Start()
        {
            if (started)
                return;

            m_Rotation = 0;

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
