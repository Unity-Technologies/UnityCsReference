// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityTexture2D = UnityEngine.Texture2D;
using System;

// We are putting this in the Editor folder for now since on SpriteEditorWindow et al. are using it
namespace UnityEngine.U2D.Interface
{
    internal abstract class ITexture2D
    {
        abstract public int width { get; }
        abstract public int height { get; }
        abstract public TextureFormat format { get; }
        abstract public Color32[] GetPixels32();
        abstract public FilterMode filterMode { get; set; }
        abstract public string name { get; }
        abstract public void SetPixels(Color[] c);
        abstract public void Apply();
        abstract public float mipMapBias { get; }

        public static bool operator==(ITexture2D t1, ITexture2D t2)
        {
            if (object.ReferenceEquals(t1, null))
            {
                return object.ReferenceEquals(t2, null) || t2 == null;
            }

            return t1.Equals(t2);
        }

        public static bool operator!=(ITexture2D t1, ITexture2D t2)
        {
            if (object.ReferenceEquals(t1, null))
            {
                return !object.ReferenceEquals(t2, null) && t2 != null;
            }

            return !t1.Equals(t2);
        }

        override public bool Equals(object other)
        {
            throw new NotImplementedException();
        }

        override public int GetHashCode()
        {
            throw new NotImplementedException();
        }

        public static implicit operator UnityEngine.Object(ITexture2D t)
        {
            return object.ReferenceEquals(t, null) ? null : t.ToUnityObject();
        }

        public static implicit operator UnityEngine.Texture2D(ITexture2D t)
        {
            return object.ReferenceEquals(t, null) ? null : t.ToUnityTexture();
        }

        abstract protected UnityEngine.Object ToUnityObject();
        abstract protected UnityEngine.Texture2D ToUnityTexture();
    }

    internal class Texture2D : ITexture2D
    {
        UnityTexture2D m_Texture;

        public Texture2D(UnityTexture2D texture)
        {
            m_Texture = texture;
        }

        override public int width
        {
            get { return m_Texture.width; }
        }

        override public int height
        {
            get { return m_Texture.height; }
        }

        override public TextureFormat format
        {
            get { return m_Texture.format; }
        }

        override public Color32[] GetPixels32()
        {
            return m_Texture.GetPixels32();
        }

        override public FilterMode filterMode
        {
            get { return m_Texture.filterMode; }
            set { m_Texture.filterMode = value; }
        }

        override public float mipMapBias
        {
            get { return m_Texture.mipMapBias; }
        }

        override public string name
        {
            get { return m_Texture.name; }
        }

        public override bool Equals(object other)
        {
            Texture2D t = other as Texture2D;
            if (object.ReferenceEquals(t, null))
                return m_Texture == null;
            return m_Texture == t.m_Texture;
        }

        public override int GetHashCode()
        {
            return m_Texture.GetHashCode();
        }

        public override void SetPixels(Color[] c)
        {
            m_Texture.SetPixels(c);
        }

        public override void Apply()
        {
            m_Texture.Apply();
        }

        override protected UnityEngine.Object ToUnityObject()
        {
            return m_Texture;
        }

        override protected UnityEngine.Texture2D ToUnityTexture()
        {
            return m_Texture;
        }
    }
}
