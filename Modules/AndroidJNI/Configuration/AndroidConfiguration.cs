// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using System.Text;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Android
{
    public class AndroidLocale
    {
        public string country { get; }
        public string language { get; }

        internal AndroidLocale(string _country, string _language)
        {
            country = _country;
            language = _language;
        }
    }

    [NativeAsStruct]
    [NativeType(Header = "Modules/AndroidJNI/Public/AndroidConfiguration.bindings.h")]
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public sealed class AndroidConfiguration
    {
        const int UiModeNightMask = 48;
        const int UiModeTypeMask = 15;

        const int ScreenLayoutDirectionMask = 192;
        const int ScreenLayoutLongMask = 48;
        const int ScreenLayoutRoundMask = 768;
        const int ScreenLayoutSizeMask = 15;

        const int ColorModeHdrMask = 12;
        const int ColorModeWideColorGamutMask = 3;

        private int colorMode { get; set; }
        public int densityDpi { get; private set; }
        public float fontScale { get; private set; }
        public int fontWeightAdjustment { get; private set; }
        public AndroidKeyboard keyboard { get; private set; }
        public AndroidHardwareKeyboardHidden hardKeyboardHidden { get; private set; }
        public AndroidKeyboardHidden keyboardHidden { get; private set; }
        public int mobileCountryCode { get; private set; }
        public int mobileNetworkCode { get; private set; }
        public AndroidNavigation navigation { get; private set; }
        public AndroidNavigationHidden navigationHidden { get; private set; }
        public AndroidOrientation orientation { get; private set; }
        public int screenHeightDp { get; private set; }
        public int screenWidthDp { get; private set; }
        public int smallestScreenWidthDp { get; private set; }
        private int screenLayout { get; set; }
        public AndroidTouchScreen touchScreen { get; private set; }
        private int uiMode { get; set; }
        private string primaryLocaleCountry { get; set; }
        private string primaryLocaleLanguage { get; set; }
        // Having this as an array, because it seems you can have multiple locales set, but for now we can only acquire primary locale
        // In case we'll have a way to acquire multiple locales in the future, have this as an array to prevent API changes
        public AndroidLocale[] locales
        {
            get
            {
                if (primaryLocaleCountry == null && primaryLocaleLanguage == null)
                    return new AndroidLocale[0];
                return new[] { new AndroidLocale(primaryLocaleCountry, primaryLocaleLanguage) };
            }
        }

        // Below properties are not marshalled
        public AndroidColorModeHdr colorModeHdr => (AndroidColorModeHdr)(colorMode & ColorModeHdrMask);
        public AndroidColorModeWideColorGamut colorModeWideColorGamut => (AndroidColorModeWideColorGamut)(colorMode & ColorModeWideColorGamutMask);
        public AndroidScreenLayoutDirection screenLayoutDirection => (AndroidScreenLayoutDirection)(screenLayout & ScreenLayoutDirectionMask);
        public AndroidScreenLayoutLong screenLayoutLong => (AndroidScreenLayoutLong)(screenLayout & ScreenLayoutLongMask);
        public AndroidScreenLayoutRound screenLayoutRound => (AndroidScreenLayoutRound)(screenLayout & ScreenLayoutRoundMask);
        public AndroidScreenLayoutSize screenLayoutSize => (AndroidScreenLayoutSize)(screenLayout & ScreenLayoutSizeMask);
        public AndroidUIModeNight uiModeNight => (AndroidUIModeNight)(uiMode & UiModeNightMask);
        public AndroidUIModeType uiModeType => (AndroidUIModeType)(uiMode & UiModeTypeMask);

        public AndroidConfiguration()
        {
        }

        public AndroidConfiguration(AndroidConfiguration otherConfiguration)
        {
            this.CopyFrom(otherConfiguration);
        }

        public void CopyFrom(AndroidConfiguration otherConfiguration)
        {
            colorMode = otherConfiguration.colorMode;
            densityDpi = otherConfiguration.densityDpi;
            fontScale = otherConfiguration.fontScale;
            fontWeightAdjustment = otherConfiguration.fontWeightAdjustment;
            keyboard = otherConfiguration.keyboard;
            hardKeyboardHidden = otherConfiguration.hardKeyboardHidden;
            keyboardHidden = otherConfiguration.keyboardHidden;
            mobileCountryCode = otherConfiguration.mobileCountryCode;
            mobileNetworkCode = otherConfiguration.mobileNetworkCode;
            navigation = otherConfiguration.navigation;
            navigationHidden = otherConfiguration.navigationHidden;
            orientation = otherConfiguration.orientation;
            screenHeightDp = otherConfiguration.screenHeightDp;
            screenWidthDp = otherConfiguration.screenWidthDp;
            smallestScreenWidthDp = otherConfiguration.smallestScreenWidthDp;
            screenLayout = otherConfiguration.screenLayout;
            touchScreen = otherConfiguration.touchScreen;
            uiMode = otherConfiguration.uiMode;
            primaryLocaleCountry = otherConfiguration.primaryLocaleCountry;
            primaryLocaleLanguage = otherConfiguration.primaryLocaleLanguage;
        }

        [Preserve]
        public override string ToString()
        {
            var contents = new StringBuilder();

            contents.AppendLine($"* ColorMode, Hdr: {colorModeHdr}");
            contents.AppendLine($"* ColorMode, Gamut: {colorModeWideColorGamut}");
            contents.AppendLine($"* DensityDpi: {densityDpi}");
            contents.AppendLine($"* FontScale: {fontScale}");
            contents.AppendLine($"* FontWeightAdj: {fontWeightAdjustment}");
            contents.AppendLine($"* Keyboard: {keyboard}");
            contents.AppendLine($"* Keyboard Hidden, Hard: {hardKeyboardHidden}");
            contents.AppendLine($"* Keyboard Hidden, Normal: {keyboardHidden}");
            contents.AppendLine($"* Mcc: {mobileCountryCode}");
            contents.AppendLine($"* Mnc: {mobileNetworkCode}");
            contents.AppendLine($"* Navigation: {navigation}");
            contents.AppendLine($"* NavigationHidden: {navigationHidden}");
            contents.AppendLine($"* Orientation: {orientation}");
            contents.AppendLine($"* ScreenHeightDp: {screenHeightDp}");
            contents.AppendLine($"* ScreenWidthDp: {screenWidthDp}");
            contents.AppendLine($"* SmallestScreenWidthDp: {smallestScreenWidthDp}");
            contents.AppendLine($"* ScreenLayout, Direction: {screenLayoutDirection}");
            contents.AppendLine($"* ScreenLayout, Size: {screenLayoutSize}");
            contents.AppendLine($"* ScreenLayout, Long: {screenLayoutLong}");
            contents.AppendLine($"* ScreenLayout, Round: {screenLayoutRound}");
            contents.AppendLine($"* TouchScreen: {touchScreen}");
            contents.AppendLine($"* UiMode, Night: {uiModeNight}");
            contents.AppendLine($"* UiMode, Type: {uiModeType}");

            contents.AppendLine($"* Locales ({locales.Length}):");
            for (int i = 0; i < locales.Length; i++)
            {
                var l = locales[i];
                contents.AppendLine($"* Locale[{i}] {l.country}-{l.language}");
            };

            return contents.ToString();
        }
    }
}
