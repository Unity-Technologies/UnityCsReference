// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Unity.Timeline.Foundation.Model.Internals
{
    static class FieldDelegateLookupContainerExtensions
    {
        internal static FieldDelegate<T> GetDelegate<T>(
            this FieldListDelegateContainer<T> fdm,
            SerializedObject lhs,
            SerializedObject rhs)
        {
            if (lhs == null && rhs == null)
                throw new ArgumentNullException(string.Empty, $"Both {nameof(lhs)} and {nameof(rhs)} cannot be null!");

            if (lhs == null || rhs == null) // Checking a new or deleted object
            {
                return fdm.fieldDelegate;
            }

            if (lhs.targetObjects.Length > 1 || rhs.targetObjects.Length > 1)
                throw new ArgumentException($"{nameof(GetDelegate)} only supports {nameof(SerializedObject)}s referencing single {nameof(Object)}");


            if (lhs.targetObject.GetType() != rhs.targetObject.GetType())
            {
                throw new ArgumentException($"TargetObject types do not match! Received {lhs.targetObject.GetType()} and {rhs.targetObject.GetType()}");
            }

            if (fdm.customComparer != null)
            {
                if (!fdm.customComparer.Invoke(lhs, rhs))
                {
                    return fdm.fieldDelegate;
                }
            }
            else if (fdm.fields == null || fdm.fields.Count == 0)
            {
                if (!DataEquals(lhs, rhs))
                {
                    return fdm.fieldDelegate;
                }
            }
            else if (!FieldDataEquals(lhs, rhs, fdm.fields))
            {
                return fdm.fieldDelegate;
            }

            return null;
        }

        static bool FieldDataEquals(SerializedObject lhs, SerializedObject rhs, IEnumerable<string> fields)
        {
            foreach (string fieldName in fields)
            {
                if (!SerializedProperty.DataEquals(lhs.FindProperty(fieldName), rhs.FindProperty(fieldName)))
                {
                    return false;
                }
            }

            return true;
        }

        static bool DataEquals(SerializedObject lhs, SerializedObject rhs)
        {
            SerializedProperty property = lhs.GetIterator();
            while (property.Next(true))
            {
                SerializedProperty rhsProp = rhs.FindProperty(property.name);
                if (rhsProp == null)
                    continue;

                if (!SerializedProperty.DataEquals(property, rhsProp))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
