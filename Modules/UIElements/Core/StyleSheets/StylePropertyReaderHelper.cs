// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.TextCore.Text;

namespace UnityEngine.UIElements.StyleSheets
{
    internal partial class StylePropertyReader
    {
        static public TransformOrigin ReadTransformOrigin(int valCount, StylePropertyValue val1, StylePropertyValue val2, StylePropertyValue zVvalue)
        {
            Length x = Length.Percent(50) , y = Length.Percent(50);
            float z = 0;

            switch (valCount)
            {
                case 1: //Single parameter, Could be x-offset(length or %) or offset-keyword (left, right, top, bottom, or center)
                {
                    var val = ReadTransformOriginEnum(val1, out bool isVertical, out bool isHorizontal);
                    if (isHorizontal)     //We apply on X by default, except if we have top or bottom
                        x = val;
                    else
                        y = val;
                    break;
                }
                case 2: //two parameter ( x, y ) whether they are value or keyword. The order can be inverted if using keywords (ie top, left)
                {
                    var len1 = ReadTransformOriginEnum(val1, out bool isVertical1, out bool isHorizontal1);
                    var len2 = ReadTransformOriginEnum(val2, out bool isVertical2, out bool isHorizontal2);

                    if (!isHorizontal1 || !isVertical2)    //Argument probably are "swapped" if one of both cant be assigned "in order" to x, y
                    {
                        if (isHorizontal2 && isVertical1)
                        {
                            x = len2;
                            y = len1;
                        }
                    }
                    else
                    {
                        x = len1;
                        y = len2;
                    }

                    break;
                }
                case 3: // xyz
                    if (zVvalue.handle.valueType == StyleValueType.Dimension || zVvalue.handle.valueType == StyleValueType.Float)
                    {
                        var dimension = zVvalue.sheet.ReadDimension(zVvalue.handle);
                        z = dimension.value;
                    }

                    goto case 2; //Go parse the first arguments
            }


            return new TransformOrigin(x, y, z);
        }

        static private Length ReadTransformOriginEnum(StylePropertyValue value, out bool isVertical, out bool isHorizontal)
        {
            if (value.handle.valueType == StyleValueType.Enum)
            {
                var enumValue = (TransformOriginOffset)ReadEnum(StyleEnumType.TransformOriginOffset, value);
                switch (enumValue)
                {
                    case TransformOriginOffset.Left:
                        isVertical = false; isHorizontal = true;
                        return Length.Percent(0);
                    case TransformOriginOffset.Top:
                        isVertical = true; isHorizontal = false;
                        return Length.Percent(0);
                    case TransformOriginOffset.Center:
                        isVertical = true; isHorizontal = true;
                        return Length.Percent(50);
                    case TransformOriginOffset.Right:
                        isVertical = false; isHorizontal = true;
                        return Length.Percent(100);
                    case TransformOriginOffset.Bottom:
                        isVertical = true; isHorizontal = false;
                        return Length.Percent(100);
                }
            }
            else if (value.handle.valueType == StyleValueType.Dimension || value.handle.valueType == StyleValueType.Float)
            {
                isVertical = true; isHorizontal = true;
                return value.sheet.ReadDimension(value.handle).ToLength();
            }

            isVertical = false; isHorizontal = false; // should not matter because there would be no ambiguity
            return Length.Percent(50);
        }

        static public Translate ReadTranslate(int valCount, StylePropertyValue val1, StylePropertyValue val2, StylePropertyValue val3)
        {
            if (val1.handle.valueType == StyleValueType.Keyword && (StyleValueKeyword)val1.handle.valueIndex == StyleValueKeyword.None)
            {
                return Translate.None();
            }


            Length x = 0, y = 0;
            float z = 0;

            switch (valCount)
            {
                case 1:// If only one argument, the translation is along both axes
                    if (val1.handle.valueType == StyleValueType.Dimension || val1.handle.valueType == StyleValueType.Float)
                    {
                        x = val1.sheet.ReadDimension(val1.handle).ToLength();
                        y = val1.sheet.ReadDimension(val1.handle).ToLength();
                    }
                    break;

                case 2://X Y value
                    if (val1.handle.valueType == StyleValueType.Dimension || val1.handle.valueType == StyleValueType.Float)
                    {
                        x = val1.sheet.ReadDimension(val1.handle).ToLength();
                    }

                    if (val2.handle.valueType == StyleValueType.Dimension || val2.handle.valueType == StyleValueType.Float)
                    {
                        y = val2.sheet.ReadDimension(val2.handle).ToLength();
                    }
                    break;

                case 3: //X Y Z value
                    if (val3.handle.valueType == StyleValueType.Dimension || val3.handle.valueType == StyleValueType.Float)
                    {
                        var dimension = val3.sheet.ReadDimension(val3.handle);
                        z = dimension.value;
                    }
                    goto case 2; //Parse the first 2 parameters
            }
            return new Translate(x, y, z);
        }

        static public Scale ReadScale(int valCount, StylePropertyValue val1, StylePropertyValue val2, StylePropertyValue val3)
        {
            if (val1.handle.valueType == StyleValueType.Keyword && (StyleValueKeyword)val1.handle.valueIndex == StyleValueKeyword.None)
            {
                return Scale.None();
            }

            var scale = Vector3.one;

            switch (valCount)
            {
                case 1: // If only one argument, the translation is along both axes
                    if (val1.handle.valueType == StyleValueType.Dimension || val1.handle.valueType == StyleValueType.Float)
                    {
                        scale.x = val1.sheet.ReadFloat(val1.handle);
                        scale.y = scale.x;
                    }
                    break;
                case 2://X Y value
                    if (val1.handle.valueType == StyleValueType.Dimension || val1.handle.valueType == StyleValueType.Float)
                    {
                        scale.x = val1.sheet.ReadFloat(val1.handle);
                    }

                    if (val2.handle.valueType == StyleValueType.Dimension || val2.handle.valueType == StyleValueType.Float)
                    {
                        scale.y = val2.sheet.ReadFloat(val2.handle);
                    }
                    break;
                case 3: //X Y Z value
                    if (val3.handle.valueType == StyleValueType.Dimension || val3.handle.valueType == StyleValueType.Float)
                    {
                        scale.z = val3.sheet.ReadFloat(val3.handle);
                    }
                    goto case 2; //Parse the first 2 parameters
            }
            return new Scale(scale);
        }
        static public Rotate ReadRotate(int valCount, StylePropertyValue val1, StylePropertyValue val2, StylePropertyValue val3, StylePropertyValue val4)
        {
            if (val1.handle.valueType == StyleValueType.Keyword && (StyleValueKeyword)val1.handle.valueIndex == StyleValueKeyword.None)
            {
                return Rotate.None();
            }

            var rot = Rotate.Initial();

            switch (valCount)
            {
                case 1: // If only one argument, the only argument is an angle and the rotation is in Z
                    if (val1.handle.valueType == StyleValueType.Dimension)
                    {
                        rot.angle = ReadAngle(val1);
                        //we leave axis to the default value;
                    }
                    break;
                case 2:// axis by name and an angle
                    rot.angle = ReadAngle(val2);
                    var enumValue = (Axis)ReadEnum(StyleEnumType.Axis, val1);
                    switch (enumValue)
                    {
                        case Axis.X:
                            rot.axis = new Vector3(1, 0, 0);
                            break;
                        case Axis.Y:
                            rot.axis = new Vector3(0, 1, 0);
                            break;
                        case Axis.Z:
                            rot.axis = new Vector3(0, 0, 1);
                            break;
                    }
                    break;
                case 4://vector+angle
                    rot.angle = ReadAngle(val4);
                    rot.axis = new(val1.sheet.ReadFloat(val1.handle), val1.sheet.ReadFloat(val2.handle), val1.sheet.ReadFloat(val3.handle)); ;
                    break;
            }
            return rot;
        }

        static bool TryReadEnum(StyleEnumType enumType, StylePropertyValue value, out int intValue)
        {
            string enumString = null;
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

            return (StylePropertyUtil.TryGetEnumIntValue(enumType, enumString, out intValue));
        }

        static int ReadEnum(StyleEnumType enumType, StylePropertyValue value)
        {
            string enumString = null;
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

        static public Angle ReadAngle(StylePropertyValue value)
        {
            if (value.handle.valueType == StyleValueType.Keyword)
            {
                var keyword = (StyleValueKeyword)value.handle.valueIndex;
                switch (keyword)
                {
                    case StyleValueKeyword.None:
                        return Angle.None();
                    default:
                        return new Angle();
                }
            }

            var dimension = value.sheet.ReadDimension(value.handle);
            return dimension.ToAngle();
        }

        static public BackgroundPosition ReadBackgroundPosition(int valCount, StylePropertyValue val1, StylePropertyValue val2, BackgroundPositionKeyword keyword)
        {
            if (valCount == 1)
            {
                if (val1.handle.valueType == StyleValueType.Enum)
                {
                    return new BackgroundPosition((BackgroundPositionKeyword)ReadEnum(StyleEnumType.BackgroundPositionKeyword, val1));
                }
                else if ((val1.handle.valueType == StyleValueType.Dimension) || (val1.handle.valueType == StyleValueType.Float))
                {
                    return new BackgroundPosition(keyword, val1.sheet.ReadDimension(val1.handle).ToLength());
                }
            }
            else if (valCount == 2)
            {
                if ((val1.handle.valueType == StyleValueType.Enum) &&
                    ((val2.handle.valueType == StyleValueType.Dimension) || (val2.handle.valueType == StyleValueType.Float)))
                {
                    return new BackgroundPosition(
                        (BackgroundPositionKeyword)ReadEnum(StyleEnumType.BackgroundPositionKeyword, val1),
                        val1.sheet.ReadDimension(val2.handle).ToLength());
                }
            }

            return new BackgroundPosition();
        }

        static public BackgroundRepeat ReadBackgroundRepeat(int valCount, StylePropertyValue val1, StylePropertyValue val2)
        {
            var backgroundRepeat = new BackgroundRepeat();

            if (valCount == 1)
            {
                int enumValue;
                if (TryReadEnum(StyleEnumType.RepeatXY, val1, out enumValue))
                {
                    if ((RepeatXY)enumValue == RepeatXY.RepeatX)
                    {
                        backgroundRepeat.x = Repeat.Repeat;
                        backgroundRepeat.y = Repeat.NoRepeat;
                    }
                    else if ((RepeatXY)enumValue == RepeatXY.RepeatY)
                    {
                        backgroundRepeat.x = Repeat.NoRepeat;
                        backgroundRepeat.y = Repeat.Repeat;
                    }
                }
                else
                {
                    backgroundRepeat.x = (Repeat)ReadEnum(StyleEnumType.Repeat, val1);
                    backgroundRepeat.y = backgroundRepeat.x;
                }
            }
            else
            {
                backgroundRepeat.x = (Repeat)ReadEnum(StyleEnumType.Repeat, val1);
                backgroundRepeat.y = (Repeat)ReadEnum(StyleEnumType.Repeat, val2);
            }

            return backgroundRepeat;
        }

        static public BackgroundSize ReadBackgroundSize(int valCount, StylePropertyValue val1, StylePropertyValue val2)
        {
            var BackgroundSize = new BackgroundSize();

            if (valCount == 1)
            {
                if (val1.handle.valueType == StyleValueType.Keyword)
                {
                    if ((StyleValueKeyword)val1.handle.valueIndex == StyleValueKeyword.Auto)
                    {
                        BackgroundSize.x = Length.Auto();
                        BackgroundSize.y = Length.Auto();
                    }
                }
                else if (val1.handle.valueType == StyleValueType.Enum)
                {
                    BackgroundSize.sizeType = (BackgroundSizeType)ReadEnum(StyleEnumType.BackgroundSizeType, val1);
                }
                else if (val1.handle.valueType == StyleValueType.Dimension)
                {
                    BackgroundSize.x = val1.sheet.ReadDimension(val1.handle).ToLength();
                    BackgroundSize.y = Length.Auto();
                }
            }
            else if (valCount == 2)
            {
                if (val1.handle.valueType == StyleValueType.Keyword)
                {
                    if ((StyleValueKeyword)val1.handle.valueIndex == StyleValueKeyword.Auto)
                    {
                        BackgroundSize.x = Length.Auto();
                    }
                }
                else if (val1.handle.valueType == StyleValueType.Dimension)
                {
                    BackgroundSize.x = val1.sheet.ReadDimension(val1.handle).ToLength();
                }

                if (val2.handle.valueType == StyleValueType.Keyword)
                {
                    if ((StyleValueKeyword)val2.handle.valueIndex == StyleValueKeyword.Auto)
                    {
                        BackgroundSize.y = Length.Auto();
                    }
                }
                else if (val2.handle.valueType == StyleValueType.Dimension)
                {
                    BackgroundSize.y = val2.sheet.ReadDimension(val2.handle).ToLength();
                }
            }

            return BackgroundSize;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static bool TryGetImageSourceFromValue(StylePropertyValue propertyValue, float dpiScaling, out ImageSource source)
        {
            source = new ImageSource();

            switch (propertyValue.handle.valueType)
            {
                case StyleValueType.ResourcePath:
                {
                    string path = propertyValue.sheet.ReadResourcePath(propertyValue.handle);
                    if (!string.IsNullOrEmpty(path))
                    {
                        //TODO: This will use GUIUtility.pixelsPerPoint as targetDpi, this may not be the best value for the current panel
                        source.sprite = Panel.LoadResource(path, typeof(Sprite), dpiScaling) as Sprite;
                        if (source.IsNull())
                            source.texture = Panel.LoadResource(path, typeof(Texture2D), dpiScaling) as Texture2D;
                        if (source.IsNull())
                            source.vectorImage = Panel.LoadResource(path, typeof(VectorImage), dpiScaling) as VectorImage;
                        if (source.IsNull())
                            source.renderTexture = Panel.LoadResource(path, typeof(RenderTexture), dpiScaling) as RenderTexture;
                    }

                    if (source.IsNull())
                    {
                        Debug.LogWarning(string.Format("Image not found for path: {0}", path));
                        return false;
                    }
                }
                break;

                case StyleValueType.AssetReference:
                {
                    var o = propertyValue.sheet.ReadAssetReference(propertyValue.handle);
                    source.texture = o as Texture2D;
                    source.sprite = o as Sprite;
                    source.vectorImage = o as VectorImage;
                    source.renderTexture = o as RenderTexture;
                    if (source.IsNull())
                    {
                        Debug.LogWarning("Invalid image specified");
                        return false;
                    }
                }
                break;

                case StyleValueType.MissingAssetReference:
                    return false;

                case StyleValueType.ScalableImage:
                {
                    var img = propertyValue.sheet.ReadScalableImage(propertyValue.handle);

                    if (img.normalImage == null && img.highResolutionImage == null)
                    {
                        Debug.LogWarning("Invalid scalable image specified");
                        return false;
                    }

                    if (dpiScaling > 1.0f)
                    {
                        source.texture = img.highResolutionImage;
                        source.texture.pixelsPerPoint = 2.0f;
                    }
                    else
                    {
                        source.texture = img.normalImage;
                    }

                    if (!Mathf.Approximately(dpiScaling % 1.0f, 0))
                    {
                        source.texture.filterMode = FilterMode.Bilinear;
                    }
                }
                break;

                default:
                    Debug.LogWarning("Invalid value for image texture " + propertyValue.handle.valueType);
                    return false;
            }

            return true;
        }
    }
}
