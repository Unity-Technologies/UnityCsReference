// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars;

/// <summary>
/// AI dropdown controller configuration.
/// </summary>
public class AIDropdownConfigArgs
{
    /// <summary>
    /// An action which will receive the ai button in the editor.
    /// </summary>
    public Action<Button> button;
    /// <summary>
    /// An action which will receive the default popup window content.
    /// </summary>
    public Action<PopupWindowContent> defaultContent;

    /// <summary>
    /// The supplied popup window content to override the default with.
    /// </summary>
    public PopupWindowContent content;
}

/// <summary>
/// AI Dropdown configuration object. This object is used to modify the behavior and content of the AI dropdown toolbar button.
/// </summary>
[Serializable]
public class AIDropdownConfig : ScriptableSingleton<AIDropdownConfig>
{
    internal AIDropdownConfigArgs config;

    /// <summary>
    /// If the AI terms have been accepted.
    /// </summary>
    public bool termsAccepted;

    /// <summary>
    /// AI Dropdown extension point. Allows external AI packages to control popup.
    /// </summary>
    /// <param name="args">Callback that receives the ai dropdown button, its default content and returns the popup window content.</param>
    public void RegisterController(AIDropdownConfigArgs args)
    {
        config = args;
        AIDropdown.instance?.RefreshContent();
    }
}
