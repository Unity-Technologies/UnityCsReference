// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Search;
using UnityEngine.Search;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    class BuilderTypeField : BaseField<string>
    {
        public new static readonly string ussClassName = "unity-builder-type-field";
        public static readonly string textUssClassName = ussClassName + "__text";
        public static readonly string buttonUssClassName = ussClassName + "__button";

        readonly Type m_BaseType;
        readonly Label m_TextElement;

        public BuilderTypeField(string label, Type baseType)
            : base(label, null)
        {
            m_BaseType = baseType;

            AddToClassList(ussClassName);

            m_TextElement = new Label { pickingMode = PickingMode.Ignore };
            m_TextElement.AddToClassList(textUssClassName);
            visualInput.Add(m_TextElement);

            var button = new Button(ShowSelector) { text = L10n.Tr("Select Type...") };
            button.AddToClassList(buttonUssClassName);
            visualInput.Add(button);
        }

        void ShowSelector()
        {
            var provider = new TypeSearchProvider(m_BaseType);
            var context = Search.SearchService.CreateContext(provider, "type:");
            var state = new SearchViewState(context)
            {
                title = "Type",
                queryBuilderEnabled = true,
                hideTabs = true,
                selectHandler = Select,
                flags = SearchViewFlags.TableView |
                        SearchViewFlags.DisableBuilderModeToggle |
                        SearchViewFlags.DisableInspectorPreview
            };

            var view = Search.SearchService.ShowPicker(state);
        }

        void Select(SearchItem item, bool cancelled)
        {
            var type = item.data as Type;
            this.value = UxmlUtility.TypeToString(type);
        }

        public override void SetValueWithoutNotify(string newValue)
        {
            base.SetValueWithoutNotify(newValue);
            UpdateLabel();
        }

        void UpdateLabel()
        {
            if (string.IsNullOrEmpty(value))
                m_TextElement.text = L10n.Tr("None");
            else
                m_TextElement.text = value;
        }
    }

    class TypeSearchProvider : SearchProvider
    {
        const string k_AssemblyToken = "asm";
        const string k_NameToken = "name";
        const string k_NamespaceToken = "ns";

        readonly Type m_BaseType;
        readonly HashSet<Assembly> m_Assemblies = new();
        readonly QueryEngine<Type> m_QueryEngine = new();

        public TypeSearchProvider(Type baseType)
            : base("type", "Type")
        {
            m_BaseType = baseType;

            // Propositions are used to provide the search filter options in the menu.
            fetchPropositions = FetchPropositions;

            // The actual items we search against.
            fetchItems = FetchItems;

            // The default table columns and the ones we show when reset is called.
            tableConfig = GetDefaultTableConfig;

            // The additional available columns for this search provider.
            fetchColumns = FetchColumns;

            // The searchable data is what we search against when just typing in the search field.
            m_QueryEngine.SetSearchDataCallback(GetSearchableData, StringComparison.OrdinalIgnoreCase);
            m_QueryEngine.AddFilter(k_AssemblyToken, o => o.Assembly.GetName().Name);
            m_QueryEngine.AddFilter(k_NameToken, o => o.Name);
            m_QueryEngine.AddFilter(k_NamespaceToken, o => o.Namespace);
        }

        IEnumerable<SearchProposition> FetchPropositions(SearchContext context, SearchPropositionOptions options)
        {
            yield return new SearchProposition(null, "Name", $"{k_NameToken}:", "Filter by type name.");
            yield return new SearchProposition(null, "Namespace", $"{k_NamespaceToken}:", "Filter by type namespace.");

            // We want to provide a list of all the assemblies that contain types derived from the base type.
            foreach (var asm in m_Assemblies)
            {
                var assemblyName = asm.GetName().Name;
                yield return new SearchProposition("Assembly", assemblyName, $"{k_AssemblyToken}={assemblyName}", "Filter by assembly name.");
            }
        }

        IEnumerator FetchItems(SearchContext context, List<SearchItem> items, SearchProvider provider)
        {
            if (context.empty)
                yield break;

            var query = m_QueryEngine.ParseQuery(context.searchQuery);
            if (!query.valid)
                yield break;

            var filteredObjects = query.Apply(GetSearchData());
            foreach (var t in filteredObjects)
            {
                yield return provider.CreateItem(context, t.AssemblyQualifiedName, t.Name, t.FullName, null, t);
            }
        }

        IEnumerable<Type> GetSearchData()
        {
            // Ignore UI Builder types
            var builderAssembly = GetType().Assembly;

            foreach (var t in GetTypesDerivedFrom(m_BaseType))
            {
                if (t.IsGenericType || t.Assembly == builderAssembly)
                    continue;

                m_Assemblies.Add(t.Assembly);
                yield return t;
            }
        }

        static IEnumerable<Type> GetTypesDerivedFrom(Type type)
        {
            if (type != typeof(object))
            {
                foreach (var t in TypeCache.GetTypesDerivedFrom(type))
                {
                    yield return t;
                }
            }
            else
            {
                // We need special handling for the System.Object type as TypeCache.GetTypesDerivedFrom(object) misses some types, such as primitives.
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    // Get all types in the assembly
                    foreach (var t in assembly.GetTypes())
                    {
                        yield return t;
                    }
                }
            }
        }

        static IEnumerable<string> GetSearchableData(Type t)
        {
            // The string that will be evaluated by default
            yield return t.AssemblyQualifiedName;
        }

        static SearchTable GetDefaultTableConfig(SearchContext context)
        {
            var defaultColumns = new List<SearchColumn>
            {
                new SearchColumn("Name", "label")
                {
                    width = 400
                }
            };
            defaultColumns.AddRange(FetchColumns(context, null));
            return new SearchTable("type", defaultColumns);
        }

        static IEnumerable<SearchColumn> FetchColumns(SearchContext context, IEnumerable<SearchItem> searchDatas)
        {
            // Note: The getter is serialized into the window so we need to use a method
            // instead of a lambda or it will break when the window is reloaded.
            // For the same reasons you should avoid renaming the methods or moving them around.

            yield return new SearchColumn("Namespace")
            {
                getter = GetNamespace,
                width = 250
            };
            yield return new SearchColumn("Assembly")
            {
                getter = GetAssemblyName,
                width = 250
            };
        }

        static object GetNamespace(SearchColumnEventArgs args)
        {
            if (!(args.item.data is Type t))
                return null;
            return t.Namespace;
        }

        static object GetAssemblyName(SearchColumnEventArgs args)
        {
            if (!(args.item.data is Type t))
                return null;
            return t.Assembly.GetName().Name;
        }
    }
}
