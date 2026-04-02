// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.UIElements.Cursor;

namespace Unity.Timeline.Foundation.View
{
    static class EditModeCursorUtils
    {
        public enum CursorType
        {
            None,
            MixBoth,
            MixLeft,
            MixRight,
            Replace,
            Ripple
        }

        readonly struct CursorInfo
        {
            public readonly string assetPath;
            public readonly Vector2 hotSpot;

            public CursorInfo(string assetPath, Vector2 hotSpot)
            {
                this.assetPath = assetPath;
                this.hotSpot = hotSpot;
            }
        }

        const string k_CursorAssetRoot = "Cursors/";
        const string k_CursorAssetsNamespace = "Timeline.";
        const string k_CursorAssetExtension = ".png";

        const string k_MixBothCursorAssetName = k_CursorAssetsNamespace + "MixBoth" + k_CursorAssetExtension;
        const string k_MixLeftCursorAssetName = k_CursorAssetsNamespace + "MixLeft" + k_CursorAssetExtension;
        const string k_MixRightCursorAssetName = k_CursorAssetsNamespace + "MixRight" + k_CursorAssetExtension;
        const string k_ReplaceCursorAssetName = k_CursorAssetsNamespace + "Replace" + k_CursorAssetExtension;
        const string k_RippleCursorAssetName = k_CursorAssetsNamespace + "Ripple" + k_CursorAssetExtension;

        static readonly string k_PlatformPath = (Application.platform == RuntimePlatform.WindowsEditor) ? "Windows/" : "macOS/";
        static readonly string k_CursorAssetDirectory = k_CursorAssetRoot + k_PlatformPath;

        static readonly Dictionary<CursorType, CursorInfo> k_CursorInfoLookup = new Dictionary<CursorType, CursorInfo>
        {
            { CursorType.MixBoth,  new CursorInfo(k_CursorAssetDirectory + k_MixBothCursorAssetName,  new Vector2(16, 18))},
            { CursorType.MixLeft,  new CursorInfo(k_CursorAssetDirectory + k_MixLeftCursorAssetName,  new Vector2(7, 18))},
            { CursorType.MixRight, new CursorInfo(k_CursorAssetDirectory + k_MixRightCursorAssetName, new Vector2(25, 18))},
            { CursorType.Replace,  new CursorInfo(k_CursorAssetDirectory + k_ReplaceCursorAssetName,  new Vector2(16, 28))},
            { CursorType.Ripple,   new CursorInfo(k_CursorAssetDirectory + k_RippleCursorAssetName,   new Vector2(26, 19))}
        };

        static readonly Dictionary<string, Texture2D> k_CursorAssetCache = new Dictionary<string, Texture2D>();

        public static Cursor GetCursor(CursorType cursorType)
        {
            if (cursorType == CursorType.None)
                return new StyleCursor(StyleKeyword.Auto).value;

            CursorInfo cursorInfo = k_CursorInfoLookup[cursorType];

            if (!k_CursorAssetCache.ContainsKey(cursorInfo.assetPath))
            {
                k_CursorAssetCache.Add(cursorInfo.assetPath, (Texture2D)EditorGUIUtility.Load(cursorInfo.assetPath));
            }

            return new Cursor { texture = k_CursorAssetCache[cursorInfo.assetPath], hotspot = cursorInfo.hotSpot };
        }
    }
}
