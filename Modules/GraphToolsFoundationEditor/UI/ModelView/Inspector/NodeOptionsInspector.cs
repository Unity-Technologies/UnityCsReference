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
        /// <param name="rootView">The root view.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="filter">A filter function to select which fields are displayed in the inspector. If null, defaults to <see cref="NodeOptionsInspector.CanBeInspected"/>.</param>
        /// <returns>A new instance of <see cref="NodeOptionsInspector"/>.</returns>
        public new static NodeOptionsInspector Create(string name, IEnumerable<Model> models, RootView rootView,
            string parentClassName, Func<FieldInfo, bool> filter = null)
        {
            return new NodeOptionsInspector(name, models, rootView, parentClassName, filter);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeOptionsInspector"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="models">The models displayed in this part.</param>
        /// <param name="rootView">The root view.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="filter">A filter function to select which fields are displayed in the inspector. If null, defaults to <see cref="NodeOptionsInspector.CanBeInspected"/>.</param>
        NodeOptionsInspector(string name, IEnumerable<Model> models, RootView rootView, string parentClassName, Func<FieldInfo, bool> filter)
            : base(name, models, rootView, parentClassName, filter) { }

        /// <inheritdoc />
        protected override IEnumerable<BaseModelPropertyField> GetFields()
        {
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
                    RootView, constants.Select(c => c.OwnerModel), constants,
                    false, options.First().PortModel.UniqueName ?? "");

                return nodeOptionEditor;
            }
        }

        List<List<NodeOption>> GetNodeOptionsToDisplay()
        {
            var nodeOptionsDict = new Dictionary<string, List<NodeOption>>();
            var isInspectorModelView = RootView is ModelInspectorView;

            for (var i = 0; i < m_Models.Count; i++)
            {
                if (m_Models[i] is NodeModel nodeModel)
                {
                    if (i == 0)
                    {
                        foreach (var option in nodeModel.NodeOptions)
                        {
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
    }
}
