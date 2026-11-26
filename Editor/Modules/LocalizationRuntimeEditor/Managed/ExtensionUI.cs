// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.Localization.Editor;

static class ExtensionUI
{
    public static VisualElement Header(Type type, string title)
    {
        var header = new VisualElement();
        header.AddToClassList(LocClasses.LocExtensionHeader);

        var icon = new Image { image = LocIcons.TypeIcon(type) };
        icon.AddToClassList(LocClasses.LocExtensionHeaderIcon);
        header.Add(icon);

        var label = new Label(title);
        label.AddToClassList(LocClasses.LocExtensionHeaderTitle);
        header.Add(label);
        return header;
    }

    public static VisualElement Row()
    {
        var row = new VisualElement();
        row.AddToClassList(LocClasses.LocExtensionRow);
        return row;
    }
}
