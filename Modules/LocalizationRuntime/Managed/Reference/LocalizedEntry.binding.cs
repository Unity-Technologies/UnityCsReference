// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.Localization;

[UxmlObject]
public partial class LocalizedEntry<TEntry>
{
    [UxmlAttribute("entry")]
    internal TableEntryReference Entry
    {
        get => TableEntryReference;
        set => TableEntryReference = value;
    }
}
