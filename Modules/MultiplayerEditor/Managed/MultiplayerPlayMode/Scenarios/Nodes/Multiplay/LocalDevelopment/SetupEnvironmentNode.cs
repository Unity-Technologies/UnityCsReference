// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    class SetupEnvironmentNode : Node
    {
        [SerializeReference] protected NodeOutput<string> m_ProjectId;
        [SerializeReference] protected NodeOutput<string> m_EnvironmentId;
        [SerializeReference] protected NodeOutput<string> m_AuthToken;

        public NodeOutput<string> ProjectId => m_ProjectId;
        public NodeOutput<string> EnvironmentId => m_EnvironmentId;
        public NodeOutput<string> AuthToken => m_AuthToken;

        public SetupEnvironmentNode(string name) : base(name)
        {
            m_ProjectId = new(this);
            m_EnvironmentId = new(this);
            m_AuthToken = new(this);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var result = await IPlayModeServices.Instance.SetupSimEnvironmentAsync(cancellationToken);
            SetOutput(ProjectId, result.ProjectId);
            SetOutput(EnvironmentId, result.EnvironmentId);
            SetOutput(AuthToken, result.AuthToken);
        }
    }
}
