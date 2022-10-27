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
        public static Type GetCommonBaseType(IEnumerable<object> objects)
        {
            Type baseType = null;
            foreach (var obj in objects.Where(t => t != null))
            {
                var type = obj.GetType();
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
