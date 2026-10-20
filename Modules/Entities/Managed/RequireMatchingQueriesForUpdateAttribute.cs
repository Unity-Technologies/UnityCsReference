// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Entities
{
    /// <summary>
    /// Use RequireMatchingQueriesForUpdate to force a System to skip calling OnUpdate if every
    /// <see cref="EntityQuery"/> in the system is empty.
    /// </summary>
    /// <description>
    /// This is a more performant way of processing a system because it avoids any overhead that
    /// OnUpdate has if there are no Entities to operate on.
    /// <br/><br/>
    /// When you add RequireMatchingQueriesForUpdate to a System it calls
    /// <see cref="EntityQuery.IsEmptyIgnoreFilter"/> to check if each Entity Query is empty. If
    /// all Entity Queries are empty, the System doesn't call OnUpdate.
    /// <br/><br/>
    /// **Important:** IsEmptyIgnoreFilter does not take into account any filters on Entity Queries.
    /// If any of the Entity Queries in a System use change filters, shared component filters, or
    /// <see cref="IEnableableComponent"/>, then it is possible for the System to call OnUpdate
    /// in a situation where all Entity Queries are empty.
    /// </description>
    /// <seealso cref="M:Unity.Entities.ComponentSystemBase.ShouldRunSystem"/>
    /// <seealso cref="M:Unity.Entities.ComponentSystemBase.RequireForUpdate``1"/>
    /// <seealso cref="M:Unity.Entities.ComponentSystemBase.RequireForUpdate(Unity.Entities.EntityQuery)"/>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public class RequireMatchingQueriesForUpdateAttribute : Attribute
    {
    }
}
