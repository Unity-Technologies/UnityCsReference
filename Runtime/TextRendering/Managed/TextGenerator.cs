// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // Must be kept in sync with the enum in TextFormatting.h
    [Flags]
    enum TextGenerationError
    {
        None = 0,

        CustomSizeOnNonDynamicFont = 1,

        CustomStyleOnNonDynamicFont = 2,

        NoFont = 4
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TextGenerationSettings
    {
        public Font font;
        public Color color;
        public int fontSize;
        public float lineSpacing;
        public bool richText;

        public float scaleFactor;

        public FontStyle fontStyle;
        public TextAnchor textAnchor;
        public bool alignByGeometry;

        public bool resizeTextForBestFit;
        public int resizeTextMinSize;
        public int resizeTextMaxSize;

        public bool updateBounds;
        public VerticalWrapMode verticalOverflow;
        public HorizontalWrapMode horizontalOverflow;

        public Vector2 generationExtents;
        public Vector2 pivot;
        public bool generateOutOfBounds;

        private bool CompareColors(Color left, Color right)
        {
            return Mathf.Approximately(left.r, right.r)
                && Mathf.Approximately(left.g, right.g)
                && Mathf.Approximately(left.b, right.b)
                && Mathf.Approximately(left.a, right.a);
        }

        private bool CompareVector2(Vector2 left, Vector2 right)
        {
            return Mathf.Approximately(left.x, right.x) && Mathf.Approximately(left.y, right.y);
        }

        public bool Equals(TextGenerationSettings other)
        {
            return CompareColors(color, other.color)
                && fontSize == other.fontSize
                && Mathf.Approximately(scaleFactor, other.scaleFactor)
                && resizeTextMinSize == other.resizeTextMinSize
                && resizeTextMaxSize == other.resizeTextMaxSize
                && Mathf.Approximately(lineSpacing, other.lineSpacing)
                && fontStyle == other.fontStyle
                && richText == other.richText
                && textAnchor == other.textAnchor
                && alignByGeometry == other.alignByGeometry
                && resizeTextForBestFit == other.resizeTextForBestFit
                && resizeTextMinSize == other.resizeTextMinSize
                && resizeTextMaxSize == other.resizeTextMaxSize
                && resizeTextForBestFit == other.resizeTextForBestFit
                && updateBounds == other.updateBounds
                && horizontalOverflow == other.horizontalOverflow
                && verticalOverflow == other.verticalOverflow
                && CompareVector2(generationExtents, other.generationExtents)
                && CompareVector2(pivot, other.pivot)
                && font == other.font;
        }
    }

    public partial class TextGenerator
    {
        // WARNING: Because this is a partial class, do not add any data members here; there is no defined ordering between fields
        // in multiple declarations of partial class. All instance fields must be in the same declaration (for this class, they
        // are in the corresponding Bindings.txt file.

        public TextGenerator()
            : this(50)
        {}

        public TextGenerator(int initialCapacity)
        {
            m_Verts = new List<UIVertex>((initialCapacity + 1) * 4);
            m_Characters = new List<UICharInfo>(initialCapacity + 1);
            m_Lines = new List<UILineInfo>(20);
            Init();
            lock (s_Instances)
            {
                m_Id = s_NextId++;
                s_Instances.Add(m_Id, new WeakReference(this));
            }
        }

        ~TextGenerator()
        {
            Dispose(false);
        }

        void IDisposable.Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            lock (s_Instances)
            {
                s_Instances.Remove(m_Id);
            }
            if (disposing)
                Dispose_cpp();
        }

        [RequiredByNativeCode]
        internal static void InvalidateAll()
        {
            lock (s_Instances)
            {
                foreach (var kvp in s_Instances)
                {
                    WeakReference wr = kvp.Value;
                    if (wr.IsAlive)
                        (wr.Target as TextGenerator).Invalidate();
                }
            }
        }


        private TextGenerationSettings ValidatedSettings(TextGenerationSettings settings)
        {
            if (settings.font != null && settings.font.dynamic)
                return settings;

            if (settings.fontSize != 0 || settings.fontStyle != FontStyle.Normal)
            {
                if (settings.font != null)
                    Debug.LogWarningFormat(settings.font, "Font size and style overrides are only supported for dynamic fonts. Font '{0}' is not dynamic.", settings.font.name);
                settings.fontSize = 0;
                settings.fontStyle = FontStyle.Normal;
            }

            if (settings.resizeTextForBestFit)
            {
                if (settings.font != null)
                    Debug.LogWarningFormat(settings.font, "BestFit is only supported for dynamic fonts. Font '{0}' is not dynamic.", settings.font.name);
                settings.resizeTextForBestFit = false;
            }
            return settings;
        }

        public void Invalidate()
        {
            m_HasGenerated = false;
        }

        public void GetCharacters(List<UICharInfo> characters)
        {
            GetCharactersInternal(characters);
        }

        public void GetLines(List<UILineInfo> lines)
        {
            GetLinesInternal(lines);
        }

        public void GetVertices(List<UIVertex> vertices)
        {
            GetVerticesInternal(vertices);
        }

        public float GetPreferredWidth(string str, TextGenerationSettings settings)
        {
            settings.horizontalOverflow = HorizontalWrapMode.Overflow;
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            settings.updateBounds = true;
            Populate(str, settings);
            return rectExtents.width;
        }

        public float GetPreferredHeight(string str, TextGenerationSettings settings)
        {
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            settings.updateBounds = true;
            Populate(str, settings);
            return rectExtents.height;
        }

        public bool PopulateWithErrors(string str, TextGenerationSettings settings, GameObject context)
        {
            var error = PopulateWithError(str, settings);
            if (error == TextGenerationError.None)
                return true;
            if ((error & TextGenerationError.CustomSizeOnNonDynamicFont) != 0)
                Debug.LogErrorFormat(context, "Font '{0}' is not dynamic, which is required to override its size", settings.font);
            if ((error & TextGenerationError.CustomStyleOnNonDynamicFont) != 0)
                Debug.LogErrorFormat(context, "Font '{0}' is not dynamic, which is required to override its style", settings.font);
            //if ((error & TextGenerationError.NoFont) == TextGenerationError.NoFont)
            //  Debug.LogErrorFormat(context, "Missing font, aborting text generation", settings.font);
            return false;
        }

        public bool Populate(string str, TextGenerationSettings settings)
        {
            var textGenerationError = PopulateWithError(str, settings);
            return textGenerationError == TextGenerationError.None;
        }

        private TextGenerationError PopulateWithError(string str, TextGenerationSettings settings)
        {
            if (m_HasGenerated && str == m_LastString && settings.Equals(m_LastSettings))
                return m_LastValid;

            m_LastValid = PopulateAlways(str, settings);
            return m_LastValid;
        }

        private TextGenerationError PopulateAlways(string str, TextGenerationSettings settings)
        {
            m_LastString = str;
            m_HasGenerated = true;
            m_CachedVerts = false;
            m_CachedCharacters = false;
            m_CachedLines = false;
            m_LastSettings = settings;

            var validSettings = ValidatedSettings(settings);

            TextGenerationError error;
            Populate_Internal(str, validSettings.font, validSettings.color, validSettings.fontSize,
                validSettings.scaleFactor, validSettings.lineSpacing, validSettings.fontStyle,
                validSettings.richText, validSettings.resizeTextForBestFit, validSettings.resizeTextMinSize,
                validSettings.resizeTextMaxSize, validSettings.verticalOverflow, validSettings.horizontalOverflow,
                validSettings.updateBounds, validSettings.textAnchor, validSettings.generationExtents,
                validSettings.pivot, validSettings.generateOutOfBounds, validSettings.alignByGeometry, out error);
            m_LastValid = error;
            return error;
        }

        public IList<UIVertex> verts
        {
            get
            {
                if (!m_CachedVerts)
                {
                    GetVertices(m_Verts);
                    m_CachedVerts = true;
                }
                return m_Verts;
            }
        }

        public IList<UICharInfo> characters
        {
            get
            {
                if (!m_CachedCharacters)
                {
                    GetCharacters(m_Characters);
                    m_CachedCharacters = true;
                }
                return m_Characters;
            }
        }

        public IList<UILineInfo> lines
        {
            get
            {
                if (!m_CachedLines)
                {
                    GetLines(m_Lines);
                    m_CachedLines = true;
                }
                return m_Lines;
            }
        }
    }
}
