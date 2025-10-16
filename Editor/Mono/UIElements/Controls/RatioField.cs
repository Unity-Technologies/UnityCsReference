// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Makes a field for entering aspect ratio.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal class RatioField : TextValueField<Ratio>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseField<Ratio>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<TextAutoSize>.UxmlSerializedData.Register();
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), Array.Empty<UxmlAttributeNames>(), true);
            }

            public override object CreateInstance() => new RatioField();
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-ratio-field";

        internal record struct CommonOption(string Name, float w, float h);

        internal static readonly List<CommonOption> commonOptions = new()
        {
             new CommonOption("auto", 0, 0),
             new CommonOption("1/1", 1, 1),
             new CommonOption("3/2", 3, 2), // Ipads
             new CommonOption("4/3", 4, 3), // Common 35mm film/TV
             new CommonOption("5/4", 5, 4), // Old SVGA monitors
             new CommonOption("16/9", 16, 9),  // common widescreen video
        };

        internal PopupField<CommonOption> commonField = new(commonOptions, commonOptions[0], FormatItemForUITK, FormatItemForOs) { style = { marginLeft = 0 } };
        static string FormatItemForOs(CommonOption myData)
        {
            //To not display the / as a sub-menu in the contextMenu
            return myData.Name.Replace("/", "\u200A\u2215\u200A");
        }

        static string FormatItemForUITK(CommonOption myData)
        {
            return myData.Name;
        }

        public RatioField() : this(null) { }

        public RatioField(int maxLength)
            : this(null, maxLength) { }

        public RatioField(string label, int maxLength = kMaxValueFieldLength)
            : base(label, maxLength, new RatioInput())
        {
            value = Ratio.Auto();
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
            AddLabelDragger<Ratio>();
            commonField.RegisterValueChangedCallback(e =>
            {
                text = e.newValue.Name;
                value = ((e.newValue.w == 0 || e.newValue.h == 0) ? Ratio.Auto() : new Ratio(e.newValue.w / e.newValue.h));
                e.StopImmediatePropagation();
            });
            Insert(childCount - 1, commonField);
        }

        public override void SetValueWithoutNotify(Ratio t)
        {
            base.SetValueWithoutNotify(t);

            if (value.IsAuto())
            {
                commonField.SetValueWithoutNotify(commonOptions[0]);
                return;
            }
            else
            {
                const float epsilon = 0.0015f;

                foreach (var option in commonOptions)
                {
                    if (Math.Abs(option.w / option.h - t.value) < epsilon)
                    {
                        commonField.SetValueWithoutNotify(option);
                        return;
                    }

                }
            }
            commonField.SetValueWithoutNotify(new("Custom", 0, 0));
        }

        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, Ratio startValue)
        {
            ((RatioInput)textInputBase).ApplyInputDeviceDelta(delta, speed, startValue);
        }

        protected override Ratio StringToValue(string str)
        {
            return ParseString(str, value);
        }

        protected override string ValueToString(Ratio value)
        {
            if (value.IsAuto())
            {
                return string.Empty;
            }
            return value.ToString();
        }

        private static Ratio ParseString(string str, StyleRatio defaultValue)
        {
            if (str.StartsWith("auto", StringComparison.OrdinalIgnoreCase))
            {
                return StyleRatio.Auto();
            }

            if (str.Contains("/") || str.Contains(":"))
            {
                var parts = str.Split('/', ':');
                if (parts.Length == 2)
                {
                    if (double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double w) && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double h))
                    {
                        if (h <= 0 || w <= 0 || double.IsNaN(h) || double.IsNaN(w))
                        {
                            return defaultValue;
                        }

                        return new Ratio((float)(w / h));
                    }
                }
            }

            if (double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                if (value == 0 || double.IsNaN(value))
                {
                    return defaultValue;
                }
                return new Ratio((float)value);
            }

            return defaultValue;
        }

        class RatioInput : TextValueInput
        {
            RatioField parentField => (RatioField)parent;

            protected override string allowedCharacters => "0123456789autone.,/:";

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, Ratio _)
            {
                var sensitivity = 1f / 300f;//300 pixels to go form one end to to the other.
                float acceleration = NumericFieldDraggerUtility.Acceleration(speed == DeltaSpeed.Fast, speed == DeltaSpeed.Slow);

                var currentValue = StringToValue(text);
                float v = currentValue.IsAuto() ? 1 : currentValue.value;

                var linearDelta = Mathf.Clamp(NumericFieldDraggerUtility.NiceDelta(delta, acceleration) * sensitivity, -0.5f, 0.5f);

                v *= (1 + linearDelta) / (1 - linearDelta);

                v = Mathf.Clamp(v, 0.01f, 100);

                // Assumes the field is not delayed
                // By setting the value directly, we have no control over the end result of the toString and fractions may pop while dragging. We might neeed something more specific
                parentField.value = v;
            }

            protected override string ValueToString(Ratio value)
            {
                return value.ToString();
            }

            protected override Ratio StringToValue(string str)
            {
                return ParseString(str, parentField.value);
            }
        }
    }
}
