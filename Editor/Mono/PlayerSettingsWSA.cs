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
                    case WSAImageType.StoreTileLogo:
                    case WSAImageType.StoreTileWideLogo:
                    case WSAImageType.StoreTileSmallLogo:
                    case WSAImageType.StoreSmallTile:
                    case WSAImageType.StoreLargeTile:
                    case WSAImageType.PhoneAppIcon:
                    case WSAImageType.PhoneSmallTile:
                    case WSAImageType.PhoneMediumTile:
                    case WSAImageType.PhoneWideTile:
                    case WSAImageType.PhoneSplashScreen:
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
        }
    }
}
