// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.DataModel;

// Class to store all the type definitions
class TypeDefinitionRegistry
{
    private Dictionary<string, int>                   typeDefinitionLookUp;
    private List<TypeDefinitionData>                  typeDefinitions;

    internal TypeDefinitionRegistry()
    {
        typeDefinitionLookUp = new(1000);
        typeDefinitions = new(1000);
    }

    internal int AddTypeIndex(TypeDefinitionData newDate)
    {
        int index = typeDefinitions.Count;
        if (!typeDefinitionLookUp.TryAdd(newDate.Name, index))
            throw new InvalidOperationException($"Type {newDate.Name} already added");
        typeDefinitions.Add(newDate);
        return index;
    }

    internal int GetOrAddTypeIndex(string typeName, out TypeDefinitionState state)
    {
        if (!typeDefinitionLookUp.TryGetValue(typeName, out var index))
        {
            state = TypeDefinitionState.Empty;
            index = typeDefinitions.Count;
            typeDefinitionLookUp.Add(typeName, index);
            typeDefinitions.Add(new()
            {
                Name = typeName,
                DefinitionDataState = state
            });
        }
        else
        {
            state = typeDefinitions[index].DefinitionDataState;
        }
        return index;
    }

    internal int GetTypeIndex(string typeName)
    {
        var foundIndex = typeDefinitionLookUp.TryGetValue(typeName, out var index);
        return foundIndex ? index : -1;
    }

    internal TypeDefinitionData GetTypeData(int typeIndex)
    {
        return typeDefinitions[typeIndex];
    }

    internal void SetTypeData(int typeIndex, TypeDefinitionData data)
    {
        typeDefinitions[typeIndex] = data;
    }

    internal IReadOnlyList<TypeDefinitionData> GetAllTypes() => typeDefinitions;
}
