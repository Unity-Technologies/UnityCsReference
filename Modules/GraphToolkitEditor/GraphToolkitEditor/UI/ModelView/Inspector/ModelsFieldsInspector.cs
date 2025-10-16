// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Inspector for the serializable fields of a <see cref="GraphElementModel"/>.
    /// </summary>
    [UnityRestricted]
    internal class ModelsFieldsInspector : SerializedFieldsInspector
    {
        /// <summary>
        /// The USS class name for type buttons.
        /// </summary>
        public static readonly string typeButtonUssClassName = "ge-type-button";

        /// <summary>
        /// Creates a new instance of the <see cref="GraphElementFieldInspector"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="models">The models displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="filter">A filter function to select which fields are displayed in the inspector. If null, defaults to <see cref="SerializedFieldsInspector.CanBeInspected"/>.</param>
        /// <returns>A new instance of <see cref="GraphElementFieldInspector"/>.</returns>
        public new static ModelsFieldsInspector Create(string name, IReadOnlyList<Model> models, ChildView ownerElement,
            string parentClassName, Func<FieldInfo, bool> filter = null)
        {
            return new ModelsFieldsInspector(name, models, ownerElement, parentClassName, filter);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializedFieldsInspector"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="models">The models displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="filter">A filter function to select which fields are displayed in the inspector. If null, defaults to <see cref="SerializedFieldsInspector.CanBeInspected"/>.</param>
        protected ModelsFieldsInspector(string name, IReadOnlyList<Model> models, ChildView ownerElement,
                                        string parentClassName, Func<FieldInfo, bool> filter)
            : base(name, models, ownerElement, parentClassName, filter) { }


        static StringBuilder s_BuildPartUI_typeText = new StringBuilder(16);

        protected override void BuildUI(VisualElement parent)
        {
            base.BuildUI(parent);
            m_Root.AddPackageStylesheet("ModelsFieldsInspector.uss");
            var commonTypes = new[] { typeof(NodeModel), typeof(VariableDeclarationModelBase), typeof(PlacematModel), typeof(StickyNoteModel), typeof(WireModel) };

            var modelsByType = new Dictionary<Type, List<Model>>();

            foreach (var model in m_Models)
            {
                var type = model.GetType();
                foreach (var commonType in commonTypes)
                {
                    if (commonType.IsAssignableFrom(type))
                    {
                        type = commonType;
                        break;
                    }
                }

                modelsByType.TryGetValue(type, out var modelsWithThisType);
                if (modelsWithThisType == null)
                {
                    modelsWithThisType = new List<Model>();
                    modelsByType[type] = modelsWithThisType;
                }
                modelsWithThisType.Add(model);
            }

            var window = m_OwnerElement.RootView.Window as GraphViewEditorWindow;
            if (window == null || window.GraphView == null)
                return;

            var graphView = window.GraphView;
            foreach (var kv in modelsByType)
            {
                s_BuildPartUI_typeText.Length = 0;
                s_BuildPartUI_typeText.Append(TypeHelpers.GetFriendlyName(kv.Key).Replace("Model", string.Empty));
                if (kv.Value.Count > 1)
                    s_BuildPartUI_typeText.Append("s");
                var button = new Button() { text = $@"{kv.Value.Count} {s_BuildPartUI_typeText}" };
                button.AddToClassList(m_ParentClassName.WithUssElement(typeButtonUssClassName));
                m_Root.Add(button);

                button.clickable.clicked += () =>
                {
                    graphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, kv.Value.OfTypeToList<GraphElementModel, Model>()));
                };
            }
        }

        public override BaseModelPropertyField GetTitleField(IReadOnlyList<object> targets)
        {
            if (OwnerRootView is ModelInspectorView miv)
            {
                return new MultipleModelsTitlePropertyField(miv, m_Models);
            }

            return null;
        }

        public override bool IsEmpty => false;
    }
}
