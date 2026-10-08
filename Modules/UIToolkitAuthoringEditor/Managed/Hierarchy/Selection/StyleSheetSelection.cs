// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal class StyleSheetSelection : UISelectionObject
{
    public static readonly BindingId StyleSheetProperty = nameof(StyleSheet);

    // No manual page exists for this type, so hide the help button.
    static StyleSheetSelection() => Help.RegisterHelpFileName(typeof(StyleSheetSelection), "");

    StyleSheet m_StyleSheet;

    [CreateProperty]
    public StyleSheet StyleSheet
    {
        get => m_StyleSheet;
        set
        {
            if (m_StyleSheet == value)
                return;
            m_StyleSheet = value;
            Notify(StyleSheetProperty);
        }
    }
}
