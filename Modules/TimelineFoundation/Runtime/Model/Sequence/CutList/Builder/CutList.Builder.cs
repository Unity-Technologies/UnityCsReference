// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.Model.Internals;

namespace Unity.Timeline.Foundation.Model
{
    partial class CutList
    {
        public class Builder
        {
            CutListData m_CutListData;

            public Builder()
            {
                m_CutListData = CutListData.New();
            }

            public Builder Add(in ItemBuilder itemBuilder)
            {
                ItemData itemData = itemBuilder.GetItemData_Internal();
                m_CutListData.items.Add(itemData);
                m_CutListData.duration += itemData.range.duration;
                return this;
            }

            public CutList Finish()
            {
                var cutList = new CutList(m_CutListData);
                m_CutListData = new CutListData();
                return cutList;
            }
        }
    }
}
