// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.AssetImporters;

namespace UnityEditor.Experimental.U2D
{
    internal interface ISpriteEditorDataProvider
    {
        SpriteImportMode spriteImportMode { get; }
        int spriteDataCount { get; set; }
        SpriteDataBase GetSpriteData(int i);
        void Apply(SerializedObject so);
        Object targetObject { get; }

        void GetTextureActualWidthAndHeight(out int width, out int height);
        void InitSpriteEditorDataProvider(SerializedObject so);
    }

    // We are doing this so that we don't have public interface API
    internal abstract class ScriptedSpriteEditorDataProviderImporter : ScriptedImporter, ISpriteEditorDataProvider
    {
        public abstract SpriteImportMode spriteImportMode { get; }
        public abstract int spriteDataCount { get; set; }
        public abstract SpriteDataBase GetSpriteData(int i);
        public abstract void Apply(SerializedObject so);
        public abstract Object targetObject { get; }

        public abstract void GetTextureActualWidthAndHeight(out int width, out int height);
        public abstract void InitSpriteEditorDataProvider(SerializedObject so);
    }


    internal abstract class SpriteDataBase
    {
        public abstract string name { get; set; }
        public abstract Rect rect { get; set; }
        public abstract SpriteAlignment alignment { get; set; }
        public abstract Vector2 pivot { get; set; }
        public abstract Vector4 border { get; set; }
        public abstract float tessellationDetail { get; set; }
        public abstract List<Vector2[]> outline { get; set; }
        public abstract List<Vector2[]> physicsShape { get; set; }
    }
}
