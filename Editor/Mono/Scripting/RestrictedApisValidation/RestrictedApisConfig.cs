// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using UnityEngine.Scripting;

namespace UnityEditor.Mono.Scripting.RestrictedApisValidation;

[RequiredByNativeCode]
record struct RestrictedApiConfigEntry(RestrictedApiSeverity Severity, string Description, string DocumentationUrl);

class RestrictedApisConfig
{
    const string RestrictedApiConfigFileName = "RestrictedApis.txt";

    readonly Dictionary<int, RestrictedApiConfigEntry> _restrictedMembersConfig = new();
    readonly Dictionary<int, RestrictedApiConfigEntry> _restrictedTypesConfig = new();

    internal List<string> Messages { get;  } = new();

    internal static RestrictedApisConfig Load(string userConfigPath)
    {
        var instance = new RestrictedApisConfig();
        instance.LoadCore(userConfigPath);
        return instance;
    }

    internal int TotalConfiguredApis => _restrictedMembersConfig.Count + _restrictedTypesConfig.Count;

    void LoadCore(string userConfigPath)
    {
        var lines = ReadConfigLines();
        foreach (var line in lines)
        {
            ProcessLine(line);
        }

        string[] ReadConfigLines()
        {
            var configurationFilePath = Path.Combine("ProjectSettings", RestrictedApiConfigFileName);
            if (!File.Exists(configurationFilePath))
            {
                configurationFilePath = Path.Combine(userConfigPath, RestrictedApiConfigFileName);
                if (!File.Exists(configurationFilePath))
                {
                    return Array.Empty<string>();
                }
            }

            try
            {
                Messages.Add($"Using restricted APIs configuration from '{configurationFilePath}'.");
                return File.ReadAllLines(configurationFilePath);
            }
            catch (IOException ioe)
            {
                Messages.Add($"Unable to read restricted APIs validation configuration file '{configurationFilePath}'; Validation skipped.\nException: {ioe}.");
                return Array.Empty<string>();
            }
        }
    }

    // Parses lines in format:
    // signature | severity [ | description | doc url]
    // comments starts with #
    void ProcessLine(string line)
    {
        var lineSpan = line.AsSpan().Trim();
        if (lineSpan.Length == 0)
            return;

        var commentIndex = lineSpan.IndexOf('#');
        if (commentIndex != -1)
            lineSpan = lineSpan[..commentIndex].Trim();

        if (lineSpan.Length == 0)
            return;

        var signature = NextToken(ref lineSpan, out var separatorIndex);
        if (separatorIndex == -1)
            throw new Exception($"Invalid signature format: {line}");

        var severity = NextToken(ref lineSpan, out separatorIndex);
        var description = "";
        var documentationUrl = "";
        if (separatorIndex != -1)
        {
            description = NextToken(ref lineSpan,  out separatorIndex).ToString();
            if (separatorIndex != -1)
            {
                documentationUrl = NextToken(ref lineSpan,  out separatorIndex).ToString();
            }
        }

        // member signatures must have a :: as a declaring type name/member name separator
        var targetContainer = line.AsSpan().IndexOf("::") == -1 ?  _restrictedTypesConfig :  _restrictedMembersConfig;

        //TODO: When we switch from netstandard to *any* .NET BCL, remove the ToString() and call String.GetHashCode(signature) : https://learn.microsoft.com/en-us/dotnet/api/system.string.gethashcode?view=net-9.0#system-string-gethashcode(system-readonlyspan((system-char)))
        targetContainer[signature.ToString().GetHashCode()] = new RestrictedApiConfigEntry(Enum.Parse<RestrictedApiSeverity>(severity.ToString(), ignoreCase: true), description, documentationUrl);

        static ReadOnlySpan<char> NextToken(ref ReadOnlySpan<char> data, out int separatorIndex)
        {
            separatorIndex = data.IndexOf('|');
            if (separatorIndex == -1)
                return data;

            var toReturn = data.Slice(0, separatorIndex).Trim();
            data = data.Slice(separatorIndex + 1);
            return toReturn;
        }
    }

    public RestrictedApiConfigEntry RestrictedApiSeverityFor(MemberReference memberReference)
    {
        return _restrictedMembersConfig.GetValueOrDefault(memberReference.FullName.GetHashCode(), new RestrictedApiConfigEntry { Severity = RestrictedApiSeverity.Hidden } );
    }

    public RestrictedApiConfigEntry RestrictedApiSeverityFor(TypeReference typeReference)
    {
        return _restrictedTypesConfig.GetValueOrDefault(typeReference.FullName.GetHashCode(), new RestrictedApiConfigEntry { Severity = RestrictedApiSeverity.Hidden } );
    }
}
