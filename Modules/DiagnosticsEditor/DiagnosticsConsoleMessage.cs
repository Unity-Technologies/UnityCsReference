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
    internal class DiagnosticSwitchesConsoleMessage : ScriptableObject
    {
        private DiagnosticSwitch[] m_SwitchesInEffect;
        public static DiagnosticSwitchesConsoleMessage Instance { get; private set; }

        [InitializeOnLoadMethod]
        public static void Init()
        {
            Instance = Resources.FindObjectsOfTypeAll<DiagnosticSwitchesConsoleMessage>().FirstOrDefault();
            if (Instance == null)
            {
                Instance = CreateInstance<DiagnosticSwitchesConsoleMessage>();
            }
        }

        public void OnEnable()
        {
            m_SwitchesInEffect = new DiagnosticSwitch[0];
            Update();
        }

        public void Update()
        {
            var switchesInEffect = Debug.diagnosticSwitches.Where(diagnosticSwitch => !diagnosticSwitch.isSetToDefault)
                .ToArray();
            if (switchesInEffect.SequenceEqual(m_SwitchesInEffect))
                return;

            m_SwitchesInEffect = switchesInEffect;
            Debug.RemoveLogEntriesByIdentifier(GetInstanceID());

            if (m_SwitchesInEffect.Length > 0)
            {
                var message =
                    "Diagnostic switches are active and may impact performance or degrade your user experience." +
                    " Switches can be configured through the Diagnostics section in the Preferences window.\n\t"
                    + string.Join("\n\t",
                        switchesInEffect.Select(
                            diagnosticSwitch =>
                            {
                                var report = $"{diagnosticSwitch.name}: {diagnosticSwitch.value}";
                                if (diagnosticSwitch.needsRestart)
                                    report += $" (will change to {diagnosticSwitch.persistentValue} after restarting Unity)";
                                return report;
                            }));

                Debug.LogSticky(GetInstanceID(), LogType.Warning, LogOption.NoStacktrace, message);
            }
        }
    }
}
