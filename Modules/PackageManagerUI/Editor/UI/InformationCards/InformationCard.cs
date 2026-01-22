// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal;

internal abstract class InformationCard : VisualElement
{
    internal const string k_InfoCardClassName = "informationCard";
    internal const string k_TitleClassName = "informationCardTitle";
    internal const string k_ContentClassName = "informationCardContent";
    internal const string k_IconClassName = "informationCardIcon";
    internal const string k_ContentTextClassName = "informationCardText";

    private SelectableLabel m_Title;

    protected abstract string titleText { get; }
    protected abstract InformationCardSize cardSize { get; }

    protected VisualElement m_Content;

    private VisualElement m_IconElement;

    private Icon m_Icon;
    public Icon icon
    {
        get => m_Icon;
        set
        {
            m_IconElement.RemoveFromClassList(m_Icon.ClassName());
            m_Icon = value;
            m_IconElement.AddToClassList(m_Icon.ClassName());
            var isIconVisible = m_Icon != Icon.None;
            UIUtils.SetElementDisplay(m_IconElement, isIconVisible);
        }
    }

    private string m_IconTooltip;
    public string iconTooltip
    {
        get => m_IconTooltip;
        set
        {
            m_IconTooltip = value ?? string.Empty;
            m_IconElement.tooltip = value;
        }
    }

    private SelectableLabel m_ContentLabel;
    public string contentText
    {
        get => m_ContentLabel?.text ?? string.Empty;
        set
        {
            m_ContentLabel.text = value ?? string.Empty;
            UIUtils.SetElementDisplay(m_ContentLabel, !string.IsNullOrEmpty(value));
        }
    }

    public string contentTooltip
    {
        get => m_ContentLabel?.tooltip ?? string.Empty;
        set => m_ContentLabel.tooltip = value ?? string.Empty;
    }

    public InformationCard()
    {
        m_Title = new SelectableLabel();
        m_Title.AddToClassList(k_TitleClassName);
        m_Title.text = titleText;
        m_Title.tooltip = titleText;
        Add(m_Title);

        m_Content = new VisualElement();
        m_Content.AddToClassList(k_ContentClassName);
        Add(m_Content);

        m_IconElement = new VisualElement();
        m_IconElement.AddToClassList(k_IconClassName);
        m_Content.Add(m_IconElement);
        icon = Icon.None;

        m_ContentLabel = new SelectableLabel();
        m_ContentLabel.AddToClassList(k_ContentTextClassName);
        m_Content.Add(m_ContentLabel);

        AddToClassList(k_InfoCardClassName);
        AddToClassList(cardSize.ClassName());
    }
}
