// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.Connect
{
    /// <summary>
    /// This class provides a method that replaces slashes in project names, keeps track of modified
    /// project names, and encapsulates the last project name used.
    /// </summary>
    /// <remarks>
    /// This is a hack to prevent UI display issues for UGS projects that contain a slash (to prevent the creation
    /// of UI sub-menus). This hack could be removed in the future, if UGS dashboard prevents users from inputting
    /// slashes in project names, or when UI team develops a feature to deactivate sub-menu creation on slashes.
    /// </remarks>
    internal class ProjectNameSlashReplacer
    {
        internal const string k_FakeSlashUnicode = "\uff0f";
        string m_LastProjectName;
        internal List<string> m_ModifiedProjectNames = new List<string>();

        internal string LastProjectName
        {
            get
            {
                // Replaces a fake slash character by a real slash if the project name was modified. Slashes in
                // project names get replaced with fake slashes for the UI. Here, we revert that. This is because
                // in the code we want the true project names.
                if (m_ModifiedProjectNames.Contains(m_LastProjectName))
                {
                    return m_LastProjectName.Replace(k_FakeSlashUnicode, "/");
                }
                else
                {
                    return m_LastProjectName;
                }
            }
            set => m_LastProjectName = value;
        }

        /// <summary>
        /// Replaces the regular slash character with a stylized slash for all strings in a list
        /// </summary>
        /// <param name="input">Strings to be modified</param>
        /// <returns>List of modified strings if they contained a slash, original strings otherwise</returns>
        internal List<string> ReplaceSlashForFakeSlash(List<string> input)
        {
            m_ModifiedProjectNames.Clear();

            if (input == null)
            {
                return null;
            }

            for (int i = 0; i < input.Count; i++)
            {
                if (input[i].Contains("/"))
                {
                    input[i] = input[i].Replace("/", k_FakeSlashUnicode);
                    m_ModifiedProjectNames.Add(input[i]);
                }
            }

            return input;
        }
    }
}
