// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor;

[Serializable]
class StartProcessNode : Node
{
    [SerializeReference] private NodeInput<string> m_ExecutablePath;
    [SerializeReference] private NodeInput<string> m_Arguments;
    [SerializeReference] private NodeOutput<int> m_ProcessId;

    public NodeInput<string> ExecutablePath => m_ExecutablePath;
    public NodeInput<string> Arguments => m_Arguments;
    public NodeOutput<int> ProcessId => m_ProcessId;

    public StartProcessNode(string name) : base(name)
    {
        m_ExecutablePath = new(this);
        m_Arguments = new(this);
        m_ProcessId = new(this);
    }

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var process = StartProcess(GetInput(ExecutablePath), GetInput(Arguments));
        SetOutput(m_ProcessId, process.Id);
        return Task.CompletedTask;
    }

    Process StartProcess(string executablePath, string arguments)
    {
        DebugUtils.Trace($"Starting {executablePath}");

        if (!File.Exists(executablePath))
            throw new FileNotFoundException($"Process executable not found ({executablePath})");

        var process = new Process { EnableRaisingEvents = true };

        process.StartInfo.FileName = executablePath;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.WorkingDirectory = Path.GetDirectoryName(executablePath);
        // The Process object must have the UseShellExecute property set to false in order to use environment variables.
        // If not, throws a InvalidOperationException upon start.
        process.StartInfo.UseShellExecute = false;

        process.Start();
        if (process.HasExited)
        {
            throw new Exception("Process exited immediately, likely caused by an issue with the executable.");
        }

        DebugUtils.Trace($"Process '{executablePath}' launched [Process id:{process.Id} ]");

        return process;
    }
}
