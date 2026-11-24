// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal class DisableConditionList<TItem>
{
    private readonly IDisableCondition<TItem>[] m_Conditions;

    public DisableConditionList(params IDisableCondition<TItem>[] conditions)
    {
        m_Conditions = conditions;
    }

    public IDisableCondition<TItem> GetActiveCondition(TItem item, out string tooltip)
    {
        foreach (var condition in m_Conditions)
            if (condition.IsActive(item, out tooltip))
                return condition;
        tooltip = null;
        return null;
    }
}
