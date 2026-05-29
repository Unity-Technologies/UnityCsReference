// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.UIToolkit.Editor;

class RequestSelectionQuery<T> : Command<RequestSelectionQuery<T>>
    where T : class
{
    public static RequestSelectionQuery<T> GetPooled(object source, T toSelect)
    {
        var cmd = GetPooled();
        cmd.Source = source;
        cmd.ToSelect = toSelect;
        return cmd;
    }

    public T ToSelect { get; private set; }

    public override CommandCategory Category => CommandCategory.Selection;

    protected override void Init()
    {
        base.Init();
        ToSelect = null;
    }

    public override bool Validate() => ToSelect != null;

    public override CommandExecutionStatus Execute() => CommandExecutionStatus.Success;
}
