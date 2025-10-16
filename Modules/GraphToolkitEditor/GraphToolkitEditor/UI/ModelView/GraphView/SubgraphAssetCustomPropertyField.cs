// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.GraphToolkit.CSO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    [UnityRestricted]
    internal class SubgraphAssetCustomPropertyField : ICustomPropertyFieldBuilder<SubgraphAssetProperty>
    {
        /// <summary>
        /// The USS class name added to a <see cref="SubgraphAssetCustomPropertyField"/>.
        /// </summary>
        public static readonly string ussClassName = "subgraph-asset-properties";

        /// <summary>
        /// The USS class name added to the button container of a <see cref="SubgraphAssetCustomPropertyField"/>.
        /// </summary>
        public static readonly string buttonContainerUSSClassName = ussClassName.WithUssElement("button-container");

        SubgraphAssetProperty m_SubgraphAssetProperty;
        ICommandTarget m_CommandTarget;
        GraphModel m_GraphModel;

        VisualElement m_Container;

        ObjectField m_ObjectField;
        Button m_SelectButton;
        Button m_OpenButton;

        public void SetMixed()
        {
            m_ObjectField.showMixedValue = true;
        }

        public bool UpdateDisplayedValue(SubgraphAssetProperty value)
        {
            m_SubgraphAssetProperty = value;
            return true;
        }

        public (Label label, VisualElement field) Build(ICommandTarget commandTargetView, string label, string tooltip, IReadOnlyList<object> obj, string propertyName)
        {
            if (obj[0] is SubgraphNodeModel { IsReferencingLocalSubgraph: false } subgraphNodeModel)
            {
                m_CommandTarget = commandTargetView;
                m_GraphModel = subgraphNodeModel.GraphModel;
                m_Container = new VisualElement();
                m_Container.AddPackageStylesheet("SubgraphAssetPropertyField.uss");

                m_ObjectField = new ObjectField("Asset");
                m_ObjectField.value = subgraphNodeModel.GetSubgraphModel()?.GraphObject;
                foreach (var child in m_ObjectField.Children())
                {
                    if (child is not Label)
                        child.SetEnabled(false);
                }

                m_Container.Add(m_ObjectField);

                var buttonContainer = new VisualElement();
                buttonContainer.AddToClassList(buttonContainerUSSClassName);

                m_OpenButton = new Button(OpenGraph) { text = "Open" };
                buttonContainer.Add(m_OpenButton);

                m_SelectButton = new Button(SelectInProjectWindow) { text = "Select" };
                buttonContainer.Add(m_SelectButton);

                m_Container.Add(buttonContainer);

                return (null, m_Container);
            }

            return (null, null);
        }

        void SelectInProjectWindow()
        {
            EditorUtility.FocusProjectWindow();
            var obj = m_GraphModel.ResolveGraphModelFromReference(m_SubgraphAssetProperty.SubgraphReference).GraphObject;
            if (obj != null)
            {
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }
        }

        void OpenGraph()
        {
            var supportsMultipleWindows = (m_CommandTarget as RootView)?.GraphTool?.SupportsMultipleWindows ?? true;
            var graphModel = m_GraphModel.ResolveGraphModelFromReference(m_SubgraphAssetProperty.SubgraphReference);
            if (supportsMultipleWindows)
            {

                if (graphModel.IsLocalSubgraph)
                    m_CommandTarget.Dispatch(new LoadGraphCommand(graphModel, LoadGraphCommand.LoadStrategies.PushOnStack));
                else if (graphModel.GraphObject != null)
                    AssetDatabase.OpenAsset(graphModel.GraphObject);
            }
            else
            {
                m_CommandTarget.Dispatch(new LoadGraphCommand(graphModel));
            }
        }
    }
}
