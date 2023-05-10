// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// UI for <see cref="ISingleInputPortNodeModel"/> and <see cref="ISingleOutputPortNodeModel"/>.
    /// </summary>
    class TokenNode : Node
    {
        public static readonly string tokenModifierUssClassName = ussClassName.WithUssModifier("token");
        public static readonly string constantModifierUssClassName = ussClassName.WithUssModifier("constant-token");
        public static readonly string variableModifierUssClassName = ussClassName.WithUssModifier("variable-token");
        public static readonly string portalModifierUssClassName = ussClassName.WithUssModifier("portal");
        public static readonly string portalEntryModifierUssClassName = ussClassName.WithUssModifier("portal-entry");
        public static readonly string portalExitModifierUssClassName = ussClassName.WithUssModifier("portal-exit");

        public static readonly string titleIconContainerPartName = "title-icon-container";
        public static readonly string constantEditorPartName = "constant-editor";
        public static readonly string inputPortContainerPartName = "inputs";
        public static readonly string outputPortContainerPartName = "outputs";

        /// The name of the LOD cache part
        public static readonly string cachePartName = "cache";

        internal bool IsHighlighted_Internal() => Border.Highlighted;

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            PartList.AppendPart(SinglePortContainerPart.Create(inputPortContainerPartName, ExtractInputPortModel(Model), this, ussClassName));
            PartList.AppendPart(NodeTitlePart.Create(titleIconContainerPartName, NodeModel, this, ussClassName, EditableTitlePart.Options.UseEllipsis | EditableTitlePart.Options.SetWidth));
            PartList.AppendPart(ConstantNodeEditorPart.Create(constantEditorPartName, Model, this, ussClassName));
            PartList.AppendPart(SinglePortContainerPart.Create(outputPortContainerPartName, ExtractOutputPortModel(Model), this, ussClassName));
            PartList.AppendPart(TokenLodCachePart.Create(cachePartName, Model, this, tokenModifierUssClassName));
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(tokenModifierUssClassName);
            this.AddStylesheet_Internal("TokenNode.uss");

            switch (Model)
            {
                case WirePortalModel and ISingleInputPortNodeModel:
                    AddToClassList(portalModifierUssClassName);
                    AddToClassList(portalEntryModifierUssClassName);
                    break;
                case WirePortalModel and ISingleOutputPortNodeModel:
                    AddToClassList(portalModifierUssClassName);
                    AddToClassList(portalExitModifierUssClassName);
                    break;
                case ConstantNodeModel:
                    AddToClassList(constantModifierUssClassName);
                    break;
                case VariableNodeModel:
                    AddToClassList(variableModifierUssClassName);
                    break;
            }
        }

        /// <inheritdoc />
        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            Border.Highlighted = ShouldBeHighlighted();
        }

        protected static Model ExtractInputPortModel(Model model)
        {
            if (model is ISingleInputPortNodeModel inputPortHolder && inputPortHolder.InputPort != null)
            {
                Debug.Assert(inputPortHolder.InputPort.Direction == PortDirection.Input);
                return inputPortHolder.InputPort;
            }

            return null;
        }

        protected static Model ExtractOutputPortModel(Model model)
        {
            if (model is ISingleOutputPortNodeModel outputPortHolder && outputPortHolder.OutputPort != null)
            {
                Debug.Assert(outputPortHolder.OutputPort.Direction == PortDirection.Output);
                return outputPortHolder.OutputPort;
            }

            return null;
        }
    }
}
