// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    internal static class UxmlUtility
    {
        internal static List<string> ParseStringListAttribute(string itemList)
        {
            if (string.IsNullOrEmpty(itemList.Trim()))
                return null;

            // Here the choices is comma separated in the string...
            var items = itemList.Split(',');

            if (items.Length != 0)
            {
                var result = new List<string>();
                foreach (var item in items)
                {
                    result.Add(item.Trim());
                }

                return result;
            }

            return null;
        }
    }
}
