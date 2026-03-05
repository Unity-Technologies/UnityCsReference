// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal class SampleMultiSelectItem : MultiSelectItemBase<Sample>
{
    public SampleMultiSelectItem(Sample item) : base(item)
    {
        m_TypeIcon.AddToClassList("sampleIcon");
        m_NameLabel.text = m_Item.displayName ?? string.Empty;
    }
}
