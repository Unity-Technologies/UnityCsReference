// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using UnityEngine;

namespace UnityEngine.WSA
{


public enum TileTemplate
{
    TileSquare150x150Image = 0,
    TileSquare150x150Block = 1,
    TileSquare150x150Text01 = 2,
    TileSquare150x150Text02 = 3,
    TileSquare150x150Text03 = 4,
    TileSquare150x150Text04 = 5,
    TileSquare150x150PeekImageAndText01 = 6,
    TileSquare150x150PeekImageAndText02 = 7,
    TileSquare150x150PeekImageAndText03 = 8,
    TileSquare150x150PeekImageAndText04 = 9,
    TileWide310x150Image = 10,
    TileWide310x150ImageCollection = 11,
    TileWide310x150ImageAndText01 = 12,
    TileWide310x150ImageAndText02 = 13,
    TileWide310x150BlockAndText01 = 14,
    TileWide310x150BlockAndText02 = 15,
    TileWide310x150PeekImageCollection01 = 16,
    TileWide310x150PeekImageCollection02 = 17,
    TileWide310x150PeekImageCollection03 = 18,
    TileWide310x150PeekImageCollection04 = 19,
    TileWide310x150PeekImageCollection05 = 20,
    TileWide310x150PeekImageCollection06 = 21,
    TileWide310x150PeekImageAndText01 = 22,
    TileWide310x150PeekImageAndText02 = 23,
    TileWide310x150PeekImage01 = 24,
    TileWide310x150PeekImage02 = 25,
    TileWide310x150PeekImage03 = 26,
    TileWide310x150PeekImage04 = 27,
    TileWide310x150PeekImage05 = 28,
    TileWide310x150PeekImage06 = 29,
    TileWide310x150SmallImageAndText01 = 30,
    TileWide310x150SmallImageAndText02 = 31,
    TileWide310x150SmallImageAndText03 = 32,
    TileWide310x150SmallImageAndText04 = 33,
    TileWide310x150SmallImageAndText05 = 34,
    TileWide310x150Text01 = 35,
    TileWide310x150Text02 = 36,
    TileWide310x150Text03 = 37,
    TileWide310x150Text04 = 38,
    TileWide310x150Text05 = 39,
    TileWide310x150Text06 = 40,
    TileWide310x150Text07 = 41,
    TileWide310x150Text08 = 42,
    TileWide310x150Text09 = 43,
    TileWide310x150Text10 = 44,
    TileWide310x150Text11 = 45,
    TileSquare310x310BlockAndText01 = 46,
    TileSquare310x310BlockAndText02 = 47,
    TileSquare310x310Image = 48,
    TileSquare310x310ImageAndText01 = 49,
    TileSquare310x310ImageAndText02 = 50,
    TileSquare310x310ImageAndTextOverlay01 = 51,
    TileSquare310x310ImageAndTextOverlay02 = 52,
    TileSquare310x310ImageAndTextOverlay03 = 53,
    TileSquare310x310ImageCollectionAndText01 = 54,
    TileSquare310x310ImageCollectionAndText02 = 55,
    TileSquare310x310ImageCollection = 56,
    TileSquare310x310SmallImagesAndTextList01 = 57,
    TileSquare310x310SmallImagesAndTextList02 = 58,
    TileSquare310x310SmallImagesAndTextList03 = 59,
    TileSquare310x310SmallImagesAndTextList04 = 60,
    TileSquare310x310Text01 = 61,
    TileSquare310x310Text02 = 62,
    TileSquare310x310Text03 = 63,
    TileSquare310x310Text04 = 64,
    TileSquare310x310Text05 = 65,
    TileSquare310x310Text06 = 66,
    TileSquare310x310Text07 = 67,
    TileSquare310x310Text08 = 68,
    TileSquare310x310TextList01 = 69,
    TileSquare310x310TextList02 = 70,
    TileSquare310x310TextList03 = 71,
    TileSquare310x310SmallImageAndText01 = 72,
    TileSquare310x310SmallImagesAndTextList05 = 73,
    TileSquare310x310Text09 = 74,
    TileSquare71x71IconWithBadge = 75,
    TileSquare150x150IconWithBadge = 76,
    TileWide310x150IconWithBadgeAndText = 77,
    TileSquare71x71Image = 78,
    TileTall150x310Image = 79,
    
    TileSquare99x99IconWithBadge = 1000,
    TileSquare210x210IconWithBadge = 1001,
    TileWide432x210IconWithBadgeAndText = 1002,
}

public enum ToastTemplate
{
    ToastImageAndText01 = 0,
    ToastImageAndText02 = 1,
    ToastImageAndText03 = 2,
    ToastImageAndText04 = 3,
    ToastText01 = 4,
    ToastText02 = 5,
    ToastText03 = 6,
    ToastText04 = 7,
}

public enum TileForegroundText
{
    Default = -1,
    Dark = 0,
    Light = 1
}


    public struct SecondaryTileData
    {
        public string arguments;
        private Color32 background;
        public Color32 backgroundColor
        {
            get
            {
                return background;
            }
            set
            {
                background = value;
                backgroundColorSet = true;
            }
        }
        public bool backgroundColorSet;
        public string displayName;
        public TileForegroundText foregroundText;
        public string lockScreenBadgeLogo;
        public bool lockScreenDisplayBadgeAndTileText;
        public string phoneticName;
        public bool roamingEnabled;
        public bool showNameOnSquare150x150Logo;
        public bool showNameOnSquare310x310Logo;
        public bool showNameOnWide310x150Logo;
        public string square150x150Logo;
        public string square30x30Logo;
        public string square310x310Logo;
        public string square70x70Logo;
        public string tileId;
        public string wide310x150Logo;

        public SecondaryTileData(string id, string displayName)
        {
            arguments = "";
            background = new UnityEngine.Color32(0, 0, 0, 0);
            backgroundColorSet = false;
            this.displayName = displayName;
            foregroundText = TileForegroundText.Default;
            lockScreenBadgeLogo = "";
            lockScreenDisplayBadgeAndTileText = false;
            phoneticName = "";
            roamingEnabled = true; 
            showNameOnSquare150x150Logo = true;
            showNameOnSquare310x310Logo = false;
            showNameOnWide310x150Logo = false;
            square150x150Logo = "";
            square30x30Logo = "";
            square310x310Logo = "";
            square70x70Logo = "";
            tileId = id;
            wide310x150Logo = "";
        }

    }


public sealed partial class Tile
{
    private string m_TileId;
    
    
    private static Tile s_MainTile;
    
    
    private Tile(string tileId)
        {
            m_TileId = tileId;
        }
    
    
    public static Tile main
        {
            get
            {
                if (s_MainTile == null)
                    s_MainTile = new Tile("");
                return s_MainTile;
            }
        }
    
    
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetTemplate (TileTemplate templ) ;

    public void Update(string xml)
        {
            Update(m_TileId, xml);
        }
    
    
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Update (string tileId, string xml) ;

    public void Update(string medium, string wide, string large, string text)
        {
            UpdateImageAndText(m_TileId, medium, wide, large, text);
        }
    
    
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void UpdateImageAndText (string tileId, string medium, string wide, string large, string text) ;

    public void PeriodicUpdate(string uri, float interval)
        {
            PeriodicUpdate(m_TileId, uri, interval);
        }
    
    
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void PeriodicUpdate (string tileId, string uri, float interval) ;

    public void StopPeriodicUpdate()
        {
            StopPeriodicUpdate(m_TileId);
        }
    
    
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void StopPeriodicUpdate (string tileId) ;

    public void UpdateBadgeImage(string image)
        {
            UpdateBadgeImage(m_TileId, image);
        }
    
    
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void UpdateBadgeImage (string tileId, string image) ;

    public void UpdateBadgeNumber(float number)
        {
            UpdateBadgeNumber(m_TileId, number);
        }
    
    
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void UpdateBadgeNumber (string tileId, float number) ;

    public void RemoveBadge()
        {
            RemoveBadge(m_TileId);
        }
    
    
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void RemoveBadge (string tileId) ;

    public void PeriodicBadgeUpdate(string uri, float interval)
        {
            PeriodicBadgeUpdate(m_TileId, uri, interval);
        }
    
    
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void PeriodicBadgeUpdate (string tileId, string uri, float interval) ;

    public void StopPeriodicBadgeUpdate()
        {
            StopPeriodicBadgeUpdate(m_TileId);
        }
    
    
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void StopPeriodicBadgeUpdate (string tileId) ;

    public string id
        {
            get
            {
                return m_TileId;
            }
        }
    
    
    
    public bool hasUserConsent
        {
            get
            {
                return HasUserConsent(m_TileId);
            }
        }
    
    
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool HasUserConsent (string tileId) ;

    public bool exists
        {
            get
            {
                return Exists(m_TileId);
            }
        }
    
    
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool Exists (string tileId) ;

    private static string[] MakeSecondaryTileSargs(SecondaryTileData data)
        {
            string[] sargs = new string[10];
            sargs[0] = data.arguments;
            sargs[1] = data.displayName;
            sargs[2] = data.lockScreenBadgeLogo;
            sargs[3] = data.phoneticName;
            sargs[4] = data.square150x150Logo;
            sargs[5] = data.square30x30Logo;
            sargs[6] = data.square310x310Logo;
            sargs[7] = data.square70x70Logo;
            sargs[8] = data.tileId;
            sargs[9] = data.wide310x150Logo;
            return sargs;
        }
    
    
    private static bool[] MakeSecondaryTileBargs(SecondaryTileData data)
        {
            bool[] bargs = new bool[6];
            bargs[0] = data.backgroundColorSet;
            bargs[1] = data.lockScreenDisplayBadgeAndTileText;
            bargs[2] = data.roamingEnabled;
            bargs[3] = data.showNameOnSquare150x150Logo;
            bargs[4] = data.showNameOnSquare310x310Logo;
            bargs[5] = data.showNameOnWide310x150Logo;
            return bargs;
        }
    
    
    public static Tile CreateOrUpdateSecondary(SecondaryTileData data)
        {
            string[] sargs = MakeSecondaryTileSargs(data);
            bool[] bargs = MakeSecondaryTileBargs(data);
            Color32 backgroundColor = data.backgroundColor;

            string tileId = CreateOrUpdateSecondaryTile(
                    sargs,
                    bargs,
                    ref backgroundColor,
                    (int)data.foregroundText
                    );
            if (string.IsNullOrEmpty(tileId))
                return null;
            return new Tile(tileId);
        }
    
    
    [ThreadAndSerializationSafe ()]
    private static string CreateOrUpdateSecondaryTile (
            string[] sargs,
            bool[] bargs,
            ref Color32 backgroundColor,
            int foregroundText
            ) {
        return INTERNAL_CALL_CreateOrUpdateSecondaryTile ( sargs, bargs, ref backgroundColor, foregroundText );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static string INTERNAL_CALL_CreateOrUpdateSecondaryTile (string[] sargs, bool[] bargs, ref Color32 backgroundColor, int foregroundText);
    public static Tile CreateOrUpdateSecondary(SecondaryTileData data, Vector2 pos)
        {
            string[] sargs = MakeSecondaryTileSargs(data);
            bool[] bargs = MakeSecondaryTileBargs(data);
            Color32 backgroundColor = data.backgroundColor;

            string tileId = CreateOrUpdateSecondaryTilePoint(
                    sargs,
                    bargs,
                    ref backgroundColor,
                    (int)data.foregroundText,
                    pos
                    );
            if (string.IsNullOrEmpty(tileId))
                return null;
            return new Tile(tileId);
        }
    
    
    [ThreadAndSerializationSafe ()]
    private static string CreateOrUpdateSecondaryTilePoint (
            string[] sargs,
            bool[] bargs,
            ref Color32 backgroundColor,
            int foregroundText,
            Vector2 pos
            ) {
        return INTERNAL_CALL_CreateOrUpdateSecondaryTilePoint ( sargs, bargs, ref backgroundColor, foregroundText, ref pos );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static string INTERNAL_CALL_CreateOrUpdateSecondaryTilePoint (string[] sargs, bool[] bargs, ref Color32 backgroundColor, int foregroundText, ref Vector2 pos);
    public static Tile CreateOrUpdateSecondary(SecondaryTileData data, Rect area)
        {
            string[] sargs = MakeSecondaryTileSargs(data);
            bool[] bargs = MakeSecondaryTileBargs(data);
            Color32 backgroundColor = data.backgroundColor;

            string tileId = CreateOrUpdateSecondaryTileArea(
                    sargs,
                    bargs,
                    ref backgroundColor,
                    (int)data.foregroundText,
                    area
                    );
            if (string.IsNullOrEmpty(tileId))
                return null;
            return new Tile(tileId);
        }
    
    
    [ThreadAndSerializationSafe ()]
    private static string CreateOrUpdateSecondaryTileArea (
            string[] sargs,
            bool[] bargs,
            ref Color32 backgroundColor,
            int foregroundText,
            Rect area
            ) {
        return INTERNAL_CALL_CreateOrUpdateSecondaryTileArea ( sargs, bargs, ref backgroundColor, foregroundText, ref area );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static string INTERNAL_CALL_CreateOrUpdateSecondaryTileArea (string[] sargs, bool[] bargs, ref Color32 backgroundColor, int foregroundText, ref Rect area);
    public static Tile GetSecondary(string tileId)
        {
            if (Exists(tileId))
                return new Tile(tileId);
            return null;
        }
    
    
    public static Tile[] GetSecondaries()
        {
            string[] ids = GetAllSecondaryTiles();
            Tile[] tiles = new Tile[ids.Length];
            for (int i = 0; i < ids.Length; ++i)
                tiles[i] = new Tile(ids[i]);
            return tiles;
        }
    
    
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  string[] GetAllSecondaryTiles () ;

    public void Delete()
        {
            DeleteSecondary(m_TileId);
        }
    
    
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void DeleteSecondary (string tileId) ;

    public void Delete(Vector2 pos)
        {
            DeleteSecondaryPos(m_TileId, pos);
        }
    
    
    public static void DeleteSecondary(string tileId, Vector2 pos)
        {
            DeleteSecondaryPos(tileId, pos);
        }
    
    
    [ThreadAndSerializationSafe ()]
    private static void DeleteSecondaryPos (string tileId, Vector2 pos) {
        INTERNAL_CALL_DeleteSecondaryPos ( tileId, ref pos );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_DeleteSecondaryPos (string tileId, ref Vector2 pos);
    public void Delete(Rect area)
        {
            DeleteSecondaryArea(m_TileId, area);
        }
    
    
    public static void DeleteSecondary(string tileId, Rect area)
        {
            DeleteSecondary(tileId, area);
        }
    
    
    [ThreadAndSerializationSafe ()]
    private static void DeleteSecondaryArea (string tileId, Rect area) {
        INTERNAL_CALL_DeleteSecondaryArea ( tileId, ref area );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_DeleteSecondaryArea (string tileId, ref Rect area);
}

public sealed partial class Toast
{
    private int m_ToastId;
    
    
    private Toast(int id)
        {
            m_ToastId = id;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetTemplate (ToastTemplate templ) ;

    public static Toast Create(string xml)
        {
            int id = CreateToastXml(xml);
            if (id < 0)
                return null;
            return new Toast(id);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int CreateToastXml (string xml) ;

    public static Toast Create(string image, string text)
        {
            int id = CreateToastImageAndText(image, text);
            if (id < 0)
                return null;
            return new Toast(id);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int CreateToastImageAndText (string image, string text) ;

    public string arguments
        {
            get
            {
                return GetArguments(m_ToastId);
            }
            set
            {
                SetArguments(m_ToastId, value);
            }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  string GetArguments (int id) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void SetArguments (int id, string args) ;

    public void Show()
        {
            Show(m_ToastId);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Show (int id) ;

    public void Hide()
        {
            Hide(m_ToastId);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Hide (int id) ;

    public bool activated
        {
            get
            {
                return GetActivated(m_ToastId);
            }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool GetActivated (int id) ;

    public bool dismissed
        {
            get
            {
                return GetDismissed(m_ToastId, false);
            }
        }
    
    
    public bool dismissedByUser
        {
            get
            {
                return GetDismissed(m_ToastId, true);
            }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool GetDismissed (int id, bool byUser) ;

}


}
