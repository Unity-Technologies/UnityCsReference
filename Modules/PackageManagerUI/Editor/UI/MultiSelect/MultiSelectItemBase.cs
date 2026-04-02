// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal;

internal abstract class MultiSelectItemBase<T> : VisualElement
{
    protected T m_Item;

    public MultiSelectItemBase(T item)
    {
        m_Item = item;
        AddToClassList("multi-select-item");

        m_MainContainer = new VisualElement { name = "mainContainer" };
        Add(m_MainContainer);

        m_LeftContainer = new VisualElement { name = "leftContainer" };
        m_MainContainer.Add(m_LeftContainer);

        m_TypeIcon = new VisualElement { name = "typeIcon" };
        m_LeftContainer.Add(m_TypeIcon);

        m_NameLabel = new Label { name = "nameLabel" };
        m_LeftContainer.Add(m_NameLabel);

        m_RightContainer = new VisualElement { name = "rightContainer" };
        m_MainContainer.Add(m_RightContainer);

        m_RightInfoLabel = new Label { name = "rightInfoLabel" };
        m_RightContainer.Add(m_RightInfoLabel);

        m_Spinner = null;
    }

    protected void StartSpinner()
    {
        if (m_Spinner == null)
        {
            m_Spinner = new LoadingSpinner { name = "packageSpinner" };
            m_RightContainer.Insert(0, m_Spinner);
        }

        m_Spinner.Start();
        m_Spinner.tooltip = L10n.Tr("Operation in progress...");
        UIUtils.SetElementDisplay(m_RightInfoLabel, false);
    }

    protected VisualElement m_MainContainer;
    protected VisualElement m_LeftContainer;
    protected VisualElement m_TypeIcon;
    protected Label m_NameLabel;
    protected Label m_VersionLabel;
    protected VisualElement m_RightContainer;
    protected Label m_RightInfoLabel;
    protected LoadingSpinner m_Spinner;
}
