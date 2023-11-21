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

        private Label m_Title;
        private LoadingSpinner m_Spinner;
        private Label m_Description;

        public InProgressView()
        {
            m_Title = new Label() { classList = { "title" } };
            m_Spinner = new LoadingSpinner();
            var spinnerContainer = new VisualElement() { classList = { "spinnerContainer" } };
            spinnerContainer.Add(m_Spinner);
            m_Description = new Label() { classList = { "description" } };

            Add(m_Title);
            Add(spinnerContainer);
            Add(m_Description);
        }

        public virtual bool ShouldDisplayProgress(IPackage package)
        {
            return package?.progress == PackageProgress.Installing &&
                   package?.versions?.primary?.HasTag(PackageTag.Placeholder) == true;
        }

        public void Refresh(IPackage package)
        {
            if (package == null)
            {
                m_Title.text = string.Empty;
                m_Description.text = string.Empty;
                m_Spinner.Stop();
                return;
            }
            m_Title.text = L10n.Tr("Please wait, installing a package...");
            m_Description.text = package.uniqueId;
            m_Spinner.Start();
        }
    }
}
