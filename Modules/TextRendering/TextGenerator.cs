// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: Text not yet converted
using System;
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

    ///<summary>A struct that stores the settings for TextGeneration.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TextGenerationSettings
    {
        ///<summary>Font to use for generation.</summary>
        public Font font;
        ///<summary>The base color for the text generation.</summary>
        public Color color;
        ///<summary>Font size.</summary>
        public int fontSize;
        ///<summary>The line spacing multiplier.</summary>
        ///<remarks>This is multiplied with the line spacing defined in the font.</remarks>
        public float lineSpacing;
        ///<summary>Allow rich text markup in generation.</summary>
        public bool richText;

        ///<summary>A scale factor for the text. This is useful if the <see cref="T:UnityEngine.UI.Text" /> is on a <see cref="T:UnityEngine.Canvas" /> and the canvas is scaled.</summary>
        public float scaleFactor;

        ///<summary>Font style.</summary>
        public FontStyle fontStyle;
        ///<summary>How is the generated text anchored.</summary>
        public TextAnchor textAnchor;
        ///<summary>Use the extents of glyph geometry to perform horizontal alignment rather than glyph metrics.</summary>
        ///<remarks>This can result in better fitting left and right alignment, but may result in incorrect positioning when attempting to overlay multiple fonts (such as a specialized outline font) on top of each other.</remarks>
        public bool alignByGeometry;

        ///<summary>Should the text be resized to fit the configured bounds?</summary>
        public bool resizeTextForBestFit;
        ///<summary>Minimum size for resized text.</summary>
        public int resizeTextMinSize;
        ///<summary>Maximum size for resized text.</summary>
        public int resizeTextMaxSize;

        ///<summary>Should the text generator update the bounds from the generated text.</summary>
        public bool updateBounds;
        ///<summary>What happens to text when it reaches the bottom generation bounds.</summary>
        public VerticalWrapMode verticalOverflow;
        ///<summary>What happens to text when it reaches the horizontal generation bounds.</summary>
        public HorizontalWrapMode horizontalOverflow;

        ///<summary>Extents that the generator will attempt to fit the text in.</summary>
        public Vector2 generationExtents;
        ///<summary>Generated vertices are offset by the pivot.</summary>
        public Vector2 pivot;
        ///<summary>Continue to generate characters even if the text runs out of bounds.</summary>
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

        ///<exclude />
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
                && updateBounds == other.updateBounds
                && horizontalOverflow == other.horizontalOverflow
                && verticalOverflow == other.verticalOverflow
                && CompareVector2(generationExtents, other.generationExtents)
                && CompareVector2(pivot, other.pivot)
                && font == other.font;
        }
    }

    ///<summary>Class that can be used to generate text for rendering.</summary>
    ///<remarks>Caches vertices, character info, and line info for memory friendlyness.</remarks>
    ///<example>
    ///  <code><![CDATA[
    ///using UnityEngine;
    ///using System.Collections;
    ///
    ///public class ExampleClass : MonoBehaviour
    ///{
    ///    public Font font;
    ///    void Start()
    ///    {
    ///        TextGenerationSettings settings = new TextGenerationSettings();
    ///        settings.textAnchor = TextAnchor.MiddleCenter;
    ///        settings.color = Color.red;
    ///        settings.generationExtents = new Vector2(500.0F, 200.0F);
    ///        settings.pivot = Vector2.zero;
    ///        settings.richText = true;
    ///        settings.font = font;
    ///        settings.fontSize = 32;
    ///        settings.fontStyle = FontStyle.Normal;
    ///        settings.verticalOverflow = VerticalWrapMode.Overflow;
    ///        TextGenerator generator = new TextGenerator();
    ///        generator.Populate("I am a string", settings);
    ///        Debug.Log("I generated: " + generator.vertexCount + " verts!");
    ///    }
    ///}
    ///]]></code>
    ///</example>
    public partial class TextGenerator : IDisposable
    {
        // WARNING: Because this is a partial class, do not add any data members here; there is no defined ordering between fields
        // in multiple declarations of partial class. All instance fields must be in the same declaration (for this class, they
        // are in the corresponding Bindings.txt file.

        ///<summary>Create a TextGenerator.</summary>
#pragma warning disable UAL0015 // chains to TextGenerator(int), whose debug weak-reference tracking is benign (see below)
        public TextGenerator()
            : this(50)
        {}
#pragma warning restore UAL0015

        ///<summary>Create a TextGenerator.</summary>
        public TextGenerator(int initialCapacity)
        {
            m_Ptr = Internal_Create();
            m_Verts = new List<UIVertex>((initialCapacity + 1) * 4);
            m_Characters = new List<UICharInfo>(initialCapacity + 1);
            m_Lines = new List<UILineInfo>(20);
#pragma warning disable UAL0015 // debug weak-reference tracker only, reset wholesale on reload
            lock (s_Instances)
            {
                m_Id = s_NextId++;
                s_Instances.Add(m_Id, new WeakReference(this));
            }
#pragma warning restore UAL0015
        }

#pragma warning disable UA5000 // The Avoid Finalizer Analyzer produces compile errors for any new finalizers. This pre-existing finalizer declaration has been suppressed, but should be rewritten if possible.
        ~TextGenerator()
        {
            ((IDisposable)this).Dispose();
        }
#pragma warning restore UA5000

        void IDisposable.Dispose()
        {
            lock (s_Instances)
            {
                s_Instances.Remove(m_Id);
            }

            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        ///<summary>The number of characters that have been generated and are included in the visible lines.</summary>
        ///<seealso cref="lines" />
        ///<seealso cref="lineCount" />
        public int characterCountVisible => characterCount - 1;

        [RequiredByNativeCode]
        internal static void InvalidateAll()
        {
            lock (s_Instances)
            {
                foreach (var kvp in s_Instances)
                {
                    WeakReference wr = kvp.Value;

                    // Explicitly resolve the weak reference to a strong reference
                    var target = wr.Target;

                    if (target != null)
                        ((TextGenerator)wr.Target).Invalidate();
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

        ///<summary>Mark the text generator as invalid. This will force a full text generation the next time Populate is called.</summary>
        public void Invalidate()
        {
            m_HasGenerated = false;
        }

        ///<summary>Populate the given List with UICharInfo.</summary>
        ///<param name="characters">List to populate.</param>
        public void GetCharacters(List<UICharInfo> characters)
        {
            GetCharactersInternal(characters);
        }

        ///<summary>Populate the given list with <see cref="UILineInfo" />.</summary>
        ///<param name="lines">List to populate.</param>
        public void GetLines(List<UILineInfo> lines)
        {
            GetLinesInternal(lines);
        }

        ///<summary>Populate the given list with generated Vertices.</summary>
        ///<param name="vertices">List to populate.</param>
        public void GetVertices(List<UIVertex> vertices)
        {
            GetVerticesInternal(vertices);
        }

        ///<summary>Given a string and settings, returns the preferred width for a container that would hold this text.</summary>
        ///<param name="str">Generation text.</param>
        ///<param name="settings">Settings for generation.</param>
        ///<returns>Preferred width.</returns>
        public float GetPreferredWidth(string str, TextGenerationSettings settings)
        {
            settings.horizontalOverflow = HorizontalWrapMode.Overflow;
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            settings.updateBounds = true;
            Populate(str, settings);
            return rectExtents.width;
        }

        ///<summary>Given a string and settings, returns the preferred height for a container that would hold this text.</summary>
        ///<param name="str">Generation text.</param>
        ///<param name="settings">Settings for generation.</param>
        ///<returns>Preferred height.</returns>
        public float GetPreferredHeight(string str, TextGenerationSettings settings)
        {
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            settings.updateBounds = true;
            Populate(str, settings);
            return rectExtents.height;
        }

        ///<summary>Will generate the vertices and other data for the given string with the given settings.</summary>
        ///<remarks>Will only regenerate if the string AND settings are different from the last execution.</remarks>
        ///<param name="str">String to generate.</param>
        ///<param name="settings">Generation settings.</param>
        ///<param name="context">The object used as context of the error log message, if necessary.</param>
        ///<returns>True if the generation is a success, false otherwise.</returns>
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

        ///<summary>Will generate the vertices and other data for the given string with the given settings.</summary>
        ///<remarks>Will only regenerate if the string AND settings are different from the last execution.</remarks>
        ///<param name="str">String to generate.</param>
        ///<param name="settings">Settings.</param>
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

        ///<summary>Array of generated vertices.</summary>
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

        ///<summary>Array of generated characters.</summary>
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

        ///<summary>Information about each generated text line.</summary>
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
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
