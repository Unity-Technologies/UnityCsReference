// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Commands.Sequence;
using Unity.Timeline.Foundation.Model;
using UnityEngine;

namespace Unity.Timeline.Foundation.ViewModel
{
    class RippleTrimBehaviour : TrimBehaviour
    {
        CutList m_Source;
        DiscreteTime m_InitialTime;

        protected override void Begin(Item item)
        {
            m_Source = item.parent.GetCutList_Internal();
            m_InitialTime = location == Location.Start ? item.start : item.end;
        }

        protected override void Trim(DiscreteTime requestedTime)
        {
            CutList.Iterator itr = m_Source.IteratorAtId(itemToTrim.ID);
            var editor = new CutList.Editor(m_Source);

            DiscreteTime deltaTime = requestedTime - m_InitialTime;
            DiscreteTime requestedDuration = itr.Current.trimmedDuration + deltaTime;
            DiscreteTime minDuration = itr.Current.visibleDuration - itr.Current.trimmedDuration;
            DiscreteTime effectiveDuration = requestedDuration.Clamp(minDuration, DiscreteTime.MaxValue);
            editor.ChangeDuration(itr, effectiveDuration);

            viewModel.Dispatch(new SetTrackContents(itemToTrim.parent, editor.Finish()));
        }

        public override IReadOnlyList<Item> GetManipulatedItems()
        {
            var items = new List<Item>() { itemToTrim };
            Item nextItem = itemToTrim.Next();

            while (nextItem != Item.Invalid)
            {
                items.Add(nextItem);
                nextItem = nextItem.Next();
            }

            return items;
        }
    }
}
