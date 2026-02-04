// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor.ContextualMenuItems;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Flags for determining whether a variable is read, write, or both.
    /// </summary>
    /// <remarks>
    /// 'ModifierFlags' defines access control options for a variable and specifies
    /// whether it can be <see cref="ModifierFlags.None"/>, <see cref="ModifierFlags.Read"/>,
    /// <see cref="ModifierFlags.Write"/>, or <see cref="ModifierFlags.ReadWrite"/>.
    /// </remarks>
    [Flags]
    [UnityRestricted]
    internal enum ModifierFlags
    {
        /// <summary>
        /// The variable is neither read nor write.
        /// </summary>
        None = 0,
        /// <summary>
        /// The variable is read-only.
        /// </summary>
        Read = 1 << 0,
        /// <summary>
        /// The variable is write-only.
        /// </summary>
        Write = 1 << 1,
        /// <summary>
        /// The variable is both read and write.
        /// </summary>
        ReadWrite = Read | Write,
    }

    /// <summary>
    /// Variable flags.
    /// </summary>
    [Flags]
    [UnityRestricted]
    internal enum VariableFlags
    {
        /// <summary>
        /// Empty flags.
        /// </summary>
        None = 0,

        /// <summary>
        /// The variable was automatically generated.
        /// </summary>
        Generated = 1,

        /// <summary>
        /// The variable is hidden.
        /// </summary>
        Hidden = 2,
    }

    /// <summary>
    /// The possible scopes of variables.
    /// </summary>
    [UnityRestricted]
    internal enum VariableScope
    {
        /// <summary>
        /// Local scope, used only within the graph.
        /// </summary>
        Local,

        /// <summary>
        /// Exposed scope, settable through the global inspector.
        /// </summary>
        Exposed,

        //Global // for the future
    }

    /// <summary>
    /// Base class for variable declarations.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal abstract class VariableDeclarationModelBase : DeclarationModel, IGroupItemModel, IVariable
    {
        [SerializeField, InspectorUseProperty(nameof(UniqueId)), DisableInInspector, VariableAdvanced]
#pragma warning disable CS0169
        // ReSharper disable once NotAccessedField.Local
        string m_UniqueId;
#pragma warning restore CS0169

        /// <summary>
        /// The variable unique id.
        /// </summary>
        public string UniqueId => Guid.ToString();

        /// <summary>
        /// The variable flags.
        /// </summary>
        /// <remarks>Setter implementations should set the <see cref="ChangeHint.Data"/> change hint.</remarks>
        public abstract VariableFlags VariableFlags { get; set; }

        /// <summary>
        /// The read/write modifiers.
        /// </summary>
        /// <remarks>Setter implementations should set the <see cref="ChangeHint.Data"/> change hint.</remarks>
        public abstract ModifierFlags Modifiers { get; set; }

        /// <summary>
        /// When acting as an input or output for a subgraph, whether the variable should show up on the inspector only,
        /// or on both the subgraph node and the inspector.
        /// </summary>
        /// <remarks>Setter implementations should set the <see cref="ChangeHint.Data"/> change hint.</remarks>
        public abstract bool ShowOnInspectorOnly { get; set; }

        /// <summary>
        /// Gets the name of the variable with non-alphanumeric characters replaced by an underscore.
        /// </summary>
        /// <returns>The name of the variable with non-alphanumeric characters replaced by an underscore.</returns>
        public virtual string GetVariableName() => Title.CodifyString();

        /// <summary>
        /// The Subtitle of the variable declaration model.
        /// </summary>
        /// <remarks>Dynamic implementations should set the <see cref="ChangeHint.Data"/> change hint when the value changes.</remarks>
        public virtual string Subtitle => string.Empty;

        /// <inheritdoc />
        #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        public virtual IEnumerable<GraphElementModel> ContainedModels => Enumerable.Repeat(this, 1);
#pragma warning restore UA2001

        /// <summary>
        /// The type of the variable.
        /// </summary>
        /// <remarks>Setter implementations should set the <see cref="ChangeHint.Data"/> change hint.</remarks>
        public abstract TypeHandle DataType { get; set; }

        /// <summary>
        /// Check if the variable declaration model is an output.
        /// </summary>
        public bool IsOutput => Modifiers == ModifierFlags.Write;

        /// <summary>
        /// Check if the variable declaration model is an input.
        /// </summary>
        public bool IsInput => Modifiers == ModifierFlags.Read;

        /// <summary>
        /// Check if the variable declaration model is an input or output.
        /// </summary>
        public bool IsInputOrOutput => IsOutput || IsInput;

        /// <summary>
        /// The scope of the variable.
        /// </summary>
        /// <remarks>Setter implementations should set the <see cref="ChangeHint.Data"/> change hint.</remarks>
        public abstract VariableScope Scope { get; set; }

        /// <inheritdoc />
        [field: NonSerialized]
        public virtual GroupModelBase ParentGroup { get; set; }

        /// <summary>
        /// A tooltip to show on nodes associated with this variable.
        /// </summary>
        public abstract string Tooltip { get; set; }

        /// <summary>
        /// The human-readable name of the data type of the variable declaration model.
        /// </summary>
        public virtual string DataTypeString => DataType.FriendlyName ?? string.Empty;

        /// <summary>
        /// The string used to describe this variable.
        /// </summary>
        public virtual string VariableString => "Variable";

        /// <summary>
        /// The default tooltip when <see cref="Tooltip"/> is not set.
        /// </summary>
        /// <remarks>The default tooltip consists of the <see cref="VariableString"/> and the <see cref="DataTypeString"/>.</remarks>
        public string DefaultTooltip => $"{VariableString}" + (string.IsNullOrEmpty(DataTypeString) ? "" : $" of type {DataTypeString}");

        /// <summary>
        /// The default value for this variable.
        /// </summary>
        public abstract Constant InitializationModel { get; set; }

        /// <summary>
        /// Indicates whether a <see cref="VariableDeclarationModelBase"/> requires initialization.
        /// </summary>
        /// <returns>True if the variable declaration model requires initialization, false otherwise.</returns>
        public virtual bool RequiresInitialization()
        {
            var dataType = DataType.Resolve();
            return dataType.IsValueType || dataType == typeof(string);
        }

        /// <summary>
        /// Sets the <see cref="InitializationModel"/> to a new value.
        /// </summary>
        public abstract void CreateInitializationValue();

        /// <summary>
        /// Returns if this variable is used in the graph, it won't be selected when select unused is dispatched.
        /// </summary>
        /// <returns>If this variable is used in the graph, it won't be selected when select unused is dispatched.</returns>
        public virtual bool IsUsed()
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var node in GraphModel.NodeModels.OfType<VariableNodeModel>())
#pragma warning restore UA2001
            {
                if (ReferenceEquals(node.VariableDeclarationModel, this) && node.GetPorts().HasAny(t => t.IsConnected()))
                    return true;
            }

            return false;
        }

        /// <inheritdoc />
        public override void OnBeforeCopy()
        {
            base.OnBeforeCopy();

            // ReSharper disable once SuspiciousTypeConversion.Global
            (InitializationModel as ICopyPasteCallbackReceiver)?.OnBeforeCopy();
        }

        /// <inheritdoc />
        public override void OnAfterCopy()
        {
            base.OnAfterCopy();

            // ReSharper disable once SuspiciousTypeConversion.Global
            (InitializationModel as ICopyPasteCallbackReceiver)?.OnAfterCopy();
        }

        /// <inheritdoc />
        public override void OnAfterPaste()
        {
            base.OnAfterPaste();

            // ReSharper disable once SuspiciousTypeConversion.Global
            (InitializationModel as ICopyPasteCallbackReceiver)?.OnAfterPaste();
        }

        /// <inheritdoc />
        public IGroupItemModel GetGroupItemInTargetGraph(GraphModel targetGraphModel, Dictionary<VariableDeclarationModelBase, VariableDeclarationModelBase> variableTranslation)
        {
            return variableTranslation[this];
        }

        bool IVariable.TryGetDefaultValue<T>(out T value)
        {
            if (InitializationModel == null)
            {
                value = default;
                return false;
            }
            return InitializationModel.TryGetValue(out value);
        }

        string IVariable.Name => Title;

        Type IVariable.DataType => DataType.Resolve();

        VariableKind IVariable.VariableKind
        {
            get
            {
                if (Modifiers == ModifierFlags.Write)
                    return VariableKind.Output;
                if (Modifiers == ModifierFlags.Read)
                    return VariableKind.Input;
                return VariableKind.Local;
            }
        }

        /// <inheritdoc />
        public override IReadOnlyList<ContextualMenuItem> ContextualMenuItems => k_ContextualMenuItems;

        static readonly List<ContextualMenuItem> k_ContextualMenuItems = new() {
            ContextualMenuHelpers.createVariableItem,
            ContextualMenuHelpers.createGroupItem,
            ContextualMenuHelpers.cutItem,
            ContextualMenuHelpers.copyItem,
            ContextualMenuHelpers.pasteItem,
            ContextualMenuHelpers.renameItem,
            ContextualMenuHelpers.duplicateItem,
            ContextualMenuHelpers.deleteItem,
            ContextualMenuHelpers.selectAllItem,
            ContextualMenuHelpers.selectUnusedItem,
        };
    }
}
