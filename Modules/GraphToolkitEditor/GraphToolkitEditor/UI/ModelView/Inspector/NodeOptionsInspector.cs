// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    [UnityRestricted]
    internal class NodeOptionsInspector : GraphElementFieldInspector
    {
        /// <summary>
        /// The USS class name added to a <see cref="NodeOptionsInspector"/>.
        /// </summary>
        public new static readonly string ussClassName = "ge-node-options-inspector-part";

        /// <summary>
        /// The USS class name when the node is collapsed.
        /// </summary>
        public static readonly string collapsedNodeOptionsUssClassName = ussClassName.WithUssModifier(GraphElementHelper.collapsedUssModifier);

        /// <summary>
        /// Creates a new instance of the <see cref="NodeOptionsInspector"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="models">The models displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="filter">A filter function to select which fields are displayed in the inspector. If null, defaults to <see cref="SerializedFieldsInspector.CanBeInspected"/>.</param>
        /// <returns>A new instance of <see cref="NodeOptionsInspector"/>.</returns>
        public new static NodeOptionsInspector Create(string name, IReadOnlyList<Model> models, ChildView ownerElement,
            string parentClassName, Func<FieldInfo, bool> filter = null)
        {
            return new NodeOptionsInspector(name, models, ownerElement, parentClassName, filter);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeOptionsInspector"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="models">The models displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="filter">A filter function to select which fields are displayed in the inspector. If null, defaults to <see cref="SerializedFieldsInspector.CanBeInspected"/>.</param>
        NodeOptionsInspector(string name, IReadOnlyList<Model> models, ChildView ownerElement, string parentClassName, Func<FieldInfo, bool> filter)
            : base(name, models, ownerElement, parentClassName, filter) { }

        struct OptionFieldInfo
        {
            public string name;
            public TypeHandle type;
            public bool inspectorOnly;
        }

        List<OptionFieldInfo> m_MutableFieldInfos = new List<OptionFieldInfo>();

        /// <inheritdoc />
        protected override IReadOnlyList<BaseModelPropertyField> GetFields()
        {
            var fieldList = new List<BaseModelPropertyField>();

            m_MutableFieldInfos.Clear();

            var targets = GetInspectedObjects().ToList();

            var inspectorOrderFields = new SortedDictionary<int, List<BaseModelPropertyField>>();

            AddFieldsFromNodeOptions(inspectorOrderFields, fieldList);
            AddFieldsFromTypes(targets, inspectorOrderFields, fieldList);
            GetCustomFields(fieldList);
            foreach (var fieldAtPositionList in inspectorOrderFields.Values)
            {
                fieldList.AddRange(fieldAtPositionList);
            }

            return fieldList;
        }

        void AddFieldsFromNodeOptions(SortedDictionary<int, List<BaseModelPropertyField>> inspectorOrderFields, List<BaseModelPropertyField> outFieldList)
        {
            var nodeOptionLists = GetNodeOptionsToDisplay();
            if (nodeOptionLists != null)
            {
                foreach (var nodeOptionList in nodeOptionLists)
                {
                    var order = nodeOptionList[0].Order;
                    if (order != 0)
                    {
                        AddFieldToInspectorOrderFields(order, GetFieldFromNodeOptions(nodeOptionList), inspectorOrderFields);
                        continue;
                    }

                    outFieldList.Add(GetFieldFromNodeOptions(nodeOptionList));
                }
            }

            BaseModelPropertyField GetFieldFromNodeOptions(IReadOnlyList<NodeOption> options)
            {
                var constants = new List<Constant>();
                var ownerModels = new List<GraphElementModel>();

                foreach (var option in options)
                {
                    constants.Add(option.PortModel.EmbeddedValue);
                    ownerModels.Add(option.PortModel);
                }

                var nodeOptionEditor = InlineValueEditor.CreateEditorForConstants(
                    OwnerRootView, ownerModels, constants, options[0].PortModel.Title ?? "");

                return nodeOptionEditor;
            }
        }

        List<List<NodeOption>> GetNodeOptionsToDisplay()
        {
            var nodeOptionsDict = new Dictionary<string, List<NodeOption>>();
            var isInspectorModelView = OwnerRootView is ModelInspectorView;

            for (var i = 0; i < m_Models.Count; i++)
            {
                if (m_Models[i] is NodeModel nodeModel)
                {
                    if (i == 0)
                    {
                        foreach (var option in nodeModel.NodeOptions)
                        {
                            m_MutableFieldInfos.Add(new OptionFieldInfo
                            {
                                name = option.PortModel.Title,
                                type = option.PortModel.DataTypeHandle,
                                inspectorOnly = option.IsInInspectorOnly
                            });
                            if (!option.IsInInspectorOnly || isInspectorModelView)
                                nodeOptionsDict[option.Id] = new List<NodeOption> { option };
                        }
                        continue;
                    }

                    // If multiple models are inspected, we only want to display the node options that are present in all models.
                    foreach (var id in nodeOptionsDict.Keys.ToList())
                    {
                        var otherOptions = nodeModel.NodeOptions.Where(o =>
                            id == o.Id && o.PortModel.DataTypeHandle == nodeOptionsDict[id].First().PortModel.DataTypeHandle).ToList();
                        if (otherOptions.Any())
                            nodeOptionsDict[id].AddRange(otherOptions);
                        else
                            nodeOptionsDict.Remove(id);
                    }
                }
            }

            return nodeOptionsDict.Values.ToList();
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            // When the node is collapsed, the node options shouldn't be displayed.
            m_Root.EnableInClassList(collapsedNodeOptionsUssClassName, m_Models[0] is ICollapsible { Collapsed: true });

            if (ShouldRebuildFields())
            {
                BuildFields();
            }

            base.UpdateUIFromModel(visitor);
        }

        bool ShouldRebuildFields()
        {
            if (m_Models.Count != 1)
                return false;
            var nodeModel = m_Models[0] as NodeModel;

            if (nodeModel == null)
                return false;

            if (nodeModel.NodeOptions.Count != m_MutableFieldInfos.Count)
                return true;

            var isInspectorModelView = OwnerRootView is ModelInspectorView;

            foreach (var oldNCurrent in nodeModel.NodeOptions.Zip(m_MutableFieldInfos, (a, b) => new { old = b, current = a }))
            {
                if (oldNCurrent.current.PortModel.Title != oldNCurrent.old.name)
                    return true;
                if (oldNCurrent.current.PortModel.DataTypeHandle != oldNCurrent.old.type)
                    return true;
                if (!isInspectorModelView)
                    if (oldNCurrent.current.IsInInspectorOnly != oldNCurrent.old.inspectorOnly)
                        return true;
            }

            return false;
        }
    }
}
