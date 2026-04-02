// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base class for references to a <see cref="VariableDeclarationModelBase"/> in another graph.
    /// </summary>
    [UnityRestricted]
    [Serializable]
    internal abstract class ExternalVariableDeclarationModelBase : VariableDeclarationModelBase
    {
        /// <inheritdoc />
        public override string Title
        {
            get => GetExternalVariableDeclaration()?.Title ?? string.Empty;
            set
            {
                var declaration = GetExternalVariableDeclaration();
                if (declaration == null)
                    return;

                var name = declaration.Title;
                if (name == value)
                    return;

                declaration.Title = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
                SetSourceDirty();
            }
        }

        /// <inheritdoc />
        public override VariableFlags VariableFlags
        {
            get => GetExternalVariableDeclaration()?.VariableFlags ?? VariableFlags.None;
            set
            {
                var declaration = GetExternalVariableDeclaration();
                if (declaration == null)
                    return;

                declaration.VariableFlags = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
                SetSourceDirty();
            }
        }

        /// <inheritdoc />
        public override ModifierFlags Modifiers
        {
            get => GetExternalVariableDeclaration()?.Modifiers ?? ModifierFlags.None;
            set
            {
                var declaration = GetExternalVariableDeclaration();
                if (declaration == null)
                    return;

                declaration.Modifiers = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
                SetSourceDirty();
            }
        }

        /// <inheritdoc />
        public override TypeHandle DataType
        {
            get => GetExternalVariableDeclaration()?.DataType ?? TypeHandle.Unknown;
            set
            {
                var declaration = GetExternalVariableDeclaration();
                if (declaration == null)
                    return;

                declaration.DataType = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
                SetSourceDirty();
            }
        }

        /// <inheritdoc />
        public override VariableScope Scope
        {
            get => GetExternalVariableDeclaration()?.Scope ?? VariableScope.Local;
            set
            {
                var declaration = GetExternalVariableDeclaration();
                if (declaration == null)
                    return;

                declaration.Scope = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
                SetSourceDirty();
            }
        }

        /// <inheritdoc />
        public override bool ShowOnInspectorOnly
        {
            get => GetExternalVariableDeclaration()?.ShowOnInspectorOnly ?? false;
            set
            {
                var declaration = GetExternalVariableDeclaration();
                if (declaration == null)
                    return;

                declaration.ShowOnInspectorOnly = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
                SetSourceDirty();
            }
        }

        /// <inheritdoc />
        public override string Tooltip
        {
            get => GetExternalVariableDeclaration()?.Tooltip ?? string.Empty;
            set
            {
                var declaration = GetExternalVariableDeclaration();
                if (declaration == null)
                    return;

                declaration.Tooltip = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Style);
                SetSourceDirty();
            }
        }

        /// <inheritdoc />
        public override Constant InitializationModel
        {
            get => GetExternalVariableDeclaration()?.InitializationModel;
            set => throw new InvalidOperationException("Cannot set initialization model on external variable.");
        }

        protected ExternalVariableDeclarationModelBase()
        {
            SetCapability(Editor.Capabilities.Copiable, false);
            SetCapability(Editor.Capabilities.Deletable, false);
            SetCapability(Editor.Capabilities.Renamable, false);
            SetCapability(Editor.Capabilities.Editable, false);
        }

        /// <inheritdoc />
        public override void CreateInitializationValue()
        {
            var declaration = GetExternalVariableDeclaration();
            if (declaration == null)
                return;

            declaration.CreateInitializationValue();
            GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            SetSourceDirty();
        }

        /// <summary>
        /// Gets the variable declaration to which this <see cref="ExternalVariableDeclarationModelBase"/> refers.
        /// </summary>
        /// <returns>The variable declaration to which this &lt;see cref="ExternalVariableDeclarationModelBase"/&gt; refers.</returns>
        protected abstract VariableDeclarationModelBase GetExternalVariableDeclaration();

        /// <summary>
        /// Sets the graph object containing the variable declaration as dirty.
        /// </summary>
        protected virtual void SetSourceDirty() { }

        /// <summary>
        /// Whether this <see cref="ExternalVariableDeclarationModelBase"/> is referring to the same variable as <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The external variable reference to compare to.</param>
        /// <returns>True if this <see cref="ExternalVariableDeclarationModelBase"/> is referring to the same variable as <paramref name="other"/>.</returns>
        public virtual bool RefersToSameVariableAs(ExternalVariableDeclarationModelBase other)
        {
            return ReferenceEquals(this, other);
        }
    }
}
