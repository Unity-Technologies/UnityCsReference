// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.Licensing.UI.Events.Buttons;
using UnityEditor.Licensing.UI.Helper;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Licensing.UI.Events.Windows
{
class TemplateLicenseEventWindowContents : VisualElement
{
    const string k_ElementNameDescription = "Description";
    const string k_ElementNameButtonsContainer = "ButtonsContainer";
    const string k_UxmlFileName = "GenericLicenseEventWindow";

    ILicenseLogger m_LicenseLogger;
    protected LogType m_LogType = LogType.Log;
    protected string m_LogTag;
    protected string m_LogMessage;

    protected string m_Description;

    protected IList<EventsButtonType> m_Buttons = new List<EventsButtonType>();

    public bool UserClosedModalFromTitleBar { get; private set; } = true;

    IEventsButtonFactory m_EventsButtonFactory;

    void AdditionalCustomClickAction()
    {
        UserClosedModalFromTitleBar = false;
    }

    protected TemplateLicenseEventWindowContents(IEventsButtonFactory eventsButtonFactory, ILicenseLogger licenseLogger)
    {
        m_EventsButtonFactory = eventsButtonFactory;
        m_LicenseLogger = licenseLogger;
    }

    protected void CreateContents()
    {
        // Loads and clones our VisualTree (eg. our UXML structure) inside the root.
        var visualTree = LoadTemplateAndStylesheet(k_UxmlFileName);

        visualTree.CloneTree(this);

        SetText();

        SetButtons();

        if (string.IsNullOrEmpty(m_LogMessage))
        {
            m_LogMessage = m_Description;
        }
        m_LicenseLogger.DebugLogNoStackTrace(m_LogMessage, m_LogType, tag: m_LogTag);
    }

    static VisualTreeAsset LoadTemplateAndStylesheet(string uxmlName)
    {
        var tpl = EditorGUIUtility.Load($"UXML/Licensing/{uxmlName}.uxml") as VisualTreeAsset;
        if (tpl == null)
        {
            Debug.Log("Failed to load template " + uxmlName);
        }

        return tpl;
    }

    void SetText()
    {
        this.Q<Label>(k_ElementNameDescription).text = m_Description;
    }

    void SetButtons()
    {
        var buttonsContainer = this.Q<VisualElement>(k_ElementNameButtonsContainer);

        foreach (var button in m_Buttons)
        {
            // For the case of Save and Close factory might return two buttons and visual element object that contains two buttons will sort
            // them in column format instead of row when this components are rendered
            m_EventsButtonFactory.Create(button, AdditionalCustomClickAction).Query().ForEach(buttonsContainer.Add);
        }
    }
}
}
