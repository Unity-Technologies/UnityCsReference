// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Makes a style field for editing a StyleEnum.
    /// </summary>
    internal class StyleEnumField<T> : StylePropertyField<StyleEnum<T>, EnumToggleField<T>, T> where T : struct, Enum, IConvertible
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StylePropertyField<StyleEnum<T>, EnumToggleField<T>, T>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StylePropertyField<StyleEnum<T>, EnumToggleField<T>, T>.UxmlSerializedData.Register();
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public StyleEnumField() : this(false) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="useIcon">Whether to use an icon for the buttons or text of the enum value.</param>
        public StyleEnumField(bool useIcon) : this(null, useIcon) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        /// <param name="useIcon">Whether to use an icon for the buttons or text of the enum value.</param>
        public StyleEnumField(string label, bool useIcon) : base(label, new EnumToggleField<T>(useIcon)) { }

        protected override EnumToggleField<T> CreateValueField()
        {
            return new EnumToggleField<T>();
        }

        protected override StyleEnum<T> CreateStyleValue(T v)
        {
            return v;
        }
    }
}
