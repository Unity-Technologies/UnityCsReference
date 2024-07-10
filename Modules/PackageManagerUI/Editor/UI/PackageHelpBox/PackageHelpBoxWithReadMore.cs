// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal abstract class PackageHelpBoxWithReadMore : PackageBaseHelpBox
    {
        public new static readonly string ussClassName = "helpbox-with-readmore";

        protected string m_ReadMoreUrl;
        protected readonly IApplicationProxy m_Application;

        protected PackageHelpBoxWithReadMore(IApplicationProxy application)
        {
            AddToClassList(ussClassName);

            m_Application = application;

            var readMoreLink = new Button { text = L10n.Tr("Read more") , classList = { "link" }};
            readMoreLink.clickable.clicked += OnReadMoreClicked;
            Add(readMoreLink);
        }

        private void OnReadMoreClicked()
        {
            if (string.IsNullOrEmpty(m_ReadMoreUrl))
                return;
            m_Application.OpenURL(m_ReadMoreUrl);
        }
    }
}
