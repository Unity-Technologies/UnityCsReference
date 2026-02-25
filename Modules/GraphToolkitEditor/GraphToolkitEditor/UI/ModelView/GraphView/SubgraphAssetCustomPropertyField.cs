// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.GraphToolkit.CSO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
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

        Object m_LoadedObject; // for tests

        public void SetMixed()
        {
            if (m_ObjectField != null)
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
            var graphObject = m_GraphModel.ResolveGraphModelFromReference(m_SubgraphAssetProperty.SubgraphReference).GraphObject;
            if (graphObject != null)
            {
                EditorUtility.FocusProjectWindow();
                var obj = AssetDatabase.LoadMainAssetAtPath(graphObject.FilePath);
                m_LoadedObject = obj;
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }
        }

        void OpenGraph()
        {
            var supportsMultipleWindows = (m_CommandTarget as RootView)?.GraphTool?.SupportsMultipleWindows ?? true;
            var subgraph = m_GraphModel.ResolveGraphModelFromReference(m_SubgraphAssetProperty.SubgraphReference);
            if (supportsMultipleWindows)
            {
                if (subgraph.IsLocalSubgraph)
                    m_CommandTarget.Dispatch(new LoadGraphCommand(subgraph, LoadGraphCommand.LoadStrategies.PushOnStack));
                else if (subgraph.GraphObject != null)
                {
                    if (ReferenceEquals(m_GraphModel, subgraph))
                    {
                        Debug.LogWarning($"The graph '{subgraph.Name}' is already open in the current window.");
                    }
                    else
                    {
                        var obj = AssetDatabase.LoadMainAssetAtPath(subgraph.GraphObject.FilePath);
                        m_LoadedObject = obj;
                        AssetDatabase.OpenAsset(obj);
                    }
                }
            }
            else
            {
                m_CommandTarget.Dispatch(new LoadGraphCommand(subgraph));
            }
        }

        internal class TestAccess
        {
            readonly SubgraphAssetCustomPropertyField m_SubgraphAssetCustomPropertyField;

            public TestAccess(SubgraphAssetCustomPropertyField subgraphAssetCustomPropertyField)
            {
                m_SubgraphAssetCustomPropertyField = subgraphAssetCustomPropertyField;
            }

            public void CallOpenGraph() => m_SubgraphAssetCustomPropertyField.OpenGraph();
            public void CallSelectInProjectWindow() => m_SubgraphAssetCustomPropertyField.SelectInProjectWindow();

            public Object LoadedObject => m_SubgraphAssetCustomPropertyField.m_LoadedObject;
        }
    }
}
