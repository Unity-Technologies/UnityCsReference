// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.TextCore.LowLevel;


namespace UnityEditor.TextCore.Text
{
    class SerializedPropertyHolder : ScriptableObject
    {
        public FontAsset fontAsset;
        public uint firstCharacter;
        public uint secondCharacter;

        public GlyphPairAdjustmentRecord glyphPairAdjustmentRecord = new GlyphPairAdjustmentRecord(new GlyphAdjustmentRecord(), new GlyphAdjustmentRecord());
    }
}
