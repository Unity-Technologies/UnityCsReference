// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements;

internal class TextJobSystem
{
    // Used in tests
    internal UITKTextJobSystem m_UITKTextJobSystem;
    ATGTextJobSystem m_ATGTextJobSystem;
    internal void GenerateText(MeshGenerationContext mgc, TextElement textElement)
    {
        if (TextUtilities.IsAdvancedTextEnabledForElement(textElement))
        {
            m_ATGTextJobSystem ??= new ATGTextJobSystem();
            m_ATGTextJobSystem.GenerateText(mgc, textElement);
        }
        else
        {
            m_UITKTextJobSystem ??= new UITKTextJobSystem();
            m_UITKTextJobSystem.GenerateText(mgc, textElement);
        }
    }
}
