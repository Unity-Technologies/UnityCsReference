// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Properties;
using UnityEngine.Pool;
using UnityEngine.UIElements.Internal;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Type result of a binding visitation.
    /// </summary>
    internal readonly struct BindingTypeResult
    {
        /// <summary>
        /// The property type.
        /// </summary>
        public readonly Type type;
        /// <summary>
        /// The visitation result.
        /// </summary>
        public readonly VisitReturnCode returnCode;
        /// <summary>
        /// The index of the property path part that is causing the error, -1 if visitation is successful.
        /// </summary>
        public readonly int errorIndex;
        /// <summary>
        /// The property path visited.
        /// </summary>
        public readonly PropertyPath resolvedPath;

        internal BindingTypeResult(Type type, in PropertyPath resolvedPath)
        {
            this.type = type;
            this.resolvedPath = resolvedPath;
            returnCode = VisitReturnCode.Ok;
            errorIndex = -1;
        }

        internal BindingTypeResult(VisitReturnCode returnCode, int errorIndex, in PropertyPath resolvedPath)
        {
            type = null;
            this.resolvedPath = resolvedPath;
            this.returnCode = returnCode;
            this.errorIndex = errorIndex;
        }
    }

    /// <summary>
    /// Provides information about a binding.
    /// </summary>
    public readonly struct BindingInfo
    {
        /// <summary>
        /// The visual element targeted by the binding.
        /// </summary>
        public VisualElement targetElement { get; }
        /// <summary>
        /// The binding id.
        /// </summary>
        public BindingId bindingId { get; }
        /// <summary>
        /// The binding matching this information.
        /// </summary>
        public Binding binding { get; }

        private BindingInfo(VisualElement targetElement, in BindingId bindingId, Binding binding)
        {
            this.targetElement = targetElement;
            this.bindingId = bindingId;
            this.binding = binding;
        }

        internal static BindingInfo FromRequest(VisualElement target, in PropertyPath targetPath, Binding binding)
        {
            return new BindingInfo(target, targetPath, binding);
        }

        internal static BindingInfo FromBindingData(in DataBindingManager.BindingData bindingData)
        {
            return new BindingInfo(bindingData.target.element, bindingData.target.bindingId, bindingData.binding);
        }
    }

    /// <summary>
    /// Provides information about the resolved type of a property.
    /// </summary>
    internal readonly struct PropertyPathInfo
    {
        /// <summary>
        /// The property path used.
        /// </summary>
        public readonly PropertyPath propertyPath;
        /// <summary>
        /// The resolved property type.
        /// </summary>
        public readonly Type type;

        internal PropertyPathInfo(in PropertyPath propertyPath, Type type)
        {
            this.propertyPath = propertyPath;
            this.type = type;
        }
    }

    /// <summary>
    /// Provides a subset of helper methods for the data binding system.
    /// </summary>
    internal static class DataBindingUtility
    {
        static readonly Pool.ObjectPool<TypePathVisitor> k_TypeVisitors = new(() => new TypePathVisitor(), v => v.Reset(), defaultCapacity: 1);
        static readonly Pool.ObjectPool<AutoCompletePathVisitor> k_AutoCompleteVisitors = new(() => new AutoCompletePathVisitor(), v => v.Reset(), defaultCapacity: 1);

        private static readonly Regex s_ReplaceIndices = new Regex("\\[[0-9]+\\]", RegexOptions.Compiled);

        /// <summary>
        /// Returns a list of all bound elements on a panel.
        /// </summary>
        /// <param name="panel">The panel to inspect.</param>
        /// <param name="boundElements">A list to contain the bound elements.</param>
        /// <remarks>
        /// The <see cref="boundElements"/> list will only be added to.
        /// </remarks>
        public static void GetBoundElements(IPanel panel, List<VisualElement> boundElements)
        {
            if (panel is BaseVisualElementPanel p)
                boundElements.AddRange(p.dataBindingManager.GetUnorderedBoundElements());
        }

        /// <summary>
        /// Fills a list of all bindings on an element.
        /// </summary>
        /// <param name="element">The element to inspect.</param>
        /// <param name="result">The resulting list of binding infos.</param>
        /// <remarks>The order in which the binding information is returned is undefined.</remarks>
        public static void GetBindingsForElement(VisualElement element, List<BindingInfo> result)
        {
            using var pool = HashSetPool<PropertyPath>.Get(out var visited);
            foreach (var bindingRequest in DataBindingManager.GetBindingRequests(element))
            {
                if (visited.Add(bindingRequest.bindingId) && null != bindingRequest.binding)
                    result.Add(BindingInfo.FromRequest(element, bindingRequest.bindingId, bindingRequest.binding));
            }

            if (element.elementPanel == null)
                return;

            var bindingData = element.elementPanel.dataBindingManager.GetBindingData(element);
            foreach (var binding in bindingData)
            {
                if (visited.Add(binding.target.bindingId))
                    result.Add(BindingInfo.FromBindingData(binding));
            }
        }

        /// <summary>
        /// Gets the binding on a visual element at the specified path.
        /// </summary>
        /// <param name="element">The element to inspect.</param>
        /// <param name="bindingId">The id of the binding.</param>
        /// <param name="bindingInfo">The binding found on the element.</param>
        /// <returns>Whether a binding was found or not.</returns>
        public static bool TryGetBinding(VisualElement element, in BindingId bindingId, out BindingInfo bindingInfo)
        {
            if (DataBindingManager.TryGetBindingRequest(element, bindingId, out var binding))
            {
                // The binding is being removed.
                if (null == binding)
                {
                    bindingInfo = default;
                    return false;
                }

                bindingInfo = BindingInfo.FromRequest(element, bindingId, binding);
                return true;
            }

            if (element.elementPanel != null)
            {
                if (element.elementPanel.dataBindingManager.TryGetBindingData(element, bindingId, out var bindingData))
                {
                    bindingInfo = BindingInfo.FromBindingData(bindingData);
                    return true;
                }
            }
            bindingInfo = default;
            return false;
        }


        /// <summary>
        /// Finds the closest unresolved data source object or the data source type and the chain of binding paths inherited from the hierarchy of the specified visual element.
        /// </summary>
        /// <param name="element">The element to start from</param>
        /// <param name="dataSourceObject">The data source object found</param>
        /// <param name="dataSourceType">The data source type found</param>
        /// <param name="fullPath">The chain of paths found</param>
        /// <returns>Returns true if at least the data source, the data source type or a binding path is inherited</returns>
        internal static bool TryGetDataSourceOrDataSourceTypeFromHierarchy(VisualElement element, out object dataSourceObject, out Type dataSourceType, out PropertyPath fullPath)
        {
            var sourceElement = element;

            dataSourceObject = null;
            dataSourceType = null;
            fullPath = new PropertyPath();

            while (sourceElement != null)
            {
                if (!sourceElement.dataSourcePath.IsEmpty)
                {
                    if (fullPath.IsEmpty)
                        fullPath = sourceElement.dataSourcePath;
                    else
                        fullPath = PropertyPath.Combine(sourceElement.dataSourcePath, fullPath);
                }

                dataSourceObject = sourceElement.dataSource;

                if (sourceElement.dataSource != null)
                    return true;

                dataSourceType = sourceElement.dataSourceType;

                if (dataSourceType != null)
                    return true;
                sourceElement = sourceElement.hierarchy.parent;
            }

            return !fullPath.IsEmpty;
        }


        /// <summary>
        /// Finds the closest resolved data source in the hierarchy, including relative data sources.
        /// </summary>
        /// <param name="element">The element to start from.</param>
        /// <param name="dataSource">The data source found.</param>
        /// <returns>The resolved data source.</returns>
        public static bool TryGetRelativeDataSourceFromHierarchy(VisualElement element, out object dataSource)
        {
            var context = element.GetHierarchicalDataSourceContext();
            dataSource = context.dataSource;

            if (context.dataSourcePath.IsEmpty)
                return null != dataSource;

            if (null == context.dataSource)
                return false;

            if (!PropertyContainer.TryGetValue(ref dataSource, context.dataSourcePath, out object value))
                return true;

            dataSource = value;
            return true;

        }

        /// <summary>
        /// Finds the closest resolved data source type in the hierarchy, including relative data source types.
        /// </summary>
        /// <param name="element">The element to start from.</param>
        /// <param name="type">The data source type found.</param>
        /// <returns>True if the resolved data source type is found.</returns>
        internal static bool TryGetRelativeDataSourceTypeFromHierarchy(VisualElement element, out Type type)
        {
            type = null;

            if (TryGetDataSourceOrDataSourceTypeFromHierarchy(element, out var dataSource, out var dataSourceType, out var bindingPath))
            {
                // If the data source is defined then the binding path is related to it even if the data source type is defined.
                if (dataSource != null || dataSourceType == null)
                    return false;

                // If the (combined) binding path is empty then returns the top data source type.
                if (bindingPath.IsEmpty)
                {
                    type = dataSourceType;
                }
                // otherwise get the type of the property resolved from the (combined) binding path from the top data source type.
                else
                {
                    var bindingPathStr = bindingPath.ToString();
                    using var pool = ListPool<PropertyPathInfo>.Get(out var  allProperties);

                    GetPropertyPaths(dataSourceType, int.MaxValue, allProperties);
                    // Replace all the indices to '0' in the binding path before searching because the indices in the paths returned by DataBindingUtility.GetPropertyPaths are all '0'.
                    bindingPathStr = ReplaceAllIndicesInPath(bindingPathStr, "0");
                    foreach (var prop in allProperties)
                    {
                        if (prop.propertyPath.ToString() != bindingPathStr) continue;
                        type = prop.type;
                        break;
                    }
                }
            }

            return type != null;
        }

        /// <summary>
        /// Replaces all indices in the specified text by the new index.
        /// </summary>
        /// <param name="path">The original path</param>
        /// <param name="newText">The text to replace the indices</param>
        /// <returns>The new path with replaced indices</returns>
        internal static string ReplaceAllIndicesInPath(string path, string newText)
        {
            return path.Contains('[') ? s_ReplaceIndices.Replace(path, $"[{newText}]") : path;
        }

        /// <summary>
        /// Retrieves the last cached binding result for the UI update.
        /// </summary>
        /// <param name="bindingId">The binding id to look for.</param>
        /// <param name="element">The element it is applied on.</param>
        /// <param name="result">The result of the last UI update for this binding, if found.</param>
        /// <returns>Whether a cached result was found or not.</returns>
        public static bool TryGetLastUIBindingResult(in BindingId bindingId, VisualElement element, out BindingResult result)
        {
            result = default;
            var bindingData = GetBindingData(bindingId, element);
            if (bindingData.binding == null || element.elementPanel == null)
            {
                return false;
            }

            return element.elementPanel.dataBindingManager.TryGetLastUIBindingResult(bindingData, out result);
        }

        /// <summary>
        /// Retrieves the last cached binding result for the source update.
        /// </summary>
        /// <param name="bindingId">The binding id to look for.</param>
        /// <param name="element">The element it is applied on.</param>
        /// <param name="result">The result of the last source update for this binding, if found.</param>
        /// <returns>Whether a cached result was found or not.</returns>
        public static bool TryGetLastSourceBindingResult(in BindingId bindingId, VisualElement element, out BindingResult result)
        {
            result = default;

            var bindingData = GetBindingData(bindingId, element);
            if (bindingData.binding == null)
            {
                return false;
            }

            return element.elementPanel.dataBindingManager.TryGetLastSourceBindingResult(bindingData, out result);
        }

        /// <summary>
        /// Fills a list of ids of all conversion groups that can convert between the 2 provided types.
        /// </summary>
        /// <param name="sourceType">The source type of the conversion.</param>
        /// <param name="destinationType">The destination type of the conversion.</param>
        /// <param name="result">The resulting list of converter ids.</param>
        public static void GetMatchingConverterGroups(Type sourceType, Type destinationType, List<string> result)
        {
            using (ListPool<ConverterGroup>.Get(out var groups))
            {
                ConverterGroups.GetAllConverterGroups(groups);
                foreach (var group in groups)
                {
                    if (group.registry.TryGetConverter(sourceType, destinationType, out _))
                    {
                        result.Add(group.id);
                    }
                }
            }
        }

        /// <summary>
        /// Fills a list of ids of all conversion groups that can convert from the source type.
        /// </summary>
        /// <param name="sourceType">The source type of the conversion.</param>
        /// <param name="result">The resulting list of converter ids.</param>
        public static void GetMatchingConverterGroupsFromType(Type sourceType, List<string> result)
        {
            using (ListPool<ConverterGroup>.Get(out var groups))
            using (ListPool<Type>.Get(out var types))
            {
                ConverterGroups.GetAllConverterGroups(groups);
                foreach (var group in groups)
                {
                    group.registry.GetAllTypesConvertingFromType(sourceType, types);
                    if (types.Count > 0)
                    {
                        result.Add(group.id);
                    }

                    types.Clear();
                }
            }
        }

        /// <summary>
        /// Fills a list of ids of all conversion groups that can convert to the destination type.
        /// </summary>
        /// <param name="destinationType">The destination type of the conversion.</param>
        /// <param name="result">The resulting list of converter ids.</param>
        public static void GetMatchingConverterGroupsToType(Type destinationType, List<string> result)
        {
            using (ListPool<ConverterGroup>.Get(out var groups))
            using (ListPool<Type>.Get(out var types))
            {
                ConverterGroups.GetAllConverterGroups(groups);
                foreach (var group in groups)
                {
                    group.registry.GetAllTypesConvertingToType(destinationType, types);
                    if (types.Count > 0)
                    {
                        result.Add(group.id);
                    }

                    types.Clear();
                }
            }
        }

        /// <summary>
        /// Fills a list of all types converting to the provided type, including local conversions on the binding and global ui conversions,
        /// when converting data between a data source to a UI control.
        /// </summary>
        /// <remarks>Does not include standard primitive conversions.</remarks>
        /// <param name="binding">The binding to inspect.</param>
        /// <param name="destinationType">The output type.</param>
        /// <param name="result">The resulting list of types that can be converted.</param>
        public static void GetAllConversionsFromSourceToUI(DataBinding binding, Type destinationType, List<Type> result)
        {
            result.Add(destinationType);
            binding?.sourceToUiConverters.registry.GetAllTypesConvertingToType(destinationType, result);
            ConverterGroups.globalConverters.registry.GetAllTypesConvertingToType(destinationType, result);
            ConverterGroups.primitivesConverters.registry.GetAllTypesConvertingToType(destinationType, result);
        }

        /// <summary>
        /// Fills a list of all types converting to the provided type, including local conversions on the binding and global ui conversions,
        /// when converting data between a UI control to a data source.
        /// </summary>
        /// <param name="binding">The binding to inspect.</param>
        /// <param name="sourceType">The type coming from the data source.</param>
        /// <param name="result">A list of types that can be converted.</param>
        public static void GetAllConversionsFromUIToSource(DataBinding binding, Type sourceType, List<Type> result)
        {
            result.Add(sourceType);
            binding?.uiToSourceConverters.registry.GetAllTypesConvertingToType(sourceType, result);
            ConverterGroups.globalConverters.registry.GetAllTypesConvertingToType(sourceType, result);
            ConverterGroups.primitivesConverters.registry.GetAllTypesConvertingToType(sourceType, result);
        }

        /// <summary>
        /// Checks whether a path exists on a data source or not and returns its type information.
        /// </summary>
        /// <param name="dataSource">The data source to inspect.</param>
        /// <param name="path">The path of the property to look for.</param>
        /// <returns>Type information of the property at the specified path.</returns>
        public static BindingTypeResult IsPathValid(object dataSource, string path)
        {
            return IsPathValid(dataSource, dataSource?.GetType(), path);
        }

        /// <summary>
        /// Checks whether a path exists on a source type or not and returns its type information.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="path">The path of the property to look for.</param>
        /// <returns>Type information of the property at the specified path.</returns>
        public static BindingTypeResult IsPathValid(Type type, string path)
        {
            return IsPathValid(null, type, path);
        }

        static BindingTypeResult IsPathValid(object dataSource, Type type, string path)
        {
            if (type == null)
                return new BindingTypeResult(VisitReturnCode.NullContainer, 0, default);

            var visitor = k_TypeVisitors.Get();
            BindingTypeResult result;

            var properties = PropertyBag.GetPropertyBag(type);

            try
            {
                visitor.Path = new PropertyPath(path);

                if (properties == null)
                {
                    visitor.ReturnCode = VisitReturnCode.MissingPropertyBag;
                }

                if (dataSource == null)
                {
                    properties?.Accept(visitor);
                }
                else
                {
                    properties?.Accept(visitor, ref dataSource);
                }

                if (visitor.ReturnCode == VisitReturnCode.Ok)
                {
                    result = new BindingTypeResult(visitor.resolvedType, visitor.Path);
                }
                else
                {
                    var resolvedPath = PropertyPath.SubPath(visitor.Path, 0, visitor.PathIndex);
                    result = new BindingTypeResult(visitor.ReturnCode, visitor.PathIndex, resolvedPath);
                }
            }
            finally
            {
                k_TypeVisitors.Release(visitor);
            }

            return result;
        }

        /// <summary>
        /// Fills a list of all property paths available on this data source.
        /// </summary>
        /// <param name="dataSource">The data source to inspect.</param>
        /// <param name="depth">The maximum path depth to visit. The recommended depth to avoid <see cref="PropertyPath"/> allocations is 4.</param>
        /// <param name="listResult">The resulting list of property paths available on this data source.</param>
        public static void GetPropertyPaths(object dataSource, int depth, List<PropertyPathInfo> listResult)
        {
            GetPropertyPaths(dataSource, dataSource?.GetType(), depth, listResult);
        }

        /// <summary>
        /// Fills a list of all property paths available on this data source.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <param name="depth">The maximum path depth to visit. The recommended depth to avoid <see cref="PropertyPath"/> allocations is 4.</param>
        /// <param name="listResult">The resulting list of property paths available on this type.</param>
        public static void GetPropertyPaths(Type type, int depth, List<PropertyPathInfo> listResult)
        {
            GetPropertyPaths(null, type, depth, listResult);
        }

        static void GetPropertyPaths(object dataSource, Type type, int depth, List<PropertyPathInfo> resultList)
        {
            if (type == null)
                return;

            var bag = PropertyBag.GetPropertyBag(type);
            if (bag == null)
                return;

            var visitor = k_AutoCompleteVisitors.Get();

            try
            {
                visitor.propertyPathList = resultList;
                visitor.maxDepth = depth;

                if (dataSource == null)
                    bag.Accept(visitor);
                else
                    bag.Accept(visitor, ref dataSource);
            }
            finally
            {
                k_AutoCompleteVisitors.Release(visitor);
            }
        }

        static DataBindingManager.BindingData GetBindingData(in BindingId bindingId, VisualElement element)
        {
            if (element.elementPanel != null && element.elementPanel.dataBindingManager.TryGetBindingData(element, bindingId, out var activeBinding))
                return activeBinding;

            return DataBindingManager.TryGetBindingRequest(element, bindingId, out var bindingRequest)
                ? new DataBindingManager.BindingData(new BindingTarget(element, bindingId), bindingRequest)
                : default;
        }
    }
}
