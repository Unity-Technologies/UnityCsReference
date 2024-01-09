// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class ScriptAttributeUtility
    {
        readonly struct CustomPropertyDrawerContainer
        {
            public readonly Type drawerType;
            public readonly Type[] supportedRenderPipelineTypes;
            public readonly bool editorForChildClasses;

            public CustomPropertyDrawerContainer(Type drawerType, Type[] supportedRenderPipelineTypes, bool editorForChildClasses)
            {
                this.drawerType = drawerType;
                this.supportedRenderPipelineTypes = supportedRenderPipelineTypes;
                this.editorForChildClasses = editorForChildClasses;
            }
        }

        // Internal API members
        internal static Stack<PropertyDrawer> s_DrawerStack = new Stack<PropertyDrawer>();
        private static Dictionary<string, List<PropertyAttribute>> s_BuiltinAttributes = null;
        static Dictionary<Type, List<FieldInfo>> s_AutoLoadProperties;
        private static PropertyHandler s_SharedNullHandler = new PropertyHandler();
        private static PropertyHandler s_NextHandler = new PropertyHandler();

        private static PropertyHandlerCache s_GlobalCache = new PropertyHandlerCache();
        private static PropertyHandlerCache s_CurrentCache = null;

        static readonly Lazy<Dictionary<Type, CustomPropertyDrawerContainer[]>> k_DrawerTypeForType = new(BuildDrawerTypeForTypeDictionary);
        static readonly Dictionary<Type, Type> k_DrawerStaticTypesCache = new();
        static readonly Dictionary<Type, Type[]> k_SupportedRenderPipelinesForSerializedObject = new();

        static readonly Comparer<CustomPropertyDrawerContainer> k_RenderPipelineTypeComparer
            = Comparer<CustomPropertyDrawerContainer>.Create((c1, c2)
                =>
            {
                var firstRenderPipelineByFullName1 = c1.supportedRenderPipelineTypes?.FirstOrDefaultSorted();
                var firstRenderPipelineByFullName2 = c2.supportedRenderPipelineTypes?.FirstOrDefaultSorted();
                return string.Compare(firstRenderPipelineByFullName1?.FullName, firstRenderPipelineByFullName2?.FullName, StringComparison.Ordinal);
            });


        static Type[] s_CurrentRenderPipelineAssetTypeArray;

        static Type[] currentRenderPipelineAssetTypeArray
        {
            get
            {
                if (s_CurrentRenderPipelineAssetTypeArray != null && s_CurrentRenderPipelineAssetTypeArray[0] == GraphicsSettings.currentRenderPipelineAssetType)
                    return s_CurrentRenderPipelineAssetTypeArray;

                s_CurrentRenderPipelineAssetTypeArray = new[] { GraphicsSettings.currentRenderPipelineAssetType };
                return s_CurrentRenderPipelineAssetTypeArray;
            }
        }


        static void ResetCachedTypesAndAsset()
        {
            k_DrawerStaticTypesCache.Clear();
            ClearGlobalCache();
        }

        internal static PropertyHandlerCache propertyHandlerCache
        {
            get => s_CurrentCache ?? s_GlobalCache;
            set => s_CurrentCache = value;
        }

        internal static void ClearGlobalCache()
        {
            s_GlobalCache.Clear();
        }

        private static void PopulateBuiltinAttributes()
        {
            s_BuiltinAttributes = new Dictionary<string, List<PropertyAttribute>>();

            AddBuiltinAttribute("TextMesh", "m_Text", new MultilineAttribute());
            // Example: Make Orthographic Size in Camera component be in range between 0 and 1000
            //AddBuiltinAttribute ("Camera", "m_OrthographicSize", new RangeAttribute (0, 1000));
        }

        private static void AddBuiltinAttribute(string componentTypeName, string propertyPath, PropertyAttribute attr)
        {
            string key = componentTypeName + "_" + propertyPath;
            if (!s_BuiltinAttributes.ContainsKey(key))
                s_BuiltinAttributes.Add(key, new List<PropertyAttribute>());
            s_BuiltinAttributes[key].Add(attr);
        }

        private static List<PropertyAttribute> GetBuiltinAttributes(SerializedProperty property)
        {
            if (property.serializedObject.targetObject == null)
                return null;
            Type t = property.serializedObject.targetObject.GetType();
            string attrKey = t.Name + "_" + property.propertyPath;
            List<PropertyAttribute> attr = null;
            s_BuiltinAttributes.TryGetValue(attrKey, out attr);
            return attr;
        }

        // Build a dictionary when k_DrawerTypeForType is first accessed
        static Dictionary<Type, CustomPropertyDrawerContainer[]> BuildDrawerTypeForTypeDictionary()
        {
            RenderPipelineManager.activeRenderPipelineDisposed += ResetCachedTypesAndAsset;
            RenderPipelineManager.activeRenderPipelineCreated += ResetCachedTypesAndAsset;

            var tempDictionary = new Dictionary<Type, List<CustomPropertyDrawerContainer>>();
            foreach (var drawerType in TypeCache.GetTypesDerivedFrom<GUIDrawer>())
            {
                //Debug.Log("Drawer: " + type);
                var customPropertyDrawers = drawerType.GetCustomAttributes<CustomPropertyDrawer>(true);
                var supportedOnRenderPipelineAttribute = drawerType.GetCustomAttribute<SupportedOnRenderPipelineAttribute>();
                foreach (CustomPropertyDrawer drawer in customPropertyDrawers)
                {
                    var propertyType = drawer.m_Type;
                    if (!tempDictionary.ContainsKey(propertyType))
                        tempDictionary.Add(propertyType, new List<CustomPropertyDrawerContainer>());

                    tempDictionary[propertyType].AddSorted(new CustomPropertyDrawerContainer(drawerType, supportedOnRenderPipelineAttribute?.renderPipelineTypes, drawer.m_UseForChildren),
                        k_RenderPipelineTypeComparer);
                }
            }

            var dictionaryWithArrays = new Dictionary<Type, CustomPropertyDrawerContainer[]>();
            foreach (var kvp in tempDictionary)
                dictionaryWithArrays.Add(kvp.Key, kvp.Value.ToArray());
            return dictionaryWithArrays;
        }

        /// <summary>
        /// We build our CustomPropertyDrawerContainer cache the first time we access it.
        /// It used solely to have a quick access to drawer types and their information.
        /// We have a separate cache to map Type -> Drawer. It will work with managed references too as we map real type to the drawers.
        /// This cache resets each type we create or dispose a Render Pipeline as SupportedOn can exist on some drawers.
        /// </summary>
        /// <param name="propertyType">Find a drawer for provided Type</param>
        /// <param name="renderPipelineAssetTypes">This can be either GraphicsSettings.currentRenderPipelineAssetType or type from SupportedOnRenderPipeline attribute for serializedObject</param>
        /// <param name="isPropertyTypeAManagedReference">Specify if it's known that we deal with ManagedReference property</param>
        /// <returns></returns>
        internal static Type GetDrawerTypeForType(Type propertyType, Type[] renderPipelineAssetTypes, bool isPropertyTypeAManagedReference = false)
        {
            //We map specific types to drawers and it will work with managed references too.
            if (k_DrawerStaticTypesCache.TryGetValue(propertyType, out var drawerTypeForType))
                return drawerTypeForType;

            var currentRenderPipelineAssetType = GraphicsSettings.currentRenderPipelineAssetType;
            Type[] allInterfaces = null;
            bool[] checkedInterfaces = null;
            for (var inspectedType = propertyType; inspectedType != null; inspectedType = inspectedType.BaseType)
            {
                //In the first case we don't need to check for some properties as we know that we deal not with a Base class
                //We check only if we have direct drawer and prepare to check interfaces in the next step if they exist
                var requestedPropertyType = inspectedType == propertyType;

                //Check for class drawers. For generics it would be 2 checks. One for generic type and one for generic definition.
                //For example, for A <bool> we will check first for A<bool> and then for A<T>.
                if (TryGetDrawerTypeForTypeFromCache(requestedPropertyType, renderPipelineAssetTypes, isPropertyTypeAManagedReference, inspectedType, currentRenderPipelineAssetType, out var drawerType))
                {
                    k_DrawerStaticTypesCache.Add(propertyType, drawerType);
                    return drawerType;
                }

                if (requestedPropertyType)
                {
                    //Gather all interfaces for requested property type
                    allInterfaces = inspectedType.GetInterfaces();
                    checkedInterfaces = new bool[allInterfaces.Length];

                    // Calculate inheritance level for each interface
                    var interfaceLevels = new Dictionary<Type, int>();
                    foreach (var interfaceType in allInterfaces)
                        CalculateInheritanceLevel(interfaceType, 0, interfaceLevels);

                    // Sort interfaces by inheritance level and then by name
                    Array.Sort(allInterfaces, (x, y) =>
                    {
                        var levelComparison = interfaceLevels[x].CompareTo(interfaceLevels[y]);
                        return levelComparison != 0 ? levelComparison : string.CompareOrdinal(x.Name, y.Name);
                    });
                }
                else if (allInterfaces.Length != 0)
                {
                    // Get all interfaces for the current inspected type
                    var baseTypeInterfaces = inspectedType.GetInterfaces();
                    for (int i = 0; i < allInterfaces.Length; i++)
                    {
                        // Skipped already checked interfaces
                        if (checkedInterfaces[i])
                            continue;

                        // Exclude interfaces that are implemented by the base type
                        var interfaceType = allInterfaces[i];
                        if (baseTypeInterfaces.ContainsByEquals(interfaceType))
                            continue;

                        // Check if there's an appropriate drawer for the interface
                        checkedInterfaces[i] = true;
                        if (TryGetDrawerTypeForTypeFromCache(requestedPropertyType, renderPipelineAssetTypes, isPropertyTypeAManagedReference, interfaceType, currentRenderPipelineAssetType, out drawerType))
                        {
                            k_DrawerStaticTypesCache.Add(propertyType, drawerType);
                            return drawerType;
                        }
                    }
                }
            }

            return null;
        }

        static int CalculateInheritanceLevel(Type interfaceType, int currentLevel, Dictionary<Type, int> processedInterfaces)
        {
            // Get parent interfaces
            var parentInterfaces = interfaceType.GetInterfaces();

            // Base case: if there are no parent interfaces, return current inheritance level
            if (parentInterfaces.Length == 0)
                return AddOrReplaceInterfaceLevel(currentLevel);

            // Recursive case: return the minimum inheritance level minus 1 of the parent interfaces
            var minParentLevel = int.MaxValue;
            for (var i = 0; i < parentInterfaces.Length; i++)
            {
                var parentLevel = CalculateInheritanceLevel(parentInterfaces[i], currentLevel + 1, processedInterfaces);
                if (parentLevel < minParentLevel)
                    minParentLevel = parentLevel;
            }

            return AddOrReplaceInterfaceLevel(minParentLevel - 1);

            //Local method just to simplify code
            int AddOrReplaceInterfaceLevel(int newLevel)
            {
                // Check if the interface has already been processed and update its inheritance level if necessary
                if (processedInterfaces.TryGetValue(interfaceType, out var level))
                {
                    // If the interface has already been processed and the new inheritance level is greater than the current one, update it
                    if (level >= newLevel)
                        return level;

                    processedInterfaces[interfaceType] = newLevel;
                    return newLevel;
                }
                processedInterfaces.Add(interfaceType, newLevel);
                return newLevel;
            }
        }

        static bool TryGetDrawerTypeForTypeFromCache(bool requestedPropertyType, Type[] renderPipelineAssetTypes, bool isPropertyTypeIsManagedReference, Type currentType, Type currentRenderPipelineAssetType,
            out Type drawerType)
        {
            drawerType = null;
            // check for exact type
            if (TryExtractDrawerTypeForTypeFromCache(requestedPropertyType, renderPipelineAssetTypes, isPropertyTypeIsManagedReference, currentType, currentRenderPipelineAssetType, out drawerType))
                return true;

            // check for base generic versions of the drawers
            if (!currentType.IsGenericType)
                return false;

            var genericDefinition = currentType.GetGenericTypeDefinition();
            return TryExtractDrawerTypeForTypeFromCache(requestedPropertyType, renderPipelineAssetTypes, isPropertyTypeIsManagedReference, genericDefinition, currentRenderPipelineAssetType, out drawerType);
        }

        static bool TryExtractDrawerTypeForTypeFromCache(bool requestedPropertyType, Type[] renderPipelineAssetTypes, bool isPropertyTypeIsManagedReference, Type currentType, Type currentRenderPipelineAssetType,
            out Type drawerType)
        {
            drawerType = null;
            // Extract drawers for the current type from the cache
            if (!k_DrawerTypeForType.Value.TryGetValue(currentType, out var drawerTypes))
                return false;

            // Check if there's an appropriate drawer for the current type and render pipeline. This is where we check for SupportedOnRenderPipelineAttribute
            var result = TryFindDrawers(renderPipelineAssetTypes, drawerTypes, currentRenderPipelineAssetType, out var customPropertyDrawerContainer);
            if (!IsAppropriateDrawerFound())
                return false;

            drawerType = customPropertyDrawerContainer.drawerType;
            return true;

            //Extracted just to simplify a condition
            bool IsAppropriateDrawerFound()
            {
                if (!result)
                    return false;

                //When we check for requested property type we don't need to check for editorForChildClasses and managed reference type
                if (requestedPropertyType)
                    return true;

                // Check for drawers with editorForChildClasses set to true and special case for managed references.
                // The custom property drawers for those are defined with 'useForChildren=false'
                // (otherwise the dynamic type is not taking into account in the custom property
                // drawer resolution) so even if 's_DrawerTypeForType' is built (based on static types)
                // we have to check base types for custom property drawers manually.
                // Managed references with no drawers should properly try to fallback
                return (customPropertyDrawerContainer.editorForChildClasses || isPropertyTypeIsManagedReference);
            }
        }

        static bool TryFindDrawers(Type[] renderPipelineAssetTypes, CustomPropertyDrawerContainer[] drawerTypes, Type currentRenderPipelineAssetType,
            out CustomPropertyDrawerContainer customPropertyDrawerContainer)
        {
            CustomPropertyDrawerContainer? supportedOnRenderPipelineDrawer = null;
            CustomPropertyDrawerContainer? supportedByBaseRenderPipelineDrawer = null;
            CustomPropertyDrawerContainer? regularDrawer = null;

            for (var i = 0; i < drawerTypes.Length; i++)
            {
                var drawerContainer = drawerTypes[i];
                if (drawerContainer.supportedRenderPipelineTypes == null)
                {
                    regularDrawer ??= drawerContainer;
                    continue;
                }

                if (renderPipelineAssetTypes == null)
                    continue;

                for (int j = 0; j < renderPipelineAssetTypes.Length; j++)
                {
                    var renderPipelineAssetType = renderPipelineAssetTypes[j];
                    var supportedMode = SupportedOnRenderPipelineAttribute.GetSupportedMode(drawerContainer.supportedRenderPipelineTypes, renderPipelineAssetType);
                    switch (supportedMode)
                    {
                        case SupportedOnRenderPipelineAttribute.SupportedMode.Supported:
                            if (supportedOnRenderPipelineDrawer == null || renderPipelineAssetType == currentRenderPipelineAssetType)
                                supportedOnRenderPipelineDrawer = drawerContainer;
                            break;
                        case SupportedOnRenderPipelineAttribute.SupportedMode.SupportedByBaseClass:
                            supportedByBaseRenderPipelineDrawer ??= drawerContainer;
                            break;
                    }
                }
            }

            if (supportedOnRenderPipelineDrawer.HasValue)
            {
                customPropertyDrawerContainer = supportedOnRenderPipelineDrawer.Value;
                return true;
            }

            if (supportedByBaseRenderPipelineDrawer.HasValue)
            {
                customPropertyDrawerContainer = supportedByBaseRenderPipelineDrawer.Value;
                return true;
            }

            customPropertyDrawerContainer = regularDrawer ?? default;
            return regularDrawer.HasValue;
        }

        /// <summary>
        /// Does the same thing as 'GetDrawerTypeForType' (with the same side effect of building the cache)
        /// but also plays well with Managed References. If the property that is used as a reference for the drawer
        /// query is of a managed reference type, the class parents are also looked up as fallbacks.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="type"></param>
        /// <returns>The custom property drawer type or 'null' if not found.</returns>
        internal static Type GetDrawerTypeForPropertyAndType(SerializedProperty property, Type type)
        {
            //Try to gather SupportedOn from Serialized Object and rely on this instead of GraphicsSettings.currentRenderPipelineAssetType if exist
            var serializedObject = property.serializedObject;
            var serializedObjectType = serializedObject.targetObject.GetType();
            if (!k_SupportedRenderPipelinesForSerializedObject.TryGetValue(serializedObjectType, out var supportedOnRenderPipelineTypes))
            {
                var supportedOn = serializedObjectType.GetCustomAttribute<SupportedOnRenderPipelineAttribute>();
                supportedOnRenderPipelineTypes = supportedOn?.renderPipelineTypes;
                k_SupportedRenderPipelinesForSerializedObject.Add(serializedObjectType, supportedOnRenderPipelineTypes);
            }

            //Choose to use SupportedOn from SO if exist or current RenderPipelineAsset.
            //If our SO has SupportedOn it will indicate to our system that this SO used specifically with defined Render Pipeline Asset types
            var renderPipelineAssetTypes = supportedOnRenderPipelineTypes ?? (GraphicsSettings.isScriptableRenderPipelineEnabled ? currentRenderPipelineAssetTypeArray : null);

            return GetDrawerTypeForType(type, renderPipelineAssetTypes, property.propertyType == SerializedPropertyType.ManagedReference);
        }

        private static List<PropertyAttribute> GetFieldAttributes(FieldInfo field)
        {
            if (field == null)
                return null;

            var attrs = field.GetCustomAttributes<PropertyAttribute>(true);
            Comparer<PropertyAttribute> comparer = null;
            List<PropertyAttribute> propertyAttributeList = null;
            foreach (PropertyAttribute attribute in attrs)
            {
                propertyAttributeList ??= new List<PropertyAttribute>();
                comparer ??= Comparer<PropertyAttribute>.Create((p1, p2) => p1.order.CompareTo(p2.order));

                propertyAttributeList.AddSorted(attribute, comparer);
            }

            return propertyAttributeList;
        }

        /// <summary>
        /// Returns the field info and field type for the property. The types are based on the
        /// static field definition.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static FieldInfo GetFieldInfoAndStaticTypeFromProperty(SerializedProperty property, out Type type)
        {
            var classType = GetScriptTypeFromProperty(property);
            if (classType == null)
            {
                type = null;
                return null;
            }

            var fieldPath = property.propertyPath;
            if (property.isReferencingAManagedReferenceField)
            {
                // When the field we are trying to access is a dynamic instance, things are a bit more tricky
                // since we cannot "statically" (looking only at the parent class field types) know the actual
                // "classType" of the parent class.

                // The issue also is that at this point our only view on the object is the very limited SerializedProperty.

                // So we have to:
                // 1. try to get the FQN from for the current managed type from the serialized data,
                // 2. get the path *in the current managed instance* of the field we are pointing to,
                // 3. foward that to 'GetFieldInfoFromPropertyPath' as if it was a regular field,

                var objectTypename = property.GetFullyQualifiedTypenameForCurrentTypeTreeInternal();
                GetTypeFromManagedReferenceFullTypeName(objectTypename, out classType);

                fieldPath = property.GetPropertyPathInCurrentManagedTypeTreeInternal();
            }

            if (classType == null)
            {
                type = null;
                return null;
            }

            return GetFieldInfoFromPropertyPath(classType, fieldPath, out type);
        }

        /// <summary>
        /// Returns the field info and type for the property. Contrary to GetFieldInfoAndStaticTypeFromProperty,
        /// when confronted with a managed reference the dynamic instance type is returned.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static FieldInfo GetFieldInfoFromProperty(SerializedProperty property, out Type type)
        {
            var fieldInfo = GetFieldInfoAndStaticTypeFromProperty(property, out type);
            if (fieldInfo == null)
                return null;

            // Managed references are a special case, we need to override the static type
            // returned by 'GetFieldInfoFromPropertyPath' for custom property handler matching
            // by the dynamic type of the instance.
            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                // Try to get a Type instance for the managed reference
                if (GetTypeFromManagedReferenceFullTypeName(property.managedReferenceFullTypename, out var managedReferenceInstanceType))
                {
                    type = managedReferenceInstanceType;
                }

                // We keep the fallback to the field type returned by 'GetFieldInfoFromPropertyPath'.
            }

            return fieldInfo;
        }

        /// <summary>
        /// Create a Type instance from the managed reference full type name description.
        /// The expected format for the typename string is the one returned by SerializedProperty.managedReferenceFullTypename.
        /// </summary>
        /// <param name="managedReferenceFullTypename"></param>
        /// <param name="managedReferenceInstanceType"></param>
        /// <returns></returns>
        internal static bool GetTypeFromManagedReferenceFullTypeName(string managedReferenceFullTypename, out Type managedReferenceInstanceType)
        {
            managedReferenceInstanceType = null;

            var splitIndex = managedReferenceFullTypename.IndexOf(' ');
            if (splitIndex > 0)
            {
                var assemblyPart = managedReferenceFullTypename.Substring(0, splitIndex);
                var nsClassnamePart = managedReferenceFullTypename.Substring(splitIndex);
                managedReferenceInstanceType = Type.GetType($"{nsClassnamePart}, {assemblyPart}");
            }

            return managedReferenceInstanceType != null;
        }

        private static Type GetScriptTypeFromProperty(SerializedProperty property)
        {
            if (property.serializedObject.targetObject != null)
                return property.serializedObject.targetObject.GetType();

            // Fallback in case the targetObject has been destroyed but the property is still valid.
            SerializedProperty scriptProp = property.serializedObject.FindProperty("m_Script");

            if (scriptProp == null)
                return null;

            MonoScript script = scriptProp.objectReferenceValue as MonoScript;

            if (script == null)
                return null;

            return script.GetClass();
        }

        struct Cache : IEquatable<Cache>
        {
            Type host;
            string path;

            public Cache(Type host, string path)
            {
                this.host = host;
                this.path = path;
            }

            public bool Equals(Cache other)
            {
                return Equals(host, other.host) && string.Equals(path, other.path);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is Cache cache && Equals(cache);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((host != null ? host.GetHashCode() : 0) * 397) ^ (path != null ? path.GetHashCode() : 0);
                }
            }
        }

        class FieldInfoCache
        {
            public FieldInfo fieldInfo;
            public Type type;
        }

        static Dictionary<Cache, FieldInfoCache> s_FieldInfoFromPropertyPathCache = new Dictionary<Cache, FieldInfoCache>();

        private static FieldInfo GetFieldInfoFromPropertyPath(Type host, string path, out Type type)
        {
            Cache cache = new Cache(host, path);

            if (s_FieldInfoFromPropertyPathCache.TryGetValue(cache, out var fieldInfoCache))
            {
                type = fieldInfoCache?.type;
                return fieldInfoCache?.fieldInfo;
            }

            const string arrayData = @"\.Array\.data\[[0-9]+\]";
            // we are looking for array element only when the path ends with Array.data[x]
            var lookingForArrayElement = Regex.IsMatch(path, arrayData + "$");
            // remove any Array.data[x] from the path because it is prevents cache searching.
            path = Regex.Replace(path, arrayData, ".___ArrayElement___");

            FieldInfo fieldInfo = null;
            type = host;
            string[] parts = path.Split('.');
            for (int i = 0; i < parts.Length; i++)
            {
                string member = parts[i];
                // GetField on class A will not find private fields in base classes to A,
                // so we have to iterate through the base classes and look there too.
                // Private fields are relevant because they can still be shown in the Inspector,
                // and that applies to private fields in base classes too.
                FieldInfo foundField = null;
                for (Type currentType = type; foundField == null && currentType != null; currentType = currentType.BaseType)
                    foundField = currentType.GetField(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (foundField == null)
                {
                    type = null;
                    s_FieldInfoFromPropertyPathCache.Add(cache, null);
                    return null;
                }

                fieldInfo = foundField;
                type = fieldInfo.FieldType;
                // we want to get the element type if we are looking for Array.data[x]
                if (i < parts.Length - 1 && parts[i + 1] == "___ArrayElement___" && type.IsArrayOrList())
                {
                    i++; // skip the "___ArrayElement___" part
                    type = type.GetArrayOrListElementType();
                }
            }

            // we want to get the element type if we are looking for Array.data[x]
            if (lookingForArrayElement && type != null && type.IsArrayOrList())
                type = type.GetArrayOrListElementType();

            fieldInfoCache = new FieldInfoCache
            {
                type = type,
                fieldInfo = fieldInfo
            };
            s_FieldInfoFromPropertyPathCache.Add(cache, fieldInfoCache);
            return fieldInfo;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static PropertyHandler GetHandler(SerializedProperty property)
        {
            if (property == null)
                return s_SharedNullHandler;

            // Don't use custom drawers in debug mode
            if (property.serializedObject.inspectorMode != InspectorMode.Normal)
                return s_SharedNullHandler;

            // If the drawer is cached, use the cached drawer
            PropertyHandler handler = propertyHandlerCache.GetHandler(property);
            if (handler != null)
                return handler;

            Type propertyType = null;
            List<PropertyAttribute> attributes = null;
            FieldInfo field = null;

            // Determine if SerializedObject target is a script or a builtin type
            Object target = property.serializedObject.targetObject;
            if (NativeClassExtensionUtilities.ExtendsANativeType(target))
            {
                // For scripts, use reflection to get FieldInfo for the member the property represents
                field = GetFieldInfoFromProperty(property, out propertyType);

                // Use reflection to see if this member has an attribute
                attributes = GetFieldAttributes(field);
            }
            else
            {
                // For builtin types, look if we hardcoded an attribute for this property
                // First initialize the hardcoded properties if not already done
                if (s_BuiltinAttributes == null)
                    PopulateBuiltinAttributes();

                attributes = GetBuiltinAttributes(property);
            }

            handler = s_NextHandler;

            if (attributes != null)
            {
                for (int i = 0; i < attributes.Count; i++)
                    handler.HandleAttribute(property, attributes[i], field, propertyType);
            }

            // Field has no CustomPropertyDrawer attribute with matching drawer so look for default drawer for field type
            if (!handler.hasPropertyDrawer && propertyType != null)
                handler.HandleDrawnType(property, propertyType, propertyType, field, null);

            if (handler.empty)
            {
                propertyHandlerCache.SetHandler(property, s_SharedNullHandler);
                handler = s_SharedNullHandler;
            }
            else
            {
                propertyHandlerCache.SetHandler(property, handler);
                s_NextHandler = new PropertyHandler();
            }

            return handler;
        }

        internal static bool CanUseSameHandler(SerializedProperty p1, SerializedProperty p2)
        {
            return PropertyHandlerCache.CanUseSameHandler(p1, p2);
        }

        internal static List<FieldInfo> GetAutoLoadProperties(Type type)
        {
            if (s_AutoLoadProperties == null)
                s_AutoLoadProperties = new Dictionary<Type, List<FieldInfo>>();

            if (!s_AutoLoadProperties.TryGetValue(type, out var list))
            {
                list = new List<FieldInfo>();
                foreach (var field in type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
                {
                    if (field.FieldType == typeof(SerializedProperty) && field.IsDefined(typeof(CachePropertyAttribute), false))
                        list.Add(field);
                }

                s_AutoLoadProperties.Add(type, list);
            }

            return list;
        }
    }
}
