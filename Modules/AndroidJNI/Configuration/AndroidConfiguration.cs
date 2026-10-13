// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Android
{
    ///<summary>Use this class to retrieve the language and region preferences set on the device.</summary>
    public class AndroidLocale
    {
        ///<summary>Indicates the geographical location as an ISO code.</summary>
        ///<remarks>For example, <c>US</c> (United States), <c>GB</c> (United Kingdom). For more information, refer to the the Android developer documentation on &lt;a href="https://developer.android.com/reference/java/util/Locale"&gt;Locale&lt;/a&gt;.</remarks>
        public string country { get; }
        ///<summary>Indicates the language as an ISO code.</summary>
        ///<remarks>For example, <c>en</c> (English), <c>de</c> (German). For more information, refer to the the Android developer documentation on &lt;a href="https://developer.android.com/reference/java/util/Locale"&gt;Locale&lt;/a&gt;.</remarks>
        public string language { get; }

        internal AndroidLocale(string _country, string _language)
        {
            country = _country;
            language = _language;
        }
    }

    ///<summary>Use this class to retrieve device specific configuration information.</summary>
    ///<seealso cref="AndroidApplication.currentConfiguration" />
    [NativeAsStruct]
    [NativeHeader("Modules/AndroidJNI/Public/AndroidConfiguration.bindings.h")]
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
        ///<summary>Mirrors the Android property <c>densityDpi</c>.</summary>
        ///<remarks>For information about this property, refer to the Android developer documentation on &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#densityDpi"&gt;densityDpi&lt;/a&gt;.</remarks>
        public int densityDpi { get; private set; }
        ///<summary>Mirrors the Android property <c>fontScale</c>.</summary>
        ///<remarks>For information about this property, refer to the Android developer documentation on &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#fontScale"&gt;fontScale&lt;/a&gt;.</remarks>
        public float fontScale { get; private set; }
        ///<summary>Mirrors the Android property <c>fontWeightAdjustment</c>.</summary>
        ///<remarks>For information about this property, refer to the Android developer documentation on &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#fontWeightAdjustment"&gt;fontWeightAdjustment&lt;/a&gt;.</remarks>
        public int fontWeightAdjustment { get; private set; }
        ///<summary>Mirrors the Android property <c>keyboard</c>.</summary>
        ///<remarks>For information about this property, refer to the Android developer documentation on &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#keyboard"&gt;keyboard&lt;/a&gt;.</remarks>
        ///<seealso cref="AndroidKeyboard" />
        public AndroidKeyboard keyboard { get; private set; }
        ///<summary>Mirrors the Android property <c>hardKeyboardHidden</c>.</summary>
        ///<remarks>For information about this property, refer to the Android developer documentation on &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#hardKeyboardHidden"&gt;hardKeyboardHidden&lt;/a&gt;.</remarks>
        ///<seealso cref="AndroidHardwareKeyboardHidden" />
        public AndroidHardwareKeyboardHidden hardKeyboardHidden { get; private set; }
        ///<summary>Mirrors the Android property <c>keyboardHidden</c>.</summary>
        ///<remarks>For information about this property, refer to the Android developer documentation on &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#keyboardHidden"&gt;keyboardHidden&lt;/a&gt;.</remarks>
        ///<seealso cref="AndroidKeyboardHidden" />
        public AndroidKeyboardHidden keyboardHidden { get; private set; }
        ///<summary>Mirrors the Android property <c>mcc</c>.</summary>
        ///<remarks>For information about this property, refer to the Android developer documentation on &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#mcc"&gt;mcc&lt;/a&gt;.</remarks>
        public int mobileCountryCode { get; private set; }
        ///<summary>Mirrors the Android property <c>mnc</c>.</summary>
        ///<remarks>For information about this property, refer to the Android developer documentation on &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#mnc"&gt;mnc&lt;/a&gt;.</remarks>
        public int mobileNetworkCode { get; private set; }
        ///<summary>Mirrors the Android property <c>navigation</c>.</summary>
        ///<remarks>For information about this property, refer to the Android developer documentation on &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#navigation"&gt;navigation&lt;/a&gt;.</remarks>
        ///<seealso cref="AndroidNavigation" />
        public AndroidNavigation navigation { get; private set; }
        ///<summary>Mirrors the Android property <c>navigationHidden</c>.</summary>
        ///<remarks>For information about this property, refer to the Android developer documentation on &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#navigationHidden"&gt;navigationHidden&lt;/a&gt;.</remarks>
        ///<seealso cref="AndroidNavigationHidden" />
        public AndroidNavigationHidden navigationHidden { get; private set; }
        ///<summary>Mirrors the Android property <c>orientation</c>.</summary>
        ///<remarks>For information about this property, refer to the Android developer documentation on &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#orientation"&gt;orientation&lt;/a&gt;.</remarks>
        ///<seealso cref="AndroidOrientation" />
        public AndroidOrientation orientation { get; private set; }
        ///<summary>Mirrors the Android property <c>screenHeightDp</c>.</summary>
        ///<remarks>For information about this property, refer to the Android developer documentation on &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#screenHeightDp"&gt;screenHeightDp&lt;/a&gt;.</remarks>
        public int screenHeightDp { get; private set; }
        ///<summary>Mirrors the Android property <c>screenWidthDp</c>.</summary>
        ///<remarks>For information about this property, refer to the Android developer documentation on &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#screenWidthDp"&gt;screenWidthDp&lt;/a&gt;.</remarks>
        public int screenWidthDp { get; private set; }
        ///<summary>Mirrors the Android property <c>smallestScreenWidthDp</c>.</summary>
        ///<remarks>For information about this property, refer to the Android developer documentation on &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#smallestScreenWidthDp"&gt;smallestScreenWidthDp&lt;/a&gt;.</remarks>
        public int smallestScreenWidthDp { get; private set; }
        private int screenLayout { get; set; }
        ///<summary>Mirrors the Android property <c>touchscreen</c>.</summary>
        ///<remarks>For information about this property, refer to the Android developer documentation on &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#touchscreen"&gt;touchscreen&lt;/a&gt;.</remarks>
        ///<seealso cref="AndroidTouchScreen" />
        public AndroidTouchScreen touchScreen { get; private set; }
        private int uiMode { get; set; }
        private string primaryLocaleCountry { get; set; }
        private string primaryLocaleLanguage { get; set; }
        // Having this as an array, because it seems you can have multiple locales set, but for now we can only acquire primary locale
        // In case we'll have a way to acquire multiple locales in the future, have this as an array to prevent API changes
        ///<summary>Indicates the language and region preferences set on the device in an array.</summary>
        ///<remarks>For example, <c>en-US</c>, <c>fr-FR</c>. The locale information can be accessed using the Android method &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#getLocales()"&gt;getLocales()&lt;/a&gt;.</remarks>
        ///<seealso cref="Android.AndroidLocale" />
        public AndroidLocale[] locales
        {
            get
            {
                if (primaryLocaleCountry == null && primaryLocaleLanguage == null)
                    return Array.Empty<AndroidLocale>();
                return new[] { new AndroidLocale(primaryLocaleCountry, primaryLocaleLanguage) };
            }
        }

        // Below properties are not marshalled
        ///<summary>Mirrors the Android property <c>colorMode</c> based on the <c>COLOR_MODE_HDR_MASK</c> value.</summary>
        ///<remarks>For information about this property, refer to the Android developer documentation on &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#colorMode"&gt;colorMode&lt;/a&gt; and &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#COLOR_MODE_HDR_MASK"&gt;COLOR_MODE_HDR_MASK&lt;/a&gt;.</remarks>
        ///<seealso cref="AndroidColorModeHdr" />
        public AndroidColorModeHdr colorModeHdr => (AndroidColorModeHdr)(colorMode & ColorModeHdrMask);
        ///<summary>Mirrors the Android property <c>colorMode</c> based on the <c>COLOR_MODE_WIDE_COLOR_GAMUT_MASK</c> value.</summary>
        ///<remarks>For information about this property, refer to the Android developer documentation on &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#colorMode"&gt;colorMode&lt;/a&gt; and &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#COLOR_MODE_WIDE_COLOR_GAMUT_MASK"&gt;COLOR_MODE_WIDE_COLOR_GAMUT_MASK&lt;/a&gt;.</remarks>
        ///<seealso cref="AndroidColorModeWideColorGamut" />
        public AndroidColorModeWideColorGamut colorModeWideColorGamut => (AndroidColorModeWideColorGamut)(colorMode & ColorModeWideColorGamutMask);
        ///<summary>Mirrors the Android property <c>screenLayout</c> based on the <c>SCREENLAYOUT_LAYOUTDIR_MASK</c> value.</summary>
        ///<remarks>For information about this property, refer to the Android developer documentation on &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#screenLayout"&gt;screenLayout&lt;/a&gt; and &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#SCREENLAYOUT_LAYOUTDIR_MASK"&gt;SCREENLAYOUT_LAYOUTDIR_MASK&lt;/a&gt;.</remarks>
        ///<seealso cref="AndroidScreenLayoutDirection" />
        public AndroidScreenLayoutDirection screenLayoutDirection => (AndroidScreenLayoutDirection)(screenLayout & ScreenLayoutDirectionMask);
        ///<summary>Mirrors the Android property <c>screenLayout</c> based on the <c>SCREENLAYOUT_LONG_MASK</c> value.</summary>
        ///<remarks>For information about this property, refer to the Android developer documentation on &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#screenLayout"&gt;screenLayout&lt;/a&gt; and &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#SCREENLAYOUT_LONG_MASK"&gt;SCREENLAYOUT_LONG_MASK&lt;/a&gt;.</remarks>
        ///<seealso cref="AndroidScreenLayoutLong" />
        public AndroidScreenLayoutLong screenLayoutLong => (AndroidScreenLayoutLong)(screenLayout & ScreenLayoutLongMask);
        ///<summary>Mirrors the Android property <c>screenLayout</c> based on the <c>SCREENLAYOUT_ROUND_MASK</c> value.</summary>
        ///<remarks>For information about this property, refer to the Android developer documentation on &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#screenLayout"&gt;screenLayout&lt;/a&gt; and &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#SCREENLAYOUT_ROUND_MASK"&gt;SCREENLAYOUT_ROUND_MASK&lt;/a&gt;.</remarks>
        ///<seealso cref="AndroidScreenLayoutRound" />
        public AndroidScreenLayoutRound screenLayoutRound => (AndroidScreenLayoutRound)(screenLayout & ScreenLayoutRoundMask);
        ///<summary>Mirrors the Android property <c>screenLayout</c> based on the <c>SCREENLAYOUT_SIZE_MASK</c> value.</summary>
        ///<remarks>For information about this property, refer to the Android developer documentation on &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#screenLayout"&gt;screenLayout&lt;/a&gt; and &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#SCREENLAYOUT_SIZE_MASK"&gt;SCREENLAYOUT_SIZE_MASK&lt;/a&gt;.</remarks>
        ///<seealso cref="AndroidScreenLayoutSize" />
        public AndroidScreenLayoutSize screenLayoutSize => (AndroidScreenLayoutSize)(screenLayout & ScreenLayoutSizeMask);
        ///<summary>Mirrors the Android property <c>uiMode</c> based on the <c>UI_MODE_NIGHT_MASK</c> value.</summary>
        ///<remarks>For information about this property, refer to the Android developer documentation on &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#uiMode"&gt;uiMode&lt;/a&gt; and &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#UI_MODE_NIGHT_MASK"&gt;UI_MODE_NIGHT_MASK&lt;/a&gt;.</remarks>
        ///<seealso cref="AndroidUIModeNight" />
        public AndroidUIModeNight uiModeNight => (AndroidUIModeNight)(uiMode & UiModeNightMask);
        ///<summary>Mirrors the Android property <c>uiMode</c> based on the <c>UI_MODE_TYPE_MASK</c> value.</summary>
        ///<remarks>For information about this property, refer to the Android developer documentation on &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#uiMode"&gt;uiMode&lt;/a&gt; and &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#UI_MODE_TYPE_MASK"&gt;UI_MODE_TYPE_MASK&lt;/a&gt;.</remarks>
        ///<seealso cref="AndroidUIModeType" />
        public AndroidUIModeType uiModeType => (AndroidUIModeType)(uiMode & UiModeTypeMask);

        ///<summary>Mirrors the Android method <c>Configuration()</c>.</summary>
        ///<remarks>For information about this method, refer to the Android developer documentation on &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#Configuration()"&gt;Configuration()&lt;/a&gt;.</remarks>
        public AndroidConfiguration()
        {
        }

        ///<summary>Creates a new instance of the configuration, copied from another instance.</summary>
        ///<param name="otherConfiguration">The configuration to copy.</param>
        public AndroidConfiguration(AndroidConfiguration otherConfiguration)
        {
            this.CopyFrom(otherConfiguration);
        }

        ///<summary>Copies the specified configuration into the current instance.</summary>
        ///<param name="otherConfiguration">The configuration to copy the values from.</param>
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

        ///<summary>Mirrors the Android method <c>toString()</c>.</summary>
        ///<returns>A string that represents the current instance.</returns>
        ///<remarks>For information about this method, refer to the Android developer documentation on &lt;a href="https://developer.android.com/reference/android/content/res/Configuration#toString()"&gt;toString()&lt;/a&gt;.</remarks>
        [RequiredMember]
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
