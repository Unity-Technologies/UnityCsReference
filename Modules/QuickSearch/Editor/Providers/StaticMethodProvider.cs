// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEditor.Search.Providers
{
    static class StaticMethodProvider
    {
        private const string type = "static_methods";
        private const string displayName = "Static API";

        private static readonly string[] _ignoredAssemblies =
        {
            "^UnityScript$", "^System$", "^mscorlib$", "^netstandard$",
            "^System\\..*", "^nunit\\..*", "^Microsoft\\..*", "^Mono\\..*", "^SyntaxTree\\..*"
        };

        private static MethodInfo[] methods;

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new SearchProvider(type, displayName)
            {
                priority = 85,
                filterId = "api:",
                active = false,
                isExplicitProvider = true,
                fetchItems = (context, items, provider) => FetchItems(context, provider),
                fetchThumbnail = (item, context) => Icons.staticAPI
            };
        }

        private static IEnumerable<SearchItem> FetchItems(SearchContext context, SearchProvider provider)
        {
            // Cache all available static APIs
            if (methods == null)
                methods = FetchStaticAPIMethodInfo();

            var casePattern = context.searchQuery.Replace(" ", "");
            var matches = new List<int>();
            foreach (var m in methods)
            {
                if (!m.IsPublic && !context.options.HasFlag(SearchFlags.WantsMore))
                    continue;
                long score = 0;
                var fullName = $"{m.DeclaringType.FullName}.{m.Name}";
                var fullNameLower = fullName.ToLowerInvariant();
                if (FuzzySearch.FuzzyMatch(casePattern, fullNameLower, ref score, matches) && score > 300)
                {
                    var visibilityString = m.IsPublic ? string.Empty : "(Non Public)";
                    yield return provider.CreateItem(context, m.Name, m.IsPublic ? ~(int)score - 999 : ~(int)score, m.Name, $"{fullName} {visibilityString}", null, m);
                }
                else
                    yield return null;
            }
        }

        private static MethodInfo[] FetchStaticAPIMethodInfo()
        {
            // Note: since 23.2 an internal api causes a hard crash when fetching for it.
            // bool isDevBuild = Unsupported.IsDeveloperBuild();
            return AppDomain.CurrentDomain.GetAllStaticMethods(false);
        }

        private static MethodInfo[] GetAllStaticMethods(this AppDomain aAppDomain, bool showInternalAPIs)
        {
            var result = new List<MethodInfo>();
            var assemblies = aAppDomain.GetAssemblies();
            var bindingFlags = BindingFlags.Static | (showInternalAPIs ? BindingFlags.Public | BindingFlags.NonPublic : BindingFlags.Public) | BindingFlags.DeclaredOnly;
            foreach (var assembly in assemblies)
            {
                if (IsIgnoredAssembly(assembly.GetName()))
                    continue;
                var types = assembly.GetLoadableTypes();
                foreach (var type in types)
                {
                    var methods = type.GetMethods(bindingFlags);
                    foreach (var m in methods)
                    {
                        if (m.IsGenericMethod)
                            continue;

                        if (m.GetCustomAttribute<ObsoleteAttribute>() != null)
                            continue;

                        if (m.Name.Contains("Begin") || m.Name.Contains("End"))
                            continue;

                        if (m.GetParameters().Length == 0)
                            result.Add(m);
                    }
                }
            }
            return result.ToArray();
        }

        private static bool IsIgnoredAssembly(AssemblyName assemblyName)
        {
            var name = assemblyName.Name;
            return _ignoredAssemblies.Any(candidate => Regex.IsMatch(name, candidate));
        }

        private static void LogResult(object result)
        {
            if (result == null)
                return;

            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, result as UnityEngine.Object, result.ToString());
        }

        [SearchActionsProvider]
        internal static IEnumerable<SearchAction> ActionHandlers()
        {
            return new[]
            {
                new SearchAction(type, "exec", null, "Execute method", (items) =>
                {
                    foreach (var item in items)
                    {
                        var m = item.data as MethodInfo;
                        if (m == null)
                            return;
                        var result = m.Invoke(null, null);
                        if (result == null)
                            return;
                        if (result is string || !(result is IEnumerable list))
                        {
                            LogResult(result);
                            EditorGUIUtility.systemCopyBuffer = result.ToString();
                        }
                        else
                        {
                            foreach (var e in list)
                                LogResult(e);
                        }
                    }
                })
            };
        }
    }
}
