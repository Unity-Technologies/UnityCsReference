// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor
{
    // Extension methods for ProjectIssues which don't form part of the API: Used in UI, Tests, and HTML/CSV exporters
    internal static class ProjectIssueExtensions
    {
        internal const string k_NotAvailable = "N/A";

        // -2 because we're not interested in "None" or "All"
        static readonly int s_NumAreaEnumValues = Enum.GetNames(typeof(Areas)).Length - 2;

        // Map of category+format to custom comparison functions. See usage of AddCustomComparer in eg. AudioClipModule
        static readonly Dictionary<ulong, Func<ReportItem, ReportItem, int>> s_CustomComparers = new Dictionary<ulong, Func<ReportItem, ReportItem, int>>();

        public static void AddCustomComparer(IssueCategory category, PropertyType format, Func<ReportItem, ReportItem, int> customComparer)
        {
            // Create a single key by smooshing together the category and format
            ulong key = (ulong)category << 32 | (ulong)format;
            s_CustomComparers[key] = customComparer;
        }

        public static int CustomCompare(IssueCategory category, PropertyType format, ReportItem a, ReportItem b)
        {
            ulong key = (ulong)category << 32 | (ulong)format;
            if (s_CustomComparers.TryGetValue(key, out var comparer))
            {
                return comparer(a, b);
            }

            return int.MaxValue; // Return invalid result if we didn't find anything (should be -1, 1 or 0 normally)
        }

        public static string GetContext(this ReportItem issue)
        {
            if (issue.Dependencies == null)
                return issue.RelativePath;

            var root = issue.Dependencies;
            return root.Name;
        }

        public static string GetProperty(this ReportItem issue, PropertyType propertyType)
        {
            switch (propertyType)
            {
                case PropertyType.LogLevel:
                    return issue.LogLevel.ToString();
                case PropertyType.Severity:
                    return issue.Severity.ToFrontendString();
                case PropertyType.Areas:
                    return issue.Id.GetDescriptor().GetAreasSummary();
                case PropertyType.FileType:
                    if (issue.Location == null)
                        return k_NotAvailable;
                    return issue.Location.Extension;
                case PropertyType.Description:
                    return issue.Description;
                case PropertyType.Descriptor:
                    return issue.Id.GetDescriptor().Title;
                case PropertyType.Filename:
                    if (string.IsNullOrEmpty(issue.Filename))
                        return k_NotAvailable;
                    return issue.Location.FormattedFilename;
                case PropertyType.Path:
                    if (string.IsNullOrEmpty(issue.RelativePath))
                        return k_NotAvailable;
                    return issue.Location.FormattedPath;
                case PropertyType.Directory:
                    if (string.IsNullOrEmpty(issue.RelativePath))
                        return k_NotAvailable;
                    return PathUtils.GetDirectoryName(issue.Location.Path);
                case PropertyType.Platform:
                    return issue.Id.GetDescriptor().GetPlatformsSummary();
                case PropertyType.IsIgnored:
                    return issue.IsIgnored.ToString();
                default:
                    var propertyIndex = propertyType - PropertyType.Num;
                    return issue.GetCustomProperty(propertyIndex);
            }
        }

        public static string GetPropertyGroup(this ReportItem issue, PropertyDefinition propertyDefinition)
        {
            switch (propertyDefinition.Type)
            {
                case PropertyType.Filename:
                    if (string.IsNullOrEmpty(issue.Filename))
                        return k_NotAvailable;
                    return issue.Location.Filename;
                case PropertyType.Path:
                    if (string.IsNullOrEmpty(issue.RelativePath))
                        return k_NotAvailable;
                    return issue.Location.Path;
                default:
                    if (propertyDefinition.Format != PropertyFormat.String)
                        return string.Format("{0}: {1}", propertyDefinition.Name, issue.GetProperty(propertyDefinition.Type));
                    return issue.GetProperty(propertyDefinition.Type);
            }
        }

        internal static int CompareTo(this ReportItem issueA, ReportItem issueB, PropertyType propertyType)
        {
            if (issueA == null && issueB == null)
                return 0;
            if (issueA == null)
                return -1;
            if (issueB == null)
                return 1;

            switch (propertyType)
            {
                case PropertyType.LogLevel:
                    return ((int)issueA.LogLevel).CompareTo((int)issueB.LogLevel);
                case PropertyType.Severity:
                    return ((int)issueA.Severity).CompareTo((int)issueB.Severity);
                case PropertyType.Areas:
                    var areasA = (int)issueA.Id.GetDescriptor().Areas.Value;
                    var areasB = (int)issueB.Id.GetDescriptor().Areas.Value;

                    if (areasA == areasB)
                        return 0;

                    // Sort according to differences in the least significant bit
                    // (i.e. the smallest enum value, which is the one that comes first alphabetically)
                    for (int i = 0; i < s_NumAreaEnumValues; ++i)
                    {
                        var mask = 1 << i;
                        var c = (areasB & mask) - (areasA & mask);
                        if (c != 0)
                            return c;
                    }
                    return 0;
                case PropertyType.Description:
                    return EditorUtility.NaturalCompare(issueA.Description, issueB.Description);
                case PropertyType.FileType:
                    {
                        return EditorUtility.NaturalCompare(issueA.FileExtension, issueB.FileExtension);
                    }
                case PropertyType.Filename:
                    {
                        var cf = EditorUtility.NaturalCompare(issueA.Filename, issueB.Filename);

                        // If it's the same filename, see if the lines are different
                        if (cf == 0)
                            return issueA.Line.CompareTo(issueB.Line);

                        return cf;
                    }
                case PropertyType.Path:
                    var cp = EditorUtility.NaturalCompare(issueA.RelativePath ?? string.Empty, issueB.RelativePath ?? string.Empty);

                    // If it's the same path, see if the lines are different
                    if (cp == 0)
                        return issueA.Line.CompareTo(issueB.Line);

                    return cp;
                default:
                    if (propertyType >= PropertyType.Num)
                    {
                        // This is a custom property - see if we have a specialised comparison function.
                        var result = CustomCompare(issueA.Category, propertyType, issueA, issueB);
                        if (result < 2)
                            return result;
                    }

                    var propA = issueA.GetProperty(propertyType);
                    var propB = issueB.GetProperty(propertyType);

                    return StringCompareWithLongIntSupport(propA, propB);
            }
        }

        // Attempts parsing both strings as long integers before falling back to a standard case-ignoring string comparison
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int StringCompareWithLongIntSupport(string a, string b)
        {
            bool parsedA = long.TryParse(a, out var longA);
            bool parsedB = long.TryParse(b, out var longB);

            if (parsedA && parsedB)
                return longA < longB ? -1 : longA > longB ? 1 : 0;
            else
                return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}
