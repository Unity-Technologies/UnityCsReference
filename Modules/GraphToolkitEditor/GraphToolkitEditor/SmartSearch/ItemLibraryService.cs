// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.ItemLibrary.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Helper class providing Item Library related functionality in GraphTools Foundation.
    /// </summary>
    [UnityRestricted]
    internal static class ItemLibraryService
    {
        /// <summary>
        /// Defines the usage context for the item library.
        /// </summary>
        /// <remarks>
        /// The 'Usage' class defines the context for how the item library is used. It specifies whether the library is intended for
        /// nodes, values, or types. These constants help categorize the item library’s functionality, which makes it easier to manage
        /// and configure based on the specific context in which the library is being used.
        /// </remarks>
        [UnityRestricted]
        internal static class Usage
        {
            /// <summary>
            /// Specifies whether the item library is used for node creation.
            /// </summary>
            public const string CreateNode = "create-node";
            /// <summary>
            /// Specifies whether the item library is used for values.
            /// </summary>
            public const string Values = "values";
            /// <summary>
            /// Specifies whether the item library is used for types.
            /// </summary>
            public const string Types = "types";
        }

        public static readonly Comparison<ItemLibraryItem> TypeComparison = (x, y) =>
        {
            return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
        };

        const string k_DefaultNodeStatusText = "'Double-click' or hit 'Enter' to add a node.";
        const string k_DefaultTypeStatusText = "'Double-click' or hit 'Enter' to select a type.";
        const string k_DefaultValueStatusText = "'Double-click' or hit 'Enter' to select a value.";

        const string k_GroupName = "Group";

        /// <summary>
        /// Use the Item Library to search for items.
        /// </summary>
        /// <param name="view">The <see cref="GraphView"/> in which to show the window.</param>
        /// <param name="position">The position in hostWindow coordinates.</param>
        /// <param name="callback">The function called when a selection is made.</param>
        /// <param name="dbs">The <see cref="ItemLibraryDatabaseBase"/> that contains the items to be displayed.</param>
        /// <param name="filter">The search filter.</param>
        /// <param name="adapter">The <see cref="ItemLibraryAdapter"/>.</param>
        /// <param name="usage">The usage string used to identify the <see cref="ItemLibraryLibrary"/> use.</param>
        /// <param name="sourcePort">The port from which the library is created, if any.</param>
        internal static ItemLibraryWindow ShowDatabases(GraphView view,
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
                graphAdapter.SetHostGraphView(view, librarySize);

            void OnItemSelectFunc(ItemLibraryItem item)
            {
                callback?.Invoke(item);
            }

            var library = new ItemLibraryLibrary(dbs, adapter, filter, usage, sourcePort);

            var window = library.Show(view.Window, rect, view.TypeHandleInfos);
            window.CloseOnFocusLost = !(preferences?.GetBool(BoolPref.ItemLibraryStaysOpenOnBlur) ?? false);
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

        /// <summary>
        /// Opens the Item Library window to search for nodes to create and connect to specified input ports.
        /// </summary>
        /// <param name="view">The <see cref="GraphView"/> in which to show the window.</param>
        /// <param name="portModels">The input port models for which the Item Library is displayed.</param>
        /// <param name="worldPosition">The position in hostWindow coordinates.</param>
        /// <param name="callback">The function to call when a selection is made.</param>
        /// <remarks>
        /// Use this method when you need to display an Item Library window to select nodes and connect them to specific input ports in a graph.
        /// It simplifies the process of node creation and connection by presenting options based on the input ports and available nodes. If the selected node
        /// is incompatible with the port, the connection will not be established.
        /// </remarks>
        public static void ShowInputToGraphNodes(GraphView view,
            IReadOnlyList<PortModel> portModels, Vector2 worldPosition, Action<ItemLibraryItem> callback)
        {
            var toolName = view.GraphTool.Name;
            var adapterTitle = "Add an input node";
            var adapter = view.GetItemLibraryHelper().GetItemLibraryAdapter(adapterTitle, toolName, portModels);
            var filter = view.GetItemLibraryHelper().GetLibraryFilterProvider()?.GetInputToGraphFilter(portModels);
            var dbProvider = view.GetItemLibraryHelper().GetItemDatabaseProvider();

            if (dbProvider == null)
                return;

            ShowDatabases(view, worldPosition, callback, GetItemLibraryDatabasesForNodes(dbProvider, portModels), filter, adapter, Usage.CreateNode, portModels.FirstOrDefault());
        }

        /// <summary>
        /// Opens the Item Library window to search for nodes to create and connect to specified output ports.
        /// </summary>
        /// <param name="view">The <see cref="GraphView"/> in which to show the window.</param>
        /// <param name="portModels">The output port models for which the Item Library is displayed.</param>
        /// <param name="worldPosition">The position in hostWindow coordinates.</param>
        /// <param name="callback">The function to call when a selection is made.</param>
        /// <remarks>
        /// Use this method when you need to display an Item Library window to select nodes and connect them to specific output ports in a graph.
        /// It simplifies the process of node creation and connection by presenting options based on the output ports and available nodes. If the selected node
        /// is incompatible with the port, the connection will not be established.
        /// </remarks>
        public static void ShowOutputToGraphNodes(GraphView view,
            IReadOnlyList<PortModel> portModels, Vector2 worldPosition, Action<ItemLibraryItem> callback)
        {
            var toolName = view.GraphTool.Name;
            var adapterTitle = $"Choose an action for {portModels.First().DataTypeHandle.FriendlyName}";
            var adapter = view.GetItemLibraryHelper().GetItemLibraryAdapter(adapterTitle, toolName, portModels);
            var filter = view.GetItemLibraryHelper().GetLibraryFilterProvider()?.GetOutputToGraphFilter(portModels);
            var dbProvider = view.GetItemLibraryHelper().GetItemDatabaseProvider();

            if (dbProvider == null)
                return;

            ShowDatabases(view, worldPosition, callback, GetItemLibraryDatabasesForNodes(dbProvider, portModels), filter, adapter, Usage.CreateNode, portModels.FirstOrDefault());
        }

        /// <summary>
        /// Displays the Item Library to search for nodes to create and connect to a wire.
        /// </summary>
        /// <param name="view">The <see cref="GraphView"/> in which to show the window.</param>
        /// <param name="wireModel">The <see cref="WireModel"/> for which the Item Library was displayed.</param>
        /// <param name="worldPosition">The position in hostWindow coordinates.</param>
        /// <param name="callback">The function called when a selection is made.</param>
        /// <remarks>
        /// Use this method when you need to display an Item Library window to select nodes and connect them to a wire in a graph.
        /// It simplifies the process of node creation and connection by presenting options based on the wire and available nodes.
        /// </remarks>
        public static void ShowNodesForWire(GraphView view,
            WireModel wireModel, Vector2 worldPosition, Action<ItemLibraryItem> callback)
        {
            var toolName = view.GraphTool.Name;
            var adapter = view.GetItemLibraryHelper().GetItemLibraryAdapter("Insert Node", toolName);
            var filter = view.GetItemLibraryHelper().GetLibraryFilterProvider()?.GetWireFilter(wireModel);
            var dbProvider = view.GetItemLibraryHelper().GetItemDatabaseProvider();

            if (dbProvider == null)
                return;

            var dbs = dbProvider.GetGraphElementsDatabases(null).ToList();
            ShowDatabases(view, worldPosition, callback, dbs, filter, adapter, Usage.CreateNode);
        }

        /// <summary>
        /// Displays the Item Library to search for nodes to create on the graph.
        /// </summary>
        /// <param name="view">The <see cref="GraphView"/> in which to show the window.</param>
        /// <param name="worldPosition">The position in hostWindow coordinates.</param>
        /// <param name="callback">The function called when a selection is made.</param>
        /// <remarks>
        /// Use this method when you need to display an Item Library window to select nodes in a graph.
        /// It simplifies the process of node creation by presenting options based on the available nodes.
        /// </remarks>
        public static void ShowGraphNodes(GraphView view, Vector2 worldPosition, Action<ItemLibraryItem> callback)
        {
            var toolName = view.GraphTool.Name;
            var adapter = view.GetItemLibraryHelper().GetItemLibraryAdapter("Add a graph node", toolName);
            var filter = view.GetItemLibraryHelper().GetLibraryFilterProvider()?.GetGraphFilter();
            var dbProvider = view.GetItemLibraryHelper().GetItemDatabaseProvider();

            if (dbProvider == null)
                return;

            var blackboardModel = (view.Window as GraphViewEditorWindow)?.BlackboardView?.BlackboardRootViewModel?.BlackboardContentState?.BlackboardModel;

            var dbs = dbProvider.GetGraphElementsDatabases(blackboardModel)
                .Concat(dbProvider.GetDynamicDatabases((PortModel)null))
                .ToList();

            ShowDatabases(view, worldPosition, callback, dbs, filter, adapter, Usage.CreateNode);
        }

        public static ItemLibraryWindow FindInGraph(
            EditorWindow host,
            GraphView graphView,
            Action<FindInGraphAdapter.FindItem> highlightDelegate,
            Action<FindInGraphAdapter.FindItem> selectionDelegate
        )
        {
            var items = graphView.GraphModel.NodeModels
                .Where(x => x is IHasTitle titled && !string.IsNullOrEmpty(titled.Title))
                .Select(x => MakeFindItems(x, x.Title))
                .ToList();

            var database = new ItemLibraryDatabase(items);
            var library = new ItemLibraryLibrary(database, new FindInGraphAdapter(highlightDelegate));
            var position = new Vector2(host.rootVisualElement.layout.center.x, 0);

            var window = library.Show(host, position, graphView.TypeHandleInfos);
            window.itemChosen += item => selectionDelegate(item as FindInGraphAdapter.FindItem);
            window.StatusBarText = ""; // no specific action to be document when user selects an element.
            return window;
        }

        internal static ItemLibraryWindow ShowEnumValues(RootView view, string title, Type enumType, Vector2 position, Action<Enum, int> callback)
        {
            return ShowEnumValues(EditorWindow.focusedWindow, view, title, enumType, position, callback);
        }

        internal static ItemLibraryWindow ShowEnumValues(EditorWindow host,
            RootView rootView,
            string title,
            Type enumType,
            Vector2 position,
            Action<Enum, int> callback)
        {
            var items = Enum.GetValues(enumType)
                .Cast<Enum>()
                .Select(v => new EnumValuesAdapter.EnumValueItem(v) as ItemLibraryItem)
                .ToList();
            var database = new ItemLibraryDatabase(items);
            var library = new ItemLibraryLibrary(database, new EnumValuesAdapter(title), context: "Enum" + enumType.FullName);

            var window = library.Show(host, position, rootView.TypeHandleInfos);
            window.StatusBarText = k_DefaultValueStatusText;
            window.itemChosen += item => callback(((EnumValuesAdapter.EnumValueItem)item)?.value, 0);
            return window;
        }

        /// <summary>
        /// Creates and displays an <see cref="ItemLibraryWindow"/> for the specified values, given as strings.
        /// </summary>
        /// <param name="rootView">The view on which to display the window.</param>
        /// <param name="preferences">The preferences for the window.</param>
        /// <param name="title">The title of the window.</param>
        /// <param name="values">The values to show in the window.</param>
        /// <param name="position">The position of the window.</param>
        /// <param name="callback">The function called when a selection is made.</param>
        /// <returns>A new <see cref="ItemLibraryWindow"/>.</returns>
        /// <remarks>
        /// 'ShowValues' creates and displays an <see cref="ItemLibraryWindow"/> to select from a list of specified values. This method presents
        /// a window that allows you to choose from a collection of values, which are provided as strings.
        /// </remarks>
        public static ItemLibraryWindow ShowValues(RootView rootView, Preferences preferences, string title, IEnumerable<string> values, Vector2 position,
            Action<string> callback)
        {
            var items = values.Select(v => new ItemLibraryItem(v)).ToList();
            return ShowValues(EditorWindow.focusedWindow, rootView, preferences, title, items, position, callback);
        }

        /// <summary>
        /// Creates and displays an <see cref="ItemLibraryWindow"/> for the specified <see cref="ItemLibraryItem"/>s.
        /// </summary>
        /// <param name="rootView">The view on which to display the window.</param>
        /// <param name="preferences">The preferences for the window.</param>
        /// <param name="title">The title of the window.</param>
        /// <param name="items">The <see cref="ItemLibraryItem"/>s to show in the window.</param>
        /// <param name="position">The position of the window.</param>
        /// <param name="callback">The function called when a selection is made.</param>
        /// <returns>A new <see cref="ItemLibraryWindow"/>.</returns>
        /// <remarks>
        /// 'ShowValues' creates and displays an <see cref="ItemLibraryWindow"/> to select from a list of specified values. This method is used to present
        /// a window that allows users to choose from a collection of values, which are provided as <see cref="ItemLibraryItem"/>.
        /// </remarks>
        public static ItemLibraryWindow ShowValues(RootView rootView, Preferences preferences, string title, IReadOnlyList<ItemLibraryItem> items, Vector2 position,
            Action<string> callback)
        {
            return ShowValues(EditorWindow.focusedWindow, rootView, preferences, title, items, position, callback);
        }

        class LibraryAdapter : SimpleLibraryAdapter
        {
            public LibraryAdapter(string title)
                : base(title)
            {
                SortComparison = (x, y) =>
                {
                    if (x.FullName == k_GroupName)
                        return -1;
                    else if (y.FullName == k_GroupName)
                        return 1;
                    return string.CompareOrdinal(x.FullName, y.FullName);
                };
            }
        }

        internal static ItemLibraryWindow ShowValues(EditorWindow host, RootView rootView, Preferences preferences, string title, IReadOnlyList<ItemLibraryItem> items, Vector2 position,
            Action<string> callback)
        {
            var librarySize = preferences.GetItemLibrarySize(Usage.Values);
            var rect = new Rect(position, librarySize.Size);

            var database = new ItemLibraryDatabase(items);
            var adapter = new LibraryAdapter(title) { CustomStyleSheetPath = (rootView as IHasItemLibrary)?.GetItemLibraryHelper()?.CustomItemLibraryStylesheetPath ?? string.Empty };
            var library = new ItemLibraryLibrary(database, adapter, context: Usage.Values);

            var window = library.Show(host, rect, rootView.TypeHandleInfos);
            window.StatusBarText = k_DefaultValueStatusText;
            window.itemChosen += item => callback(item?.Name);
            ListenToResize(preferences, Usage.Values, window);
            return window;
        }

        /// <summary>
        /// The callback type called when the user selects a variable in ShowVariableTypes.
        /// </summary>
        public delegate void CreateVariableCallBack(TypeHandle typeHandle, Type variableType, VariableScope scope, ModifierFlags modifiers);


        /// <summary>
        /// The callback type called when the user selects a type in ShowTypesForVariable.
        /// </summary>
        public delegate void SetTypeCallBack(TypeHandle typeHandle);

        /// <summary>
        /// Shows the Item Library to select variable types.
        /// </summary>
        /// <param name="rootView">The view.</param>
        /// <param name="itemDatabaseProvider">The item database provider.</param>
        /// <param name="variableDeclarationType">The type of variable declaration to create.</param>
        /// <param name="preferences">The preferences.</param>
        /// <param name="position">The position of the item library.</param>
        /// <param name="callback">The callback to be called when the user selects a variable.</param>
        /// <returns>The window/</returns>
        public static ItemLibraryWindow ShowVariableTypes(RootView rootView, IItemDatabaseProvider itemDatabaseProvider, Type variableDeclarationType, Preferences preferences, Vector2 position, CreateVariableCallBack callback)
        {
            return ShowVariableTypes(EditorWindow.focusedWindow, rootView, itemDatabaseProvider, variableDeclarationType, preferences, position, callback);
        }

        internal static ItemLibraryWindow ShowVariableTypes(EditorWindow host, RootView rootView, IItemDatabaseProvider itemDatabaseProvider, Type variableDeclarationType, Preferences preferences, Vector2 position, CreateVariableCallBack callback)
        {
            if (rootView is not IHasItemLibrary hasItemLibrary)
                return null;

            var databases = itemDatabaseProvider.GetVariableDatabases();

            var addGroupItem = new TypeLibraryItem(k_GroupName, default) { StyleName = "group" };

            var addGroupDB = new ItemLibraryDatabase(new[] { addGroupItem });

            var fullDatabases = new ItemLibraryDatabaseBase[databases.Count + 1];
            for (int i = 0; i < databases.Count; ++i)
            {
                fullDatabases[i] = databases[i];
            }

            fullDatabases[databases.Count] = addGroupDB;

            var librarySize = preferences.GetItemLibrarySize(Usage.Types);
            var rect = new Rect(position, librarySize.Size);

            var itemLibraryHelper = hasItemLibrary.GetItemLibraryHelper();

            var adapter = new LibraryAdapter("Pick a type")
            {
                CustomStyleSheetPath = itemLibraryHelper.CustomItemLibraryStylesheetPath,
                CategoryPathStyleNames = itemLibraryHelper.CategoryPathStyleNames
            };

            var library = new ItemLibraryLibrary(fullDatabases, adapter, context: Usage.Types);

            var window = library.Show(host, rect, rootView.TypeHandleInfos);
            window.CloseOnFocusLost = !(preferences?.GetBool(BoolPref.ItemLibraryStaysOpenOnBlur) ?? false);

            ListenToResize(preferences, Usage.Types, window);
            window.itemChosen += item =>
            {
                switch (item)
                {
                    case VariableLibraryItem variableItem:
                        callback(variableItem.Type, variableItem.VariableType, variableItem.Scope, variableItem.ModifierFlags);
                        break;
                    case TypeLibraryItem typeItem:
                    {
                        var scope = VariableScope.Local;
                        var modifierFlags = ModifierFlags.Read;

                        // Get default variable infos from blackboard
                        var blackboardModel = (rootView.Window as GraphViewEditorWindow)?.BlackboardView
                            ?.BlackboardRootViewModel?.BlackboardContentState?.BlackboardModel;
                        if (blackboardModel != null)
                        {
                            scope = blackboardModel.DefaultVariableInfos.Scope;
                            modifierFlags = blackboardModel.DefaultVariableInfos.ModifierFlags;
                        }

                        callback(typeItem.Type, variableDeclarationType, scope, modifierFlags);
                        break;
                    }
                }
            };
            window.StatusBarText = k_DefaultTypeStatusText;
            return window;
        }

        /// <summary>
        /// Shows the ItemLibrary to choose a different type for a variable.
        /// </summary>
        /// <param name="rootView">The view.</param>
        /// <param name="preferences">The Preferences.</param>
        /// <param name="variable">The variable which type might be changed.</param>
        /// <param name="position">The position of the item library.</param>
        /// <param name="callback">The callback to be called when a new type is selected.</param>
        /// <returns>The window.</returns>
        public static ItemLibraryWindow ShowTypesForVariable(RootView rootView, Preferences preferences, VariableDeclarationModelBase variable, Vector2 position, SetTypeCallBack callback)
        {
            if (rootView is not IHasItemLibrary hasItemLibrary)
                return null;

            var databases = hasItemLibrary.GetItemLibraryHelper().GetItemDatabaseProvider()?.GetVariableCompatibleTypesDatabases(variable);

            return ShowTypes(EditorWindow.focusedWindow, rootView, preferences, databases, position, callback);
        }

        internal static ItemLibraryWindow ShowTypes(EditorWindow host, RootView rootView, Preferences preferences, IEnumerable<ItemLibraryDatabaseBase> databases, Vector2 position, SetTypeCallBack callback)
        {
            if (rootView is not IHasItemLibrary hasItemLibrary)
                return null;

            var librarySize = preferences.GetItemLibrarySize(Usage.Types);
            var rect = new Rect(position, librarySize.Size);
            var itemLibraryHelper = hasItemLibrary.GetItemLibraryHelper();
            var adapter = new LibraryAdapter("Pick a type")
            {
                CustomStyleSheetPath = itemLibraryHelper.CustomItemLibraryStylesheetPath,
                CategoryPathStyleNames = itemLibraryHelper.CategoryPathStyleNames
            };

            var library = new ItemLibraryLibrary(databases, adapter, context: Usage.Types);

            var window = library.Show(host, rect, rootView.TypeHandleInfos);
            ListenToResize(preferences, Usage.Types, window);
            window.itemChosen += item =>
            {
                if (item is TypeLibraryItem typeItem)
                    callback(typeItem.Type);
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

        internal static IEnumerable<ItemLibraryDatabaseBase> GetItemLibraryDatabasesForNodes(IItemDatabaseProvider dbProvider, IReadOnlyList<PortModel> portModels)
        {
            var graphElementDatabases = dbProvider.GetGraphElementsDatabases(null);
            var dynamicDatabases = dbProvider.GetDynamicDatabases(portModels);
            var variableDatabaseBases = dbProvider.GetGraphVariablesDatabases();

            return graphElementDatabases.Concat(dynamicDatabases).Concat(variableDatabaseBases);
        }
    }
}
