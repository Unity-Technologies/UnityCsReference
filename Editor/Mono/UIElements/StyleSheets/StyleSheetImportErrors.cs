// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.UIElements.StyleSheets
{
    enum StyleSheetImportErrorType
    {
        Syntax,
        Semantic,
        Validation,
        Internal
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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
        public readonly int column;
        public readonly bool isWarning;

        public StyleSheetImportError(StyleSheetImportErrorType error, StyleSheetImportErrorCode code, string assetPath, string message, int line = -1, int column = -1, bool isWarning = false)
        {
            this.error = error;
            this.code = code;
            this.assetPath = assetPath;
            this.message = message;
            this.line = line;
            this.column = column;
            this.isWarning = isWarning;
        }

        public override string ToString()
        {
            return ToString(StyleValueImporter.glossary);
        }

        public string ToString(StyleSheetImportGlossary glossary)
        {
            string typeStr = isWarning ? glossary.warning : glossary.error;
            if (line > -1)
            {
                if (column > -1)
                    return $"{assetPath} ({glossary.line} {line}, {glossary.column} {column}): {typeStr}: {message}";
                else
                    return $"{assetPath} ({glossary.line} {line}): {typeStr}: {message}";
            }
            return $"{assetPath}: {typeStr}: {message}";
        }
    }

    class StyleSheetImportErrors : IEnumerable<StyleSheetImportError>
    {
        List<StyleSheetImportError> m_Errors = new List<StyleSheetImportError>();

        public string assetPath { get; set; }

        public StyleSheetImporter.ErrorHandling unsupportedSelectorAction { get; set; }
        public StyleSheetImporter.ErrorHandling unsupportedTermAction { get; set; }

        StyleSheetImporter.ErrorHandling GetHandling(StyleSheetImportErrorCode errorType, StyleSheetImporter.ErrorHandling defaultHandling)
        {
            // We pick the lowest severity. If the default is warning then we wont override it with an error.
            if (errorType == StyleSheetImportErrorCode.UnsupportedTerm)
                return (StyleSheetImporter.ErrorHandling)Mathf.Max((int)unsupportedTermAction, (int)defaultHandling);
            if (errorType == StyleSheetImportErrorCode.UnsupportedSelectorFormat || errorType == StyleSheetImportErrorCode.RecursiveSelectorDetected)
                return (StyleSheetImporter.ErrorHandling)Mathf.Max((int)unsupportedSelectorAction, (int)defaultHandling);
            return defaultHandling;
        }

        public void AddSyntaxError(string message, int line, int column = -1)
        {
            m_Errors.Add(new StyleSheetImportError(
                StyleSheetImportErrorType.Syntax,
                StyleSheetImportErrorCode.None,
                assetPath,
                message,
                line,
                column)
            );
        }

        public void AddSemanticError(StyleSheetImportErrorCode code, string message, int line, int column = -1)
        {
            var handling = GetHandling(code, StyleSheetImporter.ErrorHandling.Error);
            if (handling == StyleSheetImporter.ErrorHandling.Ignore)
                return;

            m_Errors.Add(new StyleSheetImportError(
                StyleSheetImportErrorType.Semantic,
                code,
                assetPath,
                message,
                line,
                column,
                handling == StyleSheetImporter.ErrorHandling.Warning)
            );
        }

        public void AddSemanticWarning(StyleSheetImportErrorCode code, string message, int line)
        {
            var handling = GetHandling(code, StyleSheetImporter.ErrorHandling.Warning);
            if (handling == StyleSheetImporter.ErrorHandling.Ignore)
                return;

            m_Errors.Add(new StyleSheetImportError(
                StyleSheetImportErrorType.Semantic,
                code,
                assetPath,
                message,
                line,
                isWarning: true)
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

        public void AddValidationWarning(string message, int line, int column = -1)
        {
            m_Errors.Add(new StyleSheetImportError(
                StyleSheetImportErrorType.Validation,
                StyleSheetImportErrorCode.InvalidProperty,
                assetPath,
                message,
                line,
                column,
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

        public bool hasErrors { get { return m_Errors.Exists(e => !e.isWarning); } }
        public bool hasWarning { get { return m_Errors.Exists(e => e.isWarning); } }
    }
}
