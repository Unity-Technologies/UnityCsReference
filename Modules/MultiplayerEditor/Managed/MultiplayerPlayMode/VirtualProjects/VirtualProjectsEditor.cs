// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Multiplayer.PlayMode.Editor
{
    static class VirtualProjectsEditor
    {
        public static bool IsClone
            => CommandLineParameters.ReadIsClone();

        public static VirtualProjectIdentifier CloneIdentifier
            => CommandLineParameters.ReadVirtualProjectIdentifier();

        public static bool IsScenarioClone
            => CommandLineParameters.ReadIsScenarioClone();

        public static string MainEditorProcessId
            => CommandLineParameters.ReadIsClone()
                ? CommandLineParameters.ReadMainProcessId()
                : System.Diagnostics.Process.GetCurrentProcess().Id.ToString();
    }
}
