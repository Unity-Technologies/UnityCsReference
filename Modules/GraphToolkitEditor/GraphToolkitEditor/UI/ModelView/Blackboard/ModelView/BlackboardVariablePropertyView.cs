// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A BlackboardElement to display the properties of a <see cref="VariableDeclarationModelBase"/>.
    /// </summary>
    [UnityRestricted]
    internal class BlackboardVariablePropertyView : BlackboardElement
    {
        public new static readonly string ussClassName = "ge-blackboard-variable-property-view";
        public static readonly string inspectorPartName = "inspectorPart";
        public static readonly string hiddenUssClassName = ussClassName.WithUssModifier(GraphElementHelper.hiddenUssModifier);

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardVariablePropertyView"/> class.
        /// </summary>
        public BlackboardVariablePropertyView()
        {
        }

        protected override void BuildPartList()
        {
            if (Model is VariableDeclarationModelBase variableDeclarationModel)
                PartList.AppendPart(VariableFieldsInspector.Create(
                    inspectorPartName,
                    new[] { variableDeclarationModel },
                    this,
                    ussClassName,
                    null,
                    VariableFieldsInspector.DisplayFlags.QuickSettings));
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(ussClassName);
        }
    }
}
