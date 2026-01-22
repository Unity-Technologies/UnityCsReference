// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using UnityEngine.Scripting;

namespace UnityEditor.Mono.Scripting.RestrictedApisValidation;

[RequiredByNativeCode]
record struct RestrictedApiConfigEntry(int Id, RestrictedApiSeverity Severity, string Description, string DocumentationUrl)
{
    public bool IsValid => Description != null;
}

class RestrictedApisConfig
{
    sealed class PerAssemblyConfig
    {
        internal Dictionary<int, RestrictedApiConfigEntry> _restrictedMembersConfig = new();
        internal Dictionary<int, RestrictedApiConfigEntry> _restrictedTypesConfig = new();
    }

    record struct SignatureDetails(int HashCode, bool IsMember);

    const string RestrictedApiConfigFileName = "RestrictedApis.txt";

    Dictionary<string, PerAssemblyConfig> _perAssemblyConfig = new();

    Dictionary<int, RestrictedApiConfigEntry> _restrictedMembersConfig = new();
    Dictionary<int, RestrictedApiConfigEntry> _restrictedTypesConfig = new();
    Dictionary<int, SignatureDetails> _idToSignatureDetails = new();

    internal List<string> Messages { get;  } = new();

    internal static RestrictedApisConfig Load(string userConfigPath)
    {
        var instance = new RestrictedApisConfig();
        instance.LoadCore(userConfigPath);
        return instance;
    }

    internal static RestrictedApisConfig Load(StringReader userConfig)
    {
        var instance = new RestrictedApisConfig();
        instance.LoadFrom(userConfig);
        return instance;
    }

    internal int TotalConfiguredApis => _restrictedMembersConfig.Count + _restrictedTypesConfig.Count;

    void LoadCore(string userConfigPath)
    {
        var flags = new BitArray(8);
        ref Dictionary<int, RestrictedApiConfigEntry> currentTypesConfig = ref _restrictedTypesConfig;
        ref Dictionary<int, RestrictedApiConfigEntry> currentMembersConfig = ref _restrictedMembersConfig;

        var lines = ReadConfigLines();
        foreach (var line in lines)
        {
            ProcessLine(line, ref flags, ref currentTypesConfig, ref currentMembersConfig);
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

    void LoadFrom(StringReader userConfig)
    {
        var currentTypesConfig =  _restrictedTypesConfig;
        var currentMembersConfig = _restrictedMembersConfig;

        var flags = new BitArray(8);
        while (userConfig.ReadLine() is { } line)
        {
            ProcessLine(line, ref flags, ref currentTypesConfig, ref currentMembersConfig);
        }
    }

    // Parses lines in format:
    //     :id | signature | severity [ | description | doc url]
    //     :id | | severity [ | description | doc url] : override the severity for the signature specified previously with the same :id (note that signature has been omitted)
    //     @Assembly file name with extension (any configuration following this line applies only to the specified assembly)
    //     # comments starts with #
    void ProcessLine(string line, ref BitArray flags, ref Dictionary<int, RestrictedApiConfigEntry> currentTypesConfig, ref Dictionary<int, RestrictedApiConfigEntry> currentMembersConfig)
    {
        const byte PerAssemblyConfigFlag = 1;

        var lineSpan = line.AsSpan().Trim();
        if (lineSpan.Length == 0)
            return;

        var commentIndex = lineSpan.IndexOf('#');
        if (commentIndex != -1)
            lineSpan = lineSpan[..commentIndex].Trim();

        if (lineSpan.Length == 0)
            return;

        var token = NextToken(ref lineSpan, out var separatorIndex);
        if (separatorIndex == -1)
        {
            if (token[0] != '@')
                throw new Exception($"Invalid configuration line: '{line}'");

            if (flags.Get(PerAssemblyConfigFlag))
            {
                throw new Exception($"Two consecutive assembly names found; last one: '{line}'");
            }

            // Per-assembly configuration
            flags.Set(PerAssemblyConfigFlag, true);
            var assemblyPath = token.Slice(1).ToString();
            if (!_perAssemblyConfig.TryGetValue(assemblyPath, out var assemblySpecificConfigs))
            {
                assemblySpecificConfigs = new PerAssemblyConfig();
                _perAssemblyConfig.Add(assemblyPath, assemblySpecificConfigs);
            }

            currentMembersConfig = assemblySpecificConfigs._restrictedMembersConfig;
            currentTypesConfig = assemblySpecificConfigs._restrictedTypesConfig;
            return;
        }

        if (token[0] != ':')
        {
            // Invalid line
            throw new Exception($"Invalid configuration line: '{line}'");
        }

        flags.Set(PerAssemblyConfigFlag, false); // Reset per-assembly configuration flag

        var id = token.Slice(1);
        var signature = NextToken(ref lineSpan, out separatorIndex);
        if (separatorIndex == -1)
            throw new Exception($"Invalid severity '' in line '{line}'");

        var severitySpan = NextToken(ref lineSpan, out separatorIndex);
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

        if (!Int32.TryParse(id, out var intId))
        {
            throw new Exception($"Id must be a number (line: '{line}')");
        }

        int referenceHashCode;
        var isTypeSignature = false;
        if (signature.Length == 0)
        {
            if (!_idToSignatureDetails.TryGetValue(intId, out var previousMappedId))
                throw new Exception($"Id ({intId}) overriden in configuration line '{line}' is not defined by any previous lines.");

            (referenceHashCode, isTypeSignature)  = previousMappedId;
        }
        else
        {
            isTypeSignature = line.AsSpan().IndexOf("::") == -1; // member signatures must have a :: as a declaring type name/member name separator

            //TODO: When we switch from netstandard to *any* .NET BCL, remove the ToString() and call String.GetHashCode(signature) : https://learn.microsoft.com/en-us/dotnet/api/system.string.gethashcode?view=net-9.0#system-string-gethashcode(system-readonlyspan((system-char)))
            referenceHashCode = signature.ToString().GetHashCode();
            _idToSignatureDetails[intId] = new (referenceHashCode, isTypeSignature);
        }

        var targetContainer = isTypeSignature ?  currentTypesConfig :  currentMembersConfig;
        if (!Enum.TryParse<RestrictedApiSeverity>(severitySpan.ToString(), ignoreCase: true, out var severity))
        {
            throw new Exception($"Invalid severity '{severitySpan.ToString()}' in line '{line}'");
        }

        targetContainer[referenceHashCode] = new RestrictedApiConfigEntry(intId, severity, description, documentationUrl);

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
        var referencingAssemblyName = memberReference.Module.Name;
        var configs = _perAssemblyConfig.TryGetValue(referencingAssemblyName, out var assemblySpecificConfig)
                        ? assemblySpecificConfig._restrictedMembersConfig
                        : _restrictedMembersConfig;

        return configs.GetValueOrDefault(
                    memberReference.FullName.GetHashCode(),
                    new RestrictedApiConfigEntry { Severity = RestrictedApiSeverity.Hidden } );
    }

    public RestrictedApiConfigEntry RestrictedApiSeverityFor(TypeReference typeReference)
    {
        var referencingAssemblyName = typeReference.Module.Name;
        var configs = _perAssemblyConfig.TryGetValue(referencingAssemblyName, out var assemblySpecificConfig)
            ? assemblySpecificConfig._restrictedTypesConfig
            : _restrictedTypesConfig;

        return configs.GetValueOrDefault(typeReference.FullName.GetHashCode(), new RestrictedApiConfigEntry { Severity = RestrictedApiSeverity.Hidden } );
    }
}
