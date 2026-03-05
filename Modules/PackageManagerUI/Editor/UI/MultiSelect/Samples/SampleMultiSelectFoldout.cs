// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal class SampleMultiSelectFoldout(SampleAction action = null) : MultiSelectFoldoutBase<Sample, Sample>(action)
{
    protected override MultiSelectItemBase<Sample> CreateMultiSelectItem(Sample item)
    {
        return new SampleMultiSelectItem(item);
    }
}

