// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;

namespace UnityEditor.Search
{
    namespace Providers
    {
        static class Calculator
        {
            internal static string type = "calculator";
            internal static string displayName = "Calculator";

            [SearchItemProvider]
            internal static SearchProvider CreateProvider()
            {
                return new SearchProvider(type, displayName)
                {
                    priority = 10,
                    filterId = "=",
                    isExplicitProvider = true,
                    fetchItems = (context, items, provider) =>
                    {
                        var expression = context.searchQuery;
                        if (Evaluate(context.searchQuery, out var result))
                            expression += " = " + result;

                        items.Add(provider.CreateItem(context, result.ToString(), "compute", expression, null, null));
                        return null;
                    },

                    fetchThumbnail = (item, context) => Icons.settings
                };
            }

            [SearchActionsProvider]
            internal static IEnumerable<SearchAction> ActionHandlers()
            {
                return new[]
                {
                    new SearchAction(type, "exec", null, "Compute") {
                        handler = (item) =>
                        {
                            if (Evaluate(item.context.searchQuery, out var result))
                            {
                                UnityEngine.Debug.Log(result);
                                EditorGUIUtility.systemCopyBuffer = result.ToString(CultureInfo.InvariantCulture);
                            }
                        }
                    }
                };
            }

            internal static bool Evaluate(string expression, out double result)
            {
                try
                {
                    return UnityEditor.ExpressionEvaluator.Evaluate(expression, out result);
                }
                catch (Exception)
                {
                    result = 0.0;
                    UnityEngine.Debug.LogError("Error while parsing: " + expression);
                    return false;
                }
            }
        }
    }
}
