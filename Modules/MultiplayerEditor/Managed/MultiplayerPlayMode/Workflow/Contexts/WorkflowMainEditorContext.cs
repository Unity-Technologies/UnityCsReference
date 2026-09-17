// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Multiplayer.PlayMode.Editor
{
    class WorkflowMainEditorContext
    {
        public WorkflowMainEditorContext(MainEditorContext mainEditorContext)
        {
            MainPlayerSystems = new MainPlayerSystems();
            {
                var workflow = new StandardMainEditorWorkflow();
                LogsRepository = new InMemoryRepository<PlayerIdentifier, BoxedLogCounts>();
#pragma warning disable UAL0018 // this context is only reachable through VirtualProjectWorkflow.s_WorkflowMainEditorContext, which is cleared on the same reload, so the stores it holds are rebuilt with it
                SystemDataStore = SystemDataStore.GetMain();
#pragma warning restore UAL0018
                ProjectDataStore = ProjectDataStore.GetMain();
                workflow.Initialize(mppmContext: this, vpContext: mainEditorContext);
            }
            MainPlayerSystems.Listen(mppmContext: this, vpContext: mainEditorContext);
        }

        internal InMemoryRepository<PlayerIdentifier, BoxedLogCounts> LogsRepository { get; }
        internal ProjectDataStore ProjectDataStore { get; }
        internal SystemDataStore SystemDataStore { get; }

        internal MainPlayerSystems MainPlayerSystems { get; }
        public TestResultMessage TestFailure { get; set; }
    }
}
