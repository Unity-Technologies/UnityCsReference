// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Object = UnityEngine.Object;

namespace UnityEditor.Search
{
    static partial class Evaluators
    {
        [Description("Returns the current objectfield value."), Category("Context")]
        [SearchExpressionEvaluator(SearchExpressionType.Selector, SearchExpressionType.AnyValue | SearchExpressionType.Optional)]
        static IEnumerable<SearchItem> CurrentObject(SearchExpressionContext c)
        {
            var aliasSelector = c.args.First();
            if (aliasSelector.types.HasNone(SearchExpressionType.Selector))
                yield break;

            var currentObject = c.search.runtimeContext?.currentObject;
            var item = SearchExpression.CreateItem(c.ResolveAlias("CurrentObject"), currentObject, string.Empty);
            var label = SelectorManager.SelectValue(item, c.search, aliasSelector.innerText.ToString());

            if (label != null)
            {
                item.value = label;
                yield return item;
                yield break;
            }

            if (c.args.Length <= 1)
                yield break;

            var defaultValueArg = c.args[1];
            var defaultValueItems = defaultValueArg.Execute(c);
            var firstItem = defaultValueItems.FirstOrDefault();
            if (firstItem == null)
                yield break;
            item.value = firstItem.value;
            yield return item;
        }

        [SearchSelector("name")]
        static object GetObjectName(SearchSelectorArgs args)
        {
            var item = args.current;
            var obj = item?.value as Object;
            return obj?.name;
        }

        [SearchSelector("path")]
        static object GetObjectPath(SearchSelectorArgs args)
        {
            var item = args.current;
            var obj = item?.value as Object;
            var instanceId = obj?.GetInstanceID();
            if (!instanceId.HasValue)
                return null;
            return AssetDatabase.GetAssetPath(instanceId.Value);
        }

        [SearchSelector("type")]
        static object GetObjectType(SearchSelectorArgs args)
        {
            var item = args.current;
            var obj = item?.value as Object;
            return obj?.GetType().Name;
        }
    }
}
