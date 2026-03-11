// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;
using UnityEngine.TextCore.Text;

namespace UnityEngine.UIElements;

internal static partial class UIToolkitConverters
{
    static Overflow ConvertOverflowInternalToOverflow(ref OverflowInternal value) => (Overflow)value;
    static StyleEnum<Overflow> ConvertOverflowInternalToStyleOverflow(ref OverflowInternal value) => (Overflow)value;
    static OverflowInternal ConvertOverflowToOverflowInternal(ref Overflow value) => (OverflowInternal)value;
    static OverflowInternal ConvertStyleOverflowToOverflowInternal(ref StyleEnum<Overflow> value) => (OverflowInternal)value.value;
    static StyleColor ConvertColor32ToStyleColor(ref Color32 value) => new StyleColor(value);
    static Color32 ConvertStyleColorToColor32(ref StyleColor value) => (Color32)value.value;
    static StyleRatio ConvertStyleRatioToFloat(ref float v) => new StyleRatio(v);
    static float ConvertFloatToStyleRatio(ref StyleRatio sv) => (float)sv.value;
    static StyleBackground ConvertTexture2DToStyleBackground(ref Texture2D source) => Background.FromTexture2D(source);
    static StyleBackground ConvertSpriteToStyleBackground(ref Sprite source) => Background.FromSprite(source);
    static StyleBackground ConvertVectorImageToStyleBackground(ref VectorImage source) => Background.FromVectorImage(source);
    static StyleBackground ConvertRenderTextureToStyleBackground(ref RenderTexture source) => Background.FromRenderTexture(source);
    static Texture2D ConvertStyleBackgroundToTexture2D(ref StyleBackground source) => source.value.texture;
    static Sprite ConvertStyleBackgroundToSprite(ref StyleBackground source) => source.value.sprite;
    static VectorImage ConvertStyleBackgroundToVectorImage(ref StyleBackground source) => source.value.vectorImage;
    static RenderTexture ConvertStyleBackgroundToRenderTexture(ref StyleBackground source) => source.value.renderTexture;
    static float ConvertStyleLengthToFloat(ref StyleLength source) => source.value.value;
    static int ConvertStyleLengthToInt(ref StyleLength source) => (int)source.value.value;
    static StyleLength ConvertFloatToStyleLength(ref float source) => source;
    static StyleLength ConvertIntToStyleLength(ref int source) => source;
    static int ConvertStyleFloatToInt(ref StyleFloat source) => (int)source.value;
    static StyleFloat ConvertIntToStyleFloat(ref int source) => source;
    static StyleFontDefinition ConvertFontToStyleFontDefinition(ref Font source) => new(source);
    static StyleFontDefinition ConvertFontAssetToStyleFontDefinition(ref FontAsset source) => new(source);
    static Font ConvertStyleFontDefinitionToFont(ref StyleFontDefinition source) => source.value.font;
    static FontAsset ConvertStyleFontDefinitionToFontAsset(ref StyleFontDefinition source) => source.value.fontAsset;

    public static void Register()
    {
        RegisterBuiltInStyleConverters();
        RegisterAdditionalConverters();
    }

    static void RegisterAdditionalConverters()
    {
        ConverterGroups.Unsafe.LazyRegisterGlobal(typeof(OverflowInternal), typeof(Overflow), () => (TypeConverter<OverflowInternal, Overflow>) ConvertOverflowInternalToOverflow);
        ConverterGroups.Unsafe.LazyRegisterGlobal(typeof(OverflowInternal), typeof(StyleEnum<Overflow>), () => (TypeConverter<OverflowInternal, StyleEnum<Overflow>>) ConvertOverflowInternalToStyleOverflow);
        ConverterGroups.Unsafe.LazyRegisterGlobal(typeof(Overflow), typeof(OverflowInternal), () => (TypeConverter<Overflow, OverflowInternal>)ConvertOverflowToOverflowInternal);
        ConverterGroups.Unsafe.LazyRegisterGlobal(typeof(StyleEnum<Overflow>), typeof(OverflowInternal), () => (TypeConverter<StyleEnum<Overflow>, OverflowInternal>)ConvertStyleOverflowToOverflowInternal);
        ConverterGroups.Unsafe.LazyRegisterGlobal(typeof(Color32), typeof(StyleColor), () => (TypeConverter<Color32, StyleColor>)ConvertColor32ToStyleColor);
        ConverterGroups.Unsafe.LazyRegisterGlobal(typeof(StyleColor), typeof(Color32), () => (TypeConverter<StyleColor, Color32>)ConvertStyleColorToColor32);
        ConverterGroups.Unsafe.LazyRegisterGlobal(typeof(float), typeof(StyleRatio), () => (TypeConverter<float, StyleRatio>)ConvertStyleRatioToFloat);
        ConverterGroups.Unsafe.LazyRegisterGlobal(typeof(StyleRatio), typeof(float), () => (TypeConverter<StyleRatio, float>)ConvertFloatToStyleRatio);
        ConverterGroups.Unsafe.LazyRegisterGlobal(typeof(Texture2D), typeof(StyleBackground), () => (TypeConverter<Texture2D, StyleBackground>)ConvertTexture2DToStyleBackground);
        ConverterGroups.Unsafe.LazyRegisterGlobal(typeof(Sprite), typeof(StyleBackground), () => (TypeConverter<Sprite, StyleBackground>)ConvertSpriteToStyleBackground);
        ConverterGroups.Unsafe.LazyRegisterGlobal(typeof(VectorImage), typeof(StyleBackground), () => (TypeConverter<VectorImage, StyleBackground>)ConvertVectorImageToStyleBackground);
        ConverterGroups.Unsafe.LazyRegisterGlobal(typeof(RenderTexture), typeof(StyleBackground), () => (TypeConverter<RenderTexture, StyleBackground>)ConvertRenderTextureToStyleBackground);
        ConverterGroups.Unsafe.LazyRegisterGlobal(typeof(StyleBackground), typeof(Texture2D), () => (TypeConverter<StyleBackground, Texture2D>)ConvertStyleBackgroundToTexture2D);
        ConverterGroups.Unsafe.LazyRegisterGlobal(typeof(StyleBackground), typeof(Sprite), () => (TypeConverter<StyleBackground, Sprite>)ConvertStyleBackgroundToSprite);
        ConverterGroups.Unsafe.LazyRegisterGlobal(typeof(StyleBackground), typeof(VectorImage), () => (TypeConverter<StyleBackground, VectorImage>)ConvertStyleBackgroundToVectorImage);
        ConverterGroups.Unsafe.LazyRegisterGlobal(typeof(StyleBackground), typeof(RenderTexture), () => (TypeConverter<StyleBackground, RenderTexture>)ConvertStyleBackgroundToRenderTexture);
        ConverterGroups.Unsafe.LazyRegisterGlobal(typeof(StyleLength), typeof(float), () => (TypeConverter<StyleLength, float>)ConvertStyleLengthToFloat);
        ConverterGroups.Unsafe.LazyRegisterGlobal(typeof(StyleLength), typeof(int), () => (TypeConverter<StyleLength, int>)ConvertStyleLengthToInt);
        ConverterGroups.Unsafe.LazyRegisterGlobal(typeof(float), typeof(StyleLength), () => (TypeConverter<float, StyleLength>)ConvertFloatToStyleLength);
        ConverterGroups.Unsafe.LazyRegisterGlobal(typeof(int), typeof(StyleLength), () => (TypeConverter<int, StyleLength>)ConvertIntToStyleLength);
        ConverterGroups.Unsafe.LazyRegisterGlobal(typeof(StyleFloat), typeof(int), () => (TypeConverter<StyleFloat, int>)ConvertStyleFloatToInt);
        ConverterGroups.Unsafe.LazyRegisterGlobal(typeof(int), typeof(StyleFloat), () => (TypeConverter<int, StyleFloat>)ConvertIntToStyleFloat);
        ConverterGroups.Unsafe.LazyRegisterGlobal(typeof(Font), typeof(StyleFontDefinition), () => (TypeConverter<Font, StyleFontDefinition>)ConvertFontToStyleFontDefinition);
        ConverterGroups.Unsafe.LazyRegisterGlobal(typeof(FontAsset), typeof(StyleFontDefinition), () => (TypeConverter<FontAsset, StyleFontDefinition>)ConvertFontAssetToStyleFontDefinition);
        ConverterGroups.Unsafe.LazyRegisterGlobal(typeof(StyleFontDefinition), typeof(Font), () => (TypeConverter<StyleFontDefinition, Font>)ConvertStyleFontDefinitionToFont);
        ConverterGroups.Unsafe.LazyRegisterGlobal(typeof(StyleFontDefinition), typeof(FontAsset), () => (TypeConverter<StyleFontDefinition, FontAsset>)ConvertStyleFontDefinitionToFontAsset);
    }
}
