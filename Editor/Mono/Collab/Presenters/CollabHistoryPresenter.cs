// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Connect;
using UnityEditor.Web;

namespace UnityEditor.Collaboration
{
    internal class CollabHistoryPresenter
    {
        const int k_ItemsPerPage = 5;
        ICollabHistoryWindow m_Window;
        ICollabHistoryItemFactory m_Factory;
        IRevisionsService m_Service;
        ConnectInfo m_ConnectState;
        CollabInfo m_CollabState;
        bool m_IsCollabError;
        int m_TotalRevisions;
        int m_CurrentPage;
        BuildAccess m_BuildAccess;
        string m_ProgressRevision;
        public bool BuildServiceEnabled {get; set; }

        public CollabHistoryPresenter(ICollabHistoryWindow window, ICollabHistoryItemFactory factory, IRevisionsService service)
        {
            m_Window = window;
            m_Factory = factory;
            m_Service = service;
            m_CurrentPage = 0;
            m_BuildAccess = new BuildAccess();
        }

        public void OnWindowEnabled()
        {
            UnityConnect.instance.StateChanged += OnConnectStateChanged;
            Collab.instance.StateChanged += OnCollabStateChanged;
            Collab.instance.RevisionUpdated += OnCollabRevisionUpdated;
            Collab.instance.JobsCompleted += OnCollabJobsCompleted;
            Collab.instance.ErrorOccurred += OnCollabError;
            Collab.instance.ErrorCleared += OnCollabErrorCleared;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            if (Collab.instance.IsConnected())
            {
                m_ConnectState = UnityConnect.instance.GetConnectInfo();
                m_CollabState = Collab.instance.GetCollabInfo();
            }

            m_Window.revisionActionsEnabled = !EditorApplication.isPlayingOrWillChangePlaymode;

            // Setup window callbacks
            m_Window.OnPageChangeAction = OnUpdatePage;
            m_Window.OnUpdateAction = OnUpdate;
            m_Window.OnRestoreAction = OnRestore;
            m_Window.OnGoBackAction = OnGoBack;
            m_Window.OnShowBuildAction = ShowBuildForCommit;
            m_Window.OnShowServicesAction = ShowServicePage;
            m_Window.itemsPerPage = k_ItemsPerPage;

            // Initialize data
            UpdateBuildServiceStatus();
            var state = RecalculateState();
            // Only try to load the page if we're ready
            if (state == HistoryState.Ready)
                OnUpdatePage(m_CurrentPage);
            m_Window.UpdateState(state, true);
        }

        public void OnWindowDisabled()
        {
            UnityConnect.instance.StateChanged -= OnConnectStateChanged;
            Collab.instance.StateChanged -= OnCollabStateChanged;
            Collab.instance.RevisionUpdated -= OnCollabRevisionUpdated;
            Collab.instance.JobsCompleted -= OnCollabJobsCompleted;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnConnectStateChanged(ConnectInfo state)
        {
            /* bool initialized
               bool ready
               bool online
               bool loggedIn
               bool workOffline
               bool showLoginWindow
               bool error
               string lastErrorMsg
               bool maintenance
            */
            m_ConnectState = state;

            m_Window.UpdateState(RecalculateState(), false);
        }

        private void OnCollabStateChanged(CollabInfo state)
        {
            /* bool ready
               bool update
               bool publish
               bool inProgress
               bool maintenance
               bool conflict
               bool dirty
               bool refresh
               bool seat
               string tip
            */

            // Sometimes a collab state change will trigger even though everything is the same
            if (CollabStatesEqual(m_CollabState, state))
                return;

            if (m_CollabState.tip != state.tip)
                OnUpdatePage(m_CurrentPage);

            m_CollabState = state;
            m_Window.UpdateState(RecalculateState(), false);
            if (state.inProgress)
            {
                m_Window.inProgressRevision = m_ProgressRevision;
            }
            else
            {
                m_Window.inProgressRevision = null;
            }
        }

        private void OnCollabRevisionUpdated(CollabInfo state)
        {
            OnUpdatePage(m_CurrentPage);
        }

        private void OnCollabJobsCompleted(CollabInfo state)
        {
            m_ProgressRevision = null;
        }

        private void OnCollabError()
        {
            m_IsCollabError = true;
            m_Window.UpdateState(RecalculateState(), false);
        }

        private void OnCollabErrorCleared()
        {
            m_IsCollabError = false;
            m_Window.UpdateState(RecalculateState(), false);
        }

        private void OnPlayModeStateChanged(PlayModeStateChange stateChange)
        {
            // If entering play mode, disable
            if (stateChange == PlayModeStateChange.ExitingEditMode ||
                stateChange == PlayModeStateChange.EnteredPlayMode)
            {
                m_Window.revisionActionsEnabled = false;
            }
            // If exiting play mode, enable!
            else if (stateChange == PlayModeStateChange.EnteredEditMode ||
                     stateChange == PlayModeStateChange.ExitingPlayMode)
            {
                m_Window.revisionActionsEnabled = true;
            }
        }

        private HistoryState RecalculateState()
        {
            if (!m_ConnectState.online)
                return HistoryState.Offline;
            if (m_ConnectState.maintenance || m_CollabState.maintenance)
                return HistoryState.Maintenance;
            if (!m_ConnectState.loggedIn)
                return HistoryState.LoggedOut;
            if (!m_CollabState.seat)
                return HistoryState.NoSeat;
            if (!Collab.instance.IsCollabEnabledForCurrentProject())
                return HistoryState.Disabled;
            if (!Collab.instance.IsConnected() || !m_CollabState.ready)
                return HistoryState.Waiting;
            if (m_ConnectState.error || m_IsCollabError)
                return HistoryState.Error;

            return HistoryState.Ready;
        }

        private static bool CollabStatesEqual(CollabInfo c1, CollabInfo c2)
        {
            return c1.ready == c2.ready &&
                c1.update == c2.update &&
                c1.publish == c2.publish &&
                c1.inProgress == c2.inProgress &&
                c1.maintenance == c2.maintenance &&
                c1.conflict == c2.conflict &&
                c1.dirty == c2.dirty &&
                c1.refresh == c2.refresh &&
                c1.seat == c2.seat &&
                c1.tip == c2.tip;
        }

        // TODO: Eventually this can be a listener on the build service status
        public void UpdateBuildServiceStatus()
        {
            foreach (var service in UnityConnectServiceCollection.instance.GetAllServiceInfos())
            {
                if (service.name.Equals("Build"))
                {
                    BuildServiceEnabled = service.enabled;
                }
            }
        }

        public void ShowBuildForCommit(string revisionID)
        {
            m_BuildAccess.ShowBuildForCommit(revisionID);
        }

        public void ShowServicePage()
        {
            m_BuildAccess.ShowServicePage();
        }

        public void OnUpdatePage(int page)
        {
            var revs = m_Service.GetRevisions(page * k_ItemsPerPage, k_ItemsPerPage);
            m_TotalRevisions = revs.RevisionsInRepo;
            var items = m_Factory.GenerateElements(revs.Revisions, m_TotalRevisions, page * k_ItemsPerPage, m_Service.tipRevision, m_Window.inProgressRevision,
                    m_Window.revisionActionsEnabled, BuildServiceEnabled, m_Service.currentUser);
            m_Window.UpdateRevisions(items, m_Service.tipRevision, m_TotalRevisions);
        }

        private void OnRestore(string revisionId, bool updatetorevision)
        {
            m_ProgressRevision = revisionId;
            Collab.instance.ResyncToRevision(revisionId);
        }

        private void OnGoBack(string revisionId, bool updatetorevision)
        {
            m_ProgressRevision = revisionId;
            Collab.instance.GoBackToRevision(revisionId, false);
        }

        private void OnUpdate(string revisionId, bool updatetorevision)
        {
            m_ProgressRevision = revisionId;
            Collab.instance.Update(revisionId, updatetorevision);
        }
    }
}

