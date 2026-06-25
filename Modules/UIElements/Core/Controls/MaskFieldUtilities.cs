// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    // Non-generic mask math shared by every BaseMaskField<TChoice> value type. The mask is always
    // an int regardless of TChoice, so this logic is emitted once instead of specialized per type.
    static class MaskFieldUtilities
    {
        internal static bool IsPowerOf2(int itemIndex)
        {
            return (itemIndex & (itemIndex - 1)) == 0;
        }

        internal static int ComputeFullChoiceMask(List<string> userChoices, List<int> userChoicesMasks)
        {
            if (userChoices.Count == 0)
                return 0;

            if ((userChoicesMasks != null) && (userChoicesMasks.Count == userChoices.Count))
            {
                if (userChoices.Count >= (sizeof(int) * 8))
                    return ~0;

                int mask = 0;
                foreach (int itemMask in userChoicesMasks)
                {
                    if (itemMask == ~0)
                        continue;
                    mask |= itemMask;
                }
                return mask;
            }

            if (userChoices.Count >= (sizeof(int) * 8))
                return ~0;
            return (1 << userChoices.Count) - 1;
        }

        internal static int UpdateMaskIfEverything(int currentMask, int fullChoiceMask)
        {
            var newMask = currentMask;
            if (fullChoiceMask != 0)
            {
                if ((currentMask & fullChoiceMask) == fullChoiceMask)
                    newMask = ~0;
                else
                    newMask &= fullChoiceMask;
            }
            return newMask;
        }

        internal static int GetMaskValueOfItem(string item, List<string> choices, List<string> userChoices,
            List<int> userChoicesMasks, int totalIndex)
        {
            int maskValue;
            var indexOfItem = choices.IndexOf(item);
            switch (indexOfItem)
            {
                case 0: // Nothing
                    maskValue = 0;
                    break;
                case 1: // Everything
                    maskValue = ~0;
                    break;
                default: // All others
                    if (indexOfItem > 0)
                    {
                        if ((userChoicesMasks != null) && (userChoicesMasks.Count == userChoices.Count))
                            maskValue = userChoicesMasks[(indexOfItem - totalIndex)];
                        else
                            maskValue = 1 << (indexOfItem - totalIndex);
                    }
                    else
                    {
                        maskValue = 0;
                    }
                    break;
            }
            return maskValue;
        }

        internal static string GetDisplayedValue(int itemIndex, List<string> choices, List<int> userChoicesMasks,
            int totalIndex, Func<string> getMixedString)
        {
            var newValueToShowUser = "";

            switch (itemIndex)
            {
                case 0:
                    newValueToShowUser = choices[0];
                    break;

                case ~0:
                    newValueToShowUser = choices[1];
                    break;

                default:
                    if (IsPowerOf2(itemIndex))
                    {
                        var indexOfValue = 0;
                        if (userChoicesMasks != null)
                        {
                            foreach (int itemMask in userChoicesMasks)
                            {
                                if (itemMask != ~0 && ((itemMask & itemIndex) == itemIndex))
                                {
                                    indexOfValue = userChoicesMasks.IndexOf(itemMask);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            while ((1 << indexOfValue) != itemIndex)
                            {
                                indexOfValue++;
                            }
                        }

                        // To get past the Nothing + Everything choices.
                        indexOfValue += totalIndex;
                        if (indexOfValue < choices.Count)
                        {
                            newValueToShowUser = choices[indexOfValue];
                        }
                    }
                    else
                    {
                        if (userChoicesMasks != null)
                        {
                            for (int i = 0; i < userChoicesMasks.Count; i++)
                            {
                                var itemMask = userChoicesMasks[i];
                                if (itemMask == itemIndex)
                                {
                                    var index = i + totalIndex;
                                    newValueToShowUser = choices[index];
                                    break;
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(newValueToShowUser))
                        {
                            newValueToShowUser = getMixedString();
                        }
                    }
                    break;
            }
            return newValueToShowUser;
        }
    }
}
