// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor
{
    public enum DialogIconType
    {
        Info,
        Warning,
        Error
    }

    public enum DialogOptOutDecisionType
    {
        ForThisSession,
        /// <summary>
        /// The decision to opt out of seeing a dialog box for all time for the current user.
        /// </summary>
        /// <remarks>
        /// The decision is stored using <see cref="EditorPrefs"/>, and is remembered across Editor sessions.
        /// When specified, the dialog box will contain a checkbox with the label &quot;Don't ask again on this computer&quot; located below the message.
        /// If somehow a decision is set for both <see cref="ForThisSession"/> and <see cref="ForThisUser"/>, the decision for <see cref="ForThisUser"/> will be used.
        /// </remarks>
        ForThisUser,
        ForThisMachine = ForThisUser
    }

    public enum DialogResult
    {
        Cancel = -1,
        /// <summary>
        /// The user clicked the default button, or the user pressed the enter key. (Return key on macOS.)
        /// </summary>
        DefaultAction,
        /// <summary>
        /// The user clicked the alternate button.
        /// </summary>
        AlternateAction
    }

    public sealed partial class EditorDialog
    {
        private static readonly string k_OkButtonText = L10n.Tr("OK");
        private static readonly string k_CancelButtonText = L10n.Tr("Cancel");
        private static readonly string k_DefaultOptionButtonText = L10n.Tr("Yes");
        private static readonly string k_AlternateOptionText = L10n.Tr("No");

        private static readonly string k_OptOutPrefix = "DialogOptOut.";
        private static string PrefixedKey(string key) => k_OptOutPrefix + key;

        private EditorDialog() { }

        // Returns the result of a dialog that was triggered by the interaction context.
        // To preserve existing automation, pass in the title without branding.
        private static DialogResult? GetDialogResponseFromInteractionContext(string titleText, params string[] buttons)
        {
            string automationString = GetDialogResponseFromInteractionContextNative(titleText);
            if (string.IsNullOrEmpty(automationString))
                return null;

            int buttonIndex = Array.IndexOf(buttons, automationString);

            switch (buttonIndex)
            {
                case 0:
                    return DialogResult.DefaultAction;
                case 1:
                    return buttons.Length == 3 ? DialogResult.AlternateAction : DialogResult.Cancel;
                default:
                    return DialogResult.Cancel;
            }

            throw new Exception($"When dialogs are disabled by automationString all dialogs should have responses and in the correct order. Dialog '{titleText}' doesn't have an answer.");
        }

        // Returns the opt-out result for the provided key if one has been set for either the Machine or the Session.
        // If a result is set for both Session and Machine, the Machine result is returned (overrides the Session setting).
        // It should not be possible to have a result set for both Machine and Session.
        // If a result hasn't been set for neither Machine nor Session, then null is returned.
        private static DialogResult? GetOptOutResultForKey(string key)
        {
            string prefixedKey = PrefixedKey(key);
            if (EditorPrefs.HasKey(prefixedKey))
                return (DialogResult)EditorPrefs.GetInt(prefixedKey);

            int sessionResult = SessionState.GetInt(prefixedKey, -1);

            // We never store a result of -1 in the SessionState, so if we get -1 back, it means no result has been set.
            return sessionResult == -1 ? null : (DialogResult)sessionResult;
        }

        private static void SetOptOutResultForKey(string key, DialogResult result, DialogOptOutDecisionType decisionType)
        {
            string prefixedKey = PrefixedKey(key);
            switch (decisionType)
            {
                case DialogOptOutDecisionType.ForThisSession:
                    SessionState.SetInt(prefixedKey, (int)result);
                    break;
                case DialogOptOutDecisionType.ForThisUser:
                    EditorPrefs.SetInt(prefixedKey, (int)result);
                    break;
                default:
                    throw new NotImplementedException("Unknown opt out decision type");
            }
        }

        private static string GetOptOutCheckboxLabel(DialogOptOutDecisionType optOutDecisionType)
        {
            switch (optOutDecisionType)
            {
                case DialogOptOutDecisionType.ForThisSession:
                    return L10n.Tr("Don't ask again for this session");
                case DialogOptOutDecisionType.ForThisUser:
                    return L10n.Tr("Don't ask again on this computer");
                default:
                    throw new NotImplementedException("Unknown opt out decision type");
            }
        }

        private static string AdjustTitleText(string titleText)
        {
            if (string.IsNullOrWhiteSpace(titleText))
                return "Unity";

            return titleText.Trim();
        }

        private static string AdjustButtonText(string buttonText, string defaultButtonText)
        {
            string trimmed = buttonText?.Trim();
            return string.IsNullOrEmpty(trimmed) ? defaultButtonText : trimmed;
        }

        // Limits the message length to kMaxMessageLength and logs the entire meesage if it is longer than that.
        private static string LimitMessageLength(string titleText, string messageText, DialogIconType iconType, string[] buttons)
        {
            const int kMaxMessageLength = 512; // This is historically the limit on macOS, but it's a good limit to have in general.

            if (messageText.Length <= kMaxMessageLength)
                return messageText;

            string truncationMessage = L10n.Tr("...\n\n(For the full message, see the editor log file)\n");

            int truncationLength = kMaxMessageLength - truncationMessage.Length;

            if (char.IsHighSurrogate(messageText[truncationLength - 1]))
            {
                // If we're cutting off a high surrogate, we need to include the low surrogate as well.
                truncationLength--;
            }

            LogType logType;
            switch (iconType)
            {
                case DialogIconType.Info:
                    logType = LogType.Log;
                    break;
                case DialogIconType.Warning:
                    logType = LogType.Warning;
                    break;
                case DialogIconType.Error:
                    logType = LogType.Error;
                    break;
                default:
                    throw new NotImplementedException("Unknown dialog icon type");
            }

            // Print markdown of the entire dialog to the log.
            // e.g. # Title
            //
            //      Message that spans multiple lines
            //
            //      | Button1 | Button2 |
            Debug.LogFormat(logType, LogOption.NoStacktrace, null, "# {0}\n\n{1}\n\n| {2} |\n", titleText, messageText.Trim(), string.Join(" | ", buttons));

            return messageText.Substring(0, truncationLength) + truncationMessage;
        }

        private static void ThrowIfInvalidKey(string optOutKey)
        {
            // We want to ensure that the key string is a valid XML tag.
            // More technically, we could restrict it to whichever is more restrictive of
            // XML tags (Linux), Windows Registry key names, or NSPreference dictionaries
            // (which are all valid XML tags), but this will simplify the validation for API users.
            // Our EditorPrefs are stored in the registry, with this key as the path.
            // The maximum length of a registry key is 255 characters, so we limit the key
            // to account for the "Software\Unity Technologies\Unity Editor 5.x Automated Testing\" prefix and a 12 character suffix.
            // We also prefix the key with "DialogOptOut." (13 chars) to ensure that it is unique to the dialog box API.
            // For this reason, we will limit to a safer ~ 127 character limit.

            // This regular expression ensures that the key string:
            // Contains only alphanumeric characters, periods, underscores, and hyphens.
            // Is between 1 and 127 characters long.
            if (string.IsNullOrEmpty(optOutKey) || !Regex.IsMatch(optOutKey, @"^[A-Za-z0-9._-]{1,127}$"))
                throw new ArgumentException($"Key must be non-null, between 1 and 127 characters, and be a valid XML tag.", nameof(optOutKey));
        }

        private static void ThrowIfMessageIsInvalid(string messageText)
        {
            if (string.IsNullOrWhiteSpace(messageText))
                throw new ArgumentNullException(nameof(messageText), "Dialog message text cannot be null or whitespace.");
        }

        private static void ThrowIfButtonTextIsInvalid(string buttonText, string parameterName)
        {
            const int kMaxButtonTextLength = 64; // This limit is probably not necessary, but it's good to have a limit on the button text as well.
            if (buttonText != null && buttonText.Length > kMaxButtonTextLength)
                throw new ArgumentException($"Text on buttons must be less than {kMaxButtonTextLength} characters long.", parameterName);
        }

        /// <summary>
        /// Displays a simple alert dialog box with a title, an icon, a message, and a single button.
        /// </summary>
        /// <param name="messageText">The message to display in the dialog box.</param>
        /// <param name="iconType">The icon to display in the dialog box. Defaults to <see cref="DialogIconType.Warning"/>.</param>
        /// <param name="titleText">The title of the dialog box. If left null, defaults to "Unity".</param>
        /// <param name="buttonText">The text to display on the button. If left null, defaults to "OK".</param>
        /// <remarks>
        /// If <paramref name="messageText"/> is null or whitespace, an <see cref="ArgumentNullException"/> is thrown.
        /// If <paramref name="messageText"/> is longer than 512 characters, it is truncated and the full message is logged to the console in markdown format.
        /// 
        /// If <paramref name="buttonText"/> is longer than 64 characters, an <see cref="ArgumentException"/> is thrown. 
        /// </remarks>
        [RequiredByNativeCode]
        public static void DisplayAlertDialog(
            string titleText,
            string messageText,
            string buttonText,
            DialogIconType iconType = DialogIconType.Warning)
        {
            ThrowIfMessageIsInvalid(messageText);
            ThrowIfButtonTextIsInvalid(buttonText, nameof(buttonText));

            if (!string.IsNullOrEmpty(GetDialogResponseFromInteractionContextNative(titleText)))
                return;

            var adjustedTitleText = AdjustTitleText(titleText);
            var adjustedButtonText = AdjustButtonText(buttonText, k_OkButtonText);
            var limitedMessageText = LimitMessageLength(adjustedTitleText, messageText, iconType, new[] { adjustedButtonText });

            using (new EditorGUI.DisabledGuiViewInputScope(GUIView.current, true))
            {
                DisplayAlertDialogNative(limitedMessageText, iconType, adjustedTitleText, adjustedButtonText);
            }
        }

        /// <summary>
        /// Displays a simple alert dialog box with a title, icon, message, a button, and an opt-out checkbox.
        /// </summary>
        /// <param name="optOutKey">The key to use for storing the opt-out decision.</param>
        /// <param name="optOutDecisionType">Where the result is stored if the user opts out of the dialog by checking the box.</param>
        /// <param name="messageText">The message to display in the dialog box.</param>
        /// <param name="iconType">The icon to display in the dialog box. Defaults to <see cref="DialogIconType.Warning"/>.</param>
        /// <param name="titleText">The title of the dialog box. If left null, defaults to "Unity".</param>
        /// <param name="buttonText">The text to display on the button. If left null, defaults to "OK".</param>
        /// <remarks>
        /// If <paramref name="messageText"/> is null or whitespace, an <see cref="ArgumentNullException"/> is thrown.
        /// If <paramref name="messageText"/> is longer than 512 characters, it is truncated and the full message is logged to the console in markdown format.
        /// 
        /// If <paramref name="buttonText"/> is longer than 64 characters, an <see cref="ArgumentException"/> is thrown.
        /// 
        /// If <paramref name="optOutKey"/> is not a valid XML tag, or more than 127 characters long, an <see cref="ArgumentException"/> is thrown.
        /// Care should be taken to ensure that the key is unique to the dialog box being displayed to prevent conflicts.
        /// </remarks>
        [RequiredByNativeCode]
        public static void DisplayAlertDialogWithOptOut(
            string titleText,
            string messageText,
            string buttonText,
            DialogOptOutDecisionType optOutDecisionType,
            string optOutKey,
            DialogIconType iconType = DialogIconType.Warning)
        {
            ThrowIfInvalidKey(optOutKey);
            ThrowIfMessageIsInvalid(messageText);
            ThrowIfButtonTextIsInvalid(buttonText, nameof(buttonText));

            if (!string.IsNullOrEmpty(GetDialogResponseFromInteractionContextNative(titleText)))
                return;

            var result = GetOptOutResultForKey(optOutKey);
            if (result.HasValue)
                return;

            var adjustedTitleText = AdjustTitleText(titleText);
            var adjustedButtonText = AdjustButtonText(buttonText, k_OkButtonText);
            var limitedMessageText = LimitMessageLength(adjustedTitleText, messageText, iconType, new[] { adjustedButtonText });

            bool optOut = false;

            using (new EditorGUI.DisabledGuiViewInputScope(GUIView.current, true))
            {
                DisplayAlertDialogWithOptOutNative(GetOptOutCheckboxLabel(optOutDecisionType), limitedMessageText, iconType, adjustedTitleText, adjustedButtonText, out optOut);
            }

            if (optOut)
                SetOptOutResultForKey(optOutKey, DialogResult.DefaultAction, optOutDecisionType);
        }

        /// <summary>
        /// Displays a decision dialog box with a title, icon, message, and two buttons.
        /// </summary>
        /// <param name="messageText">The message to display in the dialog box.</param>
        /// <param name="iconType">The icon to display in the dialog box. Defaults to <see cref="DialogIconType.Warning"/>.</param>
        /// <param name="titleText">The title of the dialog box. If left null, defaults to "Unity".</param>
        /// <param name="yesButtonText">The text to display on the Yes button. If left null, it defaults to "Yes".</param>
        /// <param name="noButtonText">The text to display on the No button. If left null, it defaults to "No".</param>
        /// <returns><c>true</c> if the user clicked the Yes button, <c>false</c> if the user clicked the No button.</returns>
        /// <remarks>
        /// <para>
        /// If <paramref name="yesButtonText"/> or <paramref name="noButtonText"/> is longer than 64 characters, an <see cref="ArgumentException"/> is thrown.
        /// If <paramref name="messageText"/> is null or whitespace, an <see cref="ArgumentNullException"/> is thrown.
        /// If <paramref name="messageText"/> is longer than 512 characters, it is truncated and the full message is logged to the console in markdown format.
        /// </para>
        /// <para>The default button is the Yes button. If the user presses the enter key (return key on macOS), the Yes button is clicked.
        /// The other button is the No button. If the user presses the escape key or closes the dialog, the No button is clicked.
        /// On macOS, the orientation of the buttons may be horizontal or vertical depending on the length of the message.
        /// </para>
        /// </remarks>
        [RequiredByNativeCode]
        public static bool DisplayDecisionDialog(
            string titleText,
            string messageText,
            string yesButtonText,
            string noButtonText,
            DialogIconType iconType = DialogIconType.Warning)
        {
            ThrowIfMessageIsInvalid(messageText);
            ThrowIfButtonTextIsInvalid(yesButtonText, nameof(yesButtonText));
            ThrowIfButtonTextIsInvalid(noButtonText, nameof(noButtonText));

            var adjustedTitleText = AdjustTitleText(titleText);

            var adjustedYesButtonText = AdjustButtonText(yesButtonText, k_DefaultOptionButtonText);
            var adjustedNoButtonText = AdjustButtonText(noButtonText, k_AlternateOptionText);

            var result = GetDialogResponseFromInteractionContext(titleText, adjustedYesButtonText, adjustedNoButtonText);
            if (result.HasValue)
                return result == DialogResult.DefaultAction;

            var limitedMessageText = LimitMessageLength(adjustedTitleText, messageText, iconType, new[] { adjustedYesButtonText, adjustedNoButtonText });

            using (new EditorGUI.DisabledGuiViewInputScope(GUIView.current, true))
            {
                return DisplayDecisionDialogNative(limitedMessageText, iconType, adjustedTitleText, adjustedYesButtonText, adjustedNoButtonText);
            }
        }

        /// <summary>
        /// Displays a decision dialog box with a title, icon, message, two buttons, and an opt-out checkbox.
        /// </summary>
        /// <param name="optOutKey">The key to use for storing the opt-out decision.</param>
        /// <param name="optOutDecisionType">Where the result is stored if the user opts out of the dialog by checking the box.</param>
        /// <param name="messageText">The message to display in the dialog box.</param>
        /// <param name="iconType">The icon to display in the dialog box. Defaults to <see cref="DialogIconType.Warning"/>.</param>
        /// <param name="titleText">The title of the dialog box. If left null, defaults to "Unity".</param>
        /// <param name="yesButtonText">The text to display on the Yes button. If left null, it defaults to "Yes".</param>
        /// <param name="noButtonText">The text to display on the No button. If left null, it defaults to "No".</param>
        /// <returns><c>true</c> if the user clicked the Yes button, <c>false</c> if the user clicked the No button.</returns>
        /// <remarks>
        /// <para>
        /// If <paramref name="yesButtonText"/> or <paramref name="noButtonText"/> is longer than 64 characters, an <see cref="ArgumentException"/> is thrown.
        /// If <paramref name="messageText"/> is null or whitespace, an <see cref="ArgumentNullException"/> is thrown.
        /// If <paramref name="messageText"/> is longer than 512 characters, it is truncated and the full message is logged to the console in markdown format.
        /// </para>
        /// <para>
        /// The default button is the Yes button. If the user presses the enter key (return key on macOS), the Yes button is clicked.
        /// The other button is the No button. If the user presses the escape key or closes the dialog, the No button is clicked.
        /// On macOS, the orientation of the buttons may be horizontal or vertical depending on the length of the message.
        /// </para>
        /// <para>
        /// If <paramref name="optOutKey"/> is not a valid XML tag, or more than 127 characters long, an <see cref="ArgumentException"/> is thrown.
        /// Care should be taken to ensure that the key is unique to the dialog box being displayed to prevent conflicts.
        /// The decision is only stored if the user checks the opt-out checkbox and clicks the Yes button.
        /// </para>
        /// </remarks>
        [RequiredByNativeCode]
        public static bool DisplayDecisionDialogWithOptOut(
            string titleText,
            string messageText,
            string yesButtonText,
            string noButtonText,
            DialogOptOutDecisionType optOutDecisionType,
            string optOutKey,
            DialogIconType iconType = DialogIconType.Warning)
        {
            ThrowIfInvalidKey(optOutKey);
            ThrowIfMessageIsInvalid(messageText);
            ThrowIfButtonTextIsInvalid(yesButtonText, nameof(yesButtonText));
            ThrowIfButtonTextIsInvalid(noButtonText, nameof(noButtonText));

            var result = GetOptOutResultForKey(optOutKey);
            if (result.HasValue)
                return result == DialogResult.DefaultAction;

            var adjustedTitleText = AdjustTitleText(titleText);

            var adjustedYesButtonText = AdjustButtonText(yesButtonText, k_DefaultOptionButtonText);
            var adjustedNoButtonText = AdjustButtonText(noButtonText, k_AlternateOptionText);

            result = GetDialogResponseFromInteractionContext(titleText, adjustedYesButtonText, adjustedNoButtonText);
            if (result.HasValue)
                return result == DialogResult.DefaultAction;

            var limitedMessageText = LimitMessageLength(adjustedTitleText, messageText, iconType, new[] { adjustedYesButtonText, adjustedNoButtonText });

            bool optOut = false;
            bool resultIsYes = false;

            using (new EditorGUI.DisabledGuiViewInputScope(GUIView.current, true))
            {
                resultIsYes = DisplayDecisionDialogWithOptOutNative(
                    GetOptOutCheckboxLabel(optOutDecisionType),
                    limitedMessageText,
                    iconType,
                    adjustedTitleText,
                    adjustedYesButtonText,
                    adjustedNoButtonText,
                    out optOut);
            }

            if (optOut && resultIsYes)
                SetOptOutResultForKey(optOutKey, DialogResult.DefaultAction, optOutDecisionType);

            return resultIsYes;
        }

        /// <summary>
        /// Displays a complex decision dialog box with a title, icon, message, and three buttons.
        /// </summary>
        /// <param name="messageText">The message to display in the dialog box.</param>
        /// <param name="iconType">The icon to display in the dialog box. Defaults to <see cref="DialogIconType.Warning"/>.</param>
        /// <param name="titleText">The title of the dialog box. If left null, defaults to "Unity".</param>
        /// <param name="defaultButtonText">The text to display on the default button. If left null, it defaults to "Yes".</param>
        /// <param name="altButtonText">The text to display on the alternate button. If left null, it defaults to "No".</param>
        /// <param name="cancelButtonText">The text to display on the cancel button. If left null, it defaults to "Cancel".</param>
        /// <returns>The result of the dialog box. <see cref="DialogResult.Cancel"/> if the user clicked the cancel button, <see cref="DialogResult.DefaultAction"/> if the user clicked the default button, and <see cref="DialogResult.AlternateAction"/> if the user clicked the alternate button.</returns>
        /// <remarks>
        /// <para>
        /// If <paramref name="defaultButtonText"/>, <paramref name="altButtonText"/>, or <paramref name="cancelButtonText"/> is longer than 64 characters, an <see cref="ArgumentException"/> is thrown.
        /// If <paramref name="messageText"/> is null or whitespace, an <see cref="ArgumentNullException"/> is thrown.
        /// If <paramref name="messageText"/> is longer than 512 characters, it is truncated and the full message is logged to the console in markdown format.
        /// </para>
        /// <para>
        /// If the user presses the enter key (return key on macOS), the default button is clicked.
        /// The alternate button is for additional actions, or to provide a different choice than the default button.
        /// The cancel button is for closing the dialog box without taking any action. If the user presses the escape key or closes the dialog, the cancel button is clicked.
        /// On macOS, the orientation of the buttons may be horizontal or vertical depending on the length of the message. Additionally, if the layout is horizontal, the button layout is different from Windows or Linux, with the default button on the right and the alternate button on the far left.
        /// </para>
        /// </remarks>
        [RequiredByNativeCode]
        public static DialogResult DisplayComplexDecisionDialog(
            string titleText,
            string messageText,
            string defaultButtonText,
            string altButtonText,
            string cancelButtonText,
            DialogIconType iconType = DialogIconType.Warning)
        {
            ThrowIfMessageIsInvalid(messageText);
            ThrowIfButtonTextIsInvalid(defaultButtonText, nameof(defaultButtonText));
            ThrowIfButtonTextIsInvalid(altButtonText, nameof(altButtonText));
            ThrowIfButtonTextIsInvalid(cancelButtonText, nameof(cancelButtonText));

            var adjustedTitleText = AdjustTitleText(titleText);

            var adjustedDefaultButtonText = AdjustButtonText(defaultButtonText, k_DefaultOptionButtonText);
            var adjustedAltButtonText = AdjustButtonText(altButtonText, k_AlternateOptionText);
            var adjustedCancelButtonText = AdjustButtonText(cancelButtonText, k_CancelButtonText);

            var result = GetDialogResponseFromInteractionContext(titleText, defaultButtonText, altButtonText, cancelButtonText);
            if (result.HasValue)
                return result.Value;

            var limitedMessageText = LimitMessageLength(adjustedTitleText, messageText, iconType, new[] { adjustedDefaultButtonText, adjustedAltButtonText, adjustedCancelButtonText });

            using (new EditorGUI.DisabledGuiViewInputScope(GUIView.current, true))
            {
                return DisplayComplexDecisionDialogNative(limitedMessageText, iconType, adjustedTitleText, adjustedDefaultButtonText, adjustedAltButtonText, adjustedCancelButtonText);
            }
        }

        /// <summary>
        /// Displays a complex decision dialog box with a title, icon, message, three buttons, and an opt-out checkbox.
        /// </summary>
        /// <param name="optOutKey">The key to use for storing the opt-out decision.</param>
        /// <param name="optOutDecisionType">Where the result is stored if the user opts out of the dialog by checking the box.</param>
        /// <param name="messageText">The message to display in the dialog box.</param>
        /// <param name="iconType">The icon to display in the dialog box. Defaults to <see cref="DialogIconType.Warning"/>.</param>
        /// <param name="titleText">The title of the dialog box. If left null, defaults to "Unity".</param>
        /// <param name="defaultButtonText">The text to display on the default button. If left null, it defaults to "Yes".</param>
        /// <param name="altButtonText">The text to display on the alternate button. If left null, it defaults to "No".</param>
        /// <param name="cancelButtonText">The text to display on the cancel button. If left null, it defaults to "Cancel".</param>
        /// <returns>The result of the dialog box. <see cref="DialogResult.Cancel"/> if the user clicked the cancel button, <see cref="DialogResult.DefaultAction"/> if the user clicked the default button, and <see cref="DialogResult.AlternateAction"/> if the user clicked the alternate button.</returns>
        /// <remarks>
        /// <para>If <paramref name="defaultButtonText"/>, <paramref name="altButtonText"/>, or <paramref name="cancelButtonText"/> is longer than 64 characters, an <see cref="ArgumentException"/> is thrown.
        /// If <paramref name="messageText"/> is null or whitespace, an <see cref="ArgumentNullException"/> is thrown.
        /// If <paramref name="messageText"/> is longer than 512 characters, it is truncated and the full message is logged to the console in markdown format.
        /// </para>
        /// <para>
        /// If the user presses the enter key (return key on macOS), the default button is clicked.
        /// The alternate button is for additional actions, or to provide a different choice than the default button.
        /// The cancel button is for closing the dialog box without taking any action. If the user presses the escape key or closes the dialog, the cancel button is clicked.
        /// On macOS, the orientation of the buttons may be horizontal or vertical depending on the length of the message. Additionally, if the layout is horizontal, the button layout is different from Windows or Linux, with the default button on the right and the alternate button on the far left.
        /// </para>
        /// <para>
        /// If <paramref name="optOutKey"/> is not a valid XML tag, or more than 127 characters long, an <see cref="ArgumentException"/> is thrown.
        /// Care should be taken to ensure that the key is unique to the dialog box being displayed to prevent conflicts.
        /// The decision is not stored if the user clicks the cancel button.
        /// </para>
        /// </remarks>
        [RequiredByNativeCode]
        public static DialogResult DisplayComplexDecisionDialogWithOptOut(
            string titleText,
            string messageText,
            string defaultButtonText,
            string altButtonText,
            string cancelButtonText,
            DialogOptOutDecisionType optOutDecisionType,
            string optOutKey,
            DialogIconType iconType = DialogIconType.Warning)
        {
            ThrowIfInvalidKey(optOutKey);
            ThrowIfMessageIsInvalid(messageText);
            ThrowIfButtonTextIsInvalid(defaultButtonText, nameof(defaultButtonText));
            ThrowIfButtonTextIsInvalid(altButtonText, nameof(altButtonText));
            ThrowIfButtonTextIsInvalid(cancelButtonText, nameof(cancelButtonText));

            var result = GetOptOutResultForKey(optOutKey);
            if (result.HasValue)
                return result.Value;

            var adjustedTitleText = AdjustTitleText(titleText);

            var adjustedDefaultButtonText = AdjustButtonText(defaultButtonText, k_DefaultOptionButtonText);
            var adjustedAltButtonText = AdjustButtonText(altButtonText, k_AlternateOptionText);
            var adjustedCancelButtonText = AdjustButtonText(cancelButtonText, k_CancelButtonText);

            result = GetDialogResponseFromInteractionContext(titleText, defaultButtonText, altButtonText, cancelButtonText);
            if (result.HasValue)
                return result.Value;

            var limitedMessageText = LimitMessageLength(adjustedTitleText, messageText, iconType, new[] { adjustedDefaultButtonText, adjustedAltButtonText, adjustedCancelButtonText });

            bool optOut = false;
            DialogResult decision = DialogResult.Cancel;

            using (new EditorGUI.DisabledGuiViewInputScope(GUIView.current, true))
            {
                decision = DisplayComplexDecisionDialogWithOptOutNative(
                    GetOptOutCheckboxLabel(optOutDecisionType),
                    limitedMessageText,
                    iconType,
                    adjustedTitleText,
                    adjustedDefaultButtonText,
                    adjustedAltButtonText,
                    adjustedCancelButtonText,
                    out optOut);
            }

            if (optOut && decision != DialogResult.Cancel)
                SetOptOutResultForKey(optOutKey, decision, optOutDecisionType);
            return decision;
        }

        internal static void ResetAllOptOuts()
        {
            // Reset session state
            var sessionKeys = SessionState.GetSessionStateIntTypeKeys();
            foreach (var sessionKey in sessionKeys)
            {
                if (sessionKey.StartsWith(k_OptOutPrefix))
                    SessionState.EraseInt(sessionKey);
            }

            // Reset editor prefs
            var editorPrefKeys = EditorPrefs.GetKeys();
            foreach (var editorPrefKey in editorPrefKeys)
            {
                if (editorPrefKey.StartsWith(k_OptOutPrefix))
                    EditorPrefs.DeleteKey(editorPrefKey);
            }
        }
    }
}
