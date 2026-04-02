// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.IntegerTime;
using Unity.Timeline.Foundation.Common;

namespace Unity.Timeline.Foundation.Model
{
    static class CutListExtensions
    {
        /// <summary>
        /// Copies an item from one cutList to a new one.
        /// </summary>
        /// <param name="cutList">The source cutList</param>
        /// <param name="itemToExtract">Iterator for the item to extract</param>
        /// <returns></returns>
        public static CutList Extract(this CutList cutList, CutList.Iterator itemToExtract)
        {
            var builder = new CutList.Builder();
            if (cutList != null && itemToExtract.IsValid() && itemToExtract != cutList.GetIteratorAtEnd())
            {
                CutList.Item item = itemToExtract.Current;
                CutList.ItemBuilder itemBuilder = item;
                itemBuilder = itemBuilder.WithDuration(item.visibleDuration);
                itemBuilder = itemBuilder.WithEndTransition(UniqueID.Invalid, DiscreteTime.Zero);

                if (item.visibleStart > DiscreteTime.Zero)
                    builder.Add(CutList.ItemBuilder.Gap().WithDuration(item.visibleStart));
                builder.Add(itemBuilder);
            }

            return builder.Finish();
        }
    }
}
