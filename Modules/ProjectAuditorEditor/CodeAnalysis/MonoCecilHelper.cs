// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Mono.Cecil;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.CodeAnalysis
{
    internal static class MonoCecilHelper
    {
        // Some sequence points do not map to any source document line. Such sequence points have a line number value equal to this constant (0xfeefee)
        public const int HiddenLine = 16707566;

        // for reference https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/MonoCecil/MonoCecilHelper.cs
        public static IEnumerable<TypeDefinition> AggregateAllTypeDefinitions(IEnumerable<TypeDefinition> types)
        {
            var typeDefs = new List<TypeDefinition>(types);
            foreach (var typeDefinition in types)
                if (typeDefinition.HasNestedTypes)
                    typeDefs.AddRange(AggregateAllTypeDefinitions(typeDefinition.NestedTypes));
            return typeDefs;
        }

        public static bool IsOrInheritedFrom(TypeReference typeReference, string typeName)
        {
            try
            {
                var typeDefinition = typeReference.Resolve();

                if (typeDefinition.FullName.Equals(typeName))
                    return true;

                if (typeDefinition.BaseType != null)
                    return IsOrInheritedFrom(typeDefinition.BaseType, typeName);
            }
            catch (AssemblyResolutionException e)
            {
                Debug.LogWarningFormat("Could not resolve {0}: {1}", typeReference.Name, e.Message);
            }

            return false;
        }
    }
}
