// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.CommandStateObserver;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Command to create a new sticky note.
    /// </summary>
    class CreateStickyNoteCommand : UndoableCommand
    {
        /// <summary>
        /// The position and size of the new sticky note.
        /// </summary>
        public readonly Rect Position;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateStickyNoteCommand"/> class.
        /// </summary>
        public CreateStickyNoteCommand()
        {
            UndoString = "Create Sticky Note";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateStickyNoteCommand"/> class.
        /// </summary>
        /// <param name="position">The position and size of the new sticky note.</param>
        public CreateStickyNoteCommand(Rect position) : this()
        {
            Position = position;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, CreateStickyNoteCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            StickyNoteModel stickyNote;
            using (var graphUpdater = graphModelState.UpdateScope)
            {
                stickyNote = graphModelState.GraphModel.CreateStickyNote(command.Position);
                graphUpdater.MarkNew(stickyNote);
                graphUpdater.MarkForRename(stickyNote);
            }

            if (stickyNote != null)
            {
                var selectionHelper = new GlobalSelectionCommandHelper(selectionState);
                using (var selectionUpdaters = selectionHelper.UpdateScopes)
                {
                    foreach (var updater in selectionUpdaters)
                        updater.ClearSelection();
                    selectionUpdaters.MainUpdateScope.SelectElement(stickyNote, true);
                }
            }
        }
    }

    /// <summary>
    /// Command to update the title and content of a sticky note.
    /// </summary>
    class UpdateStickyNoteCommand : UndoableCommand
    {
        /// <summary>
        /// The new title, or null if the title should not be updated.
        /// </summary>
        public readonly string Title;
        /// <summary>
        /// The new content, or null if the content should not be updated.
        /// </summary>
        public readonly string Contents;
        /// <summary>
        /// The sticky note model to update.
        /// </summary>
        public readonly StickyNoteModel StickyNoteModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateStickyNoteCommand"/> class.
        /// </summary>
        public UpdateStickyNoteCommand()
        {
            UndoString = "Update Sticky Note Content";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateStickyNoteCommand"/> class.
        /// </summary>
        /// <param name="stickyNoteModel">The sticky note model to update.</param>
        /// <param name="title">The new title, or null if the title should not be updated.</param>
        /// <param name="contents">The new content, or null if the content should not be updated.</param>
        public UpdateStickyNoteCommand(StickyNoteModel stickyNoteModel, string title, string contents) : this()
        {
            StickyNoteModel = stickyNoteModel;
            Title = title;
            Contents = contents;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, UpdateStickyNoteCommand command)
        {
            if (command.Title == null && command.Contents == null)
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            {
                if (command.Title != null)
                    command.StickyNoteModel.Title = command.Title;

                if (command.Contents != null)
                    command.StickyNoteModel.Contents = command.Contents;

                graphUpdater.MarkChanged(command.StickyNoteModel, ChangeHint.Data);
            }
        }
    }

    /// <summary>
    /// Command to update the theme of sticky notes.
    /// </summary>
    class UpdateStickyNoteThemeCommand : ModelCommand<StickyNoteModel, string>
    {
        const string k_UndoStringSingular = "Change Sticky Note Theme";
        const string k_UndoStringPlural = "Change Sticky Notes Theme";

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateStickyNoteThemeCommand"/> class.
        /// </summary>
        public UpdateStickyNoteThemeCommand()
            : base(k_UndoStringSingular) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateStickyNoteThemeCommand"/> class.
        /// </summary>
        /// <param name="theme">The new theme.</param>
        /// <param name="stickyNoteModels">The sticky notes to update.</param>
        public UpdateStickyNoteThemeCommand(string theme, IReadOnlyList<StickyNoteModel> stickyNoteModels)
            : base(k_UndoStringSingular, k_UndoStringPlural, theme, stickyNoteModels) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateStickyNoteThemeCommand"/> class.
        /// </summary>
        /// <param name="theme">The new theme.</param>
        /// <param name="stickyNoteModels">The sticky notes to update.</param>
        public UpdateStickyNoteThemeCommand(string theme, params StickyNoteModel[] stickyNoteModels)
            : this(theme, (IReadOnlyList<StickyNoteModel>)stickyNoteModels) {}

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, UpdateStickyNoteThemeCommand command)
        {
            if (!command.Models.Any())
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            {
                foreach (var noteModel in command.Models)
                {
                    noteModel.Theme = command.Value;
                }

                graphUpdater.MarkChanged(command.Models, ChangeHint.Style);
            }
        }
    }

    /// <summary>
    /// Command to update the text size of a sticky note.
    /// </summary>
    class UpdateStickyNoteTextSizeCommand : ModelCommand<StickyNoteModel, string>
    {
        const string k_UndoStringSingular = "Change Sticky Note Font Size";
        const string k_UndoStringPlural = "Change Sticky Notes Font Size";

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateStickyNoteTextSizeCommand"/> class.
        /// </summary>
        public UpdateStickyNoteTextSizeCommand()
            : base(k_UndoStringSingular) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateStickyNoteTextSizeCommand"/> class.
        /// </summary>
        /// <param name="textSize">The new text size.</param>
        /// <param name="stickyNoteModels">The sticky note models to update.</param>
        public UpdateStickyNoteTextSizeCommand(string textSize, IReadOnlyList<StickyNoteModel> stickyNoteModels)
            : base(k_UndoStringSingular, k_UndoStringPlural, textSize, stickyNoteModels) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateStickyNoteTextSizeCommand"/> class.
        /// </summary>
        /// <param name="textSize">The new text size.</param>
        /// <param name="stickyNoteModels">The sticky note models to update.</param>
        public UpdateStickyNoteTextSizeCommand(string textSize, params StickyNoteModel[] stickyNoteModels)
            : this(textSize, (IReadOnlyList<StickyNoteModel>)stickyNoteModels) {}

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, UpdateStickyNoteTextSizeCommand command)
        {
            if (!command.Models.Any())
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            {
                foreach (var noteModel in command.Models)
                {
                    noteModel.TextSize = command.Value;
                }

                graphUpdater.MarkChanged(command.Models, ChangeHint.Style);
            }
        }
    }
}
