// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.UIElements.StyleSheets
{
    enum StyleSheetImportErrorType
    {
        Syntax,
        Semantic,
        Validation,
        Internal
    }

    enum StyleSheetImportErrorCode
    {
        None,
        Internal,
        UnsupportedUnit,
        UnsupportedTerm,
        InvalidSelectorListDelimiter,
        InvalidComplexSelectorDelimiter,
        UnsupportedSelectorFormat,
        RecursiveSelectorDetected,
        MissingFunctionArgument,
        InvalidProperty,
        InvalidURILocation,
        InvalidURIScheme,
        InvalidURIProjectAssetPath,
        InvalidURIProjectAssetType,
        InvalidVarFunction,
        InvalidHighResolutionImage,
    }

    struct StyleSheetImportError
    {
        public readonly StyleSheetImportErrorType error;
        public readonly StyleSheetImportErrorCode code;
        public readonly string assetPath;
        public readonly string message;
        public readonly int line;
        public readonly bool isWarning;

        public StyleSheetImportError(StyleSheetImportErrorType error, StyleSheetImportErrorCode code, string assetPath, string message, int line = -1, bool isWarning = false)
        {
            this.error = error;
            this.code = code;
            this.assetPath = assetPath;
            this.message = message;
            this.line = line;
            this.isWarning = isWarning;
        }

        public string ToString(StyleSheetImportGlossary glossary)
        {
            string lineStr = line > -1 ? $" ({glossary.line} {line})" : "";
            string typeStr = isWarning ? glossary.warning : glossary.error;
            return $"{assetPath}{lineStr}: {typeStr}: {message}";
        }
    }

    class StyleSheetImportErrors : IEnumerable<StyleSheetImportError>
    {
        List<StyleSheetImportError> m_Errors = new List<StyleSheetImportError>();

        public string assetPath { get; set; }

        public void AddSyntaxError(string message, int line)
        {
            m_Errors.Add(new StyleSheetImportError(
                StyleSheetImportErrorType.Syntax,
                StyleSheetImportErrorCode.None,
                assetPath,
                message,
                line)
            );
        }

        public void AddSemanticError(StyleSheetImportErrorCode code, string message, int line)
        {
            m_Errors.Add(new StyleSheetImportError(
                StyleSheetImportErrorType.Semantic,
                code,
                assetPath,
                message,
                line)
            );
        }

        public void AddInternalError(string message, int line = -1)
        {
            m_Errors.Add(new StyleSheetImportError(
                StyleSheetImportErrorType.Internal,
                StyleSheetImportErrorCode.None,
                assetPath,
                message,
                line)
            );
        }

        public void AddValidationWarning(string message, int line)
        {
            m_Errors.Add(new StyleSheetImportError(
                StyleSheetImportErrorType.Validation,
                StyleSheetImportErrorCode.InvalidProperty,
                assetPath,
                message,
                line,
                true)
            );
        }

        public IEnumerator<StyleSheetImportError> GetEnumerator()
        {
            return m_Errors.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_Errors.GetEnumerator();
        }

        public bool hasErrors { get { return m_Errors.Any(e => !e.isWarning); } }
        public bool hasWarning { get { return m_Errors.Any(e => e.isWarning); } }
    }
}
