// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Globalization;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderVariantAnalyser
{
    [UxmlElement]
    partial class OverviewPanel : PanelBase
    {
        [System.Serializable]
        public class Data : IPanelData
        {
            public string Caption => "Overview";

            public PanelType Panel => PanelType.Overview;
        }

        readonly Label m_ShadersCount;
        readonly Label m_VariantsCount;
        readonly Label m_MaterialsCount;
        readonly IndexListView m_ShadersList;
        readonly IndexListView m_MaterialsList;

        IFilteredList<IndexEntryRef> m_Shaders;
        IFilteredList<IndexEntryRef> m_Materials;

        Data m_Data;

        public event System.Action<bool> OnOpenIndexClicked;
        public event System.Action<bool, int> OnItemSelected;

        static VisualTreeAsset UxmlAsset => (VisualTreeAsset)EditorGUIUtility.Load("ShaderVariantAnalyser/OverviewPanel.uxml");

        public OverviewPanel()
        {
            UxmlAsset.CloneTree(this);

            m_ShadersCount = this.Q<Label>("shader-count");
            m_VariantsCount = this.Q<Label>("variant-count");
            m_ShadersList = this.Q<IndexListView>("shader-list");
            m_ShadersList.OnItemSelected += (idx) => OnItemSelected?.Invoke(true, m_Shaders.ToRawIndex(idx));

            var viewShadersButton = this.Q<Button>("shaders-button");
            viewShadersButton.clicked += () => OnOpenIndexClicked?.Invoke(true);

            m_MaterialsCount = this.Q<Label>("material-count");
            m_MaterialsList = this.Q<IndexListView>("material-list");
            m_MaterialsList.OnItemSelected += (idx) => OnItemSelected?.Invoke(false, m_Materials.ToRawIndex(idx));

            var viewMaterialsButton = this.Q<Button>("materials-button");
            viewMaterialsButton.clicked += () => OnOpenIndexClicked?.Invoke(false);
        }

        public IPanelData BindData(DataReader reader)
        {
            var data = new Data();
            if (BindData(reader, data))
                return data;
            return null;
        }
        public override bool BindData(DataReader reader, IPanelData data)
        {
            var casted = data as Data;
            if (casted == null)
                return false;
            return BindData(reader, casted);
        }

        public bool BindData(DataReader reader, Data data)
        {
            m_Data = data;

            var stats = reader.Stats;
            m_ShadersCount.text = stats.shadersCount.ToString("N0", CultureInfo.CurrentCulture);
            m_VariantsCount.text = stats.variantsCount.ToString("N0", CultureInfo.CurrentCulture);
            m_MaterialsCount.text = stats.materialsCount.ToString("N0", CultureInfo.CurrentCulture);

            m_Shaders = reader.OverviewShaders;
            m_ShadersList.BindData(m_Shaders, true);

            m_Materials = reader.OverviewMaterials;
            m_MaterialsList.BindData(m_Materials, false);

            return true;
        }

        public override void Unbind()
        {
            m_Data = null;
            m_Shaders = null;
            m_Materials = null;

            m_ShadersList.Unbind();
            m_MaterialsList.Unbind();
        }
    }
}
