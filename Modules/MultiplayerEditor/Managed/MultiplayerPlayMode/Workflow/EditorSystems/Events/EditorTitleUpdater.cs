// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class EditorTitleUpdater
    {
        readonly ApplicationTitleDescriptorProxy m_ApplicationTitle;

        public EditorTitleUpdater(ApplicationTitleDescriptorProxy applicationTitle)
        {
            m_ApplicationTitle = applicationTitle ?? throw new ArgumentNullException(nameof(applicationTitle));
        }


        public string title
        {
            set => m_ApplicationTitle.title = value;
        }
    }
}
