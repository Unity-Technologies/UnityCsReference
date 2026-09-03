// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: HeadlessRuntime not yet converted
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
                SystemDataStore = SystemDataStore.GetMain();
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
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
