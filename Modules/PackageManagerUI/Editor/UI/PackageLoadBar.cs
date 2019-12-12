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

        private int m_Total;
        private int m_NumberOfPackagesShown;
        private const int k_Min = 25;
        private const int k_Max = 100;
        private const int k_MinMaxDifference = 25;

        private int m_Min;
        private int m_Max;
        private string m_LoadedText;
        private bool m_DoShowMinLabel;
        private bool m_DoShowMaxLabel;
        private bool m_DoShowLoadMoreLabel;

        public PackageLoadBar()
        {
            var root = Resources.GetTemplate("PackageLoadBar.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            m_DoShowMinLabel = true;
            m_DoShowMaxLabel = true;
            m_DoShowLoadMoreLabel = true;

            loadMinLabel.OnLeftClick(LoadMinItemsClicked);
            loadMaxLabel.OnLeftClick(LoadMaxItemsClicked);
        }

        public void OnEnable()
        {
            PageManager.instance.onRefreshOperationFinish += OnRefreshOperationFinish;
            AssetStoreClient.instance.onProductListFetched += OnProductListFetched;
        }

        private void OnRefreshOperationFinish()
        {
            loadMinLabel.SetEnabled(true);
            loadMaxLabel.SetEnabled(true);
            UpdateLoadBarMessage();
        }

        public void OnDisable()
        {
            PageManager.instance.onRefreshOperationFinish -= OnRefreshOperationFinish;
            AssetStoreClient.instance.onProductListFetched -= OnProductListFetched;
        }

        internal void Reset()
        {
            m_NumberOfPackagesShown = 0;
            m_Total = 0;
            m_DoShowMinLabel = true;
            m_DoShowMaxLabel = true;
            m_DoShowLoadMoreLabel = true;
        }

        public void OnProductListFetched(AssetStorePurchases assetStorePurchases, bool fetchDetailsCalled)
        {
            if (m_Total != (int)assetStorePurchases.total)
                m_Total = (int)assetStorePurchases.total;

            // This is a tweak, waiting for the filter to get the value from page
            if (m_NumberOfPackagesShown == 0)
                m_NumberOfPackagesShown = assetStorePurchases.list.Count();
            else if (m_NumberOfPackagesShown != 0 && m_NumberOfPackagesShown != m_Total)
                m_NumberOfPackagesShown += assetStorePurchases.list.Count();

            UpdateLoadBarMessage();
        }

        public void LoadMinItemsClicked()
        {
            loadMinLabel.SetEnabled(false);
            loadMaxLabel.SetEnabled(false);
            PageManager.instance.LoadMore(m_Min);
            UpdateLoadBarMessage();
        }

        public void LoadMaxItemsClicked()
        {
            loadMinLabel.SetEnabled(false);
            loadMaxLabel.SetEnabled(false);
            PageManager.instance.LoadMore(m_Max);
            UpdateLoadBarMessage();
        }

        private void UpdateLoadBarMessage()
        {
            if (m_Total == 0 || m_NumberOfPackagesShown == 0)
            {
                UIUtils.SetElementDisplay(loadBarContainer, false);
                return;
            }

            if (m_Total == m_NumberOfPackagesShown)
            {
                m_DoShowMinLabel = false;
                m_DoShowMaxLabel = false;
                m_DoShowLoadMoreLabel = false;
                m_LoadedText = string.Format(L10n.Tr("All {0} packages shown"), m_NumberOfPackagesShown);
            }
            else
            {
                int diff = m_Total - m_NumberOfPackagesShown;

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
                m_LoadedText = string.Format(L10n.Tr("{0} of {1} packages"), m_NumberOfPackagesShown, m_Total);
            }
            SetLabels();
        }

        private void SetLabels()
        {
            loadedLabel.text = m_LoadedText;
            loadMinLabel.text = m_Min.ToString();
            loadMaxLabel.text = m_Max.ToString();
            loadMoreLabel.text = L10n.Tr("Load next");

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
