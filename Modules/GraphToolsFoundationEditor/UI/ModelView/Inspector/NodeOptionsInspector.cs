// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    class NodeOptionsInspector : SerializedFieldsInspector
    {
        /// <summary>
        /// Creates a new instance of the <see cref="NodeOptionsInspector"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="models">The models displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="filter">A filter function to select which fields are displayed in the inspector. If null, defaults to <see cref="NodeOptionsInspector.CanBeInspected"/>.</param>
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
        /// <param name="filter">A filter function to select which fields are displayed in the inspector. If null, defaults to <see cref="NodeOptionsInspector.CanBeInspected"/>.</param>
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
        protected override IEnumerable<BaseModelPropertyField> GetFields()
        {
            m_MutableFieldInfos.Clear();

            var targets = GetInspectedObjects();

            if (targets == null)
                yield break;

            var inspectorOrderFields = new SortedDictionary<int, List<BaseModelPropertyField>>();

            foreach (var field in AddFieldsFromNodeOptions(inspectorOrderFields))
                yield return field;

            foreach (var field in AddFieldsFromTypes(targets, inspectorOrderFields))
                yield return field;

            var customFields = GetCustomFields();
            if (customFields != null)
            {
                foreach (var field in customFields)
                    yield return field;
            }

            foreach (var fieldAtPositionList in inspectorOrderFields.Values)
            {
                foreach (var field in fieldAtPositionList)
                    yield return field;
            }
        }

        IEnumerable<BaseModelPropertyField> AddFieldsFromNodeOptions(SortedDictionary<int, List<BaseModelPropertyField>> inspectorOrderFields)
        {
            var nodeOptionLists = GetNodeOptionsToDisplay();
            if (nodeOptionLists != null)
            {
                foreach (var nodeOptionList in nodeOptionLists)
                {
                    var order = nodeOptionList.First().Order;
                    if (order != 0)
                    {
                        AddFieldToInspectorOrderFields(order, GetFieldFromNodeOptions(nodeOptionList), inspectorOrderFields);
                        continue;
                    }

                    yield return GetFieldFromNodeOptions(nodeOptionList);
                }
            }

            BaseModelPropertyField GetFieldFromNodeOptions(IReadOnlyCollection<NodeOption> options)
            {
                var constants = options.Select(o => o.PortModel.EmbeddedValue);
                var nodeOptionEditor = InlineValueEditor.CreateEditorForConstants(
                    OwnerRootView, constants.Select(c => c.OwnerModel), constants,
                    false, options.First().PortModel.DisplayTitle ?? "");

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
                                name = option.PortModel.DisplayTitle,
                                type = option.PortModel.DataTypeHandle,
                                inspectorOnly = option.IsInInspectorOnly
                            });
                            if (!option.IsInInspectorOnly || isInspectorModelView)
                                nodeOptionsDict[option.PortModel.UniqueName] = new List<NodeOption> { option };
                        }
                        continue;
                    }

                    // If multiple models are inspected, we only want to display the node options that are present in all models.
                    foreach (var title in nodeOptionsDict.Keys.ToList())
                    {
                        var otherOptions = nodeModel.NodeOptions.Where(o =>
                            title == o.PortModel.UniqueName && o.PortModel.PortDataType == nodeOptionsDict[title].First().PortModel.PortDataType).ToList();
                        if (otherOptions.Any())
                            nodeOptionsDict[title].AddRange(otherOptions);
                        else
                            nodeOptionsDict.Remove(title);
                    }
                }
            }

            return nodeOptionsDict.Values.ToList();
        }

        protected override void UpdatePartFromModel()
        {
            if (ShouldRebuildFields())
            {
                BuildFields();
            }

            base.UpdatePartFromModel();
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
                if (oldNCurrent.current.PortModel.DisplayTitle != oldNCurrent.old.name)
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
