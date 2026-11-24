// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal abstract class MultiSelectFoldoutGroupBase<SingleType, BulkType> : IMultiSelectFoldoutElement<BulkType>
{
    public MultiSelectFoldoutBase<SingleType, BulkType> mainFoldout { get; }
    public MultiSelectFoldoutBase<SingleType, BulkType> inProgressFoldout { get; }

    public ActionBase<SingleType, BulkType> mainAction => mainFoldout.action;
    public ActionBase<SingleType, BulkType> cancelAction => inProgressFoldout.action;

    public MultiSelectFoldoutGroupBase(MultiSelectFoldoutBase<SingleType, BulkType> mainFoldout, MultiSelectFoldoutBase<SingleType, BulkType> inProgressFoldout)
    {
        this.mainFoldout = mainFoldout;
        this.inProgressFoldout = inProgressFoldout;

        if (mainFoldout.action != null)
            mainFoldout.action.onActionTriggered += () =>
            {
                if (mainFoldout.isExpanded && !inProgressFoldout.isExpanded)
                    inProgressFoldout.SetExpanded(true);
            };
    }
    public virtual void Refresh()
    {
        mainFoldout.Refresh();
        inProgressFoldout.Refresh();
    }

    public virtual bool AddItem(BulkType item)
    {
        var state = GetActionState(item);
        if (state.HasFlag(ActionState.InProgress))
            inProgressFoldout.AddItem(item);
        else if (state == ActionState.Visible || state.HasFlag(ActionState.DisabledTemporarily))
            mainFoldout.AddItem(item);
        else
            return false;
        return true;
    }

    protected abstract ActionState GetActionState(BulkType item);

    public void ClearItems()
    {
        mainFoldout.ClearItems();
        inProgressFoldout.ClearItems();
    }
}
