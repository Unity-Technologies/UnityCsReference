// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    internal class MemoryElementDataManager
    {
        private static int SortByMemoryClassName(ObjectInfo x, ObjectInfo y)
        {
            return y.className.CompareTo(x.className);
        }

        private static int SortByMemorySize(MemoryElement x, MemoryElement y)
        {
            return y.totalMemory.CompareTo(x.totalMemory);
        }

        enum ObjectTypeFilter { Scene, Asset, BuiltinResource, DontSave, Other }

        static ObjectTypeFilter GetObjectTypeFilter(ObjectInfo info)
        {
            var reason = (MemoryInfoGCReason)info.reason;

            switch (reason)
            {
                case MemoryInfoGCReason.AssetReferenced:
                case MemoryInfoGCReason.AssetReferencedByNativeCodeOnly:
                case MemoryInfoGCReason.AssetMarkedDirtyInEditor:
                    return ObjectTypeFilter.Asset;
                case MemoryInfoGCReason.BuiltinResource:
                    return ObjectTypeFilter.BuiltinResource;
                case MemoryInfoGCReason.MarkedDontSave:
                    return ObjectTypeFilter.DontSave;
                case MemoryInfoGCReason.NotApplicable:
                    return ObjectTypeFilter.Other;
                default:
                    return ObjectTypeFilter.Scene;
            }
        }

        static bool HasValidNames(List<MemoryElement> memory)
        {
            for (int i = 0; i < memory.Count; i++)
            {
                if (!string.IsNullOrEmpty(memory[i].name))
                    return true;
            }
            return false;
        }

        static List<MemoryElement> GenerateObjectTypeGroups(ObjectInfo[] memory, ObjectTypeFilter filter)
        {
            var objectGroups = new List<MemoryElement>();

            // Generate groups
            MemoryElement group = null;
            foreach (ObjectInfo t in memory)
            {
                if (GetObjectTypeFilter(t) != filter)
                    continue;

                if (group == null || t.className != group.name)
                {
                    group = new MemoryElement(t.className);
                    objectGroups.Add(group);

                    //group.name = t.className;
                    //group.color = GetColorFromClassName(t.className);
                }

                group.AddChild(new MemoryElement(t, true));
            }

            objectGroups.Sort(SortByMemorySize);


            // Sort by memory
            foreach (MemoryElement typeGroup in objectGroups)
            {
                typeGroup.children.Sort(SortByMemorySize);

                if (filter == ObjectTypeFilter.Other && !HasValidNames(typeGroup.children))
                    typeGroup.children.Clear();
            }

            return objectGroups;
        }

        static public MemoryElement GetTreeRoot(ObjectMemoryInfo[] memoryObjectList, int[] referencesIndices)
        {
            // run through all objects and do conversion from references to referenced_by
            ObjectInfo[] allObjectInfo = new ObjectInfo[memoryObjectList.Length];

            for (int i = 0; i < memoryObjectList.Length; i++)
            {
                ObjectInfo info = new ObjectInfo();
                info.instanceId = memoryObjectList[i].instanceId;
                info.memorySize = memoryObjectList[i].memorySize;
                info.reason = memoryObjectList[i].reason;
                info.name = memoryObjectList[i].name;
                info.className = memoryObjectList[i].className;
                allObjectInfo[i] = info;
            }
            int offset = 0;
            for (int i = 0; i < memoryObjectList.Length; i++)
            {
                for (int j = 0; j < memoryObjectList[i].count; j++)
                {
                    int referencedObjectIndex = referencesIndices[j + offset];
                    if (allObjectInfo[referencedObjectIndex].referencedBy == null)
                        allObjectInfo[referencedObjectIndex].referencedBy = new List<ObjectInfo>();
                    allObjectInfo[referencedObjectIndex].referencedBy.Add(allObjectInfo[i]);
                }
                offset += memoryObjectList[i].count;
            }

            MemoryElement root = new MemoryElement();
            System.Array.Sort(allObjectInfo, SortByMemoryClassName);
            root.AddChild(new MemoryElement("Scene Memory", GenerateObjectTypeGroups(allObjectInfo, ObjectTypeFilter.Scene)));
            root.AddChild(new MemoryElement("Assets", GenerateObjectTypeGroups(allObjectInfo, ObjectTypeFilter.Asset)));
            root.AddChild(new MemoryElement("Builtin Resources", GenerateObjectTypeGroups(allObjectInfo, ObjectTypeFilter.BuiltinResource)));
            root.AddChild(new MemoryElement("Not Saved", GenerateObjectTypeGroups(allObjectInfo, ObjectTypeFilter.DontSave)));
            root.AddChild(new MemoryElement("Other", GenerateObjectTypeGroups(allObjectInfo, ObjectTypeFilter.Other)));
            root.children.Sort(SortByMemorySize);

            return root;
        }
    }
}
