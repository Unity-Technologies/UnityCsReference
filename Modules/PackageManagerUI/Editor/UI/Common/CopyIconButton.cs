// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class CopyIconButton : Image
    {
        [Serializable]
        public new class UxmlSerializedData : Image.UxmlSerializedData
        {
            public override object CreateInstance()
            {
                return new CopyIconButton(
                    ServicesContainer.instance.Resolve<IApplicationProxy>());
            }
        }

        private const string k_CopyIconName = "copyIcon";
        private const string k_ClickedClassName = "clicked";
        public string textToCopy { get; private set; }

        private readonly IApplicationProxy m_ApplicationProxy;
        public CopyIconButton(IApplicationProxy applicationProxy)
        {
            m_ApplicationProxy = applicationProxy;

            name = k_CopyIconName;
            tooltip = L10n.Tr("Copy to clipboard");

            RegisterCallback<MouseDownEvent>(OnMouseDown);
        }

        public void SetTextToCopy(string textToCopy)
        {
            this.textToCopy = textToCopy;
            RemoveFromClassList(k_ClickedClassName);
        }

        private void OnMouseDown(MouseDownEvent _)
        {
            m_ApplicationProxy.systemCopyBuffer = textToCopy;
            AddToClassList(k_ClickedClassName);
            schedule.Execute(() => RemoveFromClassList(k_ClickedClassName)).StartingIn(1000);
        }
    }
}
