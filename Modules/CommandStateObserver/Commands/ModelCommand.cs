// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.CommandStateObserver
{
    /// <summary>
    /// Base class for <see cref="UndoableCommand"/> that affect one or more models that share a common type.
    /// </summary>
    /// <typeparam name="TModel">The type of the affected models.</typeparam>
    abstract class ModelCommand<TModel> : UndoableCommand
    {
        /// <summary>
        /// List of models affected by the command.
        /// </summary>
        public IReadOnlyList<TModel> Models;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelCommand{TModel}" /> class.
        /// </summary>
        /// <param name="undoString">The string to display in the undo menu item.</param>
        protected ModelCommand(string undoString)
        {
            UndoString = undoString;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelCommand{TModel}" /> class.
        /// </summary>
        /// <param name="undoStringSingular">The string to display in the undo menu item when there is only one model affected.</param>
        /// <param name="undoStringPlural">The string to display in the undo menu item when there are many models affected.</param>
        /// <param name="models">The models affected by the command.</param>
        protected ModelCommand(string undoStringSingular, string undoStringPlural, IReadOnlyList<TModel> models)
        {
            Models = models;
            UndoString = Models == null || Models.Count <= 1 ? undoStringSingular : undoStringPlural;
        }
    }

    /// <summary>
    /// Base class for <see cref="UndoableCommand"/> that set a single value on one or many models.
    /// </summary>
    /// <typeparam name="TModel">The type of the models.</typeparam>
    /// <typeparam name="TValue">The type of the value to set on the models.</typeparam>
    abstract class ModelCommand<TModel, TValue> : ModelCommand<TModel>
    {
        /// <summary>
        /// The value to set on all the affected models.
        /// </summary>
        public TValue Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelCommand{TModel, TValue}" /> class.
        /// </summary>
        /// <param name="undoString">The string to display in the undo menu item.</param>
        protected ModelCommand(string undoString) : base(undoString)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelCommand{TModel, TValue}" /> class.
        /// </summary>
        /// <param name="undoStringSingular">The string to display in the undo menu item when there is only one model affected.</param>
        /// <param name="undoStringPlural">The string to display in the undo menu item when there are many models affected.</param>
        /// <param name="models">The models affected by the command.</param>
        protected ModelCommand(string undoStringSingular, string undoStringPlural,
                               TValue value,
                               IReadOnlyList<TModel> models)
            : base(undoStringSingular, undoStringPlural, models)
        {
            Value = value;
        }
    }
}
