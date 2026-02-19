// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UIElements;

namespace UnityEditor
{
    internal class DiagnosticSwitchesConsoleMessage : ScriptableSingleton<DiagnosticSwitchesConsoleMessage>
    {
        private DiagnosticSwitch[] m_SwitchesInEffect;

        [InitializeOnLoadMethod]
        public static void Init()
        {
            _ = instance;
        }

        public void OnEnable()
        {
            m_SwitchesInEffect = Array.Empty<DiagnosticSwitch>();
            Update();
        }

        public void Update()
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var switchesInEffect = Debug.diagnosticSwitches.Where(diagnosticSwitch => !diagnosticSwitch.isSetToDefault)
#pragma warning restore UA2001
                .ToArray();
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (switchesInEffect.SequenceEqual(m_SwitchesInEffect))
#pragma warning restore UA2001
                return;

            m_SwitchesInEffect = switchesInEffect;

            // Note: RemoveLogEntriesByIdentifier should be using EntityId just like LogSticky
            // Work is ongoing to get this fixed, so as a temporary solution we force the cast to int
            // And this should be turned back into an EntityId asap so it's symmetrical with LogSticky
            var fixme = (int)EntityId.ToULong(GetEntityId());
            Debug.RemoveLogEntriesByIdentifier(fixme);

            if (m_SwitchesInEffect.Length > 0)
            {
                var message =
                    "Diagnostic switches are active and may impact performance or degrade your user experience." +
                    " Switches can be configured through the Diagnostics section in the Preferences window.\n\t"
                    + string.Join("\n\t",
                        #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                        switchesInEffect.Select(
#pragma warning restore UA2001
                            diagnosticSwitch =>
                            {
                                var report = $"{diagnosticSwitch.name}: {diagnosticSwitch.value}";
                                if (diagnosticSwitch.needsRestart)
                                    report += $" (will change to {diagnosticSwitch.persistentValue} after restarting Unity)";
                                return report;
                            }));

                Debug.LogSticky(GetEntityId(), LogType.Warning, LogOption.NoStacktrace, message);
            }
        }
    }
}
