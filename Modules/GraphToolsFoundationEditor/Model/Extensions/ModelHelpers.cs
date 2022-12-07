// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.GraphToolsFoundation.Editor
{
    static class ModelHelpers
    {
        /// <summary>
        /// Finds a common base type for objects.
        /// </summary>
        /// <param name="objects">The objects for which we need to find a common base type.</param>
        /// <returns>The most specialized common base type for the objects, or null if all objects are null.</returns>
        public static Type GetCommonBaseType(IEnumerable<object> objects)
        {
            return GetCommonBaseType(objects.Where(o => o != null).Select(o => o.GetType()));
        }

        /// <summary>
        /// Finds a common base type for types.
        /// </summary>
        /// <param name="types">The types for which we need to find a common base type.</param>
        /// <returns>The most specialized common base type for the types, or null if all types are null.</returns>
        public static Type GetCommonBaseType(IEnumerable<Type> types)
        {
            Type baseType = null;
            foreach (var type in types)
            {
                if (type == null)
                    continue;

                if (baseType == null)
                    baseType = type;
                else if (type.IsAssignableFrom(baseType))
                { }
                else
                {
                    while (baseType != null && !baseType.IsAssignableFrom(type))
                    {
                        baseType = baseType.BaseType;
                    }
                }
            }

            return baseType;
        }
    }
}
