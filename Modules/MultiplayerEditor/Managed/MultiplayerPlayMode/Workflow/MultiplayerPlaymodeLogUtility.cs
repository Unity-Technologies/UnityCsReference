// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Multiplayer.PlayMode.Editor
{
    static class MultiplayerPlaymodeLogUtility
    {
        public static BoxedLogCounts PlayerLogs(PlayerIdentifier identifier)
        {
            var mppmContext = VirtualProjectWorkflow.WorkflowMainEditorContext;

            if (!mppmContext.LogsRepository.TryGetValue(identifier, out var playerLogs))
            {
                playerLogs = new BoxedLogCounts();
                mppmContext.LogsRepository.Create(identifier, playerLogs);
            }

            return playerLogs;
        }
    }
}
