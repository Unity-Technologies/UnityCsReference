// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Debug = Unity.Scripting.LowLevel.Debug;

namespace Unity.Entities
{
    [BurstCompile]
    internal class ComponentSystemSorter
    {
        private struct Heap
        {
            private readonly UnsafeList<TypeHeapElement> _elements;
            private int _size;
            private readonly int _capacity;
            private static readonly int BaseIndex = 1;

            public Heap(int capacity)
            {
                _capacity = capacity;
                _size = 0;
                var initialCapacity = capacity + BaseIndex;
                _elements = new UnsafeList<TypeHeapElement>(initialCapacity, Allocator.Temp);
                _elements.Length = initialCapacity;
            }

            public bool Empty => _size <= 0;

            public void Insert(TypeHeapElement e)
            {
                if (_size >= _capacity)
                {
                    throw new InvalidOperationException($"Attempted to Insert() to a full heap.");
                }
                var i = BaseIndex + _size++;
                while (i > BaseIndex)
                {
                    var parent = i / 2;

                    if (e.CompareTo(_elements[parent]) > 0)
                    {
                        break;
                    }

                    _elements.ElementAt(i) = _elements[parent];
                    i = parent;
                }

                _elements.ElementAt(i) = e;
            }

            public TypeHeapElement Peek()
            {
                if (Empty)
                {
                    throw new InvalidOperationException($"Attempted to Peek() an empty heap.");
                }
                return _elements[BaseIndex];
            }

            public TypeHeapElement Extract()
            {
                if (Empty)
                {
                    throw new InvalidOperationException($"Attempted to Extract() from an empty heap.");
                }
                var top = _elements[BaseIndex];
                _elements.ElementAt(BaseIndex) = _elements[_size--];
                if (!Empty)
                {
                    Heapify(BaseIndex);
                }

                return top;
            }

            private void Heapify(int i)
            {
                // The index taken by this function is expected to be already biased by BaseIndex.
                // Thus, m_Heap[size] is a valid element (specifically, the final element in the heap)
                //Debug.Assert(i >= BaseIndex && i < (_size+BaseIndex), $"heap index {i} is out of range with size={_size}");
                var val = _elements[i];
                while (i <= _size / 2)
                {
                    var child = 2 * i;
                    if (child < _size && _elements[child + 1].CompareTo(_elements[child]) < 0)
                    {
                        child++;
                    }

                    if (val.CompareTo(_elements[child]) < 0)
                    {
                        break;
                    }

                    _elements.ElementAt(i) = _elements[child];
                    i = child;
                }

                _elements.ElementAt(i) = val;
            }
        }

        public struct TypeHeapElement : IComparable<TypeHeapElement>
        {
            private readonly SystemTypeIndex systemTypeIndex;
            private readonly long systemTypeHash;
            public int unsortedIndex;

            public TypeHeapElement(int index, SystemTypeIndex _systemTypeIndex)
            {
                unsortedIndex = index;
                systemTypeIndex = _systemTypeIndex;
                systemTypeHash = TypeManager.GetSystemTypeHash(systemTypeIndex);
            }

            public int CompareTo(TypeHeapElement other)
            {
                var cmp = systemTypeHash.CompareTo(other.systemTypeHash);
                return cmp != 0 ? cmp : unsortedIndex.CompareTo(other.unsortedIndex);
            }
        }

        internal static unsafe int LookupSystemElement(SystemTypeIndex typeIndex, NativeHashMap<SystemTypeIndex, int>* lookupDictionaryPtr)
        {
            ref var mylookupDictionary = ref *lookupDictionaryPtr;

            if (mylookupDictionary.ContainsKey(typeIndex))
                return mylookupDictionary[typeIndex];
            return -1;
        }

        internal struct SystemElement 
        {
            public SystemTypeIndex SystemTypeIndex;
            public UpdateIndex Index;
            public int OrderingBucket; // 0 = OrderFirst, 1 = none, 2 = OrderLast
            public NativeList<int> updateBefore;
            public int nAfter;
        }


        internal static unsafe void Sort(
            UnsafeList<SystemElement>* elementsptr,
            NativeHashMap<SystemTypeIndex, int>* lookupDictionary)
        {
            var badTypeIndices = new NativeList<SystemTypeIndex>(16, Allocator.Temp);
            
            // Find & validate constraints between systems in the group
            var badTypeIndicesPtr = (NativeList<SystemTypeIndex>*)UnsafeUtility.AddressOf(ref badTypeIndices);
            SortInternal(elementsptr, lookupDictionary, badTypeIndicesPtr);
            
            //the below can't be bursted yet because of https://jira.unity3d.com/browse/BUR-2232 and friends, which are
            //slated to be fixed in 1.8.4. 
            if (badTypeIndices.Length > 0)
            {
                FixedString4096Bytes msg = "The following systems form a circular dependency cycle (check their [*Before]/[*After] attributes):\n";

                FixedString32Bytes newline = "\n";

                for (int i=0; i< badTypeIndices.Length; i++)
                {
                    FixedString512Bytes line = "- ";
                    line.Append(TypeManager.GetSystemName(badTypeIndices[i]));
                    msg.Append(line); 

                    if (i < badTypeIndices.Length - 1)
                    {
                        msg.Append(newline);
                    }
                }
                        
                throw new InvalidOperationException(msg.ToString());
            }
        }
        
        [BurstCompile]
        internal static unsafe void SortInternal(
            UnsafeList<SystemElement>* elementsptr,
            NativeHashMap<SystemTypeIndex, int>* lookupDictionary, 
            NativeList<SystemTypeIndex>* badSystemTypeIndices)
        {
            ref var elements = ref *elementsptr;
            lookupDictionary->Clear();
            
            var sortedElements = new UnsafeList<SystemElement>(elements.Length,
                Allocator.Temp);
            sortedElements.Length = elements.Length;
            var sortedIndices = new UnsafeList<int>(elements.Length,
                Allocator.Temp);
            sortedIndices.Length = elements.Length;
            
            int nextOutIndex = 0;

            var readySystems = new Heap(elements.Length);
            
            for (int i = 0; i < elements.Length; ++i)
            {
                
                if (elements[i].nAfter == 0)
                {
                    readySystems.Insert(new TypeHeapElement(i, elements[i].SystemTypeIndex));
                }
            }
            
            PopulateSystemElementLookup(lookupDictionary, elements);

            while (!readySystems.Empty)
            {
                var sysIndex = readySystems.Extract().unsortedIndex;
                var elem = elements[sysIndex];

                sortedElements[nextOutIndex++] = new SystemElement
                {
                    SystemTypeIndex = elem.SystemTypeIndex,
                    Index = elem.Index, 
                    nAfter = elem.nAfter, 
                    updateBefore = elem.updateBefore, 
                    OrderingBucket = elem.OrderingBucket
                    
                };
                foreach (var beforeType in elem.updateBefore)
                {
                    int beforeIndex = LookupSystemElement(beforeType, lookupDictionary);
                    if (beforeIndex < 0) throw new Exception("Bug in SortSystemUpdateList(), beforeIndex < 0");
                    if (elements[beforeIndex].nAfter <= 0)
                        throw new Exception("Bug in SortSystemUpdateList(), nAfter <= 0");

                    elements.ElementAt(beforeIndex).nAfter--;
                    if (elements[beforeIndex].nAfter == 0)
                    {
                        readySystems.Insert(new TypeHeapElement(beforeIndex, elements[beforeIndex].SystemTypeIndex));
                    }
                }
                elements.ElementAt(sysIndex).nAfter = -1; // "Remove()"
            }

            
            if (nextOutIndex < elements.Length)
            {
               /*
                * we failed to sort all the things, which happens if and only if there's a cycle in the before/after
                * graph. but, the unsorted things will also include any systems that were supposed to be after the
                * systems in a cycle. 
                *
                * We should actually throw the exception right here inside burst, but we are blocked by Burst bugs
                * https://jira.unity3d.com/browse/BUR-2245
                * https://jira.unity3d.com/browse/BUR-2231
                * https://jira.unity3d.com/browse/BUR-2216
                * so instead we write down the indices and throw outside burst.
                */

                var tmp = new NativeHashSet<int>(nextOutIndex, Allocator.Temp);
                for (int i = 0; i < nextOutIndex; i++)
                {
                    tmp.Add(sortedElements[i].SystemTypeIndex);
                }

                for (int i = 0; i < elements.Length; i++)
                {
                    if (!tmp.Contains(elements[i].SystemTypeIndex))
                    {
                        badSystemTypeIndices->Clear();
                        FindExactCycleInSystemGraph(elements[i].SystemTypeIndex, elements, lookupDictionary, badSystemTypeIndices->m_ListData);
                        if (badSystemTypeIndices->Length > 0)
                        {
                            //we found a cycle, so we're done
                            return;
                        }
                        //if we didn't write anything into the array, the type index in question must not been just
                        //downstream of a cycle, rather than actually part of the cycle, so just try to find a cycle
                        //starting from another system. 
                    }
                }

                throw new InvalidOperationException(
                    "Internal error: failed sorting systems but also couldn't find a cycle in the system graph. Please report this with Help->Report a bug...");
            }
            else
            {
                elements.CopyFrom(sortedElements);
            }
        }

        private static unsafe void FindExactCycleInSystemGraph(
            SystemTypeIndex startingSystemTypeIndex,
            UnsafeList<SystemElement> elements,
            NativeHashMap<SystemTypeIndex, int>* lookup,
            UnsafeList<SystemTypeIndex>* finalCycle)
        {
            var indexInList = -1;
            for (int i = 0; i < elements.Length; i++)
            {
                if (elements[i].SystemTypeIndex == startingSystemTypeIndex)
                {
                    indexInList = i;
                    break;
                }
            }

            if (indexInList == -1)
            {
                throw new InvalidOperationException("Internal error starting type index was bad couldn't find it in list");
            }

            var pathSoFarInTypeIndices = new NativeList<int>(16, Allocator.Temp);
            var visitedSoFarTypeIndices = new NativeHashSet<int>(elements.Length, Allocator.Temp);

            var currentSystemTypeIndex = startingSystemTypeIndex;
            var currentIndexInList = indexInList;
            visitedSoFarTypeIndices.Add(currentSystemTypeIndex);

            while (visitedSoFarTypeIndices.Count > 0)
            {
                var continueflag = false;
                for (int i = 0; i < elements[currentIndexInList].updateBefore.Length; i++)
                {
                    var newTypeIndex = elements[currentIndexInList].updateBefore[i];
                    var cycleStart = pathSoFarInTypeIndices.IndexOf(newTypeIndex);

                    if (cycleStart != -1)
                    {
                        //found a cycle! make sure not to miss the last node
                        pathSoFarInTypeIndices.Add(currentSystemTypeIndex);

                        for (int j = cycleStart; j < pathSoFarInTypeIndices.Length; j++)
                        {
                            finalCycle->Add(pathSoFarInTypeIndices[j]);
                        }

                        //finalcycle represents the exact cycle now
                        return;
                    }
                    
                    if (!visitedSoFarTypeIndices.Contains(newTypeIndex))
                    {
                        pathSoFarInTypeIndices.Add(currentSystemTypeIndex);
                        visitedSoFarTypeIndices.Add(currentSystemTypeIndex);
                        
                        //we've never been here before, so expand this node
                        currentSystemTypeIndex = newTypeIndex;
                        currentIndexInList = LookupSystemElement(currentSystemTypeIndex, lookup);
                        
                        continueflag = true;
                        break;
                    }
                }

                if (!continueflag)
                {
                    //if we get here, we looked at all the constraints of the current element and we had seen them all before,
                    //and none of them formed a cycle with anything in the path so far.
                    //so, we have to backtrack.

                    if (pathSoFarInTypeIndices.Length > 0)
                    {
                        var newCurrentSystemTypeIndex = pathSoFarInTypeIndices[pathSoFarInTypeIndices.Length-1];
                        pathSoFarInTypeIndices.RemoveAt(pathSoFarInTypeIndices.Length-1);
                        visitedSoFarTypeIndices.Remove(newCurrentSystemTypeIndex);
                        currentSystemTypeIndex = newCurrentSystemTypeIndex;
                        currentIndexInList = LookupSystemElement(currentSystemTypeIndex, lookup);
                    }
                }
            }
        }


        private static unsafe void PopulateSystemElementLookup(NativeHashMap<SystemTypeIndex, int>* lookupDictionary, UnsafeList<SystemElement> elements)
        {
            //fill in for fast lookups in LookupSystemElement
            lookupDictionary->Capacity = elements.Length;
            for (int i = 0; i < elements.Length; ++i)
            {
                (*lookupDictionary)[elements[i].SystemTypeIndex] = i;
            }
        }


        internal static unsafe void WarnAboutAnySystemAttributeBadness(int systemTypeIndex, ComponentSystemGroup group)
        {
            var systemType = TypeManager.GetSystemType(systemTypeIndex);
            var updateInGroups =
                TypeManager.GetSystemAttributes(systemType, typeof(UpdateInGroupAttribute));
            Type groupType = null;
            if (group != null)
            {
                groupType = group.GetType();
                UpdateInGroupAttribute groupAttr;
                foreach (var attr in updateInGroups)
                {
                    groupAttr = (UpdateInGroupAttribute)attr;
                    groupType = groupAttr.GroupType;
                    if (!TypeManager.IsSystemType(groupType))
                    {
                        Debug.LogWarning(
                            $"Ignoring invalid UpdateInGroup attribute on {systemType} targeting {groupType}, because {groupType} is not a system type.");
                        continue;
                    }

                    if (!TypeManager.IsSystemAGroup(groupType))
                    {
                        Debug.LogWarning(
                            $"Ignoring invalid UpdateInGroup attribute on {systemType} targeting {groupType}, because {groupType} is not a ComponentSystemGroup.");
                        continue;
                    }

                    if (groupType == systemType)
                    {
                        Debug.LogWarning(
                            $"Ignoring invalid UpdateInGroup attribute on {systemType} because a system group cannot be updated inside itself.\n");
                        continue;
                    }

                    if (groupAttr.OrderFirst && groupAttr.OrderLast)
                    {
                        Debug.LogWarning(
                            $"Ignoring invalid OrderFirst & OrderLast directives on UpdateInGroup attribute on {systemType} because a system cannot be ordered both first and last in a group.");
                    }
                }
            }

            foreach (var attrType in new[]
                     {
                         typeof(UpdateAfterAttribute),
                         typeof(UpdateBeforeAttribute),
                         typeof(CreateAfterAttribute),
                         typeof(CreateBeforeAttribute)
                     })
            {
                var updates = TypeManager.GetSystemAttributes(systemType, attrType);

                var field = attrType.GetProperty("SystemType");
                foreach (var attr in updates)
                {
                    var targetType = (Type)field.GetValue(attr);
                    
                    if (!TypeManager.IsSystemType(targetType))
                    {
                        Debug.LogWarning(
                            $"Ignoring invalid [{attrType}] attribute on {systemType} targeting {targetType}, because {targetType} is not a subclass of ComponentSystemBase and does not implement ISystem");
                        continue;
                    }

                    if (targetType == systemType)
                    {
                        Debug.LogWarning(
                            $"Ignoring invalid [{attrType}] attribute on {systemType} because a system cannot be updated or created after or before itself.\n");
                        continue;
                    }

                    if (group != null && attrType.Name.Contains("Update"))
                    {
                        if (TypeManager.IsSystemManaged(targetType))
                        {
                            bool foundTargetType = false;
                            for (int i = 0; i < group.m_managedSystemsToUpdate.Count; i++)
                            {
                                if (group.m_managedSystemsToUpdate[i].GetType() == targetType)
                                {
                                    foundTargetType = true;
                                    break;
                                }
                            }
                            if (!foundTargetType)
                            {
                                Debug.LogWarning(
                                    $"Ignoring invalid [{attrType}] attribute on {systemType} targeting {targetType}.\n" +
                                    $"This attribute can only order systems that are members of the same {nameof(ComponentSystemGroup)} instance.\n" +
                                    $"Make sure that both systems are in the same system group with [UpdateInGroup(typeof({groupType}))],\n" +
                                    $"or by manually adding both systems to the same group's update list.");
                                continue;
                            }
                        }
                        else
                        {
                            //it must be unmanaged if we get here, so look for it in the unmanaged of the group, and warn if it isn't there
                            var badUnmanagedUpdateInGroup = true;
                            for (int i = 0; i < group.m_UnmanagedSystemsToUpdate.Length; i++)
                            {
                                if (TypeManager.GetSystemType(
                                        group.World.Unmanaged.ResolveSystemState(
                                                group.m_UnmanagedSystemsToUpdate[i])->
                                            m_SystemTypeIndex) ==
                                    targetType)
                                {
                                    badUnmanagedUpdateInGroup = false;
                                    break;
                                }
                            }

                            if (badUnmanagedUpdateInGroup)
                            {
                                Debug.LogWarning(
                                    $"Ignoring invalid [{attrType}] attribute on {systemType} targeting {targetType}.\n" +
                                    $"This attribute can only order systems that are members of the same {nameof(ComponentSystemGroup)} instance.\n" +
                                    $"Make sure that both systems are in the same system group with [UpdateInGroup(typeof({groupType}))],\n" +
                                    $"or by manually adding both systems to the same group's update list.");
                                continue;
                            }
                        }

                        var groupTypeIndex = TypeManager.GetSystemTypeIndex(groupType);
                        var thisBucket =
                            ComponentSystemGroup.ComputeSystemOrdering(systemTypeIndex, groupTypeIndex);

                        var otherBucket =
                            ComponentSystemGroup.ComputeSystemOrdering(TypeManager.GetSystemTypeIndex(targetType),
                                groupTypeIndex);
                        if (thisBucket != otherBucket)
                        {
                            Debug.LogWarning(
                                $"Ignoring invalid [{attrType}({targetType})] attribute on {systemType} because OrderFirst/OrderLast has higher precedence.");
                            continue;
                        }
                    }
                }
            }
        }

        [BurstCompile]
        internal static unsafe void FindConstraints(
            int parentTypeIndex,
            UnsafeList<SystemElement>* sysElemsPtr,
            NativeHashMap<SystemTypeIndex, int>* lookupDictionary,
            TypeManager.SystemAttributeKind afterkind,
            TypeManager.SystemAttributeKind beforekind,
            NativeHashSet<SystemTypeIndex>* badSystemTypeIndices)
        {
            ref var sysElems = ref *sysElemsPtr;
            lookupDictionary->Clear();
            
            PopulateSystemElementLookup(lookupDictionary, sysElems);

            for (int i = 0; i < sysElems.Length; ++i)
            {
                var systemTypeIndex = sysElems[i].SystemTypeIndex;

                var before = TypeManager.GetSystemAttributes(systemTypeIndex, beforekind);
                var after = TypeManager.GetSystemAttributes(systemTypeIndex, afterkind);

                for (int j = 0; j < before.Length; j++) 
                {
                    var attr = before[j];
                    bool warn = false;
                    if (CheckBeforeConstraints(parentTypeIndex, attr, systemTypeIndex, out warn))
                    {
                        if (warn)
                            badSystemTypeIndices->Add(systemTypeIndex);
                        continue;
                    }

                    int depIndex = LookupSystemElement(attr.TargetSystemTypeIndex, lookupDictionary);
                    if (depIndex < 0)
                    {
                        badSystemTypeIndices->Add(systemTypeIndex);
                        continue;
                    }

                    sysElems.ElementAt(i).updateBefore.Add(attr.TargetSystemTypeIndex);
                    sysElems.ElementAt(depIndex).nAfter++;
                }

                for (int j = 0; j < after.Length; j++) 
                {
                    var attr = after[j];
                    bool warn = false;
                    if (CheckAfterConstraints(parentTypeIndex, attr, systemTypeIndex, out warn))
                    {
                        if (warn)
                            badSystemTypeIndices->Add(systemTypeIndex);
                        continue;
                    }

                    int depIndex = LookupSystemElement(attr.TargetSystemTypeIndex, lookupDictionary);
                    if (depIndex < 0)
                    {
                        badSystemTypeIndices->Add(systemTypeIndex);

                        continue;
                    }

                    sysElems.ElementAt(depIndex).updateBefore.Add(systemTypeIndex);
                    sysElems.ElementAt(i).nAfter++;
                }
            }
        }

        private static bool CheckBeforeConstraints(int parentTypeIndex, TypeManager.SystemAttribute dep, int systemTypeIndex, out bool warn)
        {
            warn = false;
            if (dep.TargetSystemTypeIndex == systemTypeIndex)
            {
                warn = true;
                return true;
            }

            int systemBucket = ComponentSystemGroup.ComputeSystemOrdering(systemTypeIndex, parentTypeIndex);
            int depBucket = ComponentSystemGroup.ComputeSystemOrdering(dep.TargetSystemTypeIndex, parentTypeIndex);
            if (depBucket > systemBucket)
            {
                // This constraint is redundant, but harmless; it is accounted for by the bucketing order, and can be quietly ignored.
                return true;
            }
            if (depBucket < systemBucket)
            {
                warn = true;
                return true;
            }

            return false;
        }

        private static unsafe bool CheckAfterConstraints(int parentTypeIndex, TypeManager.SystemAttribute dep, int systemTypeIndex, out bool warn)
        {
            warn = false;
            if (dep.TargetSystemTypeIndex == systemTypeIndex)
            {
                warn = true;
                return true;
            }

            int systemBucket = ComponentSystemGroup.ComputeSystemOrdering(systemTypeIndex, parentTypeIndex);
            int depBucket = ComponentSystemGroup.ComputeSystemOrdering(dep.TargetSystemTypeIndex, parentTypeIndex);
            if (depBucket < systemBucket)
            {
                // This constraint is redundant, but harmless; it is accounted for by the bucketing order, and can be quietly ignored.
                return true;
            }
            if (depBucket > systemBucket)
            {
                warn = true;
                return true;
            }

            return false;
        }
    }
}
