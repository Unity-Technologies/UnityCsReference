// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    internal class BlockDropdownItem : AdvancedDropdownItem
    {
        public Type BlockType { get; }
        public string Description { get; }

        public BlockDropdownItem(string name, Type blockType, string description = null) : base(name)
        {
            BlockType = blockType;
            Description = description;
        }
    }

    internal partial class BlockDropdownItemCache : IBlockDropdownItemCache
    {
        [AutoStaticsCleanupOnCodeReload]
        static Dictionary<Type, AdvancedDropdownItem> s_CachedRoots = new Dictionary<Type, AdvancedDropdownItem>();

        public AdvancedDropdownItem GetOrBuildDropdownRoot(Type requiredInterface)
        {
            if (s_CachedRoots.TryGetValue(requiredInterface, out var cachedRoot))
            {
                return CloneDropdownItem(cachedRoot);
            }

            var root = BuildDropdownRoot(requiredInterface);
            s_CachedRoots[requiredInterface] = CloneDropdownItem(root);
            return root;
        }

        internal static bool IsInstantiatable(Type t)
        {
            if (t.IsAbstract || t.IsInterface) return false;
            if (!t.IsDefined(typeof(SerializableAttribute), inherit: false)) return false;  
            if (t.GetConstructor(Type.EmptyTypes) == null) return false;
            var attr = t.GetCustomAttribute<ImportBlockAttribute>(inherit: false);
            return attr != null && attr.Instantiatable;
        }

        AdvancedDropdownItem BuildDropdownRoot(Type requiredInterface)
        {
            var root = new AdvancedDropdownItem("Add block");
            var iBlockType = typeof(IBlock);

            if (requiredInterface == iBlockType)
            {
                var allIBlockTypes = TypeCache.GetTypesDerivedFrom<IBlock>();

                // Find all direct child interfaces of IBlock (exclude indirect descendants)
                var directChildInterfaces = new List<Type>();
                foreach (var t in allIBlockTypes)
                {
                    if (!t.IsInterface || t == iBlockType)
                        continue;

                    var interfaces = t.GetInterfaces();
                    bool containsIBlock = false;
                    int iBlockCount = 0;
                    bool allInterfacesValid = true;

                    foreach (var i in interfaces)
                    {
                        if (i == iBlockType)
                        {
                            containsIBlock = true;
                            iBlockCount++;
                        }
                        else if (iBlockType.IsAssignableFrom(i) && i != t)
                        {
                            allInterfacesValid = false;
                            break;
                        }
                    }

                    if (containsIBlock &&
                        (t.BaseType == null || !iBlockType.IsAssignableFrom(t.BaseType)) &&
                        iBlockCount == 1 &&
                        allInterfacesValid)
                    {
                        directChildInterfaces.Add(t);
                    }
                }

                directChildInterfaces.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

                // For each direct child interface, find all concrete types that implement it
                foreach (var iface in directChildInterfaces)
                {
                    var implementingTypes = new List<Type>();
                    foreach (var t in TypeCache.GetTypesDerivedFrom(iface))
                    {
                        if (IsInstantiatable(t))
                            implementingTypes.Add(t);
                    }

                    implementingTypes.Sort((a, b) => string.CompareOrdinal(GetBlockMetadata(a).name, GetBlockMetadata(b).name));

                    if (implementingTypes.Count > 0)
                    {
                        var parentMenuName = FormatParentMenuName(iface.Name);
                        var parentItem = new AdvancedDropdownItem(parentMenuName.TrimEnd('/'));
                        root.AddChild(parentItem);

                        foreach (var blockType in implementingTypes)
                        {
                            var (blockName, blockDescription) = GetBlockMetadata(blockType);
                            var blockItem = new BlockDropdownItem(blockName, blockType, blockDescription);
                            parentItem.AddChild(blockItem);
                        }
                    }
                }

                // Also support direct IBlock implementations (not via sub-interfaces)
                var directBlockTypes = new List<Type>();
                foreach (var t in allIBlockTypes)
                {
                    if (!IsInstantiatable(t))
                        continue;

                    var interfaces = t.GetInterfaces();
                    bool isDirectImplementation = true;
                    foreach (var i in interfaces)
                    {
                        if (i != iBlockType)
                        {
                            foreach (var childInterface in directChildInterfaces)
                            {
                                if (i == childInterface)
                                {
                                    isDirectImplementation = false;
                                    break;
                                }
                            }

                            if (!isDirectImplementation)
                                break;
                        }
                    }

                    if (isDirectImplementation)
                        directBlockTypes.Add(t);
                }

                directBlockTypes.Sort((a, b) => string.CompareOrdinal(GetBlockMetadata(a).name, GetBlockMetadata(b).name));

                foreach (var blockType in directBlockTypes)
                {
                    var (blockName, blockDescription) = GetBlockMetadata(blockType);
                    var blockItem = new BlockDropdownItem(blockName, blockType, blockDescription);
                    root.AddChild(blockItem);
                }
            }
            else
            {
                var blockTypes = new List<Type>();
                foreach (var t in TypeCache.GetTypesDerivedFrom(requiredInterface))
                {
                    if (IsInstantiatable(t))
                        blockTypes.Add(t);
                }
                blockTypes.Sort((a, b) => string.CompareOrdinal(GetBlockMetadata(a).name, GetBlockMetadata(b).name));

                foreach (var blockType in blockTypes)
                {
                    var (blockName, blockDescription) = GetBlockMetadata(blockType);
                    root.AddChild(new BlockDropdownItem(blockName, blockType, blockDescription));
                }
            }

            return root;
        }

        (string name, string description) GetBlockMetadata(Type blockType)
        {
            var attr = blockType.GetCustomAttribute<ImportBlockAttribute>(inherit: false);
            var name = !string.IsNullOrEmpty(attr?.Name) ? attr.Name : Block.FormatTypeName(blockType.Name);
            var description = !string.IsNullOrEmpty(attr?.Description) ? attr.Description : null;
            return (name, description);
        }

        string FormatParentMenuName(string menuName)
        {
            if (menuName.StartsWith("I") && menuName.Length > 1 && char.IsUpper(menuName[1]))
                menuName = menuName.Substring(1); // Remove leading 'I'
            menuName = Regex.Replace(menuName, "(\\B[A-Z])", " $1");
            menuName += "/";
            return menuName;
        }

        AdvancedDropdownItem CloneDropdownItem(AdvancedDropdownItem source)
        {
            AdvancedDropdownItem clone = source is BlockDropdownItem blockSource
                ? new BlockDropdownItem(source.name, blockSource.BlockType, blockSource.Description)
                : new AdvancedDropdownItem(source.name);

            foreach (var child in source.childList)
            {
                clone.AddChild(CloneDropdownItem(child));
            }

            return clone;
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        static void OnScriptsReloaded()
        {
            s_CachedRoots.Clear();

            // Preload common types
            EditorApplication.delayCall += () =>
            {
                try
                {
                    var cache = new BlockDropdownItemCache();
                    cache.GetOrBuildDropdownRoot(typeof(IBlock));
                }
                catch (Exception e)
                {
                    Debug.Log($"[ImportBlocks] Type cache preload failed: {e.Message}");
                }
            };
        }
    }
}
