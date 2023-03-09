// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ItemLibrary.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Helper class providing Item Library related functionality in GraphTools Foundation.
    /// </summary>
    static class ItemLibraryService
    {
        public static class Usage
        {
            public const string CreateNode = "create-node";
            public const string Values = "values";
            public const string Types = "types";
        }

        static readonly SimpleLibraryAdapter k_TypeAdapter = new SimpleLibraryAdapter("Pick a type");

        public static readonly Comparison<ItemLibraryItem> TypeComparison = (x, y) =>
        {
            return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
        };

        const string k_DefaultNodeStatusText = "'Double-click' or hit 'Enter' to add a node.";
        const string k_DefaultTypeStatusText = "'Double-click' or hit 'Enter' to select a type.";
        const string k_DefaultValueStatusText = "'Double-click' or hit 'Enter' to select a value.";

        /// <summary>
        /// Use the Item Library to search for items.
        /// </summary>
        /// <param name="view">The <see cref="GraphView"/> in which to show the window.</param>
        /// <param name="position">The position in hostWindow coordinates.</param>
        /// <param name="callback">The function called when a selection is made.</param>
        /// <param name="dbs">The <see cref="ItemLibraryDatabaseBase"/> that contains the items to be displayed.</param>
        /// <param name="filter">The search filter.</param>
        /// <param name="adapter">The <see cref="ItemLibraryAdapter"/>.</param>
        /// <param name="usage">The usage string used to identify the <see cref="ItemLibraryLibrary_Internal"/> use.</param>
        /// <param name="sourcePort">The port from which the library is created, if any.</param>
        internal static ItemLibraryWindow ShowDatabases_Internal(GraphView view,
            Vector2 position,
            Action<ItemLibraryItem> callback,
            IEnumerable<ItemLibraryDatabaseBase> dbs,
            ItemLibraryFilter filter,
            IItemLibraryAdapter adapter,
            string usage,
            PortModel sourcePort = null)
        {
            var preferences = view.GraphTool.Preferences;
            var librarySize = preferences.GetItemLibrarySize(usage);
            var rect = new Rect(position, librarySize.Size);

            if (adapter is IGraphElementLibraryAdapter graphAdapter)
                graphAdapter.SetHostGraphView(view);

            void OnItemSelectFunc(ItemLibraryItem item)
            {
                callback?.Invoke(item);
            }

            var library = new ItemLibraryLibrary_Internal(dbs, adapter, filter, usage, sourcePort);

            var window = library.Show(view.Window, rect);
            window.CloseOnFocusLost = !(preferences?.GetBool( BoolPref.ItemLibraryStaysOpenOnBlur) ?? false);
            ListenToResize(preferences, usage, window);
            window.itemChosen += OnItemSelectFunc;
            window.StatusBarText = k_DefaultNodeStatusText;
            return window;
        }

        static void ListenToResize(Preferences preferences, string usage, ItemLibraryWindow window)
        {
            var resizer = window.rootVisualElement.Q("windowResizer");
            var rightPanel = window.rootVisualElement.Q("windowDetailsVisualContainer");
            var leftPanel = window.rootVisualElement.Q("libraryViewContainer");

            if (resizer != null)
            {
                EventCallback<GeometryChangedEvent> callback = _ =>
                {
                    float ratio = 1.0f;
                    if (rightPanel != null && leftPanel != null)
                        ratio = rightPanel.resolvedStyle.flexGrow / leftPanel.resolvedStyle.flexGrow;

                    preferences.SetItemLibrarySize(usage ?? "", window.position.size, ratio);
                };

                window.rootVisualElement.RegisterCallback(callback);
                leftPanel?.RegisterCallback(callback);
            }
        }

        public static void ShowInputToGraphNodes(Stencil stencil, GraphView view,
            IReadOnlyList<PortModel> portModels, Vector2 worldPosition, Action<ItemLibraryItem> callback)
        {
            var graphModel = view.GraphModel;
            var toolName = view.GraphTool.Name;
            var adapterTitle = "Add an input node";
            var adapter = stencil.GetItemLibraryAdapter(graphModel, adapterTitle, toolName, portModels);
            var filter = stencil.GetLibraryFilterProvider()?.GetInputToGraphFilter(portModels);
            var dbProvider = stencil.GetItemDatabaseProvider();

            if (dbProvider == null)
                return;

            var dbs = dbProvider.GetGraphElementsDatabases(graphModel)
                .Concat(dbProvider.GetGraphVariablesDatabases(graphModel))
                .Concat(dbProvider.GetDynamicDatabases(portModels))
                .ToList();

            ShowDatabases_Internal(view, worldPosition, callback, dbs, filter, adapter, Usage.CreateNode, portModels.FirstOrDefault());
        }

        public static void ShowOutputToGraphNodes(Stencil stencil, GraphView view,
            IReadOnlyList<PortModel> portModels, Vector2 worldPosition, Action<ItemLibraryItem> callback)
        {
            var graphModel = view.GraphModel;
            var toolName = view.GraphTool.Name;
            var adapterTitle = $"Choose an action for {portModels.First().DataTypeHandle.GetMetadata(stencil).FriendlyName}";
            var adapter = stencil.GetItemLibraryAdapter(graphModel, adapterTitle, toolName, portModels);
            var filter = stencil.GetLibraryFilterProvider()?.GetOutputToGraphFilter(portModels);
            var dbProvider = stencil.GetItemDatabaseProvider();

            if (dbProvider == null)
                return;

            var dbs = dbProvider.GetGraphElementsDatabases(graphModel).ToList();
            ShowDatabases_Internal(view, worldPosition, callback, dbs, filter, adapter, Usage.CreateNode, portModels.FirstOrDefault());
        }

        public static void ShowNodesForWire(Stencil stencil, GraphView view,
            WireModel wireModel, Vector2 worldPosition, Action<ItemLibraryItem> callback)
        {
            var graphModel = view.GraphModel;
            var toolName = view.GraphTool.Name;
            var adapter = stencil.GetItemLibraryAdapter(graphModel, "Insert Node", toolName);
            var filter = stencil.GetLibraryFilterProvider()?.GetWireFilter(wireModel);
            var dbProvider = stencil.GetItemDatabaseProvider();

            if (dbProvider == null)
                return;

            var dbs = dbProvider.GetGraphElementsDatabases(graphModel).ToList();
            ShowDatabases_Internal(view, worldPosition, callback, dbs, filter, adapter, Usage.CreateNode);
        }

        public static void ShowGraphNodes(Stencil stencil, GraphView view, Vector2 worldPosition, Action<ItemLibraryItem> callback)
        {
            var graphModel = view.GraphModel;
            var toolName = view.GraphTool.Name;
            var adapter = stencil.GetItemLibraryAdapter(graphModel, "Add a graph node", toolName);
            var filter = stencil.GetLibraryFilterProvider()?.GetGraphFilter();
            var dbProvider = stencil.GetItemDatabaseProvider();

            if (dbProvider == null)
                return;

            var dbs = dbProvider.GetGraphElementsDatabases(view.GraphModel)
                .Concat(dbProvider.GetDynamicDatabases((PortModel)null))
                .ToList();

            ShowDatabases_Internal(view, worldPosition, callback, dbs, filter, adapter, Usage.CreateNode);
        }

        public static ItemLibraryWindow FindInGraph(
            EditorWindow host,
            GraphModel graph,
            Action<FindInGraphAdapter.FindItem> highlightDelegate,
            Action<FindInGraphAdapter.FindItem> selectionDelegate
        )
        {
            var items = graph.NodeModels
                .Where(x => x is IHasTitle titled && !string.IsNullOrEmpty(titled.Title))
                .Select(x => MakeFindItems(x, x.Title))
                .ToList();

            var database = new ItemLibraryDatabase(items);
            var library = new ItemLibraryLibrary_Internal(database, new FindInGraphAdapter(highlightDelegate));
            var position = new Vector2(host.rootVisualElement.layout.center.x, 0);

            var window = library.Show(host, position);
            window.itemChosen += item => selectionDelegate(item as FindInGraphAdapter.FindItem);
            window.StatusBarText = ""; // no specific action to be document when user selects an element.
            return window;
        }

        internal static ItemLibraryWindow ShowEnumValues_Internal(string title, Type enumType, Vector2 position, Action<Enum, int> callback)
        {
            var items = Enum.GetValues(enumType)
                .Cast<Enum>()
                .Select(v => new EnumValuesAdapter_Internal.EnumValueItem(v) as ItemLibraryItem)
                .ToList();
            var database = new ItemLibraryDatabase(items);
            var library = new ItemLibraryLibrary_Internal(database, new EnumValuesAdapter_Internal(title), context: "Enum" + enumType.FullName);

            var window = library.Show(EditorWindow.focusedWindow, position);
            window.StatusBarText = k_DefaultValueStatusText;
            window.itemChosen += item => callback(((EnumValuesAdapter_Internal.EnumValueItem)item)?.value, 0);
            return window;
        }

        public static ItemLibraryWindow ShowValues(Preferences preferences, string title, IEnumerable<string> values, Vector2 position,
            Action<string> callback)
        {
            var librarySize = preferences.GetItemLibrarySize(Usage.Values);
            var rect = new Rect(position, librarySize.Size);

            var items = values.Select(v => new ItemLibraryItem(v)).ToList();
            var database = new ItemLibraryDatabase(items);
            var adapter = new SimpleLibraryAdapter(title);
            var library = new ItemLibraryLibrary_Internal(database, adapter, context: Usage.Values);

            var window = library.Show(EditorWindow.focusedWindow, rect);
            window.StatusBarText = k_DefaultValueStatusText;
            window.itemChosen += item => callback(item?.Name);
            ListenToResize(preferences, Usage.Values, window);
            return window;
        }

        public static ItemLibraryWindow ShowVariableTypes(Stencil stencil, Preferences preferences, Vector2 position, Action<TypeHandle, int> callback)
        {
            var databases = stencil.GetItemDatabaseProvider()?.GetVariableTypesDatabases();
            return databases != null ? ShowTypes_Internal(preferences, databases, position, callback) : null;
        }

        internal static ItemLibraryWindow ShowTypes_Internal(Preferences preferences, IEnumerable<ItemLibraryDatabaseBase> databases, Vector2 position, Action<TypeHandle, int> callback)
        {
            var librarySize = preferences.GetItemLibrarySize(Usage.Types);
            var rect = new Rect(position, librarySize.Size);

            var library = new ItemLibraryLibrary_Internal(databases, k_TypeAdapter, context: Usage.Types);

            var window = library.Show(EditorWindow.focusedWindow, rect);
            ListenToResize(preferences, Usage.Types, window);
            window.itemChosen += item =>
            {
                if (item is TypeLibraryItem typeItem)
                    callback(typeItem.Type, 0);
            };
            window.StatusBarText = k_DefaultTypeStatusText;
            return window;
        }

        static ItemLibraryItem MakeFindItems(AbstractNodeModel node, string title)
        {
            switch (node)
            {
                // TODO virtual property in NodeModel formatting what's displayed in the find hostWindow
                case ConstantNodeModel cnm:
                {
                    var nodeTitle = cnm.Type == typeof(string) ? $"\"{title}\"" : title;
                    title = $"Const {cnm.Type.Name} {nodeTitle}";
                    break;
                }
            }

            return new FindInGraphAdapter.FindItem(title, node);
        }
    }
}
