// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text.RegularExpressions;

using UnityEngine;

namespace UnityEditor
{
    public sealed partial class PlayerSettings : UnityEngine.Object
    {
        public sealed partial class WSA
        {
            internal static string ValidatePackageVersion(string value)
            {
                Regex metroPackageVersionRegex = new Regex(@"^(\d+)\.(\d+)\.(\d+)\.(\d+)$", (RegexOptions.Compiled | RegexOptions.CultureInvariant));

                if (metroPackageVersionRegex.IsMatch(value))
                {
                    return value;
                }
                else
                {
                    return "1.0.0.0";
                }
            }

            private static void ValidateWSAImageType(WSAImageType type)
            {
                switch (type)
                {
                    case WSAImageType.PackageLogo:
                    case WSAImageType.SplashScreenImage:
                    case WSAImageType.UWPSquare44x44Logo:
                    case WSAImageType.UWPSquare71x71Logo:
                    case WSAImageType.UWPSquare150x150Logo:
                    case WSAImageType.UWPSquare310x310Logo:
                    case WSAImageType.UWPWide310x150Logo:
                        return;
                    default:
                        throw new Exception("Unknown WSA image type: " + type);
                }
            }

            private static void ValidateWSAImageScale(WSAImageScale scale)
            {
                switch (scale)
                {
                    case WSAImageScale._80:
                    case WSAImageScale._100:
                    case WSAImageScale._125:
                    case WSAImageScale._140:
                    case WSAImageScale._150:
                    case WSAImageScale._180:
                    case WSAImageScale._200:
                    case WSAImageScale._240:
                    case WSAImageScale._400:
                    case WSAImageScale.Target16:
                    case WSAImageScale.Target24:
                    case WSAImageScale.Target32:
                    case WSAImageScale.Target48:
                    case WSAImageScale.Target256:
                        return;
                    default:
                        throw new Exception("Unknown image scale: " + scale);
                }
            }

            public static string GetVisualAssetsImage(WSAImageType type, WSAImageScale scale)
            {
                ValidateWSAImageType(type);
                ValidateWSAImageScale(scale);
                return GetWSAImage(type, scale);
            }

            public static void SetVisualAssetsImage(string image, WSAImageType type, WSAImageScale scale)
            {
                ValidateWSAImageType(type);
                ValidateWSAImageScale(scale);
                SetWSAImage(image, type, scale);
            }

            public static System.Version packageVersion
            {
                get
                {
                    try
                    {
                        return new System.Version(ValidatePackageVersion(packageVersionRaw));
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(string.Format("{0}, the raw string was {1}", ex.Message, packageVersionRaw));
                    }
                }
                set { packageVersionRaw = value.ToString(); }
            }


            public static System.DateTime? certificateNotAfter
            {
                get
                {
                    long value = certificateNotAfterRaw;
                    if (value != 0)
                        return System.DateTime.FromFileTime(value);
                    else
                        return null;
                }
            }

            public static Color? splashScreenBackgroundColor
            {
                get
                {
                    if (splashScreenUseBackgroundColor)
                        return splashScreenBackgroundColorRaw;
                    else
                        return null;
                }
                set
                {
                    splashScreenUseBackgroundColor = value.HasValue;
                    if (value.HasValue)
                        splashScreenBackgroundColorRaw = value.Value;
                }
            }

            public static void SetCapability(WSACapability capability, bool value)
            {
                InternalSetCapability(capability.ToString(), value.ToString());
            }

            public static bool GetCapability(WSACapability capability)
            {
                string stringValue = InternalGetCapability(capability.ToString());

                if (string.IsNullOrEmpty(stringValue)) return false;

                try
                {
                    return (bool)System.ComponentModel.TypeDescriptor.GetConverter(typeof(bool)).ConvertFromString(stringValue);
                }
                catch
                {
                    Debug.LogError("Failed to parse value  ('" + capability.ToString() + "," + stringValue + "') to bool type.");
                    return false;
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string storeTileLogo80
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.storeTileLogo80 is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.storeTileLogo80 is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string storeTileLogo
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.storeTileLogo is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.storeTileLogo is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string storeTileLogo140
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.storeTileLogo140 is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.storeTileLogo140 is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string storeTileLogo180
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.storeTileLogo180 is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.storeTileLogo180 is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string storeTileWideLogo80
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.storeTileWideLogo80 is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.storeTileWideLogo80 is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string storeTileWideLogo
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.storeTileWideLogo is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.storeTileWideLogo is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string storeTileWideLogo140
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.storeTileWideLogo140 is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.storeTileWideLogo140 is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string storeTileWideLogo180
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.storeTileWideLogo180 is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.storeTileWideLogo180 is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string storeTileSmallLogo80
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.storeTileSmallLogo80 is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.storeTileSmallLogo80 is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string storeTileSmallLogo
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.storeTileSmallLogo is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.storeTileSmallLogo is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string storeTileSmallLogo140
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.storeTileSmallLogo140 is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.storeTileSmallLogo140 is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string storeTileSmallLogo180
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.storeTileSmallLogo180 is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.storeTileSmallLogo180 is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string storeSmallTile80
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.storeSmallTile80 is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.storeSmallTile80 is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string storeSmallTile
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.storeSmallTile is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.storeSmallTile is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string storeSmallTile140
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.storeSmallTile140 is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.storeSmallTile140 is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string storeSmallTile180
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.storeSmallTile180 is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.storeSmallTile180 is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string storeLargeTile80
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.storeLargeTile80 is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.storeLargeTile80 is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string storeLargeTile
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.storeLargeTile is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.storeLargeTile is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string storeLargeTile140
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.storeLargeTile140 is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.storeLargeTile140 is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string storeLargeTile180
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.storeLargeTile180 is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.storeLargeTile180 is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string storeSplashScreenImage
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.storeSplashScreenImage is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.storeSplashScreenImage is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string storeSplashScreenImageScale140
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.storeSplashScreenImageScale140 is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.storeSplashScreenImageScale140 is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string storeSplashScreenImageScale180
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.storeSplashScreenImageScale180 is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.storeSplashScreenImageScale180 is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string phoneAppIcon
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.phoneAppIcon is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.phoneAppIcon is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string phoneAppIcon140
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.phoneAppIcon140 is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.phoneAppIcon140 is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string phoneAppIcon240
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.phoneAppIcon240 is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.phoneAppIcon240 is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string phoneSmallTile
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.phoneSmallTile is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.phoneSmallTile is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string phoneSmallTile140
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.phoneSmallTile140 is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.phoneSmallTile140 is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string phoneSmallTile240
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.phoneSmallTile240 is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.phoneSmallTile240 is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string phoneMediumTile
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.phoneMediumTile is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.phoneMediumTile is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string phoneMediumTile140
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.phoneMediumTile140 is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.phoneMediumTile140 is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string phoneMediumTile240
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.phoneMediumTile240 is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.phoneMediumTile240 is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string phoneWideTile
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.phoneWideTile is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.phoneWideTile is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string phoneWideTile140
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.phoneWideTile140 is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.phoneWideTile140 is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string phoneWideTile240
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.phoneWideTile240 is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.phoneWideTile240 is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string phoneSplashScreenImage
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.phoneSplashScreenImage is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.phoneSplashScreenImage is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string phoneSplashScreenImageScale140
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.phoneSplashScreenImageScale140 is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.phoneSplashScreenImageScale140 is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string phoneSplashScreenImageScale240
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.phoneSplashScreenImageScale240 is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.phoneSplashScreenImageScale240 is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string packageLogo140
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.packageLogo140 is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.packageLogo140 is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string packageLogo180
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.packageLogo180 is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.packageLogo180 is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("Use GetVisualAssetsImage()/SetVisualAssetsImage()", true)]
            public static string packageLogo240
            {
                get
                {
                    throw new NotSupportedException("PlayerSettings.packageLogo240 is deprecated. Use GetVisualAssetsImage() instead.");
                }
                set
                {
                    throw new NotSupportedException("PlayerSettings.packageLogo240 is deprecated. Use SetVisualAssetsImage() instead.");
                }
            }

            [Obsolete("PlayerSettings.enableLowLatencyPresentationAPI is deprecated. It is now always enabled.", true)]
            public static bool enableLowLatencyPresentationAPI
            {
                get
                {
                    return true;
                }
                set
                {
                }
            }
        }
    }
}
