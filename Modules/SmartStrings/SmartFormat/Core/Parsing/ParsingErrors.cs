// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Unity.SmartStrings.Pooling.SmartPools;

namespace Unity.SmartStrings.Core.Parsing;

/// <summary>
/// Represents parsing errors in a format string.
/// This exception only gets thrown when Parser.ErrorAction is set to ThrowError.
/// </summary>
[Serializable]
public class ParsingErrors : Exception   //NOSONAR
{
    Format m_Result;

    /// <summary>
    /// Creates a new instance for object pooling.
    /// Immediately after creating the instance, an overload of 'Initialize' must be called.
    /// </summary>
    public ParsingErrors()
    {
    }

    /// <summary>
    /// Initializes the instance of <see cref="ParsingErrors"/>.
    /// </summary>
    /// <param name="result"><see cref="Format"/> that caused the error.</param>
    /// <returns>This <see cref="ParsingErrors"/> instance.</returns>
    public ParsingErrors Initialize(Format result)
    {
        m_Result = result;
        return this;
    }

    /// <summary>
    /// Clears the <see cref="Issues"/> list.
    /// This method gets called by <see cref="ParsingErrorsPool"/> when it releases an instance.
    /// </summary>
    public void Clear()
    {
        Issues.Clear();
    }

    /// <summary>
    /// Gets an <see cref="IList{T}"/> of <see cref="ParsingIssue"/>s.
    /// </summary>
    public List<ParsingIssue> Issues { get; } = new();

    /// <summary>
    /// Returns <see langword="true"/> if the <see cref="IList{T}"/> of <see cref="ParsingIssue"/>s contains elements.
    /// </summary>
    public bool HasIssues => Issues.Count > 0;

    /// <summary>
    /// Gets the short version of an error message.
    /// </summary>
    public string MessageShort => $"The format string has {Issues.Count} issue{(Issues.Count == 1 ? string.Empty : "s")}: {FormatIssues()}";

    /// <summary>
    /// Gets the long version of an error message.
    /// </summary>
    public override string Message
    {
        get
        {
            var arrows = new StringBuilder();
            var lastArrow = 0;
            foreach (var issue in Issues)
            {
                arrows.Append(new string('-', issue.Index - lastArrow));
                if (issue.Length > 0)
                {
                    arrows.Append(new string('^', Math.Max(issue.Length, 1)));
                    lastArrow = issue.Index + issue.Length;
                }
                else
                {
                    arrows.Append('^');
                    lastArrow = issue.Index + 1;
                }
            }

            return $"The format string has {Issues.Count} issue{(Issues.Count == 1 ? string.Empty : "s")}:\n{FormatIssues()}\nIn: \"{m_Result.BaseString}\"\nAt:  {arrows} ";
        }
    }

    string FormatIssues()
    {
        var issues = new string[Issues.Count];
        for (var i = 0; i < Issues.Count; i++)
            issues[i] = Issues[i].Issue;
        return string.Join(", ", issues);
    }

    ///<inheritdoc/>
    protected ParsingErrors(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    /// <summary>
    /// Adds a new <see cref="ParsingIssue"/>.
    /// </summary>
    /// <param name="parent"><see cref="Format"/> that contains the issue.</param>
    /// <param name="issue">Description of the issue.</param>
    /// <param name="startIndex">Start index of the issue within the format string.</param>
    /// <param name="endIndex">End index of the issue within the format string.</param>
    public void AddIssue(Format parent, string issue, int startIndex, int endIndex)
    {
        Issues.Add(new ParsingIssue(issue, startIndex, endIndex - startIndex));
    }

    /// <summary>
    /// Represents a single parsing issue in a format string.
    /// </summary>
    public class ParsingIssue
    {
        /// <summary>
        /// Creates a new instance of <see cref="ParsingIssue"/>.
        /// </summary>
        /// <param name="issue">Description of the issue.</param>
        /// <param name="index">Index within the format string where the issue occurred.</param>
        /// <param name="length">Number of characters affected, starting from the index.</param>
        public ParsingIssue(string issue, int index, int length)
        {
            Issue = issue;
            Index = index;
            Length = length;
        }

        /// <summary>
        /// Gets the index within the format string, where an error occurred.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Gets the length starting from the <see cref="Index"/> which has errors.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Gets the description of an error issue.
        /// </summary>
        public string Issue { get; }
    }
}
