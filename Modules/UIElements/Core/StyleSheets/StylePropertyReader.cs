// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements.Layout;
using UnityEngine.UIElements.Unmanaged;

namespace UnityEngine.UIElements.StyleSheets
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal struct StylePropertyValue
    {
        public StyleSheet sheet;
        public StyleValueHandle handle;
    }

    [NativeHeader("Modules/UIElements/Core/Native/Style/StylePropertyValue.h")]
    [NativeClass("UnmanagedStylePropertyValue")]
    [StructLayout(LayoutKind.Sequential)]
    internal struct UnmanagedStylePropertyValue
    {
        public EntityId sheet;
        public StyleValueHandle handle;

        public static implicit operator StylePropertyValue(UnmanagedStylePropertyValue value)
        {
            return new() { sheet = (StyleSheet)Resources.EntityIdToObject(value.sheet), handle = value.handle };
        }
        public static implicit operator UnmanagedStylePropertyValue(StylePropertyValue value)
        {
            return new()
            {
                sheet = value.sheet != null ? value.sheet.GetEntityId() : EntityId.None, handle = value.handle
            };
        }
    }

    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal struct ImageSource
    {
        public Texture2D texture;
        public Sprite sprite;
        public VectorImage vectorImage;
        public RenderTexture renderTexture;

        public  bool IsNull()
        {
            return texture == null && sprite == null && vectorImage == null && renderTexture == null;
        }
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal partial class StylePropertyReader
    {
        // Strategy to create default cursor must be provided in the context of Editor or Runtime
        internal delegate int GetCursorIdFunction(StyleSheet sheet, StyleValueHandle handle);

        [AutoStaticsCleanupOnCodeReload]
        internal static GetCursorIdFunction getCursorIdFunc = null;

        // One-shot per session — avoids flooding the console when a stylesheet with a
        // limitation-tripping gradient is applied to many elements.
        [NoAutoStaticsCleanup] // one-shot warning flags; safe to persist across reload
        static bool s_WarnedCircleCoerce;
        [NoAutoStaticsCleanup] // one-shot warning flags; safe to persist across reload
        static bool s_WarnedRadialPositionUnit;
        [NoAutoStaticsCleanup] // one-shot warning flags; safe to persist across reload
        static bool s_WarnedPixelStopPosition;

        // Styles re-resolve on every hover/layout/animation change, so a missing asset baked into a
        // sheet would otherwise warn on each pass. A reimport reuses the sheet's EntityId and so
        // stays deduped here; the importer logs its own warning for every import.
        [NoAutoStaticsCleanup]
        static HashSet<(EntityId sheet, string url)> s_WarnedMissingAssets;

        static void WarnMissingAssetOnce(StyleSheet sheet, string format, string url)
        {
            s_WarnedMissingAssets ??= new HashSet<(EntityId, string)>();
            if (!s_WarnedMissingAssets.Add((sheet.GetEntityId(), url)))
                return;

            Debug.LogWarning(string.Format(CultureInfo.InvariantCulture, format, url, sheet.name), sheet);
        }

        private List<StylePropertyValue> m_Values = new List<StylePropertyValue>();
        private List<int> m_ValueCount = new List<int>();
        private StyleVariableResolver m_Resolver = new StyleVariableResolver();
        private StyleSheet m_Sheet;
        private StyleProperty[] m_Properties;
        private int m_CurrentValueIndex { get; set; }
        private int m_CurrentPropertyIndex;

        public StyleProperty property { get; private set; }
        public StylePropertyId propertyId { get; private set; }
        public int valueCount { get; private set; }

        public float dpiScaling { get; private set; }

        public void SetContext(StyleSheet sheet, StyleComplexSelector selector, StyleVariableContext varContext, float dpiScaling = 1.0f)
        {
            m_Sheet = sheet;
            m_Properties = selector.rule.properties;
            m_Resolver.variableContext = varContext;

            this.dpiScaling = dpiScaling;
            LoadProperties();
        }

        // This is for UXML inline sheet
        public void SetInlineContext(StyleSheet sheet, StyleProperty[] properties, StyleVariableContext varContext, float dpiScaling = 1.0f)
        {
            m_Sheet = sheet;
            m_Properties = properties;
            m_Resolver.variableContext = varContext;

            this.dpiScaling = dpiScaling;
            LoadProperties();
        }

        public StylePropertyId MoveNextProperty()
        {
            ++m_CurrentPropertyIndex;
            m_CurrentValueIndex += valueCount;
            SetCurrentProperty();
            return propertyId;
        }

        public StylePropertyValue GetValue(int index)
        {
            return m_Values[m_CurrentValueIndex + index];
        }

        public StyleValueType GetValueType(int index)
        {
            return m_Values[m_CurrentValueIndex + index].handle.valueType;
        }

        public bool IsValueType(int index, StyleValueType type)
        {
            return m_Values[m_CurrentValueIndex + index].handle.valueType == type;
        }

        public bool IsKeyword(int index, StyleValueKeyword keyword)
        {
            var value = m_Values[m_CurrentValueIndex + index];
            return value.handle.valueType == StyleValueType.Keyword && (StyleValueKeyword)value.handle.valueIndex == keyword;
        }

        public string ReadAsString(int index)
        {
            var value = m_Values[m_CurrentValueIndex + index];
            return value.sheet.ReadAsString(value.handle);
        }

        public Length ReadLength(int index)
        {
            var value = m_Values[m_CurrentValueIndex + index];

            if (value.handle.valueType == StyleValueType.Keyword)
            {
                var keyword = (StyleValueKeyword)value.handle.valueIndex;
                switch (keyword)
                {
                    case StyleValueKeyword.Auto:
                        return Length.Auto();
                    case StyleValueKeyword.None:
                        return Length.None();
                    default:
                        return new Length();
                }
            }

            var dimension = value.sheet.ReadDimension(value.handle);
            return dimension.ToLength();
        }

        public TimeValue ReadTimeValue(int index)
        {
            var value = m_Values[m_CurrentValueIndex + index];
            return value.sheet.ReadDimension(value.handle).ToTime();
        }

        public float ReadTimeValueAsSeconds(int index)
        {
            var time = ReadTimeValue(index);
            return time.unit == TimeUnit.Millisecond ? time.value / 1000f : time.value;
        }

        public Translate ReadTranslate(int index)
        {
            var val1 = m_Values[m_CurrentValueIndex + index];
            var val2 = valueCount > 1 ? m_Values[m_CurrentValueIndex + index + 1] : default;
            var val3 = valueCount > 2 ? m_Values[m_CurrentValueIndex + index + 2] : default;

            return ReadTranslate(valueCount, val1, val2, val3);
        }

        public TransformOrigin ReadTransformOrigin(int index)
        {
            var val1 = m_Values[m_CurrentValueIndex + index];
            var val2 = valueCount > 1 ? m_Values[m_CurrentValueIndex + index + 1] : default;
            var val3 = valueCount > 2 ? m_Values[m_CurrentValueIndex + index + 2] : default;

            return ReadTransformOrigin(valueCount, val1, val2, val3);
        }

        public Rotate ReadRotate(int index)
        {
            var val1 = m_Values[m_CurrentValueIndex + index];
            var val2 = valueCount > 1 ? m_Values[m_CurrentValueIndex + index + 1] : default;
            var val3 = valueCount > 2 ? m_Values[m_CurrentValueIndex + index + 2] : default;
            var val4 = valueCount > 3 ? m_Values[m_CurrentValueIndex + index + 3] : default;

            return ReadRotate(valueCount, val1, val2, val3, val4);
        }

        public Scale ReadScale(int index)
        {
            var val1 = m_Values[m_CurrentValueIndex + index];
            var val2 = valueCount > 1 ? m_Values[m_CurrentValueIndex + index + 1] : default;
            var val3 = valueCount > 2 ? m_Values[m_CurrentValueIndex + index + 2] : default;

            return ReadScale(valueCount, val1, val2, val3);
        }

        public Curvature ReadCurvature(int index)
        {
            var val1 = m_Values[m_CurrentValueIndex + index];
            var val2 = valueCount > 1 ? m_Values[m_CurrentValueIndex + index + 1] : default;

            return ReadCurvature(valueCount, val1, val2);
        }

        public float ReadFloat(int index)
        {
            var value = m_Values[m_CurrentValueIndex + index];
            return value.sheet.ReadFloat(value.handle);
        }

        public int ReadInt(int index)
        {
            var value = m_Values[m_CurrentValueIndex + index];
            return (int)value.sheet.ReadFloat(value.handle);
        }

        public Color ReadColor(int index)
        {
            var value = m_Values[m_CurrentValueIndex + index];
            return value.sheet.ReadColor(value.handle);
        }

        public int ReadEnum(StyleEnumType enumType, int index)
        {
            string enumString = null;
            var value = m_Values[m_CurrentValueIndex + index];
            var handle = value.handle;

            if (handle.valueType == StyleValueType.Keyword)
            {
                var keyword = value.sheet.ReadKeyword(handle);
                enumString = keyword.ToUssString();
            }
            else
            {
                enumString = value.sheet.ReadEnum(handle);
            }

            StylePropertyUtil.TryGetEnumIntValue(enumType, enumString, out var intValue);
            return intValue;
        }

        public Object ReadAsset(int index)
        {
            Object o = null;
            var value = m_Values[m_CurrentValueIndex + index];
            switch (value.handle.valueType)
            {
                case StyleValueType.ResourcePath:
                {
                    o = value.sheet.ReadResourcePath(value.handle).LoadResource<Object>(dpiScaling);
                    break;
                }
                case StyleValueType.AssetReference:
                {
                    o = value.sheet.ReadAssetReference(value.handle);
                    break;
                }
            }
            return o;
        }

        public EntityId ReadFontDefinition(int index)
        {
            FontAsset fontAsset = null;
            Font font = null;
            var value = m_Values[m_CurrentValueIndex + index];
            switch (value.handle.valueType)
            {
                case StyleValueType.ResourcePath:
                {
                    var resourcePath = value.sheet.ReadResourcePath(value.handle);
                    font = resourcePath.LoadResource<Font>(dpiScaling);
                    if (font == null)
                        fontAsset = resourcePath.LoadResource<FontAsset>(dpiScaling);

                    if (fontAsset == null && font == null)
                        Debug.LogWarning(string.Format(CultureInfo.InvariantCulture, "Font not found for path: {0}", resourcePath.ToString()));

                    break;
                }

                case StyleValueType.AssetReference:
                {
                    font = value.sheet.ReadAssetReference(value.handle) as Font;
                    if (font == null)
                        fontAsset = value.sheet.ReadAssetReference(value.handle) as FontAsset;

                    break;
                }

                case StyleValueType.Keyword:
                {
                    if (value.handle.valueIndex != (int)StyleValueKeyword.None)
                        Debug.LogWarning("Invalid keyword for font " + (StyleValueKeyword)value.handle.valueIndex);

                    break;
                }

                case StyleValueType.MissingAssetReference:
                {
                    var missingAssetUrl = value.sheet.ReadMissingAssetReferenceUrl(value.handle);
                    WarnMissingAssetOnce(value.sheet,
                        "Missing font asset reference '{0}' in stylesheet '{1}'. The font asset may have been deleted or moved.",
                        missingAssetUrl);
                    break;
                }

                default:
                    Debug.LogWarning("Invalid value for font " + value.handle.valueType);
                    break;
            }

            FontDefinition sfd;
            if (font != null)
                sfd = FontDefinition.FromFont(font);
            else if (fontAsset != null)
                sfd = FontDefinition.FromSDFFont(fontAsset);
            else
                sfd = new FontDefinition();

            FontDefinition.To(in sfd, out var entityId);
            return entityId;
        }

        public EntityId ReadFont(int index)
        {
            var font = ReadAssetObject<Font>(index);
            return font != null ? font.GetEntityId() : EntityId.None;
        }

        public EntityId ReadUIAnimationClip(int index)
        {
            var clip = ReadAssetObject<UIAnimationClip>(index);
            return clip != null ? clip.GetEntityId() : EntityId.None;
        }

        T ReadAssetObject<T>(int index) where T : Object
        {
            T asset = null;
            var value = m_Values[m_CurrentValueIndex + index];
            switch (value.handle.valueType)
            {
                case StyleValueType.ResourcePath:
                {
                    var resourcePath = value.sheet.ReadResourcePath(value.handle);
                    asset = resourcePath.LoadResource<T>(dpiScaling);
                    if (asset == null)
                        Debug.LogWarning(string.Format(CultureInfo.InvariantCulture, "{0} not found for path: {1}", typeof(T).Name, resourcePath.ToString()));
                    break;
                }

                case StyleValueType.AssetReference:
                {
                    asset = value.sheet.ReadAssetReference(value.handle) as T;
                    break;
                }

                case StyleValueType.Keyword:
                {
                    if (value.handle.valueIndex != (int)StyleValueKeyword.None)
                        Debug.LogWarning("Invalid keyword for " + typeof(T).Name + " " + (StyleValueKeyword)value.handle.valueIndex);
                    break;
                }

                case StyleValueType.MissingAssetReference:
                {
                    var missingAssetUrl = value.sheet.ReadMissingAssetReferenceUrl(value.handle);
                    WarnMissingAssetOnce(value.sheet,
                        "Missing asset reference '{0}' in stylesheet '{1}'. The asset may have been deleted or moved.",
                        missingAssetUrl);
                    break;
                }

                default:
                    Debug.LogWarning("Invalid value for " + typeof(T).Name + " " + value.handle.valueType);
                    break;
            }

            return asset;
        }

        public void ReadMaterialDefinition(ref UnmanagedMaterialDefinition data, int index)
        {
            if (!property.TryGetMaterialDefinition(m_Sheet, ref data))
            {
                data.CopyFrom(UnmanagedMaterialDefinition.Empty);
            }
        }

        public void ReadBackground(ref UnmanagedBackground target, int index)
        {
            var source = new ImageSource();
            var value = m_Values[m_CurrentValueIndex + index];

            // Gradient functions share the background-image slot. Store the gradient
            // metadata here and defer the bake to render time (UIRElementBuilder).
            if (value.handle.valueType == StyleValueType.Function)
            {
                var fn = (StyleValueFunction)value.handle.valueIndex;
                if (fn == StyleValueFunction.LinearGradient || fn == StyleValueFunction.RadialGradient)
                {
                    var gradient = ReadBackgroundGradient(index);
                    target.imageEntityId = EntityId.None;
                    if (gradient.IsEmpty())
                    {
                        target.gradient.Clear();
                        return;
                    }
                    Span<UnmanagedBackgroundGradient> single = stackalloc UnmanagedBackgroundGradient[1];
                    single[0] = gradient;
                    target.gradient.CopyFrom((ReadOnlySpan<UnmanagedBackgroundGradient>)single);
                    return;
                }
            }

            if (value.handle.valueType == StyleValueType.Keyword)
            {
                if (value.handle.valueIndex != (int)StyleValueKeyword.None)
                {
                    Debug.LogWarning("Invalid keyword for image source " + (StyleValueKeyword)value.handle.valueIndex);
                }
                else
                {
                    // it's OK, we let none be assigned to the source
                }
            }
            else if (TryGetImageSourceFromValue(value, dpiScaling, out source) == false)
            {
                // Load a stand-in picture to make it easier to identify which image element is missing its picture
                source.texture = Panel.LoadResource("d_console.warnicon", typeof(Texture2D), dpiScaling) as Texture2D;
            }

            UnityEngine.Object obj =
                source.texture ?? (UnityEngine.Object)
                source.sprite ?? source.vectorImage ?? (UnityEngine.Object)source.renderTexture;
            target.imageEntityId = obj != null ? obj.GetEntityId() : EntityId.None;
            target.gradient.Clear();
        }

        public Cursor ReadCursor(int index)
        {
            var val1 = m_Values[m_CurrentValueIndex + index];
            var val2 = valueCount > 1 ? m_Values[m_CurrentValueIndex + index + 1] : default;
            var val3 = valueCount > 2 ? m_Values[m_CurrentValueIndex + index + 2] : default;
            return ReadCursor(valueCount, val1, val2, val3, dpiScaling);
        }

        public TextShadow ReadTextShadow(int index)
        {
            var val1 = m_Values[m_CurrentValueIndex + index];
            var val2 = valueCount > 1 ? m_Values[m_CurrentValueIndex + index + 1] : default;
            var val3 = valueCount > 2 ? m_Values[m_CurrentValueIndex + index + 2] : default;
            var val4 = valueCount > 3 ? m_Values[m_CurrentValueIndex + index + 3] : default;
            return ReadTextShadow(valueCount, val1, val2, val3, val4);
        }

        public TextAutoSize ReadTextAutoSize(int index)
        {
            var val1 = m_Values[m_CurrentValueIndex + index];
            var val2 = valueCount > 1 ? m_Values[m_CurrentValueIndex + index + 1] : default;
            var val3 = valueCount > 2 ? m_Values[m_CurrentValueIndex + index + 2] : default;
            return ReadTextAutoSize(valueCount, val1, val2, val3);
        }

        public BackgroundPosition ReadBackgroundPositionX(int index)
        {
            return ReadBackgroundPosition(index, BackgroundPositionKeyword.Left);
        }

        public BackgroundPosition ReadBackgroundPositionY(int index)
        {
            return ReadBackgroundPosition(index, BackgroundPositionKeyword.Top);
        }

        private BackgroundPosition ReadBackgroundPosition(int index, BackgroundPositionKeyword keyword)
        {
            var val1 = m_Values[m_CurrentValueIndex + index];
            var val2 = valueCount > 1 ? m_Values[m_CurrentValueIndex + index + 1] : default;
            return ReadBackgroundPosition(valueCount, val1, val2, keyword);
        }


        public BackgroundRepeat ReadBackgroundRepeat(int index)
        {
            var val1 = m_Values[m_CurrentValueIndex + index];
            var val2 = valueCount > 1 ? m_Values[m_CurrentValueIndex + index + 1] : default;
            return ReadBackgroundRepeat(valueCount, val1, val2);
        }

        public BackgroundSize ReadBackgroundSize(int index)
        {
            var val1 = m_Values[m_CurrentValueIndex + index];
            var val2 = valueCount > 1 ? m_Values[m_CurrentValueIndex + index + 1] : default;
            return ReadBackgroundSize(valueCount, val1, val2);
        }

        // Parses a linear-/radial-gradient() USS function into a BackgroundGradient.
        // Angles normalised to CSS radians (0 = "to top", clockwise). Returns default on `none`.
        BackgroundGradient ReadBackgroundGradient(int index)
        {
            var value = m_Values[m_CurrentValueIndex + index];
            if (value.handle.valueType == StyleValueType.Keyword)
                return default; // `none`

            if (value.handle.valueType != StyleValueType.Function)
                return default;

            var fn = (StyleValueFunction)value.handle.valueIndex;
            int cursor = index + 1;
            int argCount = ReadInt(cursor++);
            int argsEnd = cursor + argCount;
            if (argsEnd > valueCount)
                argsEnd = valueCount;

            return fn switch
            {
                StyleValueFunction.LinearGradient => ParseLinearGradient(ref cursor, argsEnd),
                StyleValueFunction.RadialGradient => ParseRadialGradient(ref cursor, argsEnd),
                _ => default,
            };
        }

        BackgroundGradient ParseLinearGradient(ref int cursor, int argsEnd)
        {
            float angle = Mathf.PI; // CSS default: "to bottom"

            // Direction phase (optional) up to the first comma.
            while (cursor < argsEnd)
            {
                var value = GetValue(cursor);
                var vt = value.handle.valueType;

                if (vt == StyleValueType.CommaSeparator)
                {
                    cursor++;
                    break;
                }

                if (vt == StyleValueType.Dimension && TryReadAngle(value, out var dirAngle))
                {
                    angle = dirAngle;
                    cursor++;
                    continue;
                }

                if (vt == StyleValueType.Enum)
                {
                    var ident = value.sheet.ReadEnum(value.handle);
                    if (string.Equals(ident, "to", StringComparison.OrdinalIgnoreCase))
                    {
                        cursor++;
                        float sideA = float.NaN, sideB = float.NaN;
                        while (cursor < argsEnd)
                        {
                            var v2 = GetValue(cursor);
                            if (v2.handle.valueType != StyleValueType.Enum)
                                break;
                            var sk = v2.sheet.ReadEnum(v2.handle);
                            if (!TryGetSideAngle(sk, out var sa))
                                break;
                            if (float.IsNaN(sideA)) sideA = sa;
                            else if (float.IsNaN(sideB)) sideB = sa;
                            else break;
                            cursor++;
                        }
                        if (!float.IsNaN(sideA) && !float.IsNaN(sideB))
                            angle = AverageAngles(sideA, sideB);
                        else if (!float.IsNaN(sideA))
                            angle = sideA;
                        continue;
                    }
                    break; // named color / first stop — direction phase is done
                }

                break; // color/dimension → already in stops phase
            }

            var stops = ReadColorStops(ref cursor, argsEnd);
            if (stops.Length == 0)
                return default;

            return new BackgroundGradient
            {
                type = GradientType.Linear,
                angle = angle,
                stops = stops,
                position = new Vector2(0.5f, 0.5f),
                shape = BackgroundGradientShape.Ellipse,
                size = BackgroundGradientSize.FarthestCorner,
            };
        }

        BackgroundGradient ParseRadialGradient(ref int cursor, int argsEnd)
        {
            var shape = BackgroundGradientShape.Ellipse;
            var size = BackgroundGradientSize.FarthestCorner;
            var position = new Vector2(0.5f, 0.5f);

            // Prefix phase (optional): shape / size / position up to the first comma.
            while (cursor < argsEnd)
            {
                var value = GetValue(cursor);
                var vt = value.handle.valueType;

                if (vt == StyleValueType.CommaSeparator)
                {
                    cursor++;
                    break;
                }

                if (vt == StyleValueType.Enum)
                {
                    var ident = value.sheet.ReadEnum(value.handle);

                    if (string.Equals(ident, "circle", StringComparison.OrdinalIgnoreCase))
                    {
                        // circle needs element-aspect-aware UVs; the hash-cached baker can't deliver — coerce to ellipse.
                        if (!s_WarnedCircleCoerce)
                        {
                            s_WarnedCircleCoerce = true;
                            Debug.LogWarning(
                                "radial-gradient(circle, ...) is not supported on rectangular " +
                                "elements yet; falling back to `ellipse` (which stretches " +
                                "with the element's aspect ratio).");
                        }
                        shape = BackgroundGradientShape.Ellipse;
                        cursor++;
                        continue;
                    }
                    if (string.Equals(ident, "ellipse", StringComparison.OrdinalIgnoreCase))
                    {
                        shape = BackgroundGradientShape.Ellipse;
                        cursor++;
                        continue;
                    }
                    if (TryGetRadialExtent(ident, out var extent))
                    {
                        size = extent;
                        cursor++;
                        continue;
                    }
                    if (string.Equals(ident, "at", StringComparison.OrdinalIgnoreCase))
                    {
                        cursor++;
                        position = ReadRadialPosition(ref cursor, argsEnd);
                        continue;
                    }
                    break; // likely a named color — prefix done
                }

                break; // color/dimension before a comma → no prefix
            }

            var stops = ReadColorStops(ref cursor, argsEnd);
            if (stops.Length == 0)
                return default;

            return new BackgroundGradient
            {
                type = GradientType.Radial,
                angle = 0f,
                stops = stops,
                position = position,
                shape = shape,
                size = size,
            };
        }

        BackgroundGradientStop[] ReadColorStops(ref int cursor, int argsEnd)
        {
            var stops = new List<BackgroundGradientStop>(4);
            var hasExplicitPosition = new List<bool>(4);

            while (cursor < argsEnd)
            {
                var value = GetValue(cursor);
                var vt = value.handle.valueType;

                if (vt == StyleValueType.CommaSeparator)
                {
                    cursor++;
                    continue;
                }

                if (!TryReadStopColor(value, out var color))
                {
                    Debug.LogWarning($"Unexpected value type '{vt}' in gradient color-stop list");
                    cursor++;
                    continue;
                }
                cursor++;

                bool gotPosition = false;
                float position = 0f;
                bool isPercent = false;
                if (cursor < argsEnd)
                {
                    var nextVal = GetValue(cursor);
                    if (nextVal.handle.valueType == StyleValueType.Dimension)
                    {
                        var dim = nextVal.sheet.ReadDimension(nextVal.handle);
                        if (dim.unit == Dimension.Unit.Percent)
                        {
                            position = dim.value / 100f;
                            isPercent = true;
                            gotPosition = true;
                            cursor++;
                        }
                        else if (dim.unit == Dimension.Unit.Pixel)
                        {
                            // Pixel-positioned stops can't be resolved against the element size at bake
                            // time (the atlas is size-independent). Consume the token and treat the stop
                            // as auto-distributed so the gradient still looks sensible.
                            if (!s_WarnedPixelStopPosition)
                            {
                                s_WarnedPixelStopPosition = true;
                                Debug.LogWarning(
                                    "Pixel-positioned gradient stops (e.g. `red 10px`) aren't supported; " +
                                    "the position is dropped and the stop is auto-distributed.");
                            }
                            cursor++;
                        }
                    }
                }

                stops.Add(new BackgroundGradientStop
                {
                    color = color,
                    position = position,
                    positionIsPercent = !gotPosition || isPercent,
                });
                hasExplicitPosition.Add(gotPosition);
            }

            if (stops.Count == 0)
                return Array.Empty<BackgroundGradientStop>();

            // Auto-distribute stops without explicit positions; anchor endpoints to 0%/100%.
            if (stops.Count >= 1 && !hasExplicitPosition[0])
            {
                var s = stops[0];
                s.position = 0f;
                s.positionIsPercent = true;
                stops[0] = s;
                hasExplicitPosition[0] = true;
            }
            if (stops.Count >= 2 && !hasExplicitPosition[stops.Count - 1])
            {
                var s = stops[stops.Count - 1];
                s.position = 1f;
                s.positionIsPercent = true;
                stops[stops.Count - 1] = s;
                hasExplicitPosition[stops.Count - 1] = true;
            }
            for (int i = 1; i < stops.Count - 1; i++)
            {
                if (hasExplicitPosition[i]) continue;
                int prev = i - 1;
                int next = i + 1;
                while (next < stops.Count && !hasExplicitPosition[next]) next++;
                if (next >= stops.Count) break; // shouldn't happen given anchoring above
                int gap = next - prev;
                for (int j = prev + 1; j < next; j++)
                {
                    float t = (float)(j - prev) / gap;
                    var s = stops[j];
                    // Percent-positioned stops only; pixel positions stay as-authored.
                    if (!hasExplicitPosition[j])
                    {
                        s.position = Mathf.Lerp(stops[prev].position, stops[next].position, t);
                        s.positionIsPercent = stops[prev].positionIsPercent && stops[next].positionIsPercent;
                        stops[j] = s;
                        hasExplicitPosition[j] = true;
                    }
                }
                i = next - 1;
            }

            return stops.ToArray();
        }

        Vector2 ReadRadialPosition(ref int cursor, int argsEnd)
        {
            // CSS <position> after `at`: 1-2 tokens → single fraction-of-element-box Vector2 (default: center).
            var pos = new Vector2(0.5f, 0.5f);
            int read = 0;
            float pendingX = float.NaN;
            float pendingY = float.NaN;

            while (cursor < argsEnd && read < 2)
            {
                var value = GetValue(cursor);
                var vt = value.handle.valueType;

                if (vt == StyleValueType.CommaSeparator)
                    break;

                if (vt == StyleValueType.Enum)
                {
                    var ident = value.sheet.ReadEnum(value.handle);
                    if (TryGetPositionFraction(ident, out var fracX, out var fracY))
                    {
                        if (!float.IsNaN(fracX)) pendingX = fracX;
                        if (!float.IsNaN(fracY)) pendingY = fracY;
                        cursor++;
                        read++;
                        continue;
                    }
                    break;
                }

                if (vt == StyleValueType.Dimension)
                {
                    var dim = value.sheet.ReadDimension(value.handle);
                    float fraction;
                    if (dim.unit == Dimension.Unit.Percent)
                    {
                        fraction = dim.value / 100f;
                    }
                    else
                    {
                        // Non-percent units aren't resolvable without the element's bounds; center as a safe fallback.
                        if (!s_WarnedRadialPositionUnit)
                        {
                            s_WarnedRadialPositionUnit = true;
                            Debug.LogWarning(
                                "radial-gradient positions only support percentages; " +
                                "non-percent values fall back to the center (50%).");
                        }
                        fraction = 0.5f;
                    }
                    if (float.IsNaN(pendingX)) pendingX = fraction;
                    else if (float.IsNaN(pendingY)) pendingY = fraction;
                    cursor++;
                    read++;
                    continue;
                }

                break;
            }

            if (!float.IsNaN(pendingX)) pos.x = pendingX;
            if (!float.IsNaN(pendingY)) pos.y = pendingY;
            return pos;
        }

        static bool TryReadAngle(StylePropertyValue value, out float radians)
        {
            radians = 0f;
            // CSS treats a bare `0` (a plain float, no unit) as a valid <angle>.
            if (value.handle.valueType == StyleValueType.Float
                && Mathf.Approximately(value.sheet.ReadFloat(value.handle), 0f))
                return true;
            if (value.handle.valueType != StyleValueType.Dimension) return false;
            var dim = value.sheet.ReadDimension(value.handle);
            switch (dim.unit)
            {
                case Dimension.Unit.Degree:   radians = dim.value * Mathf.Deg2Rad; return true;
                case Dimension.Unit.Gradian:  radians = dim.value * (Mathf.PI / 200f); return true;
                case Dimension.Unit.Radian:   radians = dim.value; return true;
                case Dimension.Unit.Turn:     radians = dim.value * (2f * Mathf.PI); return true;
                default: return false;
            }
        }

        static bool TryGetSideAngle(string sideKeyword, out float radians)
        {
            switch (sideKeyword.ToLowerInvariant())
            {
                case "top":    radians = 0f;                    return true;
                case "right":  radians = Mathf.PI / 2f;         return true;
                case "bottom": radians = Mathf.PI;              return true;
                case "left":   radians = 3f * Mathf.PI / 2f;    return true;
                default:       radians = 0f;                    return false;
            }
        }

        static float AverageAngles(float a, float b)
        {
            // Bisect the shorter arc between a and b on the unit circle.
            float twoPi = 2f * Mathf.PI;
            float diff = b - a;
            if (diff > Mathf.PI) diff -= twoPi;
            else if (diff < -Mathf.PI) diff += twoPi;
            float mid = a + diff * 0.5f;
            if (mid < 0f) mid += twoPi;
            if (mid >= twoPi) mid -= twoPi;
            return mid;
        }

        static bool TryGetRadialExtent(string kw, out BackgroundGradientSize size)
        {
            switch (kw.ToLowerInvariant())
            {
                case "closest-corner":  size = BackgroundGradientSize.ClosestCorner;  return true;
                case "closest-side":    size = BackgroundGradientSize.ClosestSide;    return true;
                case "farthest-corner": size = BackgroundGradientSize.FarthestCorner; return true;
                case "farthest-side":   size = BackgroundGradientSize.FarthestSide;   return true;
                default: size = BackgroundGradientSize.FarthestCorner; return false;
            }
        }

        static bool TryGetPositionFraction(string kw, out float fracX, out float fracY)
        {
            fracX = float.NaN;
            fracY = float.NaN;
            switch (kw.ToLowerInvariant())
            {
                case "left":   fracX = 0f;   return true;
                case "right":  fracX = 1f;   return true;
                case "top":    fracY = 0f;   return true;
                case "bottom": fracY = 1f;   return true;
                // `center` is the "default axis" — leaves both fractions NaN so the caller
                // doesn't overwrite an axis already set by a paired keyword. Unset axes fall
                // back to 0.5 in the finalizer, so `at center` alone still yields (0.5, 0.5).
                case "center": return true;
                default: return false;
            }
        }

        bool TryReadStopColor(StylePropertyValue value, out Color color)
        {
            color = default;
            var vt = value.handle.valueType;
            if (vt == StyleValueType.Color)
            {
                color = value.sheet.ReadColor(value.handle);
                return true;
            }
            if (vt == StyleValueType.Enum)
            {
                var colorName = value.sheet.ReadAsString(value.handle);
                if (StyleSheetColor.TryGetColor(colorName, out color))
                    return true;
            }
            return false;
        }

        public void ReadListEasingFunction(ref UnmanagedRefCountedList<EasingFunction> result, int index)
        {
            using var list = new UnmanagedTempList<EasingFunction>(4);
            do
            {
                var value = m_Values[m_CurrentValueIndex + index];
                var handle = value.handle;
                if (handle.valueType == StyleValueType.Enum)
                {
                    var enumString = value.sheet.ReadEnum(handle);
                    StylePropertyUtil.TryGetEnumIntValue(StyleEnumType.EasingMode, enumString, out var intValue);
                    list.Add(new EasingFunction((EasingMode)intValue));
                    ++index;
                }

                if (index < valueCount)
                {
                    var nextValue = m_Values[m_CurrentValueIndex + index];
                    if (nextValue.handle.valueType == StyleValueType.CommaSeparator)
                        ++index;
                }
            }
            while (index < valueCount);
            result.CopyFrom(list.Span);
        }

        public void ReadListTimeValue(ref UnmanagedRefCountedList<TimeValue> result, int index)
        {
            using var list = new UnmanagedTempList<TimeValue>(4);
            do
            {
                var value = m_Values[m_CurrentValueIndex + index];
                var time = value.sheet.ReadDimension(value.handle).ToTime();
                list.Add(time);
                ++index;

                if (index < valueCount)
                {
                    var nextValue = m_Values[m_CurrentValueIndex + index];
                    if (nextValue.handle.valueType == StyleValueType.CommaSeparator)
                        ++index;
                }
            }
            while (index < valueCount);

            result.CopyFrom(list.Span);
        }

        static GridTrackSize GridTrackSizeFromDimension(Dimension dim)
        {
            switch (dim.unit)
            {
                case Dimension.Unit.Percent: return GridTrackSize.Percent(dim.value);
                case Dimension.Unit.Fraction: return GridTrackSize.Fraction(dim.value);
                default: return GridTrackSize.Pixels(dim.value);
            }
        }

        // Reads a single (non-function) track value at the given index. Does not advance.
        GridTrackSize ReadSingleTrack(int index)
        {
            var value = m_Values[m_CurrentValueIndex + index];
            var handle = value.handle;
            switch (handle.valueType)
            {
                case StyleValueType.Dimension:
                    return GridTrackSizeFromDimension(value.sheet.ReadDimension(handle));
                case StyleValueType.Float:
                    return GridTrackSize.Pixels(value.sheet.ReadFloat(handle));
                case StyleValueType.Enum:
                {
                    var s = value.sheet.ReadEnum(handle);
                    if (string.Equals(s, "min-content", StringComparison.OrdinalIgnoreCase)) return GridTrackSize.MinContent();
                    if (string.Equals(s, "max-content", StringComparison.OrdinalIgnoreCase)) return GridTrackSize.MaxContent();
                    return GridTrackSize.Auto();
                }
                default:
                    return GridTrackSize.Auto(); // Keyword 'auto' etc.
            }
        }

        // Reads one <track-size> at index (a single track, or a minmax()/fit-content() function),
        // advancing index past every handle it occupies, including a nested function's flattened args.
        // The importer counts a nested function as one top-level arg token, so callers walk an
        // argument list by top-level count and let this consume the extra handles a function expands to.
        GridTrackSize ReadTrackEntry(ref int index)
        {
            if (GetValueType(index) != StyleValueType.Function)
                return ReadSingleTrack(index++);

            var func = (StyleValueFunction)GetValue(index++).handle.valueIndex;
            int argCount = ReadInt(index++);
            int consumed = 0;
            GridTrackSize entry = GridTrackSize.Auto();
            if (func == StyleValueFunction.Minmax)
            {
                var min = ReadSingleTrack(index++); ++consumed;
                if (consumed < argCount && GetValueType(index) == StyleValueType.CommaSeparator) { ++index; ++consumed; }
                var max = ReadSingleTrack(index++); ++consumed;
                entry = GridTrackSize.Minmax(min, max);
            }
            else if (func == StyleValueFunction.FitContent)
            {
                var len = ReadSingleTrack(index++); ++consumed;
                entry = GridTrackSize.FitContent(len.maxValue, len.maxUnit);
            }
            for (; consumed < argCount; ++consumed) ++index;
            return entry;
        }

        // CSS Grid track list: grid-template-columns/rows, grid-auto-columns/rows.
        // Space-separated (no CommaSeparator between top-level items). Handles single tracks plus
        // the minmax(), fit-content() and repeat() track functions.
        public void ReadListGridTrackSize(ref UnmanagedRefCountedList<GridTrackSize> result, int index)
        {
            using var list = new UnmanagedTempList<GridTrackSize>(4);
            while (index < valueCount)
            {
                var handle = m_Values[m_CurrentValueIndex + index].handle;
                if (handle.valueType == StyleValueType.Function)
                {
                    var func = (StyleValueFunction)GetValue(index).handle.valueIndex;
                    if (func == StyleValueFunction.Repeat)
                    {
                        ++index;
                        int argCount = ReadInt(index++);

                        // First top-level arg is either an integer count or the auto-fill / auto-fit
                        // keyword; the rest are the repeated <track-size> pattern (a comma is one arg).
                        bool autoFill = false, autoFit = false;
                        int count = 1;
                        if (GetValueType(index) == StyleValueType.Float || GetValueType(index) == StyleValueType.Dimension)
                        {
                            count = ReadInt(index++);
                            if (count < 0) count = 0;
                        }
                        else
                        {
                            var firstVal = m_Values[m_CurrentValueIndex + index];
                            var kw = firstVal.sheet.ReadAsString(firstVal.handle);
                            autoFill = string.Equals(kw, "auto-fill", StringComparison.OrdinalIgnoreCase);
                            autoFit = string.Equals(kw, "auto-fit", StringComparison.OrdinalIgnoreCase);
                            ++index;
                        }

                        if (autoFill || autoFit)
                        {
                            // repeat(auto-fill|auto-fit, <track>): exactly one track pattern (the count
                            // resolves at layout time). Reading a single entry rather than iterating argCount
                            // keeps this robust to the writer emitting a flattened arg count for a nested
                            // pattern (StyleProperty.WriteGridTrackSize) vs the importer's top-level count.
                            if (index < valueCount && GetValueType(index) == StyleValueType.CommaSeparator) ++index;
                            var pattern = index < valueCount ? ReadTrackEntry(ref index) : GridTrackSize.Auto();
                            list.Add(autoFill ? GridTrackSize.RepeatAutoFill(pattern) : GridTrackSize.RepeatAutoFit(pattern));
                        }
                        else
                        {
                            using var group = new UnmanagedTempList<GridTrackSize>(4);
                            for (int arg = 1; arg < argCount; ++arg)
                            {
                                if (GetValueType(index) == StyleValueType.CommaSeparator) { ++index; continue; }
                                group.Add(ReadTrackEntry(ref index));
                            }
                            var groupSpan = group.Span;
                            for (int r = 0; r < count; ++r)
                                for (int g = 0; g < groupSpan.Length; ++g)
                                    list.Add(groupSpan[g]);
                        }
                    }
                    else
                    {
                        list.Add(ReadTrackEntry(ref index));
                    }
                }
                else if (handle.valueType == StyleValueType.Keyword)
                {
                    var kw = m_Values[m_CurrentValueIndex + index].sheet.ReadKeyword(handle);
                    ++index;
                    if (kw == StyleValueKeyword.Auto) list.Add(GridTrackSize.Auto());
                    // 'none' -> no explicit tracks (empty list); other keywords ignored.
                }
                else if (handle.valueType == StyleValueType.CommaSeparator)
                {
                    ++index;
                }
                else
                {
                    list.Add(ReadSingleTrack(index));
                    ++index;
                }
            }

            result.CopyFrom(list.Span);
        }

        public void ReadListUnmanagedFilterFunction(ref UnmanagedRefCountedList<UnmanagedFilterFunction> result, int index)
        {
            using var list = new UnmanagedTempList<UnmanagedFilterFunction>(4);
            do
            {
                var value = m_Values[m_CurrentValueIndex + index];

                if (value.handle.valueType == StyleValueType.Keyword)
                {
                    break;
                }

                var filterType = (StyleValueFunction)GetValue(index++).handle.valueIndex;
                int argCount = ReadInt(index++);

                bool isCustom = false;
                FilterFunctionDefinition filterDef = null;
                if (filterType == StyleValueFunction.CustomFilter && argCount > 0)
                {
                    isCustom = true;
                    filterDef = ReadAsset(index++) as FilterFunctionDefinition;
                    --argCount;
                }

                var args = new FixedBuffer4<FilterParameter>();
                for (int i = 0; i < argCount; i++)
                {
                    var valueType = GetValueType(index);
                    if (valueType == StyleValueType.Color || valueType == StyleValueType.Enum)
                    {
                        var color = ReadColor(index++);
                        args[i] = new FilterParameter()
                        {
                            type = FilterParameterType.Color,
                            colorValue = color
                        };
                    }
                    else if (valueType == StyleValueType.Dimension || valueType == StyleValueType.Float)
                    {
                        var dimValue = GetValue(index++);
                        var dim = dimValue.sheet.ReadDimension(dimValue.handle);
                        args[i] = new FilterParameter()
                        {
                            type = FilterParameterType.Float,
                            floatValue = StyleProperty.ConvertDimensionToFilterFloat(dim)
                        };
                    }
                    else if (valueType == StyleValueType.CommaSeparator)
                    {
                        // Not technically a valid syntax, but we'll allow it
                        continue;
                    }
                    else
                    {
                        Debug.LogError($"Unexpected value type {valueType} in filter function argument");
                    }
                }

                if (isCustom)
                    list.Add(new FilterFunction(filterDef, args, argCount));
                else
                    list.Add(new FilterFunction(StyleProperty.ToFilterFunctionType(filterType), args, argCount));
            }
            while (index < valueCount);

            result.CopyFrom(list.Span);
        }

        public void ReadListStylePropertyId(ref UnmanagedRefCountedList<StylePropertyId> result, int index)
        {
            using var list = new UnmanagedTempList<StylePropertyId>(4);
            do
            {
                var value = m_Values[m_CurrentValueIndex + index];

                StylePropertyName propertyName;
                if (value.handle.valueType == StyleValueType.Keyword)
                {
                    var keyword = value.sheet.ReadKeyword(value.handle);
                    propertyName = new StylePropertyName(keyword.ToUssString());
                }
                else
                {
                    propertyName = value.sheet.ReadStylePropertyName(value.handle);
                }
                list.Add(propertyName.id);
                ++index;

                if (index < valueCount)
                {
                    var nextValue = m_Values[m_CurrentValueIndex + index];
                    if (nextValue.handle.valueType == StyleValueType.CommaSeparator)
                        ++index;
                }
            }
            while (index < valueCount);

            result.CopyFrom(list.Span);
        }

        public void ReadListFloat(ref UnmanagedRefCountedList<float> result, int index)
        {
            using var list = new UnmanagedTempList<float>(4);
            do
            {
                list.Add(ReadFloat(index));
                ++index;

                if (index < valueCount)
                {
                    var nextValue = m_Values[m_CurrentValueIndex + index];
                    if (nextValue.handle.valueType == StyleValueType.CommaSeparator)
                        ++index;
                }
            }
            while (index < valueCount);

            result.CopyFrom(list.Span);
        }

        public void ReadListAnimationIterationCount(ref UnmanagedRefCountedList<AnimationIterationCount> result, int index)
        {
            using var list = new UnmanagedTempList<AnimationIterationCount>(4);
            do
            {
                // A finite count is a Float handle; the `infinite` keyword is an Enum handle (see the importer).
                list.Add(GetValueType(index) == StyleValueType.Float
                    ? new AnimationIterationCount(ReadFloat(index))
                    : AnimationIterationCount.Infinite());
                ++index;

                if (index < valueCount)
                {
                    var nextValue = m_Values[m_CurrentValueIndex + index];
                    if (nextValue.handle.valueType == StyleValueType.CommaSeparator)
                        ++index;
                }
            }
            while (index < valueCount);

            result.CopyFrom(list.Span);
        }

        public void ReadListAnimationDirection(ref UnmanagedRefCountedList<AnimationDirection> result, int index)
        {
            using var list = new UnmanagedTempList<AnimationDirection>(4);
            do
            {
                list.Add((AnimationDirection)ReadEnum(StyleEnumType.AnimationDirection, index));
                ++index;

                if (index < valueCount)
                {
                    var nextValue = m_Values[m_CurrentValueIndex + index];
                    if (nextValue.handle.valueType == StyleValueType.CommaSeparator)
                        ++index;
                }
            }
            while (index < valueCount);

            result.CopyFrom(list.Span);
        }

        public void ReadListAnimationPlayState(ref UnmanagedRefCountedList<AnimationPlayState> result, int index)
        {
            using var list = new UnmanagedTempList<AnimationPlayState>(4);
            do
            {
                list.Add((AnimationPlayState)ReadEnum(StyleEnumType.AnimationPlayState, index));
                ++index;

                if (index < valueCount)
                {
                    var nextValue = m_Values[m_CurrentValueIndex + index];
                    if (nextValue.handle.valueType == StyleValueType.CommaSeparator)
                        ++index;
                }
            }
            while (index < valueCount);

            result.CopyFrom(list.Span);
        }

        public void ReadListEntityId(ref UnmanagedRefCountedList<EntityId> result, int index)
        {
            using var list = new UnmanagedTempList<EntityId>(4);
            do
            {
                list.Add(ReadUIAnimationClip(index));
                ++index;

                if (index < valueCount)
                {
                    var nextValue = m_Values[m_CurrentValueIndex + index];
                    if (nextValue.handle.valueType == StyleValueType.CommaSeparator)
                        ++index;
                }
            }
            while (index < valueCount);

            result.CopyFrom(list.Span);
        }

        public void ReadListString(List<string> list, int index)
        {
            list.Clear();
            do
            {
                var value = m_Values[m_CurrentValueIndex + index];
                var str = value.sheet.ReadAsString(value.handle);
                list.Add(str);
                ++index;

                if (index < valueCount)
                {
                    var nextValue = m_Values[m_CurrentValueIndex + index];
                    if (nextValue.handle.valueType == StyleValueType.CommaSeparator)
                        ++index;
                }
            }
            while (index < valueCount);
        }

        public StyleRatio ReadRatio(int index)
        {
            if(valueCount == 1 && GetValueType(0) == StyleValueType.Float )
                return new StyleRatio(ReadFloat(index));

            if (valueCount == 3)
            {
                var val2 = m_Values[m_CurrentValueIndex + index + 1];
                var str = val2.sheet.ReadAsString(val2.handle);

                if(str == "/" )
                    return ReadFloat(index) / ReadFloat(index + 2);

            }

            if(!IsKeyword(0, StyleValueKeyword.Auto))
                Debug.LogError($"Unexpected value {m_Values[0].ToString() } in ratio parsing");

            return StyleRatio.Auto();

        }

        // CSS Grid. Reads one grid-line placement value: auto | <integer >= 1> | span [<n>].
        public StyleGridLine ReadGridLine(int index)
        {
            if (IsKeyword(index, StyleValueKeyword.Auto))
                return new StyleGridLine(GridLine.Auto);

            var type = GetValueType(index);
            if (type == StyleValueType.Enum)
            {
                var v = m_Values[m_CurrentValueIndex + index];
                var str = v.sheet.ReadAsString(v.handle);
                if (string.Equals(str, "span", System.StringComparison.OrdinalIgnoreCase))
                {
                    int count = 1; // "span" with the integer omitted defaults to 1
                    if (valueCount > index + 1 && GetValueType(index + 1) == StyleValueType.Float)
                        count = (int)ReadFloat(index + 1);
                    if (count >= 1)
                        return new StyleGridLine(GridLine.Span(count));
                    Debug.LogError($"Invalid grid span '{count}'; a span must be >= 1.");
                    return new StyleGridLine(GridLine.Auto);
                }
            }

            if (type == StyleValueType.Float)
            {
                int line = (int)ReadFloat(index);
                if (line >= 1)
                    return new StyleGridLine(GridLine.AtLine(line));
                Debug.LogError($"Invalid grid line '{line}'; a line must be >= 1 (0 and negatives are invalid).");
                return new StyleGridLine(GridLine.Auto);
            }

            return new StyleGridLine(GridLine.Auto);
        }



        private void LoadProperties()
        {
            m_CurrentPropertyIndex = 0;
            m_CurrentValueIndex = 0;
            m_Values.Clear();
            m_ValueCount.Clear();

            foreach (var sp in m_Properties)
            {
                int count = 0;
                bool valid = true;

                if (sp.requireVariableResolve)
                {
                    // Slow path - Values contain one or more var
                    m_Resolver.Init(sp, m_Sheet, sp.values);
                    for (int i = 0; i < sp.values.Length && valid; ++i)
                    {
                        var handle = sp.values[i];
                        if (handle.IsVarFunction())
                        {
                            valid = m_Resolver.ResolveVarFunction(ref i);
                        }
                        else
                        {
                            m_Resolver.AddValue(handle);
                        }
                    }

                    if (valid && m_Resolver.ValidateResolvedValues())
                    {
                        m_Values.AddRange(m_Resolver.resolvedValues);
                        count += m_Resolver.resolvedValues.Count;
                    }
                    else
                    {
                        // Resolve failed
                        // When this happens, the computed value of the property is either the property’s
                        // inherited value or its initial value depending on whether the property is inherited or not.
                        // This is the same behavior as the unset keyword so we simply resolve to that value.
                        var unsetHandle = new StyleValueHandle() { valueType = StyleValueType.Keyword, valueIndex = (int)StyleValueKeyword.Unset};
                        m_Values.Add(new StylePropertyValue() { sheet = m_Sheet, handle = unsetHandle });
                        ++count;
                    }
                }
                else
                {
                    // Fast path - no var
                    count = sp.values.Length;
                    for (int i = 0; i < count; ++i)
                    {
                        m_Values.Add(new StylePropertyValue() { sheet = m_Sheet, handle = sp.values[i] });
                    }
                }

                m_ValueCount.Add(count);
            }

            SetCurrentProperty();
        }

        private void SetCurrentProperty()
        {
            if (m_CurrentPropertyIndex < m_Properties.Length)
            {
                property = m_Properties[m_CurrentPropertyIndex];
                propertyId = property.id;
                valueCount = m_ValueCount[m_CurrentPropertyIndex];
            }
            else
            {
                property = null;
                propertyId = StylePropertyId.Unknown;
                valueCount = 0;
            }
        }
    }
}
