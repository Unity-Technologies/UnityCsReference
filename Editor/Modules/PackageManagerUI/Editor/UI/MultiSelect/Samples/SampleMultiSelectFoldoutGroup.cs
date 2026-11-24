// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal class SampleMultiSelectFoldoutGroup : MultiSelectFoldoutGroupBase<Sample, Sample>
{
    public SampleMultiSelectFoldoutGroup(SampleAction mainAction, SampleAction cancelAction = null)
        : base(new SampleMultiSelectFoldout(mainAction), new SampleMultiSelectFoldout(cancelAction))
    {
    }

    public SampleMultiSelectFoldoutGroup(SampleMultiSelectFoldout main, SampleMultiSelectFoldout cancel) : base(main, cancel)
    {
    }

    protected override ActionState GetActionState(Sample item)
    {
        return mainAction.GetActionState(item, out _, out _);
    }
}
