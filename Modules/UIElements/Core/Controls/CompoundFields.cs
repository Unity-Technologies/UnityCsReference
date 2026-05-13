// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A <see cref="Rect"/> field. For more information, refer to [[wiki:UIE-uxm-element-RectField|UXML element RectField]].
    /// </summary>
    [MovedFrom(true, UpgradeConstants.EditorNamespace, UpgradeConstants.EditorAssembly)]
    [UxmlElement(libraryPath = "Numeric Fields"), UxmlPartialSerializedData]
    [Icon("UIToolkit/Icons/RectField.png")]
    public partial class RectField : BaseCompositeField<Rect, FloatField, float>
    {
        internal override FieldDescription[] DescribeFields()
        {
            return new[]
            {
                new FieldDescription("X", "unity-x-input", r => r.x, (ref Rect r, float v) => r.x = v),
                new FieldDescription("Y", "unity-y-input", r => r.y, (ref Rect r, float v) => r.y = v),
                new FieldDescription("W", "unity-width-input", r => r.width, (ref Rect r, float v) => r.width = v),
                new FieldDescription("H", "unity-height-input", r => r.height, (ref Rect r, float v) => r.height = v),
            };
        }

        [UnityEngine.Internal.ExcludeFromDocs]
        public new partial class UxmlSerializedData : IUxmlSerializedDataCustomAttributeHandler
        {
            void IUxmlSerializedDataCustomAttributeHandler.SerializeCustomAttributes(UxmlAsset bag, HashSet<string> handledAttributes)
            {
                // Its possible to only specify 1 attribute so we need to check them all and if we get at least 1 match then we can proceed.
                int foundAttributeCounter = 0;
                var x = UxmlUtility.TryParseFloatAttribute("x", bag, ref foundAttributeCounter);
                var y = UxmlUtility.TryParseFloatAttribute("y", bag, ref foundAttributeCounter);
                var w = UxmlUtility.TryParseFloatAttribute("w", bag, ref foundAttributeCounter);
                var h = UxmlUtility.TryParseFloatAttribute("h", bag, ref foundAttributeCounter);

                if (foundAttributeCounter > 0)
                {
                    valueUXML = new Rect(x, y, w, h);
                    handledAttributes.Add("value");

                    if (bag is UxmlAsset uxmlAsset)
                    {
                        uxmlAsset.RemoveAttribute("x");
                        uxmlAsset.RemoveAttribute("y");
                        uxmlAsset.RemoveAttribute("w");
                        uxmlAsset.RemoveAttribute("h");
                        uxmlAsset.SetAttribute("value", UxmlUtility.ValueToString(valueUXML));
                    }
                }
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-rect-field";
        internal new static readonly UniqueStyleString ussClassNameUnique = new(ussClassName);

        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        internal new static readonly UniqueStyleString labelUssClassNameUnique = new(labelUssClassName);

        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";
        internal new static readonly UniqueStyleString inputUssClassNameUnique = new(inputUssClassName);

        /// <summary>
        /// Initializes and returns an instance of RectField.
        /// </summary>
        public RectField()
            : this(null) {}

        /// <summary>
        /// Initializes and returns an instance of RectField.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public RectField(string label)
            : base(label, 2)
        {
            AddToClassList(ussClassNameUnique);
            AddToClassList(twoLinesVariantUssClassNameUnique);
            labelElement.AddToClassList(labelUssClassNameUnique);
            visualInput.AddToClassList(inputUssClassNameUnique);
        }
    }

    /// <summary>
    /// A <see cref="RectInt"/> field. For more information, refer to [[wiki:UIE-uxm-element-RectIntField|UXML element RectIntField]].
    /// </summary>
    [MovedFrom(true, UpgradeConstants.EditorNamespace, UpgradeConstants.EditorAssembly)]
    [UxmlElement(libraryPath = "Numeric Fields"), UxmlPartialSerializedData]
    [Icon("UIToolkit/Icons/RectIntField.png")]
    public partial class RectIntField : BaseCompositeField<RectInt, IntegerField, int>
    {
        internal override FieldDescription[] DescribeFields()
        {
            return new[]
            {
                new FieldDescription("X", "unity-x-input", r => r.x, (ref RectInt r, int v) => r.x = v),
                new FieldDescription("Y", "unity-y-input", r => r.y, (ref RectInt r, int v) => r.y = v),
                new FieldDescription("W", "unity-width-input", r => r.width, (ref RectInt r, int v) => r.width = v),
                new FieldDescription("H", "unity-height-input", r => r.height, (ref RectInt r, int v) => r.height = v),
            };
        }

        [UnityEngine.Internal.ExcludeFromDocs]
        public new partial class UxmlSerializedData : IUxmlSerializedDataCustomAttributeHandler
        {
            void IUxmlSerializedDataCustomAttributeHandler.SerializeCustomAttributes(UxmlAsset bag, HashSet<string> handledAttributes)
            {
                // Its possible to only specify 1 attribute so we need to check them all and if we get at least 1 match then we can proceed.
                int foundAttributeCounter = 0;
                var x = UxmlUtility.TryParseIntAttribute("x", bag, ref foundAttributeCounter);
                var y = UxmlUtility.TryParseIntAttribute("y", bag, ref foundAttributeCounter);
                var w = UxmlUtility.TryParseIntAttribute("w", bag, ref foundAttributeCounter);
                var h = UxmlUtility.TryParseIntAttribute("h", bag, ref foundAttributeCounter);

                if (foundAttributeCounter > 0)
                {
                    valueUXML = new RectInt(x, y, w, h);
                    handledAttributes.Add("value");

                    if (bag is UxmlAsset uxmlAsset)
                    {
                        uxmlAsset.RemoveAttribute("x");
                        uxmlAsset.RemoveAttribute("y");
                        uxmlAsset.RemoveAttribute("w");
                        uxmlAsset.RemoveAttribute("h");
                        uxmlAsset.SetAttribute("value", UxmlUtility.ValueToString(valueUXML));
                    }
                }
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-rect-int-field";
        internal new static readonly UniqueStyleString ussClassNameUnique = new(ussClassName);

        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        internal new static readonly UniqueStyleString labelUssClassNameUnique = new(labelUssClassName);

        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";
        internal new static readonly UniqueStyleString inputUssClassNameUnique = new(inputUssClassName);

        /// <summary>
        /// Initializes and returns an instance of RectIntField.
        /// </summary>
        public RectIntField()
            : this(null) {}

        /// <summary>
        /// Initializes and returns an instance of RectIntField.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public RectIntField(string label)
            : base(label, 2)
        {
            AddToClassList(ussClassNameUnique);
            AddToClassList(twoLinesVariantUssClassNameUnique);
            labelElement.AddToClassList(labelUssClassNameUnique);
            visualInput.AddToClassList(inputUssClassNameUnique);
        }
    }

    /// <summary>
    /// A <see cref="Vector2"/> field. For more information, refer to [[wiki:UIE-uxm-element-Vector2Field|UXML element Vector2Field]].
    /// </summary>
    [MovedFrom(true, UpgradeConstants.EditorNamespace, UpgradeConstants.EditorAssembly)]
    [UxmlElement(libraryPath = "Numeric Fields"), UxmlPartialSerializedData]
    [Icon("UIToolkit/Icons/Vector2Field.png")]
    public partial class Vector2Field : BaseCompositeField<Vector2, FloatField, float>
    {
        internal override FieldDescription[] DescribeFields()
        {
            return new[]
            {
                new FieldDescription("X", "unity-x-input", r => r.x, (ref Vector2 r, float v) => r.x = v),
                new FieldDescription("Y", "unity-y-input", r => r.y, (ref Vector2 r, float v) => r.y = v),
            };
        }

        [UnityEngine.Internal.ExcludeFromDocs]
        public new partial class UxmlSerializedData : IUxmlSerializedDataCustomAttributeHandler
        {
            void IUxmlSerializedDataCustomAttributeHandler.SerializeCustomAttributes(UxmlAsset bag, HashSet<string> handledAttributes)
            {
                // Its possible to only specify 1 attribute so we need to check them all and if we get at least 1 match then we can proceed.
                int foundAttributeCounter = 0;
                var x = UxmlUtility.TryParseFloatAttribute("x", bag, ref foundAttributeCounter);
                var y = UxmlUtility.TryParseFloatAttribute("y", bag, ref foundAttributeCounter);

                if (foundAttributeCounter > 0)
                {
                    valueUXML = new Vector2(x, y);
                    handledAttributes.Add("value");

                    if (bag is UxmlAsset uxmlAsset)
                    {
                        uxmlAsset.RemoveAttribute("x");
                        uxmlAsset.RemoveAttribute("y");
                        uxmlAsset.SetAttribute("value", UxmlUtility.ValueToString(valueUXML));
                    }
                }
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-vector2-field";
        internal new static readonly UniqueStyleString ussClassNameUnique = new(ussClassName);

        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        internal new static readonly UniqueStyleString labelUssClassNameUnique = new(labelUssClassName);

        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";
        internal new static readonly UniqueStyleString inputUssClassNameUnique = new(inputUssClassName);

        /// <summary>
        /// Initializes and returns an instance of Vector2Field.
        /// </summary>
        public Vector2Field()
            : this(null) {}

        /// <summary>
        /// Initializes and returns an instance of Vector2Field.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public Vector2Field(string label)
            : base(label, 2)
        {
            AddToClassList(ussClassNameUnique);
            labelElement.AddToClassList(labelUssClassNameUnique);
            visualInput.AddToClassList(inputUssClassNameUnique);
        }
    }

    /// <summary>
    /// A <see cref="Vector3"/> field. For more information, refer to [[wiki:UIE-uxm-element-Vector3Field|UXML element Vector3Field]].
    /// </summary>
    [MovedFrom(true, UpgradeConstants.EditorNamespace, UpgradeConstants.EditorAssembly)]
    [UxmlElement(libraryPath = "Numeric Fields"), UxmlPartialSerializedData]
    [Icon("UIToolkit/Icons/Vector3Field.png")]
    public partial class Vector3Field : BaseCompositeField<Vector3, FloatField, float>
    {
        internal override FieldDescription[] DescribeFields()
        {
            return new[]
            {
                new FieldDescription("X", "unity-x-input", r => r.x, (ref Vector3 r, float v) => r.x = v),
                new FieldDescription("Y", "unity-y-input", r => r.y, (ref Vector3 r, float v) => r.y = v),
                new FieldDescription("Z", "unity-z-input", r => r.z, (ref Vector3 r, float v) => r.z = v),
            };
        }

        [UnityEngine.Internal.ExcludeFromDocs]
        public new partial class UxmlSerializedData : IUxmlSerializedDataCustomAttributeHandler
        {
            void IUxmlSerializedDataCustomAttributeHandler.SerializeCustomAttributes(UxmlAsset bag, HashSet<string> handledAttributes)
            {
                // Its possible to only specify 1 attribute so we need to check them all and if we get at least 1 match then we can proceed.
                int foundAttributeCounter = 0;
                var x = UxmlUtility.TryParseFloatAttribute("x", bag, ref foundAttributeCounter);
                var y = UxmlUtility.TryParseFloatAttribute("y", bag, ref foundAttributeCounter);
                var z = UxmlUtility.TryParseFloatAttribute("z", bag, ref foundAttributeCounter);

                if (foundAttributeCounter > 0)
                {
                    valueUXML = new Vector3(x, y, z);
                    handledAttributes.Add("value");

                    if (bag is UxmlAsset uxmlAsset)
                    {
                        uxmlAsset.RemoveAttribute("x");
                        uxmlAsset.RemoveAttribute("y");
                        uxmlAsset.RemoveAttribute("z");
                        uxmlAsset.SetAttribute("value", UxmlUtility.ValueToString(valueUXML));
                    }
                }
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-vector3-field";
        internal new static readonly UniqueStyleString ussClassNameUnique = new(ussClassName);

        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        internal new static readonly UniqueStyleString labelUssClassNameUnique = new(labelUssClassName);

        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";
        internal new static readonly UniqueStyleString inputUssClassNameUnique = new(inputUssClassName);

        /// <summary>
        /// Initializes and returns an instance of Vector3Field.
        /// </summary>
        public Vector3Field()
            : this(null) {}

        /// <summary>
        /// Initializes and returns an instance of Vector3Field.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public Vector3Field(string label)
            : base(label, 3)
        {
            AddToClassList(ussClassNameUnique);
            labelElement.AddToClassList(labelUssClassNameUnique);
            visualInput.AddToClassList(inputUssClassNameUnique);
        }
    }


    /// <summary>
    /// A <see cref="Vector4"/> field. For more information, refer to [[wiki:UIE-uxm-element-Vector4Field|UXML element Vector4Field]].
    /// </summary>
    [MovedFrom(true, UpgradeConstants.EditorNamespace, UpgradeConstants.EditorAssembly)]
    [UxmlElement(libraryPath = "Numeric Fields"), UxmlPartialSerializedData]
    [Icon("UIToolkit/Icons/Vector4Field.png")]
    public partial class Vector4Field : BaseCompositeField<Vector4, FloatField, float>
    {
        internal override FieldDescription[] DescribeFields()
        {
            return new[]
            {
                new FieldDescription("X", "unity-x-input", r => r.x, (ref Vector4 r, float v) => r.x = v),
                new FieldDescription("Y", "unity-y-input", r => r.y, (ref Vector4 r, float v) => r.y = v),
                new FieldDescription("Z", "unity-z-input", r => r.z, (ref Vector4 r, float v) => r.z = v),
                new FieldDescription("W", "unity-w-input", r => r.w, (ref Vector4 r, float v) => r.w = v),
            };
        }

        [UnityEngine.Internal.ExcludeFromDocs]
        public new partial class UxmlSerializedData : IUxmlSerializedDataCustomAttributeHandler
        {
            void IUxmlSerializedDataCustomAttributeHandler.SerializeCustomAttributes(UxmlAsset bag, HashSet<string> handledAttributes)
            {
                // Its possible to only specify 1 attribute so we need to check them all and if we get at least 1 match then we can proceed.
                int foundAttributeCounter = 0;
                var x = UxmlUtility.TryParseFloatAttribute("x", bag, ref foundAttributeCounter);
                var y = UxmlUtility.TryParseFloatAttribute("y", bag, ref foundAttributeCounter);
                var z = UxmlUtility.TryParseFloatAttribute("z", bag, ref foundAttributeCounter);
                var w = UxmlUtility.TryParseFloatAttribute("w", bag, ref foundAttributeCounter);

                if (foundAttributeCounter > 0)
                {
                    valueUXML = new Vector4(x, y, z, w);
                    handledAttributes.Add("value");

                    if (bag is UxmlAsset uxmlAsset)
                    {
                        uxmlAsset.RemoveAttribute("x");
                        uxmlAsset.RemoveAttribute("y");
                        uxmlAsset.RemoveAttribute("z");
                        uxmlAsset.RemoveAttribute("w");
                        uxmlAsset.SetAttribute("value", UxmlUtility.ValueToString(valueUXML));
                    }
                }
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-vector4-field";
        internal new static readonly UniqueStyleString ussClassNameUnique = new(ussClassName);

        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        internal new static readonly UniqueStyleString labelUssClassNameUnique = new(labelUssClassName);

        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";
        internal new static readonly UniqueStyleString inputUssClassNameUnique = new(inputUssClassName);

        /// <summary>
        /// Initializes and returns an instance of Vector4Field.
        /// </summary>
        public Vector4Field()
            : this(null) {}

        /// <summary>
        /// Initializes and returns an instance of Vector4Field.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public Vector4Field(string label)
            : base(label, 4)
        {
            AddToClassList(ussClassNameUnique);
            labelElement.AddToClassList(labelUssClassNameUnique);
            visualInput.AddToClassList(inputUssClassNameUnique);
        }
    }


    /// <summary>
    /// A <see cref="Vector2Int"/> field. For more information, refer to [[wiki:UIE-uxml-element-Vector2IntField|UXML element Vector2IntField]].
    /// </summary>
    [MovedFrom(true, UpgradeConstants.EditorNamespace, UpgradeConstants.EditorAssembly)]
    [UxmlElement(libraryPath = "Numeric Fields"), UxmlPartialSerializedData]
    [Icon("UIToolkit/Icons/Vector2IntField.png")]
    public partial class Vector2IntField : BaseCompositeField<Vector2Int, IntegerField, int>
    {
        internal override FieldDescription[] DescribeFields()
        {
            return new[]
            {
                new FieldDescription("X", "unity-x-input", r => r.x, (ref Vector2Int r, int v) => r.x = v),
                new FieldDescription("Y", "unity-y-input", r => r.y, (ref Vector2Int r, int v) => r.y = v),
            };
        }

        [UnityEngine.Internal.ExcludeFromDocs]
        public new partial class UxmlSerializedData : IUxmlSerializedDataCustomAttributeHandler
        {
            void IUxmlSerializedDataCustomAttributeHandler.SerializeCustomAttributes(UxmlAsset bag, HashSet<string> handledAttributes)
            {
                // Its possible to only specify 1 attribute so we need to check them all and if we get at least 1 match then we can proceed.
                int foundAttributeCounter = 0;
                var x = UxmlUtility.TryParseIntAttribute("x", bag, ref foundAttributeCounter);
                var y = UxmlUtility.TryParseIntAttribute("y", bag, ref foundAttributeCounter);

                if (foundAttributeCounter > 0)
                {
                    // Upgrade the attributes
                    valueUXML = new Vector2Int(x, y);
                    handledAttributes.Add("value");

                    if (bag is UxmlAsset uxmlAsset)
                    {
                        uxmlAsset.RemoveAttribute("x");
                        uxmlAsset.RemoveAttribute("y");
                        uxmlAsset.SetAttribute("value", UxmlUtility.ValueToString(valueUXML));
                    }
                }
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-vector2-int-field";
        internal new static readonly UniqueStyleString ussClassNameUnique = new(ussClassName);

        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        internal new static readonly UniqueStyleString labelUssClassNameUnique = new(labelUssClassName);

        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";
        internal new static readonly UniqueStyleString inputUssClassNameUnique = new(inputUssClassName);

        /// <summary>
        /// Initializes and returns an instance of Vector2IntField.
        /// </summary>
        public Vector2IntField()
            : this(null) {}

        /// <summary>
        /// Initializes and returns an instance of Vector2IntField.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public Vector2IntField(string label)
            : base(label, 2)
        {
            AddToClassList(ussClassNameUnique);
            labelElement.AddToClassList(labelUssClassNameUnique);
            visualInput.AddToClassList(inputUssClassNameUnique);
        }
    }

    /// <summary>
    /// A <see cref="Vector3Int"/> field. For more information, refer to [[wiki:UIE-uxm-element-Vector3IntField|UXML element Vector3IntField]].
    /// </summary>
    [MovedFrom(true, UpgradeConstants.EditorNamespace, UpgradeConstants.EditorAssembly)]
    [UxmlElement(libraryPath = "Numeric Fields"), UxmlPartialSerializedData]
    [Icon("UIToolkit/Icons/Vector3IntField.png")]
    public partial class Vector3IntField : BaseCompositeField<Vector3Int, IntegerField, int>
    {
        internal override FieldDescription[] DescribeFields()
        {
            return new[]
            {
                new FieldDescription("X", "unity-x-input", r => r.x, (ref Vector3Int r, int v) => r.x = v),
                new FieldDescription("Y", "unity-y-input", r => r.y, (ref Vector3Int r, int v) => r.y = v),
                new FieldDescription("Z", "unity-z-input", r => r.z, (ref Vector3Int r, int v) => r.z = v),
            };
        }

        [UnityEngine.Internal.ExcludeFromDocs]
        public new partial class UxmlSerializedData : IUxmlSerializedDataCustomAttributeHandler
        {
            void IUxmlSerializedDataCustomAttributeHandler.SerializeCustomAttributes(UxmlAsset bag, HashSet<string> handledAttributes)
            {
                // Its possible to only specify 1 attribute so we need to check them all and if we get at least 1 match then we can proceed.
                int foundAttributeCounter = 0;
                var x = UxmlUtility.TryParseIntAttribute("x", bag, ref foundAttributeCounter);
                var y = UxmlUtility.TryParseIntAttribute("y", bag, ref foundAttributeCounter);
                var z = UxmlUtility.TryParseIntAttribute("z", bag, ref foundAttributeCounter);

                if (foundAttributeCounter > 0)
                {
                    valueUXML = new Vector3Int(x, y, z);
                    handledAttributes.Add("value");

                    if (bag is UxmlAsset uxmlAsset)
                    {
                        uxmlAsset.RemoveAttribute("x");
                        uxmlAsset.RemoveAttribute("y");
                        uxmlAsset.RemoveAttribute("z");
                        uxmlAsset.SetAttribute("value", UxmlUtility.ValueToString(valueUXML));
                    }
                }
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-vector3-int-field";
        internal new static readonly UniqueStyleString ussClassNameUnique = new(ussClassName);

        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        internal new static readonly UniqueStyleString labelUssClassNameUnique = new(labelUssClassName);

        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";
        internal new static readonly UniqueStyleString inputUssClassNameUnique = new(inputUssClassName);

        /// <summary>
        /// Initializes and returns an instance of Vector3IntField.
        /// </summary>
        public Vector3IntField()
            : this(null) {}

        /// <summary>
        /// Initializes and returns an instance of Vector3IntField.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public Vector3IntField(string label)
            : base(label, 3)
        {
            AddToClassList(ussClassNameUnique);
            labelElement.AddToClassList(labelUssClassNameUnique);
            visualInput.AddToClassList(inputUssClassNameUnique);
        }
    }
}
