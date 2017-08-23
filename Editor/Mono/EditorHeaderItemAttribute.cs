// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Scripting;

namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    internal sealed partial class EditorHeaderItemAttribute : CallbackOrderAttribute
    {
        public EditorHeaderItemAttribute(Type targetType, int priority = 1)
        {
            TargetType = targetType;
            m_CallbackOrder = priority;
        }

        public Type TargetType;

        [RequiredSignature]
        static extern bool SignatureBool(Rect rectangle, UnityEngine.Object[] targetObjets);
    }
}
