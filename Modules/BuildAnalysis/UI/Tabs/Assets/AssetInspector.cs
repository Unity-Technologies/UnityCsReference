// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    /// <summary>
    /// Right-docked side panel that shows properties of the asset currently selected in the
    /// Asset Table.
    /// </summary>
    internal class AssetInspector : VisualElement
    {
        private const string k_UxmlPath = "BuildAnalysis/UXML/AssetInspector.uxml";
        private const string k_EmptyClass = "asset-inspector--empty";

        private readonly Image m_HeaderIcon;
        private readonly Label m_HeaderName;
        private readonly Button m_SelectButton;
        private readonly Label m_OutputSizeValue;
        private readonly Label m_ImporterTypeValue;
        private readonly Label m_ObjectsCountValue;
        private readonly Label m_ResourcesFilesValue;
        private readonly Label m_AssetPathValue;
        private readonly VisualElement m_Root;

        private string m_CurrentPath;

        public AssetInspector()
        {
            var template = EditorGUIUtility.LoadRequired(k_UxmlPath) as VisualTreeAsset;
            template.CloneTree(this);

            m_Root = this.Q<VisualElement>("asset-inspector");
            m_HeaderIcon = this.Q<Image>("header-icon");
            m_HeaderName = this.Q<Label>("header-name");
            m_SelectButton = this.Q<Button>("select-button");
            m_OutputSizeValue = this.Q<Label>("output-size-value");
            m_ImporterTypeValue = this.Q<Label>("importer-type-value");
            m_ObjectsCountValue = this.Q<Label>("objects-count-value");
            m_ResourcesFilesValue = this.Q<Label>("resources-files-value");
            m_AssetPathValue = this.Q<Label>("asset-path-value");

            m_HeaderIcon.scaleMode = ScaleMode.ScaleToFit;

            m_SelectButton.clicked += OnSelectClicked;
        }

        /// <summary>
        /// Populate the inspector for the given asset, or clear it if <paramref name="asset"/> is null.
        /// <paramref name="importerType"/> is the resolved importer for the asset; pass null when
        /// the asset's <c>ImporterTypeId</c> doesn't resolve to a known type, and the Importer Type
        /// field renders empty.
        /// </summary>
        public void SetAsset(BuildAnalysisAsset? asset, BuildAnalysisImporterType? importerType)
        {
            if (!asset.HasValue)
            {
                Reset();
                return;
            }

            var a = asset.Value;
            m_Root.RemoveFromClassList(k_EmptyClass);

            var path = a.Path ?? string.Empty;
            m_CurrentPath = path;
            m_HeaderIcon.image = IconUtility.GetAssetIcon(path);
            m_HeaderName.text = Path.GetFileNameWithoutExtension(path);

            m_OutputSizeValue.text = FormatUtility.FormatSize(a.OutputSizeBytes);
            m_ImporterTypeValue.text = importerType?.Name ?? string.Empty;
            m_ObjectsCountValue.text = a.ObjectCount.ToString();
            m_ResourcesFilesValue.text = a.ResourceCount.ToString();
            m_AssetPathValue.text = path;
            m_AssetPathValue.tooltip = path;
        }

        private void Reset()
        {
            m_Root.AddToClassList(k_EmptyClass);
            m_CurrentPath = string.Empty;
            m_HeaderIcon.image = null;
            m_HeaderName.text = string.Empty;
            m_OutputSizeValue.text = string.Empty;
            m_ImporterTypeValue.text = string.Empty;
            m_ObjectsCountValue.text = string.Empty;
            m_ResourcesFilesValue.text = string.Empty;
            m_AssetPathValue.text = string.Empty;
            m_AssetPathValue.tooltip = string.Empty;
        }

        private void OnSelectClicked()
        {
            AssetActions.ShowInProject(m_CurrentPath);
        }
    }
}
