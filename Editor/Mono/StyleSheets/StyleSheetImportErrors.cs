// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.StyleSheets
{
    enum StyleSheetImportErrorType
    {
        Syntax,
        Semantic,
        Other,
        Internal
    }

    enum StyleSheetImportErrorCode
    {
        None,
        Internal,
        UnsupportedFunction,
        UnsupportedParserType,
        UnsupportedUnit,
        InvalidSelectorListDelimiter,
        InvalidComplexSelectorDelimiter,
        UnsupportedSelectorFormat,
        RecursiveSelectorDetected
    }

    class StyleSheetImportErrors
    {
        struct Error
        {
            public readonly StyleSheetImportErrorType error;
            public readonly StyleSheetImportErrorCode code;
            public readonly string context;

            public Error(StyleSheetImportErrorType error, StyleSheetImportErrorCode code, string context)
            {
                this.error = error;
                this.code = code;
                this.context = context;
            }

            public override string ToString()
            {
                return string.Format("[StyleSheetImportError: error={0}, code={1}, context={2}]", error, code, context);
            }
        }

        List<Error> m_Errors = new List<Error>();

        public void AddSyntaxError(string context)
        {
            m_Errors.Add(new Error(
                    StyleSheetImportErrorType.Syntax,
                    StyleSheetImportErrorCode.None,
                    context)
                );
        }

        public void AddSemanticError(StyleSheetImportErrorCode code, string context)
        {
            m_Errors.Add(new Error(
                    StyleSheetImportErrorType.Semantic,
                    code,
                    context)
                );
        }

        public void AddInternalError(string context)
        {
            m_Errors.Add(new Error(
                    StyleSheetImportErrorType.Internal,
                    StyleSheetImportErrorCode.None,
                    context)
                );
        }

        public bool hasErrors { get { return m_Errors.Count > 0; } }

        public IEnumerable<string> FormatErrors()
        {
            return m_Errors.Select(e => e.ToString());
        }
    }
}
