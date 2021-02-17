using System;

namespace UnityEditor.UIElements
{
    internal class SerializedPropertyDelegates
    {
        internal static Func<SerializedProperty, bool> IsPropertyValid = property => property.isValid;
    }
}
