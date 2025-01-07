// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements.Experimental;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Give access to TextElement experimental features.
    /// </summary>
    public interface ITextElementExperimentalFeatures : IExperimentalFeatures
    {
        /// <summary>
        /// Setting this property will override the displayed text while preserving the original text value.
        /// </summary>
        /// <remarks>
        /// It is frequently employed to integrate third-party plugins available in the Unity Asset Store for text shaping purposes in combination with the <see cref="LanguageDirection"/> property.
        /// </remarks>
        void SetRenderedText(string renderedText);
    }
    
    public partial class TextElement : ITextElementExperimentalFeatures
    {
        /// <summary>
        /// Returns the TextElement experimental interface.
        /// </summary>
        public new ITextElementExperimentalFeatures experimental => this;

        /// <summary>
        /// Setting this property will override the displayed text while preserving the original text value.
        /// </summary>
        /// <remarks>
        /// It is frequently employed to integrate third-party plugins available in the Unity Asset Store for text shaping purposes in combination with the <see cref="LanguageDirection"/> property.
        /// </remarks>
        void ITextElementExperimentalFeatures.SetRenderedText(string renderedText)
        {
            this.SetRenderedText(renderedText);
        }
    }
}
