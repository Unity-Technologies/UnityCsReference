// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Search;
using UnityEngine;
using Object = UnityEngine.Object;
using SService = UnityEditor.Search.SearchService;

namespace UnityEditor.Presets
{
    class PresetContext
    {
        public readonly Object Target;
        public readonly Object[] Targets;
        public readonly Preset[] Presets;
        public readonly SerializedProperty PresetProperty;
        public readonly PresetType PresetType;
        public readonly Preset CurrentSelection;
        public readonly bool CreateNewAllowed;
        public readonly bool RevertOnNullSelection;

        public PresetContext(Object[] targets, bool createNewAllowed) : this(targets, null, createNewAllowed) {}

        public PresetContext(Object[] targets, Preset currentSelection, bool createNewAllowed)
        {
            Targets = targets.Where(t => t != null).ToArray();
            Target = Targets.FirstOrDefault();
            Presets = Targets.Select(t => new Preset(t)).ToArray();
            PresetProperty = null;
            PresetType = new PresetType(Target);
            CurrentSelection = currentSelection;
            CreateNewAllowed = createNewAllowed;
            RevertOnNullSelection = true;
        }

        public PresetContext(PresetType presetType, Preset currentSelection, SerializedProperty presetProperty, bool createNewAllowed)
        {
            Target = null;
            Targets = Array.Empty<Object>();
            Presets = Array.Empty<Preset>();
            PresetProperty = presetProperty;
            PresetType = presetType;
            CurrentSelection = currentSelection;
            CreateNewAllowed = createNewAllowed;
            RevertOnNullSelection = false;
        }
    }

    class PresetSearchProvider
    {
        private const string k_ProviderId = "presets_provider";
        public static readonly string CreateItemID = GUID.Generate().ToString();

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            var QE = new QueryEngine<Preset>(new QueryValidationOptions() { validateFilters = true, skipUnknownFilters = true });
            QE.SetFilter("t", GetType, new[] {":"}).AddOrUpdatePropositionData(category: "Presets", label: "Preset Type", help: "Filter the presets by the type of the target object.");
            QE.SetFilter<string>("prop", GetProperty, new []{":"}).AddOrUpdatePropositionData(category: "Presets", label: "Preset Property", help: "Filter presets with a specific property.");
            QE.SetFilter<string>("exclude", GetExcludedProperty, new []{":"}).AddOrUpdatePropositionData(category: "Presets", label: "Excluded Property", help: "Filter the presets with a specific excluded property.");
            QE.SetSearchDataCallback(SearchWords, StringComparison.OrdinalIgnoreCase);
            SearchValue.SetupEngine(QE);

            return new SearchProvider(k_ProviderId, "Presets", (context, provider) => FetchItems(context, provider, QE))
            {
                filterId = "preset:",
                showDetails = true,
                showDetailsOptions = ShowDetailsOptions.Inspector | ShowDetailsOptions.Actions,
                priority = -10,
                isExplicitProvider = true, // yield results only if explicitly invoked
                active = false, // not active by default
                fetchPropositions = (context, options) => FetchPropositions(context, options, QE)
            };
        }

        internal static SearchProvider CreateProvider(PresetContext presetContext)
        {
            return new SearchProvider(k_ProviderId, "Presets", (context, provider) => FetchItems(context, provider, presetContext))
            {
                filterId = "preset:",
                showDetails = true,
                showDetailsOptions = ShowDetailsOptions.Inspector | ShowDetailsOptions.Actions,
                priority = -10,
                isExplicitProvider = true, // yield results only if explicitly invoked
                active = false, // not active by default
            };
        }

        static IEnumerable<SearchItem> FetchItems(SearchContext context, SearchProvider provider, QueryEngine<Preset> QE)
        {
            var query = QE.ParseQuery(context.searchQuery);
            if (!query.valid)
            {
                context.AddSearchQueryErrors(query.errors.Select(e => new SearchQueryError(e, context, provider)));
                yield break;
            }

            var assetProvider = SService.GetProvider("adb");
            using (var assetContext = SService.CreateContext(assetProvider, $"t:{nameof(Preset)}"))
            using (var results = SService.Request(assetContext))
            {
                foreach (var presetAsset in results)
                {
                    var preset = presetAsset.ToObject<Preset>();
                    if (preset == null)
                    {
                        yield return null;
                        continue;
                    }

                    if (query.Test(preset))
                        yield return presetAsset;
                    else
                        yield return null;
                }
            }
        }

        static IEnumerable<SearchItem> FetchItems(SearchContext context, SearchProvider provider, PresetContext presetContext)
        {
            if (presetContext.CreateNewAllowed)
            {
                var presetType = presetContext.PresetType;
                yield return provider.CreateItem(context, CreateItemID, int.MinValue + 2, GetNewPresetString(presetType), null, presetType.GetIcon(), null);
            }

            var assetProvider = SService.GetProvider("adb");
            using (var assetContext = SService.CreateContext(assetProvider, $"t:{nameof(Preset)} {context.searchQuery}"))
            using (var results = SService.Request(assetContext))
            {
                foreach (var presetAsset in results)
                {
                    yield return presetAsset;
                }
            }
        }

        static string GetNewPresetString(PresetType presetType)
        {
            var targetTypeName = presetType.GetManagedTypeName();
            if (!string.IsNullOrEmpty(targetTypeName))
            {
                targetTypeName = targetTypeName.Substring(targetTypeName.LastIndexOf('.') + 1);
                return $"<b>Create New {targetTypeName} Preset...</b>";
            }

            return $"<b>Create New Preset...</b>";
        }

        static IEnumerable<SearchProposition> FetchPropositions(SearchContext context, SearchPropositionOptions options, QueryEngine<Preset> QE)
        {
            if (!options.flags.HasAny(SearchPropositionFlags.QueryBuilder))
                yield break;

            foreach (var proposition in QE.GetPropositions())
                yield return proposition;
        }

        static string GetType(Preset p)
        {
            return p.GetTargetTypeName();
        }

        static bool GetProperty(Preset p, string op, string value)
        {
            foreach (var prop in p.PropertyModifications)
            {
                if (prop.propertyPath.Contains(value, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        static bool GetExcludedProperty(Preset p, string op, string value)
        {
            foreach (var prop in p.excludedProperties)
            {
                if (prop.Contains(value, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        static IEnumerable<string> SearchWords(Preset p)
        {
            yield return p.name;
            yield return p.GetTargetTypeName();
        }
    }
}
