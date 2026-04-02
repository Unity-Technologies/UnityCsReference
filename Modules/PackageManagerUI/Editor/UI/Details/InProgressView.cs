// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class InProgressView : VisualElement
    {
        [Serializable]
        internal new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new InProgressView();
        }

        private readonly Label m_Title;
        private readonly LoadingSpinner m_Spinner;
        private readonly Label m_Description;

        public InProgressView()
        {
            m_Title = new Label().WithClassList("title");
            m_Spinner = new LoadingSpinner();
            var spinnerContainer = new VisualElement().WithClassList("spinnerContainer");
            spinnerContainer.Add(m_Spinner);
            m_Description = new Label().WithClassList("description");

            Add(m_Title);
            Add(spinnerContainer);
            Add(m_Description);
        }

        public void UpdateMessage(string title, string description)
        {
            m_Title.text = title;
            m_Description.text = description;
        }

        public void UpdateProgress(bool inProgress)
        {
            if (inProgress)
                m_Spinner.Start();
            else
                m_Spinner.Stop();
        }
    }
}
