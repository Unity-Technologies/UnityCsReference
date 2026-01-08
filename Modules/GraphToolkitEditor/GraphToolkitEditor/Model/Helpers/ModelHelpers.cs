// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.GraphToolkit.Editor
{
    [UnityRestricted]
    internal static class ModelHelpers
    {
        /// <summary>
        /// Finds a common base type for objects.
        /// </summary>
        /// <param name="objects">The objects for which we need to find a common base type.</param>
        /// <returns>The most specialized common base type for the objects, or null if all objects are null.</returns>
        public static Type GetCommonBaseType(IEnumerable<object> objects)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return GetCommonBaseType(objects.Where(o => o != null).Select(o => o.GetType()));
#pragma warning restore RS0030
        }

        /// <summary>
        /// Finds a common base type for types.
        /// </summary>
        /// <param name="types">The types for which we need to find a common base type.</param>
        /// <returns>The most specialized common base type for the types, or null if all types are null.</returns>
        internal static Type GetCommonBaseType(IEnumerable<Type> types)
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

        /// <summary>
        /// Instantiates an object of type <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of the object to instantiate.</param>
        /// <typeparam name="TBaseType">A base type for <paramref name="type"/>.</typeparam>
        /// <returns>A new object.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="type"/> does not derive from <typeparamref name="TBaseType"/></exception>
        internal static TBaseType Instantiate<TBaseType>(Type type) where TBaseType : Model
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            TBaseType obj;
            if (typeof(TBaseType).IsAssignableFrom(type))
                obj = (TBaseType)Activator.CreateInstance(type);
            else
                throw new ArgumentOutOfRangeException(nameof(type));

            return obj;
        }

        /// <summary>
        /// Whether the given node is one of the common nodes of the BasicModel that must authorize copy/paste.
        /// </summary>
        /// <param name="nodeModel">The node model to be tested.</param>
        /// <returns>True if the given node is one of the common nodes of the BasicModel that must be copy/pasted.</returns>
        public static bool IsCommonNodeThatCanBePasted(AbstractNodeModel nodeModel)
        {
            return nodeModel is ConstantNodeModel || nodeModel is VariableNodeModel || nodeModel is ContextNodeModel || nodeModel is WirePortalModel;
        }
    }
}
