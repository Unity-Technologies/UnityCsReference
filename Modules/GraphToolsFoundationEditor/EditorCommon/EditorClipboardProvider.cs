// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor;

/// <summary>
/// A clipboard provider that uses <see cref="EditorGUIUtility.systemCopyBuffer"/> and JSON serialization.
/// </summary>
class EditorClipboardProvider : ClipboardProvider
{
    /// <inheritdoc />
    public override string Clipboard
    {
        get => EditorGUIUtility.systemCopyBuffer;
        set => EditorGUIUtility.systemCopyBuffer = value;
    }

    /// <inheritdoc />
    public override void SerializeDataToClipboard(CopyPasteData copyPasteData)
    {
        var serializedData = JsonUtility.ToJson(copyPasteData, true);
        if (!string.IsNullOrEmpty(serializedData))
        {
            Clipboard = $"{k_SerializedDataMimeType} {serializedData}";
        }
    }

    /// <inheritdoc />
    public override CopyPasteData DeserializeDataFromClipboard()
    {
        var data = Clipboard;
        if (data.StartsWith(k_SerializedDataMimeType))
        {
            data = data[(k_SerializedDataMimeType.Length + 1)..];
        }

        return JsonUtility.FromJson<CopyPasteData>(data);
    }

    /// <inheritdoc />
    public override bool CanDeserializeDataFromClipboard()
    {
        return Clipboard.StartsWith(k_SerializedDataMimeType);
    }

    /// <inheritdoc />
    public override CopyPasteData Duplicate(CopyPasteData copyPasteData)
    {
        var serializedData = JsonUtility.ToJson(copyPasteData, false);
        return JsonUtility.FromJson<CopyPasteData>(serializedData);
    }
}
