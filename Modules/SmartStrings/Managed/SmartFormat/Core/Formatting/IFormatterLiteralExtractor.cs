// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.SmartStrings.Core.Extensions;

/// <summary>
/// Extracts the literal characters from a Format string
/// </summary>
public interface IFormatterLiteralExtractor
{
    /// <summary>
    /// Ignores the format arguments and writes every possible literal value.
    /// This is used to extract all possible values so that we can determine the distinct characters for font generation etc.
    /// </summary>
    /// <param name="formattingInfo">Formatting details, including the format and the target output.</param>
    void WriteAllLiterals(IFormattingInfo formattingInfo);
}
