// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;

namespace Unity.Entities.Serialization
{
    /// <summary>
    /// Marks a field or property to be ignored by Entities' serialization paths
    /// (clipboard, content provider state, vendored binary serializer).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    [VisibleToOtherModules("UnityEngine.EntitiesPropertiesModule")]
    internal sealed class DontSerializeAttribute : Attribute
    {
    }

    /// <summary>
    /// Records a former name for a field, property, struct, or class so that the vendored
    /// binary serializer can resolve types whose qualified name has changed.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    [VisibleToOtherModules("UnityEngine.EntitiesPropertiesModule")]
    internal sealed partial class FormerNameAttribute : Attribute
    {
        public string OldName { get; }

        public FormerNameAttribute(string oldName)
        {
            OldName = oldName;
        }

        [AutoStaticsCleanupOnCodeReload]
        static readonly Dictionary<string, string> s_FormerlySerializedAsToCurrentName = new Dictionary<string, string>();
        [AutoStaticsCleanupOnCodeReload]
        static bool s_Registered;

        static void RegisterFormerlySerializedAsTypes()
        {
            if (s_Registered)
                return;

            s_Registered = true;

            foreach (var type in EditorLightShim.Hooks.GetTypesWithAttribute(typeof(FormerNameAttribute)))
            {
                if (type.IsAbstract || type.IsGenericType)
                    continue;

                var attributes = (FormerNameAttribute[])type.GetCustomAttributes(typeof(FormerNameAttribute), false);
                foreach (var attribute in attributes)
                    s_FormerlySerializedAsToCurrentName[attribute.OldName] = $"{type}, {type.Assembly.GetName().Name}";
            }
        }

        /// <summary>
        /// Gets the current name based on the previous name.
        /// </summary>
        public static bool TryGetCurrentTypeName(string oldName, out string currentName)
        {
            RegisterFormerlySerializedAsTypes();
            return s_FormerlySerializedAsToCurrentName.TryGetValue(oldName, out currentName);
        }
    }

    /// <summary>
    /// Thrown when an error occurs during binary or JSON serialization within Entities.
    /// </summary>
    [Serializable]
    [VisibleToOtherModules("UnityEngine.EntitiesPropertiesModule")]
    internal class SerializationException : Exception
    {
        public SerializationException(string message) : base(message)
        {
        }
    }
}
