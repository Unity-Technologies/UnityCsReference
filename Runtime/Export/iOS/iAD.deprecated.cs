// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.iOS
{
    [Obsolete("iOS.ADBannerView class is obsolete, Apple iAD service discontinued", true)]
    public sealed partial class ADBannerView
    {
        public enum
        Layout
        {
            // banner
            Top         = 0,
            Bottom      = 1,

            // rect
            TopLeft     = 0,
            TopRight    = 4,
            TopCenter   = 8,
            BottomLeft  = 1,
            BottomRight = 5,
            BottomCenter = 9,
            CenterLeft  = 2,
            CenterRight = 6,
            Center      = 10,

            Manual = -1
        };

        public enum
        Type
        {
            Banner = 0,
            MediumRect = 1
        };

        public static bool IsAvailable(Type type) { return false; }

        public ADBannerView(Type type, Layout layout) {}

        public bool loaded { get { return false; } }
        public bool visible  { get { return false; } set {} }

        public Layout layout  { get { return default(Layout); } set {} }
        public Vector2 position { get { return default(Vector2); } set {} }
        public Vector2 size  { get { return default(Vector2); } }

        public delegate void BannerWasClickedDelegate();
        public static event BannerWasClickedDelegate onBannerWasClicked
        {
            add {}
            remove {}
        }

        public delegate void BannerWasLoadedDelegate();
        public static event BannerWasLoadedDelegate onBannerWasLoaded
        {
            add {}
            remove {}
        }

        public delegate void BannerFailedToLoadDelegate();
        public static event BannerFailedToLoadDelegate onBannerFailedToLoad
        {
            add {}
            remove {}
        }
    }

    [Obsolete("iOS.ADInterstitialAd class is obsolete, Apple iAD service discontinued", true)]
    public sealed partial class ADInterstitialAd
    {
        public static bool isAvailable { get { return false; } }

        public ADInterstitialAd(bool autoReload) {}
        public ADInterstitialAd() {}

        public void Show() {}
        public void ReloadAd() {}

        public bool loaded  { get { return false; } }

        public delegate void InterstitialWasLoadedDelegate();
        public static event InterstitialWasLoadedDelegate onInterstitialWasLoaded
        {
            add {}
            remove {}
        }

        public delegate void InterstitialWasViewedDelegate();
        public static event InterstitialWasViewedDelegate onInterstitialWasViewed
        {
            add {}
            remove {}
        }
    }
}

namespace UnityEngine
{
    [Obsolete("ADBannerView class is obsolete, Apple iAD service discontinued", true)]
    public sealed class ADBannerView
    {
        public enum Layout
        {
            Top,
            Bottom,
            TopLeft = 0,
            TopRight = 4,
            TopCenter = 8,
            BottomLeft = 1,
            BottomRight = 5,
            BottomCenter = 9,
            CenterLeft = 2,
            CenterRight = 6,
            Center = 10,
            Manual = -1
        }

        public enum Type
        {
            Banner,
            MediumRect
        }

        public delegate void BannerWasClickedDelegate();
        public delegate void BannerWasLoadedDelegate();

        public static event ADBannerView.BannerWasClickedDelegate onBannerWasClicked
        {
            add {}
            remove {}
        }

        public static event ADBannerView.BannerWasLoadedDelegate onBannerWasLoaded
        {
            add {}
            remove {}
        }

        public bool loaded { get { return false; } }
        public bool visible  { get { return false; } set {} }
        public ADBannerView.Layout layout  { get { return default(ADBannerView.Layout); } set {} }
        public Vector2 position { get { return default(Vector2); } set {} }
        public Vector2 size  { get { return default(Vector2); } }
        public ADBannerView(ADBannerView.Type type, ADBannerView.Layout layout) {}
        public static bool IsAvailable(ADBannerView.Type type) { return false; }
    }

    [Obsolete("ADInterstitialAd class is obsolete, Apple iAD service discontinued", true)]
    public sealed class ADInterstitialAd
    {
        public delegate void InterstitialWasLoadedDelegate();

        public static event ADInterstitialAd.InterstitialWasLoadedDelegate onInterstitialWasLoaded
        {
            add {}
            remove {}
        }

        public static bool isAvailable { get { return false; } }

        public bool loaded  { get { return false; } }

        public ADInterstitialAd(bool autoReload) {}
        public ADInterstitialAd() {}

        ~ADInterstitialAd() {}

        public void Show() {}

        public void ReloadAd() {}
    }
}

