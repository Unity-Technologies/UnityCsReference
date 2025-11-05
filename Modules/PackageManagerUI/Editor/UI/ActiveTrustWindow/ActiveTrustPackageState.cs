// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class ActiveTrustPackageState: VisualElement
    {
        private IResourceLoader m_ResourceLoader;
        private void ResolveDependencies(IResourceLoader resourceLoader)
        {
            m_ResourceLoader = resourceLoader;
        }

        public ActiveTrustPackageState(IResourceLoader resourceLoader, string headerText, Icon icon, PackageInfo[] items, bool isOrgKnown = true)
        {
            ResolveDependencies(resourceLoader);
            Init(headerText, icon, items, isOrgKnown);
        }

        private void Init(string headerText, Icon icon, PackageInfo[] items, bool isOrgKnown)
        {
            var root = m_ResourceLoader.GetTemplate("ActiveTrustPackageState.uxml");
            cache = new VisualElementCache(root);

            stateIcon.AddToClassList(icon.ClassName());
            stateLabel.text = headerText;

            packageMultiColumnListView.reorderable = false;
            packageMultiColumnListView.showAlternatingRowBackgrounds = AlternatingRowBackground.None;
            packageMultiColumnListView.selectionType = SelectionType.None;
            packageMultiColumnListView.itemsSource = items;
            packageMultiColumnListView.fixedItemHeight = 20;
            packageMultiColumnListView.columns.reorderable = false;
            packageMultiColumnListView.columns.resizable = false;
            packageMultiColumnListView.columns["displayName"].bindCell = (label, i) => ((Label)label).text = string.IsNullOrEmpty(items[i].displayName) ? items[i].name : items[i].displayName;
            packageMultiColumnListView.columns["displayName"].stretchable = true; // We can't use flex-grow for this element so we must set it here
            packageMultiColumnListView.columns["orgName"].bindCell = (label, i) => ((Label)label).text = string.IsNullOrEmpty(items[i].author?.name) ? L10n.Tr("Unknown") : items[i].author.name;
            packageMultiColumnListView.columns["technicalName"].bindCell = (label, i) => ((Label)label).text = items[i].name;
            packageMultiColumnListView.columns["source"].bindCell = (e, i) =>
            {
                var label = (Label)e;
                switch (items[i].source)
                {
                    case PackageSource.LocalTarball:
                        label.text = L10n.Tr("Tarball");
                        break;
                    case PackageSource.BuiltIn:
                        label.text = L10n.Tr("Built-in");
                        break;
                    case PackageSource.Embedded:
                        label.text = L10n.Tr("Custom");
                        break;
                    case PackageSource.Registry:
                        label.text = string.IsNullOrEmpty(items[i].registry?.name) ? L10n.Tr("Registry") : items[i].registry.name;
                        break;
                    default:
                        label.text = items[i].source.ToString();
                        break;
                }
            };
            packageMultiColumnListView.columns["version"].bindCell = (e, i) => BindVersionCell(items[i], e as Label);

            if (!isOrgKnown)
                packageMultiColumnListView.columns["orgName"].title = L10n.Tr("Author");

            Add(root);
        }

        private void BindVersionCell(PackageInfo packageInfo, Label label)
        {
            if (!SemVersionParser.TryParse(packageInfo.version, out var result))
                return;

            if (result?.GetExpOrPreOrReleaseTag() == PackageTag.Experimental)
            {
                var packageTag = new PackageSimpleTagLabel(PackageTag.Experimental, L10n.Tr("Exp"));
                packageTag.AddToClassList("Experimental");
                label.parent.Add(packageTag);
                label.style.display = DisplayStyle.None;
            }
            else if (result?.GetExpOrPreOrReleaseTag() == PackageTag.PreRelease)
            {
                var packageTag = new PackageSimpleTagLabel(PackageTag.PreRelease, L10n.Tr("Pre"));
                packageTag.AddToClassList("PreRelease");
                label.parent.Add(packageTag);
                label.style.display = DisplayStyle.None;
            }
            else
            {
                label.text = packageInfo.version;
            }
        }

        private VisualElementCache cache { get; set; }
        private MultiColumnListView packageMultiColumnListView => cache.Get<MultiColumnListView>("packageMultiColumnListView");
        private VisualElement stateIcon => cache.Get<VisualElement>("stateIcon");
        private Label stateLabel => cache.Get<Label>("stateLabel");
    }
}
