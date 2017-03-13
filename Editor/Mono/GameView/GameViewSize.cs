// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;

namespace UnityEditor
{
    internal enum GameViewSizeType
    {
        AspectRatio,
        FixedResolution
    };

    [Serializable]
    internal class GameViewSize
    {
        [SerializeField] string m_BaseText;
        [SerializeField] GameViewSizeType m_SizeType;
        [SerializeField] int m_Width;
        [SerializeField] int m_Height;
        [NonSerialized] string m_CachedDisplayText;
        const int kMaxBaseTextLength = 40;
        const int kMinResolution = 10;
        const int kMinAspect = 0;
        const int kMaxResolutionOrAspect = 99999;

        public string baseText
        {
            get {return m_BaseText; }
            set
            {
                m_BaseText = value;
                if (m_BaseText.Length > kMaxBaseTextLength)
                    m_BaseText = m_BaseText.Substring(0, kMaxBaseTextLength);
                Changed();
            }
        }
        public GameViewSizeType sizeType
        {
            get { return m_SizeType; }
            set
            {
                m_SizeType = value; Clamp(); Changed();
            }
        }
        public int width
        {
            get { return m_Width; }
            set { m_Width = value; Clamp(); Changed(); }
        }
        public int height
        {
            get { return m_Height; }
            set { m_Height = value; Clamp(); Changed(); }
        }

        void Clamp()
        {
            int orgWidth = m_Width;
            int orgHeight = m_Height;

            int minValue = 0;
            switch (sizeType)
            {
                case GameViewSizeType.AspectRatio:
                    minValue = kMinAspect;
                    break;
                case GameViewSizeType.FixedResolution:
                    minValue = kMinResolution;
                    break;
                default:
                    Debug.LogError("Unhandled enum!");
                    break;
            }

            m_Width = Mathf.Clamp(m_Width, minValue, kMaxResolutionOrAspect);
            m_Height = Mathf.Clamp(m_Height, minValue, kMaxResolutionOrAspect);

            if (m_Width != orgWidth || m_Height != orgHeight)
                Changed();
        }

        public GameViewSize(GameViewSizeType type, int width, int height, string baseText)
        {
            this.sizeType = type;
            this.width = width;
            this.height = height;
            this.baseText = baseText;
        }

        public GameViewSize(GameViewSize other)
        {
            Set(other);
        }

        public void Set(GameViewSize other)
        {
            sizeType = other.sizeType;
            width = other.width;
            height = other.height;
            baseText = other.baseText;
            Changed();
        }

        public bool isFreeAspectRatio
        {
            get { return (width == 0); }
        }

        public float aspectRatio
        {
            get
            {
                if (height == 0)
                    return 0f;
                return width / (float)height;
            }
        }

        public string displayText
        {
            get { return m_CachedDisplayText ?? (m_CachedDisplayText = ComposeDisplayString()); }
        }

        private string sizeText
        {
            get
            {
                if (sizeType == GameViewSizeType.AspectRatio)
                    return string.Format("{0}:{1}", width, height);
                else if (sizeType == GameViewSizeType.FixedResolution)
                    return string.Format("{0}x{1}", width, height);

                Debug.LogError("Unhandled game view size type");
                return "";
            }
        }

        private string ComposeDisplayString()
        {
            if (width == 0 && height == 0)
                return baseText;

            if (string.IsNullOrEmpty(baseText))
                return sizeText;

            return baseText + " (" + sizeText + ")";
        }

        private void Changed()
        {
            m_CachedDisplayText = null;
            GameViewSizes.instance.Changed();
        }
    }
}
