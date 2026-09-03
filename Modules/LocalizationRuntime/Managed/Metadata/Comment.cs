// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.Localization;

/// <summary>
/// Stores a free-text author comment that can be attached to any localization target.
/// </summary>
/// <remarks>
/// A comment can hold any information, but it is most useful for giving translators context about an entry. It implements
/// <see cref="IMetadata"/>, so it is stored in a <see cref="MetadataCollection"/>. The <see cref="MetadataAttribute"/> on
/// this type allows it on every target and limits each target to a single comment.
/// </remarks>
/// <example>
/// <para>Attach a comment to a metadata collection.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Metadata/CommentExample.cs"/>
/// </example>
/// <seealso cref="IMetadata"/>
/// <seealso cref="MetadataCollection"/>
/// <seealso cref="MetadataAttribute"/>
/// <seealso cref="ExcludeEntryFromEditorExport"/>
[Metadata(AllowedTypes = MetadataType.All, MenuItem = "Comment", AllowMultiple = false)]
[Serializable]
public class Comment : IMetadata
{
    [SerializeField, TextArea] string m_CommentText;

    /// <summary>
    /// The comment text.
    /// </summary>
    public string CommentText
    {
        get => m_CommentText;
        set => m_CommentText = value;
    }
}
