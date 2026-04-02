// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace Unity.UIToolkit.Editor;

internal class Clipboard
{
    readonly List<VisualElement> m_CutElements = new();
    static string s_BatchModeCopyBuffer;

    public static string SystemCopyBuffer
    {
        get
        {
            if (Application.isBatchMode || !Application.isHumanControllingUs)
                return s_BatchModeCopyBuffer;
            return GUIUtility.systemCopyBuffer;
        }
        set
        {
            if (Application.isBatchMode || !Application.isHumanControllingUs)
                s_BatchModeCopyBuffer = value;
            else
                GUIUtility.systemCopyBuffer = value;
        }
    }

    public void SetCutElements(IReadOnlyList<VisualElement> cutElements)
    {
        m_CutElements.Clear();
        m_CutElements.AddRange(cutElements);
    }

    public IReadOnlyList<VisualElement> GetCutElements() => m_CutElements;

    public void ClearCutElements()
    {
        m_CutElements.Clear();
    }

    public void Dispose()
    {
        m_CutElements.Clear();
    }

    public static bool IsSystemCopyBufferUxml()
    {
        var buffer = SystemCopyBuffer;
        if (string.IsNullOrWhiteSpace(buffer))
            return false;

        var trimmedBuffer = buffer.Trim();
        return trimmedBuffer.StartsWith("<") && trimmedBuffer.EndsWith(">");
    }

    public static bool IsSystemCopyBufferUss()
    {
        var buffer = SystemCopyBuffer;
        if (string.IsNullOrWhiteSpace(buffer))
            return false;

        var trimmedBuffer = buffer.Trim();
        return trimmedBuffer.EndsWith("}");
    }
}
