// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageLoadBar : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageLoadBar> {}

        private long m_Total;
        private long m_NumberOfPackagesShown;
        private const long k_Min = 25;
        private const long k_Max = 100;
        private const long k_MinMaxDifference = 25;

        private long m_Min;
        private long m_Max;
        private string m_LoadedText;
        private bool m_DoShowMinLabel;
        private bool m_DoShowMaxLabel;
        private bool m_DoShowLoadMoreLabel;
        private bool m_LoadMoreInProgress;

        private ResourceLoader m_ResourceLoader;
        private ApplicationProxy m_Application;
        private UnityConnectProxy m_UnityConnect;
        private PageManager m_PageManager;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_Application = container.Resolve<ApplicationProxy>();
            m_UnityConnect = container.Resolve<UnityConnectProxy>();
            m_PageManager = container.Resolve<PageManager>();
        }

        public PackageLoadBar()
        {
            ResolveDependencies();

            var root = m_ResourceLoader.GetTemplate("PackageLoadBar.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            loadMinLabel.OnLeftClick(LoadMinItemsClicked);
            loadMaxLabel.OnLeftClick(LoadMaxItemsClicked);
        }

        public void OnEnable()
        {
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;
            m_Application.onInternetReachabilityChange += OnInternetReachabilityChange;
            m_PageManager.onRefreshOperationFinish += Refresh;
            Refresh();

            loadMinLabel.SetEnabled(m_Application.isInternetReachable);
            loadMaxLabel.SetEnabled(m_Application.isInternetReachable);
        }

        public void OnDisable()
        {
            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;
            m_Application.onInternetReachabilityChange -= OnInternetReachabilityChange;

            m_PageManager.onRefreshOperationFinish -= Refresh;
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            UpdateLoadBarMessage();
        }

        private void OnInternetReachabilityChange(bool value)
        {
            loadMinLabel.SetEnabled(value && !m_LoadMoreInProgress);
            loadMaxLabel.SetEnabled(value && !m_LoadMoreInProgress);
        }

        public void Refresh()
        {
            var page = m_PageManager.GetCurrentPage();
            Set(page?.numTotalItems ?? 0, page?.numCurrentItems ?? 0);
        }

        internal void Set(long total, long current)
        {
            Reset();
            m_Total = total;
            m_NumberOfPackagesShown = current;

            loadMinLabel.SetEnabled(true);
            loadMaxLabel.SetEnabled(true);
            m_LoadMoreInProgress = false;
            UpdateLoadBarMessage();
        }

        internal void Reset()
        {
            m_DoShowMinLabel = true;
            m_DoShowMaxLabel = true;
            m_DoShowLoadMoreLabel = true;
        }

        public void LoadMinItemsClicked()
        {
            loadMinLabel.SetEnabled(false);
            loadMaxLabel.SetEnabled(false);
            m_LoadMoreInProgress = true;
            m_PageManager.LoadMore((int)m_Min);
        }

        public void LoadMaxItemsClicked()
        {
            loadMinLabel.SetEnabled(false);
            loadMaxLabel.SetEnabled(false);
            m_LoadMoreInProgress = true;
            m_PageManager.LoadMore((int)m_Max);
        }

        private void UpdateLoadBarMessage()
        {
            if (!m_UnityConnect.isUserLoggedIn || m_Total == 0 || m_NumberOfPackagesShown == 0)
            {
                UIUtils.SetElementDisplay(loadBarContainer, false);
                return;
            }

            if (m_Total == m_NumberOfPackagesShown)
            {
                m_DoShowMinLabel = false;
                m_DoShowMaxLabel = false;
                m_DoShowLoadMoreLabel = false;
                m_LoadedText = m_Total == 1 ? L10n.Tr("One package shown") : string.Format(L10n.Tr("All {0} packages shown"), m_NumberOfPackagesShown);
            }
            else
            {
                var diff = m_Total - m_NumberOfPackagesShown;

                if (diff >= k_Max)
                {
                    m_Min = k_Min;
                    m_Max = k_Max;
                }
                else // diff < max
                {
                    // If the difference between the min and the max is bigger than k_MinMaxDifference
                    // We show the two labels, else we only show that one label of the number of packages left
                    if (diff > k_Min && (diff - k_Min) > k_MinMaxDifference)
                    {
                        m_Min = k_Min;
                        m_Max = diff;
                    }
                    else if (diff > k_Min && (diff - k_Min) <= k_MinMaxDifference)
                    {
                        m_Min = k_Min;
                        m_Max = diff;
                        m_DoShowMinLabel = false;
                    }
                    else // diff <= min
                    {
                        m_Min = diff;
                        m_DoShowMaxLabel = false;
                    }
                }
                m_LoadedText = string.Format(L10n.Tr("{0} of {1}"), m_NumberOfPackagesShown, m_Total);
            }
            SetLabels();
        }

        private void SetLabels()
        {
            loadedLabel.text = m_LoadedText;
            loadMinLabel.text = m_Min.ToString();
            loadMaxLabel.text = m_Max.ToString();
            loadMoreLabel.text = L10n.Tr("Load");

            UIUtils.SetElementDisplay(loadMoreLabel, m_DoShowLoadMoreLabel);
            UIUtils.SetElementDisplay(loadMinLabel, m_DoShowMinLabel);
            UIUtils.SetElementDisplay(loadMaxLabel, m_DoShowMaxLabel);

            UIUtils.SetElementDisplay(loadBarContainer, true);
        }

        private VisualElementCache cache { get; set; }

        private Label loadedLabel { get { return cache.Get<Label>("loadedLabel"); } }
        private Label loadMoreLabel { get { return cache.Get<Label>("loadMoreLabel"); } }
        private Label loadMinLabel { get { return cache.Get<Label>("loadMinLabel"); } }
        private Label loadMaxLabel { get { return cache.Get<Label>("loadMaxLabel"); } }
        private VisualElement loadBarContainer { get { return cache.Get<VisualElement>("loadBarContainer"); } }
    }
}
