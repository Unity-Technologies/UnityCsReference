// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;
using UnityEditor.Search.Providers;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UnityEditor.Search
{
    // TODO: Make this public and add documentation.
    internal class ResourceDescriptor
    {
        public virtual int Priority => 0;

        public virtual bool Match(Object obj)
        {
            return obj != null;
        }

        /// <summary>
        /// Get a description for this resource. The resource provider iterates over all matching descriptor and
        /// builds a description by calling each descriptors' GetDescription. You can stop the iteration by returning
        /// false, otherwise return true. Stopping the iteration is useful if you want only your custom description
        /// to show up.
        /// </summary>
        /// <param name="obj">Object for which to get the description.</param>
        /// <param name="sb">StringBuilder used to build the entire description.</param>
        /// <returns>True to stop the description building.</returns>
        public virtual bool GetDescription(Object obj, StringBuilder sb)
        {
            if (sb.Length > 0)
                sb.Append(" ");
            sb.AppendFormat("[{0}]", obj.hideFlags);
            return false;
        }

        public virtual Texture2D GetThumbnail(Object obj)
        {
            var thumbnail = EditorGUIUtility.FindTexture(obj.GetType().Name);
            return thumbnail ?? Icons.quicksearch;
        }

        public virtual Texture2D GetPreview(Object obj, int width, int height)
        {
            return GetThumbnail(obj);
        }

        public virtual void TrackSelection(Object obj)
        {}

        protected static void AddToDescription(StringBuilder sb, string desc)
        {
            AddSeparatorIfNeeded(sb);
            sb.Append(desc);
        }

        protected static void AddSeparatorIfNeeded(StringBuilder sb)
        {
            if (sb.Length > 0)
                sb.Append(", ");
        }
    }

    internal class AssetDescriptor : ResourceDescriptor
    {
        public override int Priority => 100;

        public override bool Match(Object obj)
        {
            return IsAsset(obj);
        }

        public override bool GetDescription(Object obj, StringBuilder sb)
        {
            var path = AssetDatabase.GetAssetOrScenePath(obj);
            AddToDescription(sb, AssetProvider.GetAssetDescription(path));
            return true;
        }

        public override Texture2D GetThumbnail(Object obj)
        {
            var path = AssetDatabase.GetAssetOrScenePath(obj);
            return Utils.GetAssetThumbnailFromPath(path);
        }

        public override Texture2D GetPreview(Object obj, int width, int height)
        {
            var path = AssetDatabase.GetAssetOrScenePath(obj);
            return Utils.GetAssetPreviewFromPath(path, new Vector2(width, height), FetchPreviewOptions.Preview2D);
        }

        public override void TrackSelection(Object obj)
        {
            var path = AssetDatabase.GetAssetOrScenePath(obj);
            Utils.PingAsset(path);
        }

        private static bool IsAsset(Object obj)
        {
            return obj && (AssetDatabase.IsForeignAsset(obj) ||
                AssetDatabase.IsMainAsset(obj) ||
                AssetDatabase.IsNativeAsset(obj) ||
                AssetDatabase.IsSubAsset(obj));
        }
    }

    internal class ComponentDescriptor : ResourceDescriptor
    {
        public override int Priority => 90;

        public override bool Match(Object obj)
        {
            return obj is Component;
        }

        public override bool GetDescription(Object obj, StringBuilder sb)
        {
            AddSeparatorIfNeeded(sb);
            var component = obj as Component;
            if (component.gameObject)
                sb.AppendFormat("{0}/", component.gameObject.name);
            sb.Append(component.name);
            return true;
        }

        public override Texture2D GetThumbnail(Object obj)
        {
            Texture2D thumbnail = null;
            if (obj is MonoBehaviour)
            {
                thumbnail = Utils.GetIconForObject(obj) ?? EditorGUIUtility.FindTexture("cs Script Icon");
            }
            return thumbnail ?? Utils.FindTextureForType(obj.GetType());
        }

        public override void TrackSelection(Object obj)
        {
            EditorGUIUtility.PingObject(obj);
        }
    }

    internal class GameObjectDescriptor : ResourceDescriptor
    {
        public override int Priority => 80;

        public override bool Match(Object obj)
        {
            return obj is GameObject;
        }

        public override bool GetDescription(Object obj, StringBuilder sb)
        {
            AddSeparatorIfNeeded(sb);
            var go = obj as GameObject;
            if (go.scene.IsValid())
                sb.AppendFormat("{0} ({1})", SearchUtils.GetHierarchyPath(go), go.tag);
            else
                sb.Append(go.tag);
            return true;
        }

        public override Texture2D GetThumbnail(Object obj)
        {
            var assetPath = SearchUtils.GetHierarchyAssetPath(obj as GameObject, true);
            if (String.IsNullOrEmpty(assetPath))
                return Utils.GetThumbnailForGameObject(obj as GameObject);
            return AssetPreview.GetAssetPreview(obj) ?? Utils.GetAssetPreviewFromPath(assetPath, new Vector2(64, 64), FetchPreviewOptions.Preview2D);
        }

        public override void TrackSelection(Object obj)
        {
            EditorGUIUtility.PingObject(obj);
        }
    }

    // TODO: Add MaterialDescriptor
    internal class TextureDescriptor : ResourceDescriptor
    {
        public override int Priority => 70;

        public override bool Match(Object obj)
        {
            return obj is Texture;
        }

        public override bool GetDescription(Object obj, StringBuilder sb)
        {
            AddSeparatorIfNeeded(sb);
            var tex = obj as Texture;
            switch (tex.dimension)
            {
                case TextureDimension.Tex3D:
                    var tex3d = obj as Texture3D;
                    if (tex3d != null)
                    {
                        sb.Append($"{tex3d.width}x{tex3d.height}x{tex3d.depth}");
                        break;
                    }
                    var rt = obj as RenderTexture;
                    if (rt != null)
                    {
                        sb.Append($"{rt.width}x{rt.height}x{rt.volumeDepth}");
                        break;
                    }
                    sb.Append($"{tex.width}x{tex.height}");
                    break;
                default:
                    sb.Append($"{tex.width}x{tex.height}");
                    break;
            }
            sb.AppendFormat(" {0}", tex.graphicsFormat);
            return true;
        }

        public override Texture2D GetPreview(Object obj, int width, int height)
        {
            var texture = obj as Texture;
            // TODO: Add support for cubemaps
            if (texture.dimension == TextureDimension.Cube)
                return null;

            RenderTexture savedRT = RenderTexture.active;

            RenderTexture tmp = RenderTexture.GetTemporary(
                width, height,
                0,
                SystemInfo.GetGraphicsFormat(DefaultFormat.LDR));
            Graphics.Blit(texture, tmp);

            RenderTexture.active = tmp;
            Texture2D copy;
            Texture2D tex2d = obj as Texture2D;
            if (tex2d != null && tex2d.alphaIsTransparency)
            {
                copy = new Texture2D(width, height, TextureFormat.RGBA32, false);
            }
            else
            {
                copy = new Texture2D(width, height, TextureFormat.RGB24, false);
            }
            copy.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            copy.Apply();
            RenderTexture.ReleaseTemporary(tmp);
            RenderTexture.active = savedRT;

            return copy;
        }
    }

    internal class MonoScriptDescriptor : ResourceDescriptor
    {
        public override int Priority => 50;

        public override bool Match(Object obj)
        {
            return obj is MonoScript;
        }

        public override Texture2D GetThumbnail(Object obj)
        {
            return EditorGUIUtility.FindTexture("cs Script Icon");
        }
    }
}
