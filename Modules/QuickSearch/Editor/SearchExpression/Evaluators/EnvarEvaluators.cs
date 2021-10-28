// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor.Utils;
using UnityEngine;

namespace UnityEditor.Search
{
    static partial class Evaluators
    {
        [Description("Returns the currently selected folder."), Category("Env")]
        [SearchExpressionEvaluator]
        public static IEnumerable<SearchItem> CurrentFolder(SearchExpressionContext c)
        {
            string currentSelectedPath = string.Empty;
            if (ProjectBrowser.s_LastInteractedProjectBrowser)
            {
                if (ProjectBrowser.s_LastInteractedProjectBrowser.IsTwoColumns())
                    currentSelectedPath = ProjectBrowser.s_LastInteractedProjectBrowser.GetActiveFolderPath() ?? string.Empty;
                else
                {
                    currentSelectedPath = ProjectBrowser.GetSelectedPath() ?? string.Empty;
                    var isFile = File.Exists(currentSelectedPath);
                    var isDirectory = Directory.Exists(currentSelectedPath);
                    if (!isDirectory && !isFile)
                        currentSelectedPath = string.Empty;
                    else if (isFile)
                    {
                        currentSelectedPath = Path.GetDirectoryName(currentSelectedPath) ?? string.Empty;
                    }
                }
            }

            if (!string.IsNullOrEmpty(currentSelectedPath))
                currentSelectedPath = currentSelectedPath.ConvertSeparatorsToUnity();
            yield return EvaluatorUtils.CreateItem(currentSelectedPath, c.ResolveAlias("CurrentFolder"));
        }

        [Description("Returns the name of the current project."), Category("Env")]
        [SearchExpressionEvaluator]
        public static IEnumerable<SearchItem> ProjectName(SearchExpressionContext c)
        {
            var desc = TaskEvaluatorManager.EvaluateMainThread(() => EditorApplication.GetApplicationTitleDescriptor());
            yield return EvaluatorUtils.CreateItem(desc.projectName ?? string.Empty, c.ResolveAlias("ProjectName"));
        }

        [Description("Returns the name of the currently opened scene."), Category("Env")]
        [SearchExpressionEvaluator]
        public static IEnumerable<SearchItem> SceneName(SearchExpressionContext c)
        {
            var desc = TaskEvaluatorManager.EvaluateMainThread(() => EditorApplication.GetApplicationTitleDescriptor());
            yield return EvaluatorUtils.CreateItem(desc.activeSceneName ?? string.Empty, c.ResolveAlias("SceneName"));
        }

        readonly struct SelectionResult
        {
            public readonly int instanceId;
            public readonly string assetPath;

            public SelectionResult(int instanceId, string assetPath)
            {
                this.instanceId = instanceId;
                this.assetPath = assetPath;
            }
        }

        [Description("Returns the current selection."), Category("Env")]
        [SearchExpressionEvaluator]
        public static IEnumerable<SearchItem> Selection(SearchExpressionContext c)
        {
            var selection = TaskEvaluatorManager.EvaluateMainThread(() =>
            {
                var instanceIds = UnityEditor.Selection.instanceIDs;
                return instanceIds.Select(id =>
                {
                    string assetPath = AssetDatabase.GetAssetPath(id);
                    return new SelectionResult(id, assetPath);
                }).ToList();
            });
            foreach (var selectionResult in selection)
            {
                if (string.IsNullOrEmpty(selectionResult.assetPath))
                    yield return EvaluatorUtils.CreateItem(selectionResult.instanceId, c.ResolveAlias("Selection"));
                else
                    yield return EvaluatorUtils.CreateItem(selectionResult.assetPath, c.ResolveAlias("Selection"));
            }
        }

        [Description("Returns the path to the game data folder."), Category("Env")]
        [SearchExpressionEvaluator]
        public static IEnumerable<SearchItem> DataPath(SearchExpressionContext c)
        {
            var dataPath = TaskEvaluatorManager.EvaluateMainThread(() => Application.dataPath);
            yield return EvaluatorUtils.CreateItem(dataPath ?? string.Empty, c.ResolveAlias("DataPath"));
        }

        static Dictionary<string, MethodInfo> s_EnvFunctions = null;
        static object s_EnvFunctionsLock = new object();
        [Description("Returns the value of one or more environment variables."), Category("Env")]
        [SearchExpressionEvaluator(SearchExpressionEvaluationHints.ImplicitArgsLiterals)]
        [SearchExpressionEvaluatorSignatureOverload( SearchExpressionType.Text | SearchExpressionType.Variadic)]
        public static IEnumerable<SearchItem> Env(SearchExpressionContext c)
        {
            lock (s_EnvFunctionsLock)
            {
                if (s_EnvFunctions == null)
                {
                    var searchExpressionEvaluators = TypeCache.GetMethodsWithAttribute<SearchExpressionEvaluatorAttribute>();
                    s_EnvFunctions = searchExpressionEvaluators.Where(mi =>
                    {
                        // Discard self
                        if (mi.Name == "Env")
                            return false;
                        var categories = mi.GetCustomAttributes<CategoryAttribute>();
                        return categories.Any(category => category.Category == "Env");
                    }).ToDictionary(mi => Utils.FastToLower(mi.Name));
                }

                string[] envNames = null;
                if (c.args.Length == 0)
                    envNames = s_EnvFunctions.Keys.ToArray();
                else
                    envNames = c.args.Select(exp => Utils.FastToLower(exp.innerText.ToString())).ToArray();

                foreach (var envName in envNames)
                {
                    if (!s_EnvFunctions.TryGetValue(envName, out var mi))
                    {
                        yield return null;
                        continue;
                    }
                    var searchItems = mi.Invoke(null, new object[] { c }) as IEnumerable<SearchItem>;
                    if (searchItems == null)
                    {
                        yield return null;
                        continue;
                    }
                    foreach (var searchItem in searchItems)
                        yield return searchItem;
                }
            }
        }
    }
}
