// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI
{
    internal class IncorrectFieldTypeException : InvalidCastException
    {
        public IncorrectFieldTypeException(string fieldName, Type expectedType, Type actualType)
            : base($"Unable to cast {actualType.Name} to {expectedType.Name} for the field {fieldName}.")
        {
        }
    }
}
