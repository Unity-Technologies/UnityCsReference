// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal;

internal class MainContainerOverlay : VisualElement
{
    [Serializable]
    public new class UxmlSerializedData : VisualElement.UxmlSerializedData
    {
        public override object CreateInstance()
        {
            return new MainContainerOverlay(ServicesContainer.instance.Resolve<IApplicationProxy>());
        }
    }

    public ExtendedHelpBox extendedHelpBox { get; }
    public Label titleLabel { get; }
    private VisualElement m_Container;

    public MainContainerOverlay(IApplicationProxy applicationProxy)
    {
        m_Container = new VisualElement { name = "overlayContainer" };
        Add(m_Container);
        titleLabel = new Label { name = "overlayLabel" };
        extendedHelpBox = new ExtendedHelpBox(applicationProxy) { name = "overlayHelpBox" };
        m_Container.Add(titleLabel);
        m_Container.Add(extendedHelpBox);
    }
}
