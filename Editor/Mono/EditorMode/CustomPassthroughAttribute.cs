// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.Experimental.EditorMode
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    internal class CustomPassthroughAttribute : Attribute
    {
        public Type EditorModeType { get; }

        public CustomPassthroughAttribute(Type editorModeType)
        {
            try
            {
                if (!editorModeType.IsSubclassOf(typeof(EditorMode)))
                {
                    throw new ArgumentException($"{typeof(CustomPassthroughAttribute).Name} can only be used with a type derived from {typeof(EditorMode)}.");
                }
                EditorModeType = editorModeType;
            }
            catch (ArgumentException ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}
