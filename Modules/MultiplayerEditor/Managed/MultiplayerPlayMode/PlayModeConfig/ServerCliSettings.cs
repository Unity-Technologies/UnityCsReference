// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor;

[Serializable]
struct ServerCliSettings
{
    [Tooltip("Use the default arguments for this build profile.")]
    public bool UseDefaultArguments;

    public ushort Port;

    [Tooltip("The protocol used to query information from a server.")]
    public SimulatorSettings.ProtocolType QueryProtocol;

    public ushort QueryPort;

    // These default values match the values used in the Dedidicated Server package.
    internal static ServerCliSettings Default => new()
    {
        UseDefaultArguments = true,
        Port = 7777,
        QueryProtocol = SimulatorSettings.ProtocolType.SQP,
        QueryPort = 20000
    };

    internal void ComputeFinalArguments(out ushort port, out SimulatorSettings.ProtocolType queryType, out ushort queryPort)
    {
        if (UseDefaultArguments)
        {
            port = ushort.TryParse(GetDefaultArgumentString("port", Default.Port.ToString()), out var p) ? p : Default.Port;
            queryType = Enum.Parse<SimulatorSettings.ProtocolType>(GetDefaultArgumentString("querytype", Default.QueryProtocol.ToString().ToLowerInvariant()), true);
            queryPort = ushort.TryParse(GetDefaultArgumentString("queryport", Default.QueryPort.ToString()), out var qp) ? qp : Default.QueryPort;
            return;
        }

        port = Port;
        queryType = QueryProtocol;
        queryPort = QueryPort;
        return;
    }

    private static string GetDefaultArgumentString(string argumentName, string defaultValue)
    {
        var value = EditorUserBuildSettings.GetPlatformSettings(NamedBuildTarget.Server.TargetName, $"arg-default-{argumentName}");
        if (string.IsNullOrEmpty(value))
            return defaultValue;

        return value;
    }
}
