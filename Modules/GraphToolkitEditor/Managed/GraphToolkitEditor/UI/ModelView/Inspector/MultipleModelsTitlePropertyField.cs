// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Displays a property field to edit a IHasTitle model's title.
    /// </summary>
    [UnityRestricted]
    internal class MultipleModelsTitlePropertyField : BaseModelPropertyField
    {
        /// <summary>
        /// The USS class name added to a <see cref="MultipleModelsTitlePropertyField"/>.
        /// </summary>
        public new static readonly string ussClassName = "ge-multiple-models-title-property-field";

        /// <summary>
        /// The USS class name added to the title element.
        /// </summary>
        public static readonly string titleUssClassName = ussClassName.WithUssElement(GraphElementHelper.titleName);

        /// <summary>
        /// The USS class name added to the info element.
        /// </summary>
        public static readonly string infoUssClassName = ussClassName.WithUssElement("info");

        Label m_Title;
        Label m_Info;
        IReadOnlyList<Model> m_Models;
        ModelInspectorView m_ModelInspectorView;

        /// <summary>
        /// Creates an instance of <see cref="MultipleModelsTitlePropertyField"/>.
        /// </summary>
        /// <param name="modelInspectorView">The model inspector view.</param>
        /// <param name="models">The models. </param>
        public MultipleModelsTitlePropertyField(ModelInspectorView modelInspectorView, IReadOnlyList<Model> models)
            : base(modelInspectorView)
        {
            m_ModelInspectorView = modelInspectorView;
            this.AddPackageStylesheet("ModelsFieldsInspector.uss");

            m_Models = models;
            AddToClassList(ussClassName);
            m_Title = new Label();
            m_Title.AddToClassList(titleUssClassName);
            Add(m_Title);
            m_Info = new Label();
            m_Info.AddToClassList(infoUssClassName);
            Add(m_Info);
        }

        /// <inheritdoc />
        public override void UpdateDisplayedValue()
        {
            m_Title.text = $@"{m_Models.Count} Objects";

            var window = m_ModelInspectorView.Window as GraphViewEditorWindow;
            if (window.GraphView != null)
            {
                m_Info.style.display = DisplayStyle.Flex;
                m_Info.text = "Narrow the Selection:";
            }
            else
            {
                m_Info.style.display = DisplayStyle.None;
            }
        }
    }
}
