// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Styled visual element to match the IMGUI Box Style. For more information, refer to [[wiki:UIE-uxml-element-box|UXML element Box]].
    /// </summary>
    public partial class Box : VisualElement
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new Box();
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = "unity-box";
        internal static readonly UniqueStyleString ussClassNameUnique = new(ussClassName);

        /// <summary>
        ///  Initializes and returns an instance of Box.
        /// </summary>
        public Box()
        {
            AddToClassList(ussClassNameUnique);
        }
    }
}
