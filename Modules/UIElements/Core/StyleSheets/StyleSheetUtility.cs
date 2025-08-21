// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Collections;
using UnityEngine.Assertions;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal static class StyleSheetUtility
    {
        private static readonly Dictionary<string, string> SpecialEnumToStringCases = new Dictionary<string, string>
        {
            {"no-wrap", "nowrap"},
        };

        private static readonly Dictionary<string, string> SpecialStringToEnumCases = new Dictionary<string, string>
        {
            {"nowrap", "NoWrap"},
            {"sdf", "SDF"},
            {"uv", "UV"},
        };

        public static StyleSheet CreateInstanceWithHideFlags()
        {
            var newStyleSheet = ScriptableObject.CreateInstance<StyleSheet>();
            newStyleSheet.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.DontSaveInEditor;

            return newStyleSheet;
        }

        public static Dimension ToDimension(this Length length)
        {
            if (length.IsAuto() || length.IsNone())
                throw new InvalidCastException(
                    $"Can't convert a Length to a Dimension because it contains the '{length}' keyword.");
            return new Dimension(length.value, ToDimensionUnit(length.unit));
        }

        public static Dimension.Unit ToDimensionUnit(this LengthUnit unit)
        {
            return unit switch
            {
                LengthUnit.Pixel => Dimension.Unit.Pixel,
                LengthUnit.Percent => Dimension.Unit.Percent,
                _ => throw new InvalidCastException(
                    $"Can't convert a LengthUnit to a Dimension.Unit because it does not contain a valid keyword. Expected 'px' or '%', but was {unit}")
            };
        }

        public static Dimension ToDimension(this Angle angle)
        {
            if (angle.IsNone())
                throw new InvalidCastException(
                    $"Can't convert a Rotate to a Dimension because it contains the '{angle}' keyword.");
            return new Dimension(angle.value, angle.unit.ToDimensionUnit());
        }

        public static Dimension.Unit ToDimensionUnit(this AngleUnit unit)
        {
            return unit switch
            {
                AngleUnit.Degree => Dimension.Unit.Degree,
                AngleUnit.Gradian => Dimension.Unit.Gradian,
                AngleUnit.Radian => Dimension.Unit.Radian,
                AngleUnit.Turn => Dimension.Unit.Turn,
                _ => throw new InvalidCastException(
                    $"Can't convert a AngleUnit to a Dimension.Unit because it does not contain a valid keyword. Expected 'deg', 'grad', 'rad' or 'turn', but was {unit}")
            };
        }

        public static Dimension ToDimension(this TimeValue timeValue)
        {
            return new Dimension(timeValue.value, timeValue.unit.ToDimensionUnit());
        }

        public static Dimension.Unit ToDimensionUnit(this TimeUnit unit)
        {
            return unit switch
            {
                TimeUnit.Second => Dimension.Unit.Second,
                TimeUnit.Millisecond => Dimension.Unit.Millisecond,
                _ => throw new InvalidCastException(
                    $"Can't convert a TimeUnit to a Dimension.Unit because it does not contain a valid keyword. Expected 's' or 'ms', but was {unit}")
            };
        }

        public static StyleValueKeyword ToStyleValueKeyword(this StyleKeyword keyword)
        {
            return keyword switch
            {
                StyleKeyword.Auto => StyleValueKeyword.Auto,
                StyleKeyword.None => StyleValueKeyword.None,
                StyleKeyword.Initial => StyleValueKeyword.Initial,
                _ => throw new InvalidCastException(
                    $"Can't convert a StyleKeyword to a StyleValueKeyword because it does not contain a valid keyword. Expected 'auto', 'none' or 'initial', but was {keyword}.")
            };
        }

        public static void TransferStylePropertyHandles(
            StyleSheet fromStyleSheet, StyleProperty fromStyleProperty,
            StyleSheet toStyleSheet, StyleProperty toStyleProperty)
        {
            Assert.IsNotNull(fromStyleSheet);
            Assert.IsNotNull(toStyleSheet);
            Assert.IsNotNull(fromStyleProperty);
            Assert.IsNotNull(toStyleProperty);
            Assert.IsFalse(fromStyleProperty == toStyleProperty, "Cannot transfer a StyleProperty unto itself.");

            using var _ = ListPool<StyleValueHandle>.Get(out var handles);
            handles.AddRange(toStyleProperty.values);

            foreach (var handle in fromStyleProperty.values)
            {
                var type = handle.valueType;
                var index = type switch
                {
                    StyleValueType.Float => toStyleSheet.AddValue(fromStyleSheet.ReadFloat(handle)),
                    StyleValueType.Dimension => toStyleSheet.AddValue(fromStyleSheet.ReadDimension(handle)),
                    StyleValueType.Enum => toStyleSheet.AddValue(fromStyleSheet.ReadEnum(handle)),
                    StyleValueType.String => toStyleSheet.AddValue(fromStyleSheet.ReadString(handle)),
                    StyleValueType.Color => toStyleSheet.AddValue(fromStyleSheet.ReadColor(handle)),
                    StyleValueType.AssetReference => toStyleSheet.AddValue(fromStyleSheet.ReadAssetReference(handle)),
                    StyleValueType.ResourcePath => toStyleSheet.AddValue(fromStyleSheet.ReadResourcePath(handle)),
                    StyleValueType.Variable => toStyleSheet.AddValue(fromStyleSheet.ReadVariable(handle)),
                    StyleValueType.Keyword => toStyleSheet.AddValue(fromStyleSheet.ReadKeyword(handle)),
                    StyleValueType.CommaSeparator => handle.valueIndex,
                    StyleValueType.Function => toStyleSheet.AddValue(fromStyleSheet.ReadFunction(handle)),
                    StyleValueType.ScalableImage => toStyleSheet.AddValue(fromStyleSheet.ReadScalableImage(handle)),
                    StyleValueType.MissingAssetReference => toStyleSheet.AddValue(fromStyleSheet.ReadMissingAssetReferenceUrl(handle)),
                    StyleValueType.Invalid => handle.valueIndex,
                    _ => throw new ArgumentOutOfRangeException()
                };
                handles.Add(new StyleValueHandle(index, type));
            }

            toStyleProperty.requireVariableResolve |= fromStyleProperty.requireVariableResolve;

            toStyleProperty.values = handles.ToArray();
        }

        public static string GetEnumExportString(Enum value)
        {
            return ConvertCamelToDash(value.ToString());
        }

        public static string ConvertCamelToDash(string camel)
        {
            var split = Regex.Replace(Regex.Replace(camel, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1-$2"), @"(\p{Ll})(\P{Ll})", "$1-$2");
#pragma warning disable CA1308
            var lowerCase = split.ToLowerInvariant();
#pragma warning restore CA1308
            return SpecialEnumToStringCases.GetValueOrDefault(lowerCase, lowerCase);
        }

        public static string ConvertDashToHungarian(string dash)
        {
            return ConvertDashToUpperNoSpace(dash, true, false);
        }

        public static string ConvertDashToCamel(string dash)
        {
            return ConvertDashToUpperNoSpace(dash, false, false);
        }

        public static string ConvertDashToHuman(string dash)
        {
            return ConvertDashToUpperNoSpace(dash, true, true);
        }

        public static string ConvertDashToUpperNoSpace(string dash, bool firstCase, bool addSpace)
        {
            if (SpecialStringToEnumCases.TryGetValue(dash, out var replacement))
                return replacement;

            var sb = Pool.GenericPool<StringBuilder>.Get();

            try
            {
                var caseFlag = firstCase;
                foreach (var c in dash)
                {
                    if (c == '-')
                    {
                        if (addSpace)
                            sb.Append(' ');
                        caseFlag = true;
                    }
                    else if (caseFlag)
                    {
                        sb.Append(char.ToUpper(c, CultureInfo.InvariantCulture));
                        caseFlag = false;
                    }
                    else
                    {
                        sb.Append(char.ToLowerInvariant(c));
                    }
                }

                return sb.ToString();
            }
            finally
            {
                Pool.GenericPool<StringBuilder>.Release(sb.Clear());
            }
        }

        public static string GetDimensionUnitExportString(Dimension.Unit unit)
        {
            return unit switch
            {
                Dimension.Unit.Pixel => "px",
                Dimension.Unit.Percent => "%",
                Dimension.Unit.Second => "s",
                Dimension.Unit.Millisecond => "ms",
                Dimension.Unit.Degree => "deg",
                Dimension.Unit.Gradian => "grad",
                Dimension.Unit.Radian => "rad",
                Dimension.Unit.Turn => "turn",
                Dimension.Unit.Unitless => string.Empty,
                _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
            };
        }

        public static void GetValueOffsets(StyleSheet styleSheet, Span<StyleValueHandle> handles, List<int> offsets)
        {
            offsets.Clear();
            if (handles.Length == 0)
                return;

            offsets.Add(0);

            var currentIndex = 0;
            while (true)
            {
                currentIndex = GetNextValueOffset(styleSheet, handles, currentIndex);
                if (currentIndex >= 0 && currentIndex < handles.Length)
                    offsets.Add(currentIndex);
                else
                    break;
            }

            return;
        }

        // This assumes a well formed style property value.
        internal static int GetNextValueOffset(StyleSheet styleSheet, Span<StyleValueHandle> handles, int index)
        {
            static int OffsetByComma(Span<StyleValueHandle> handles, int next)
            {
                return next < handles.Length && handles[next].valueType == StyleValueType.CommaSeparator
                    ? 1
                    : 0;
            }

            if (index < 0 || index >= handles.Length)
                return -1;

            var initialIndex = index;
            var handle = handles[index];

            switch (handle.valueType)
            {
                case StyleValueType.Keyword:
                case StyleValueType.Float:
                case StyleValueType.Dimension:
                case StyleValueType.Color:
                case StyleValueType.ResourcePath:
                case StyleValueType.AssetReference:
                case StyleValueType.Enum:
                // Should not happen, but we do not validate against this use-case.
                case StyleValueType.Variable:
                case StyleValueType.String:
                case StyleValueType.ScalableImage:
                case StyleValueType.MissingAssetReference:
                    return initialIndex + 1 + OffsetByComma(handles, initialIndex + 1);

                case StyleValueType.Function:
                    var argCountHandle = handles[++index];
                    var argCount = styleSheet.ReadFloat(argCountHandle);
                    var argumentStart = ++index;
                    for (var i = argumentStart; i < argumentStart + argCount; ++i)
                    {
                        if (handles[i].valueType == StyleValueType.Function)
                        {
                            var nestedOffset = GetNextValueOffset(styleSheet, handles, i);
                            if (nestedOffset > 0)
                                index = nestedOffset;
                            else
                                return -1;
                        }
                        else
                        {
                            ++index;
                        }
                    }

                    // Here, index already points to the next value
                    return index + OffsetByComma(handles, index);
                case StyleValueType.Invalid:
                case StyleValueType.CommaSeparator:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
