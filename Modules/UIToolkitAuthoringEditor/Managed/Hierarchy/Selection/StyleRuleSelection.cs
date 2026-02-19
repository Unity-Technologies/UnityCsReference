// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[CreateAssetMenu]
internal class StyleRuleSelection : UISelectionObject
{
    public static readonly BindingId StyleRuleProperty = nameof(StyleRule);

    StyleRule m_StyleRule;

    [CreateProperty]
    public StyleRule StyleRule
    {
        get => m_StyleRule;
        set
        {
            if (m_StyleRule == value)
                return;
            m_StyleRule = value;
            Notify(StyleRuleProperty);
        }
    }
}
