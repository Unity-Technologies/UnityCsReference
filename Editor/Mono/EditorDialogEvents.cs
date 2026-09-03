// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Scripting;
using DialogType = UnityEditor.DialogIconType;

namespace UnityEditor
{
    /// <summary>Options for identifying which mechanism produced a <see cref="DialogEventInfo"/>.</summary>
    public enum DialogSource
    {
        /// <summary>A native dialog, such as the `EditorUtility.DisplayDialog` family or a native-originated alert. Carries full structured content.</summary>
        NativeMessageBox,
        /// <summary>A custom `EditorWindow` shown using `ShowModal` or `ShowModalUtility`. Opaque; the content is unknown.</summary>
        ManagedCustomWindow
    }

    /// <summary>Information about a modal dialog's shape at the moment it displays.</summary>
    public readonly struct DialogEventInfo
    {
        /// <summary>Unique identifier for this dialog occurrence, stable from when it's raised until it's dismissed.</summary>
        public readonly int Id;
        /// <summary>The mechanism that produced this dialog.</summary>
        public readonly DialogSource Source;
        /// <summary>The text shown in the dialog's title bar.</summary>
        public readonly string Title;
        /// <summary>The dialog's body text when <see cref="Source"/> is <see cref="DialogSource.NativeMessageBox"/>; null when <see cref="Source"/> is <see cref="DialogSource.ManagedCustomWindow"/>.</summary>
        public readonly string Message;
        /// <summary>The dialog's button labels, in display order, when <see cref="Source"/> is <see cref="DialogSource.NativeMessageBox"/>; null when <see cref="Source"/> is <see cref="DialogSource.ManagedCustomWindow"/>. Backed by a private copy, so it can't be mutated by a subscriber.</summary>
        public readonly IReadOnlyList<string> ButtonLabels;
        /// <summary>The severity level of the dialog, such as Info, Warning, or Error when <see cref="Source"/> is <see cref="DialogSource.NativeMessageBox"/>; null when <see cref="Source"/> is <see cref="DialogSource.ManagedCustomWindow"/>.</summary>
        public readonly DialogType? Level;
        /// <summary>UTC time at which the dialog was about to be shown.</summary>
        public readonly DateTime OpenedAtUtc;

        internal DialogEventInfo(int id, DialogSource source, string title, string message, IReadOnlyList<string> buttonLabels, DialogType? level, DateTime openedAtUtc)
        {
            Id = id;
            Source = source;
            Title = title;
            Message = message;
            ButtonLabels = buttonLabels;
            Level = level;
            OpenedAtUtc = openedAtUtc;
        }
    }

    /// <summary>
    /// Raises events when a modal dialog opens or closes anywhere in the Editor.
    /// Use these events to detect that a dialog is blocking the Editor's main thread rather than that the Editor
    /// has stopped responding.
    /// </summary>
    /// <remarks>
    /// Coverage is mechanism-based, not exhaustive — only dialogs shown through the two paths above
    /// are visible here. Confirmed covered: the `EditorUtility.DisplayDialog` family and other
    /// native alerts (via `DialogScope`), and any `EditorWindow` shown with `ShowModal()` or
    /// `ShowModalUtility()` (both route through `MakeModal()`, on every platform). Confirmed NOT
    /// covered: OS-native file and folder pickers (`EditorUtility.OpenFilePanel`, `SaveFilePanel`,
    /// and their siblings) — they bind directly to the platform's own picker APIs, with no
    /// `DialogScope` or `MakeModal()` involved. Any other dialog implemented through a different
    /// mechanism (a custom native window, or Editor UI that isn't an `EditorWindow`) is also not
    /// covered. Treat the absence of a <see cref="dialogWillShow"/>/<see cref="dialogDismissed"/>
    /// pair as "no dialog of a covered kind is open," not as proof the Editor isn't blocked by some
    /// other popup — callers still need a fallback signal (e.g. a command timeout) for the
    /// uncovered cases.
    /// </remarks>
    public static partial class EditorDialogEvents
    {
        /// <summary>Raised before a dialog is shown to the user.</summary>
        [AutoStaticsCleanupOnCodeReload]
        public static event Action<DialogEventInfo> dialogWillShow;
        /// <summary>Raised when a dialog that previously raised <see cref="dialogWillShow"/> is dismissed.</summary>
        [AutoStaticsCleanupOnCodeReload]
        public static event Action<DialogEventInfo> dialogDismissed;

        [NoAutoStaticsCleanup] // lock object, no state to reset, safe to persist across reload
        static readonly object s_Lock = new object();
        [AutoStaticsCleanupOnCodeReload]
        static readonly Dictionary<int, DialogEventInfo> s_Open = new Dictionary<int, DialogEventInfo>();

        /// <summary>Called from the native `DialogScope` constructors (Windows, macOS, and Linux) before the dialog is shown. Returns the ID to pass to `Internal_DialogDismissed`.</summary>
        [RequiredByNativeCode]
        internal static int Internal_DialogWillShow(string title, string message, string[] buttonLabels, int level)
        {
            return RaiseWillShow(DialogSource.NativeMessageBox, title, message, buttonLabels, (DialogType?)level);
        }

        /// <summary>Called from the native `DialogScope` destructors (Windows/OSX/Linux).</summary>
        [RequiredByNativeCode]
        internal static void Internal_DialogDismissed(int id)
        {
            RaiseDismissed(id);
        }

        /// <summary>Called directly from `EditorWindow.MakeModal` before the modal window is shown. No native dialog-notification bridge is needed, unlike the native message box path — it still asks native for the next dialog event id (see <see cref="GetNextDialogEventId"/>).</summary>
        internal static int RaiseManagedWillShow(string title)
        {
            return RaiseWillShow(DialogSource.ManagedCustomWindow, title, null, null, null);
        }

        /// <summary>Called directly from `EditorWindow.MakeModal`. No native bridge is needed.</summary>
        internal static void RaiseManagedDismissed(int id)
        {
            RaiseDismissed(id);
        }

        static int RaiseWillShow(DialogSource source, string title, string message, string[] buttonLabels, DialogType? level)
        {
            // Defensive copy: buttonLabels may come from native code that could reuse the array,
            // and the wrapper is read-only so no dialogWillShow/dialogDismissed subscriber can mutate
            // what other subscribers or the later dismissed event observe.
            IReadOnlyList<string> buttonLabelsSnapshot = buttonLabels != null ? Array.AsReadOnly((string[])buttonLabels.Clone()) : null;

            DialogEventInfo info;
            lock (s_Lock)
            {
                // Native-backed (see EditorDialogEvents.bindings.h): stays unique for the whole
                // editor session, not just since the last domain reload.
                var id = GetNextDialogEventId();
                info = new DialogEventInfo(id, source, title, message, buttonLabelsSnapshot, level, DateTime.UtcNow);
                s_Open[id] = info;
            }

            InvokeSafely(dialogWillShow, info);
            return info.Id;
        }

        static void RaiseDismissed(int id)
        {
            DialogEventInfo info;
            lock (s_Lock)
            {
                if (!s_Open.TryGetValue(id, out info))
                    return;
                s_Open.Remove(id);
            }

            InvokeSafely(dialogDismissed, info);
        }

        static void InvokeSafely(Action<DialogEventInfo> handlers, DialogEventInfo info)
        {
            if (handlers == null)
                return;

            foreach (Action<DialogEventInfo> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(info);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
    }
}
