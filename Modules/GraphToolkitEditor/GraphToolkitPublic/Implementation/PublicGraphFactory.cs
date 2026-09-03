// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: GraphToolkit not yet converted
#pragma warning disable UAL0010,UAL0011,UAL0012,UAL0013,UAL0014 // AutoStaticsCleanup: GraphToolkit not yet converted
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.Pool;

namespace Unity.GraphToolkit.Editor.Implementation
{
    static partial class PublicGraphFactory
    {
        class GraphTypeInfos
        {
            public GraphAttribute attribute;
            public List<Type> subgraphTypes;
            public List<Type> nodeTypes;
            public List<Type> stateTypes;
            public List<Type> conditionTypes;
            public List<Type> stateTransitionTypes;
            public Dictionary<Type, List<Type>> blockTypes;
        }

        [AutoStaticsCleanupOnCodeReload]
        static Dictionary<string, Type> s_ExtensionToGraphTypes = new ();
        [AutoStaticsCleanupOnCodeReload]
        static Dictionary<Type, string> s_GraphTypeToExtensions = new ();
        [AutoStaticsCleanupOnCodeReload]
        static Dictionary<Type, GraphTypeInfos> s_GraphInfos = new ();
        [AutoStaticsCleanupOnCodeReload]
        static Dictionary<Type, Type> s_GraphModelImpTypes = new ();

        [OnCodeLoaded]
        static void Initialize()
        {
            var graphTypes = TypeCache.GetTypesDerivedFrom<IGraphInternal>();

            foreach (var graphType in graphTypes)
            {
                HandleGraphAttribute(graphType);

                HandleSubGraphAttribute(graphType);
            }
            int graphInfoCount = s_GraphInfos.Count;

            foreach (var graphType in s_GraphInfos.Keys)
            {
                --graphInfoCount;

                if (graphType.Assembly.GetName().Name == "Unity.GraphToolkit.Editor.Tests") // Don't include test assemblies graph types. TestGraph is conflicting with another, non public, Test Graph type.
                    continue;

                var contextType = typeof(GraphToolkitShortcuts<>).MakeGenericType(graphType);

                var contextInstance = Activator.CreateInstance(contextType);

                ShortcutProviderProxy.RegisterShortcutContext(contextInstance as IShortcutContext);

                ShortcutProviderProxy.GetInstance().AddTool(graphType.Name, contextType, null, graphInfoCount == 0);
            }


            //Graph that supports sub graphs but have no subgraph type use their own type as subgraph type
            foreach (var kv in s_ExtensionToGraphTypes)
            {
                if( kv.Key[0] == '.')
                    continue;

                var graphInfos = s_GraphInfos[kv.Value];

                var supportsSubgraphs = graphInfos.attribute.Options.HasFlag(GraphOptions.SupportsSubgraphs) ||
                    (graphInfos.attribute is StateMachineAttribute sm && sm.Options.HasFlag(StateMachineOptions.SupportsSubgraphs));
                if (supportsSubgraphs)
                {
                    if (graphInfos.subgraphTypes == null || graphInfos.subgraphTypes.Count == 0)
                    {
                        graphInfos.subgraphTypes ??= new List<Type>(1);
                        graphInfos.subgraphTypes.Add(kv.Value);
                    }
                }
                else
                {
                    if (graphInfos.subgraphTypes != null && graphInfos.subgraphTypes.Count != 0)
                    {
                        var sb = new System.Text.StringBuilder();

                        sb.Append(graphInfos.subgraphTypes[0].FullName);
                        for(int i = 1 ; i < graphInfos.subgraphTypes.Count; i++)
                        {
                            sb.Append(", ");
                            sb.Append(graphInfos.subgraphTypes[i].FullName);
                        }

                        Debug.LogError($"There are subgraph types defined for graph type {kv.Value.FullName} : {sb}, but the SupportsSubgraphs option is not set on its GraphAttribute. ");
                    }
                    graphInfos.subgraphTypes = null;
                }
            }

            // Validate that for each graph type, either itself of one of its subclass has a registered extension.
            foreach (var kv in s_GraphInfos)
            {
                var mainGraphType = kv.Key;
                if (s_GraphTypeToExtensions.ContainsKey(mainGraphType))
                {
                    continue;
                }

                bool foundValidDerivedType = false;
                foreach (var graphType in TypeCache.GetTypesDerivedFrom(mainGraphType))
                {
                    if (!graphType.IsAbstract && s_GraphTypeToExtensions.ContainsKey(graphType))
                    {
                        foundValidDerivedType = true;
                        break;
                    }
                }

                if (!foundValidDerivedType)
                {
                    if (kv.Value.subgraphTypes is { Count: > 0 })
                    {
                        foreach (var subGraphType in kv.Value.subgraphTypes)
                        {
                            Debug.LogError($"Subgraph type {subGraphType.FullName} has {mainGraphType.FullName} for mainGraphType, but neither it nor any of its subclasses have a registered extension. ");
                        }
                    }
                }
            }

            void HandleGraphAttribute(Type graphType)
            {
                var graphAttribute = graphType.GetCustomAttribute<GraphAttribute>(false);
                if (graphAttribute == null)
                    return;

                if (string.IsNullOrEmpty(graphAttribute.Extension))
                {
                    Debug.LogError($"{graphType.FullName} has a GraphAttribute with an empty extension. Specify a valid extension.");
                    return;
                }

                if (graphAttribute.Extension[0] == '.')
                {
                    Debug.LogError($"{graphType.FullName} has a GraphAttribute with an invalid extension {graphAttribute.Extension}. The extension cannot start with a '.'");
                    return;
                }

                if (graphAttribute.Extension == "asset")
                {
                    Debug.LogError($"{graphType.FullName} has a GraphAttribute with an invalid extension. You cannot use 'asset' as a graph extension because it is reserved for Unity's asset system.");
                    return;
                }

                if (graphType.IsAbstract)
                {
                    Debug.LogError($"{graphType.FullName} has a GraphAttribute but is abstract. Graph types with GraphAttribute will be instantiated and must be concrete classes. Remove the GraphAttribute or make the class concrete.");
                    return;
                }

                if (!s_ExtensionToGraphTypes.TryAdd(graphAttribute.Extension, graphType))
                {
                    var existingType = s_ExtensionToGraphTypes[graphAttribute.Extension];
                    Debug.LogError($"{graphType.FullName} has a GraphAttribute with an extension that is already used by {existingType.FullName}. Use a different extension.");
                    return;
                }

                // as Path.GetExtension returns the extension with a leading dot, we add it here so we don't have to use .Substring(1) everywhere
                s_ExtensionToGraphTypes.Add($".{graphAttribute.Extension}", graphType);

                s_GraphTypeToExtensions.Add(graphType, graphAttribute.Extension);

                if (!s_GraphInfos.TryGetValue(graphType, out var graphInfo))
                {
                    graphInfo = new GraphTypeInfos();
                    s_GraphInfos.Add(graphType, graphInfo);
                }
                graphInfo.attribute = graphAttribute;

                GraphObjectFactory.RegisterGraphObjectType(graphAttribute.Extension, typeof(GraphObjectImp));
            }

            void HandleSubGraphAttribute(Type graphType)
            {
                var subGraphAttribute = graphType.GetCustomAttribute<SubgraphAttribute>(false);
                if (subGraphAttribute == null) return;
                if (subGraphAttribute.MainGraphType == null)
                {
                    Debug.LogError($"{graphType.FullName} has a SubgraphAttribute with a null mainGraphType. Specify a valid mainGraphType.");
                    return;
                }

                if (!typeof(Graph).IsAssignableFrom(subGraphAttribute.MainGraphType) && !typeof(StateMachine).IsAssignableFrom(subGraphAttribute.MainGraphType))
                {
                    Debug.LogError($"{graphType.FullName} has a SubgraphAttribute with a mainGraphType that isn't a subclass of Graph: {subGraphAttribute.MainGraphType.FullName}. Specify a valid mainGraphType.");
                    return;
                }

                if(!s_GraphInfos.TryGetValue(subGraphAttribute.MainGraphType, out var mainGraphInfo))
                {
                    mainGraphInfo = new GraphTypeInfos();
                    s_GraphInfos.Add(subGraphAttribute.MainGraphType, mainGraphInfo);
                }

                mainGraphInfo.subgraphTypes ??= new List<Type>();
                mainGraphInfo.subgraphTypes.Add(graphType);
            }
        }

        /// <summary>
        /// Gets the graph type by a file extension.
        /// </summary>
        /// <param name="extension"> Can either have or not have a leading '.'</param>
        /// <returns>The graph type by a file extension.</returns>
        public static Type GetGraphTypeByExtension(string extension)
        {
            return s_ExtensionToGraphTypes.GetValueOrDefault(extension);
        }

        public static string GetExtensionByGraphType(Type graphType)
        {
            if (s_GraphInfos.TryGetValue(graphType, out var graphInfos))
            {
                return graphInfos.attribute.Extension;
            }
            return null;
        }

        /// <summary>
        /// Gets the <see cref="GraphModelImp"/> subtype that backs the given public graph type.
        /// </summary>
        public static Type GetGraphModelImpType(Type graphType)
        {
            if (graphType == null)
                return typeof(GraphModelImp);

            if (!s_GraphModelImpTypes.TryGetValue(graphType, out var graphModelImpType))
            {
                graphModelImpType = typeof(StateMachine).IsAssignableFrom(graphType) ? typeof(StateMachineImp) : typeof(GraphModelImp);
                s_GraphModelImpTypes.Add(graphType, graphModelImpType);
            }

            return graphModelImpType;
        }

        public static IReadOnlyList<Type> GetSubGraphTypes(Type graphType)
        {
            var graphInfos  = s_GraphInfos[graphType];
            if( graphInfos.subgraphTypes == null)
                return Array.Empty<Type>();
            return graphInfos.subgraphTypes;
        }

        public static IReadOnlyList<Type> GetBlockTypes(Type graphType, Type contextType)
        {
            if (!s_GraphInfos.TryGetValue(graphType, out var graphInfos))
                return Array.Empty<Type>();

            if (graphInfos.blockTypes == null)
            {
                graphInfos.blockTypes = new Dictionary<Type, List<Type>>();

                using var dispose = HashSetPool<Type>.Get(out var nodeTypes);
                nodeTypes.UnionWith(GetNodeTypes(graphType));

                var blockTypes = TypeCache.GetTypesWithAttribute<UseWithContextAttribute>();

                foreach (var blockType in blockTypes)
                {
                    var blockNodeAttribute = blockType.GetCustomAttribute<UseWithContextAttribute>();
                    var nodeAttribute = blockType.GetCustomAttribute<UseWithGraphAttribute>();

                    if (blockNodeAttribute == null)
                        continue;
                    if (nodeAttribute != null && !nodeAttribute.IsGraphTypeSupported(graphType))
                        continue;

                    AddBlockType(nodeTypes, blockNodeAttribute, blockType);

                    var subBlockTypes = TypeCache.GetTypesDerivedFrom(blockType);
                    foreach (var subBlockType in subBlockTypes)
                    {
                        var subAttribute = GetSpecificAttribute<UseWithContextAttribute>(subBlockType, blockType);
                        if( subAttribute != null) // if it has its own BlockNodeAttribute, it will be handled in the loop above
                            continue;
                        AddBlockType(nodeTypes, blockNodeAttribute, subBlockType);
                    }
                }
            }

            return (IReadOnlyList<Type>)graphInfos.blockTypes.GetValueOrDefault(contextType) ?? Array.Empty<Type>();

            void AddBlockType(HashSet<Type> nodeTypes, UseWithContextAttribute blockNodeAttribute, Type blockType)
            {
                if (blockType.IsAbstract)
                    return;
                foreach (var cType in blockNodeAttribute.contextTypes)
                {
                    AddBlockTypeToContextType(nodeTypes, cType, blockType);

                    // Also add any context type that is derived from the specified context type
                    foreach(var subContextType in TypeCache.GetTypesDerivedFrom(cType))
                        AddBlockTypeToContextType(nodeTypes, subContextType, blockType);
                }
            }

            void AddBlockTypeToContextType(HashSet<Type> nodeTypes, Type contextType, Type blockType)
            {
                if (!nodeTypes.Contains(contextType)) // If the context type does not support the graph type, we don't add the block type to the list.
                    return;

                if( ! graphInfos.blockTypes.TryGetValue(contextType, out var blockList))
                {
                    blockList = new List<Type>();
                    graphInfos.blockTypes[contextType] = blockList;
                }
                blockList.Add(blockType);
            }
        }

        public static IReadOnlyList<Type> GetNodeTypes(Type graphType)
        {
            if (!s_GraphInfos.TryGetValue(graphType, out var graphInfos))
                return Array.Empty<Type>();

            if (graphInfos.nodeTypes == null)
            {
                graphInfos.nodeTypes = new List<Type>();

                // Todo this could be done only once in a static constructor
                var nodeTypes = TypeCache.GetTypesDerivedFrom<Node>();

                using var dispose = HashSetPool<Type>.Get(out var addedTypes);
                foreach (var nodeType in nodeTypes)
                {
                    var attribute = nodeType.GetCustomAttribute<UseWithGraphAttribute>();
                    if (attribute == null)
                        continue;

                    addedTypes.Add(nodeType);
                    if (!attribute.IsGraphTypeSupported(graphType))
                        continue;

                    HandleNodeType(nodeType);
                    var subNodeTypes = TypeCache.GetTypesDerivedFrom(nodeType);
                    foreach (var subNodeType in subNodeTypes)
                    {
                        if( GetSpecificAttribute<UseWithGraphAttribute>(subNodeType, nodeType) != null) // if it has its own NodeAttribute, it will be handled in the loop above
                            continue;
                        addedTypes.Add(subNodeType);
                        HandleNodeType(subNodeType);
                    }
                }

                var graphAttribute = graphType.GetCustomAttribute<GraphAttribute>();

                if (graphAttribute != null && !graphAttribute.Options.HasFlag(GraphOptions.DisableAutoInclusionOfNodesFromGraphAssembly))
                {
                    foreach (var type in nodeTypes)
                    {
                        if (type.Assembly != graphType.Assembly)
                            continue;
                        if (addedTypes.Contains(type))
                            continue;

                        HandleNodeType(type);
                    }
                }

                void HandleNodeType(Type type)
                {
                    if( type.IsAbstract)
                        return;

                    if( !typeof(BlockNode).IsAssignableFrom(type) )
                        graphInfos.nodeTypes.Add(type);
                }
            }

            return graphInfos.nodeTypes;
        }

        public static IReadOnlyList<Type> GetStateTypes(Type graphType)
        {
            if (!s_GraphInfos.TryGetValue(graphType, out var graphInfos))
                return Array.Empty<Type>();

            if (graphInfos.stateTypes == null)
            {
                graphInfos.stateTypes = new List<Type>();

                var stateTypes = TypeCache.GetTypesDerivedFrom<State>();

                using var dispose = HashSetPool<Type>.Get(out var addedTypes);
                foreach (var stateType in stateTypes)
                {
                    var attribute = stateType.GetCustomAttribute<UseWithStateMachineAttribute>();
                    if (attribute == null)
                        continue;

                    addedTypes.Add(stateType);
                    if (!attribute.IsStateMachineTypeSupported(graphType))
                        continue;

                    HandleStateType(stateType);
                    var subStateTypes = TypeCache.GetTypesDerivedFrom(stateType);
                    foreach (var subStateType in subStateTypes)
                    {
                        if( GetSpecificAttribute<UseWithStateMachineAttribute>(subStateType, stateType) != null) // if it has its own attribute, it will be handled in the loop above
                            continue;
                        addedTypes.Add(subStateType);
                        HandleStateType(subStateType);
                    }
                }

                var graphAttribute = graphType.GetCustomAttribute<GraphAttribute>();

                if (graphAttribute != null && !graphAttribute.Options.HasFlag(GraphOptions.DisableAutoInclusionOfNodesFromGraphAssembly))
                {
                    foreach (var type in stateTypes)
                    {
                        if (type.Assembly != graphType.Assembly)
                            continue;
                        if (addedTypes.Contains(type))
                            continue;

                        HandleStateType(type);
                    }
                }

                void HandleStateType(Type type)
                {
                    if( type.IsAbstract)
                        return;

                    graphInfos.stateTypes.Add(type);
                }
            }

            return graphInfos.stateTypes;
        }

        /// <summary>
        /// Gets the <see cref="Condition"/> types that can be used with the given graph type.
        /// </summary>
        /// <param name="graphType">The graph type.</param>
        /// <returns>The condition types that can be used with the given graph type.</returns>
        public static IReadOnlyList<Type> GetConditionTypes(Type graphType)
        {
            if (!s_GraphInfos.TryGetValue(graphType, out var graphInfos))
                return Array.Empty<Type>();

            if (graphInfos.conditionTypes == null)
            {
                graphInfos.conditionTypes = new List<Type>();

                var conditionTypes = TypeCache.GetTypesDerivedFrom<Condition>();

                var addedTypes = new HashSet<Type>();
                foreach (var conditionType in conditionTypes)
                {
                    var attribute = conditionType.GetCustomAttribute<UseWithStateMachineAttribute>();
                    if (attribute == null)
                        continue;

                    addedTypes.Add(conditionType);
                    if (!attribute.IsStateMachineTypeSupported(graphType))
                        continue;

                    HandleConditionType(conditionType);
                    var subConditionTypes = TypeCache.GetTypesDerivedFrom(conditionType);
                    foreach (var subConditionType in subConditionTypes)
                    {
                        // if it has its own attribute, it will be handled in the loop above
                        if (GetSpecificAttribute<UseWithStateMachineAttribute>(subConditionType, conditionType) != null)
                            continue;
                        addedTypes.Add(subConditionType);
                        HandleConditionType(subConditionType);
                    }
                }

                var graphAttribute = graphType.GetCustomAttribute<GraphAttribute>();

                if (graphAttribute != null && !graphAttribute.Options.HasFlag(GraphOptions.DisableAutoInclusionOfNodesFromGraphAssembly))
                {
                    foreach (var type in graphType.Assembly.GetTypes())
                    {
                        if (addedTypes.Contains(type))
                            continue;
                        if (!typeof(Condition).IsAssignableFrom(type))
                            continue;

                        HandleConditionType(type);
                    }
                }

                void HandleConditionType(Type type)
                {
                    if (type.IsAbstract || type.ContainsGenericParameters)
                        return;

                    graphInfos.conditionTypes.Add(type);
                }
            }

            return graphInfos.conditionTypes;
        }

        public static IReadOnlyList<Type> GetSelfTransitionTypes(Type graphType)
        {
            if (!s_GraphInfos.TryGetValue(graphType, out var graphInfos))
                return Array.Empty<Type>();

            if (graphInfos.stateTransitionTypes == null)
            {
                graphInfos.stateTransitionTypes = new List<Type>();

                var stateTransitionTypes = TypeCache.GetTypesDerivedFrom<SelfTransition>();

                var addedTypes = new HashSet<Type>();
                foreach (var stateTransitionType in stateTransitionTypes)
                {
                    var attribute = stateTransitionType.GetCustomAttribute<UseWithStateMachineAttribute>();
                    if (attribute == null)
                        continue;

                    addedTypes.Add(stateTransitionType);
                    if (!attribute.IsStateMachineTypeSupported(graphType))
                        continue;

                    HandleStateTransitionType(stateTransitionType);
                    var subStateTransitionTypes = TypeCache.GetTypesDerivedFrom(stateTransitionType);
                    foreach (var subStateTransitionType in subStateTransitionTypes)
                    {
                        if (GetSpecificAttribute<UseWithStateMachineAttribute>(subStateTransitionType, stateTransitionType) != null) // if it has its own attribute, it will be handled in the loop above
                            continue;
                        addedTypes.Add(subStateTransitionType);
                        HandleStateTransitionType(subStateTransitionType);
                    }
                }

                var graphAttribute = graphType.GetCustomAttribute<GraphAttribute>();

                if (graphAttribute != null && !graphAttribute.Options.HasFlag(GraphOptions.DisableAutoInclusionOfNodesFromGraphAssembly))
                {
                    foreach (var type in stateTransitionTypes)
                    {
                        if (type.Assembly != graphType.Assembly)
                            continue;
                        if (addedTypes.Contains(type))
                            continue;

                        HandleStateTransitionType(type);
                    }
                }

                void HandleStateTransitionType(Type type)
                {
                    if (type.IsAbstract)
                        return;

                    graphInfos.stateTransitionTypes.Add(type);
                }
            }

            return graphInfos.stateTransitionTypes;
        }

        [OnOpenAsset(999)]
        public static bool OpenGraphAsset(EntityId entityId, int line)
        {
            var path = AssetDatabase.GetAssetPath(entityId);

            if( string.IsNullOrEmpty(path) )
                return false;

            var extension = Path.GetExtension(path);

            if (!s_ExtensionToGraphTypes.TryGetValue(extension, out var graphType))
                return false;

            var graphObject = GraphObject.LoadGraphObjectAtPath<GraphObjectImp>(path);
            if (graphObject == null)
                return false;

            if (graphObject.GraphModel is not GraphModelImp graphModel)
                return false;

            if (graphModel.GetType() != GetGraphModelImpType(graphType))
                return false;

            if( graphModel.Graph.GetType() != graphType)
                return false;

            GraphViewEditorWindowImp.ShowGraph(graphObject);

            return true;
        }

        public static void PromptInProjectBrowserToCreateNewAsset<T>(string defaultName = "New Graph")
            where T : Graph, new()
        {
            PromptInProjectBrowserToCreateNewAsset(defaultName, typeof(T));
        }

        public static void PromptInProjectBrowserToCreateNewAsset(string defaultName, Type graphType)
        {
            var template = new GraphTemplateImp(graphType, defaultName);

            if (string.IsNullOrEmpty(template.GraphFileExtension))
                return;

            var graphObject = ScriptableObject.CreateInstance<GraphObjectImp>();
            graphObject.GraphType  = graphType;

            var endAction = ScriptableObject.CreateInstance<DoCreateAsset>();
            endAction.SetUp(graphObject, template, go =>
            {
                AssetDatabase.OpenAsset(go);
            });

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                graphObject.GetEntityId(),
                endAction,
                $"{template.NewAssetName}.{template.GraphFileExtension}",
                AssetPreview.GetMiniThumbnail(graphObject),
                null);
        }

        public static void EnsureStaticConstructorIsCalled()
        {

        }

        internal static T GetSpecificAttribute<T>(Type type, Type baseType) where T : Attribute
        {
            while (type != baseType && type != null)
            {
                var attribute = type.GetCustomAttribute<T>(false);
                if( attribute != null)
                    return attribute;
                type = type.BaseType;

                if (type?.IsGenericType == true && type.GetGenericTypeDefinition() == baseType)
                    return null;
            }

            return null;
        }

    }
}
#pragma warning restore UAL0010,UAL0011,UAL0012,UAL0013,UAL0014
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
