// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(Texture3D))]
    [CanEditMultipleObjects]
    internal class Texture3DInspector : TextureInspector
    {
        new Texture3DPreview preview;

        protected override void OnEnable()
        {
            if (preview == null) preview = CreateInstance<Texture3DPreview>();

            base.OnEnable();
            preview.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            preview.OnDisable();
        }

        public override string GetInfoString() => preview.GetInfoString();
        public override void OnPreviewSettings()
        {
            preview.OnPreviewSettings(targets);
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            preview.Texture = target as Texture;
            preview.OnPreviewGUI(r, background);
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
            => preview.RenderStaticPreview(target as Texture, width, height);
    }
}
