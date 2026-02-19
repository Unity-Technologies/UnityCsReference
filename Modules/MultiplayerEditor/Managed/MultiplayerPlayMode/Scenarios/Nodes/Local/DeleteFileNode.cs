// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    class DeleteFileNode : Node
    {
        [SerializeReference] private NodeInput<string> m_FilePath;

        public NodeInput<string> FilePath => m_FilePath;

        public DeleteFileNode(string name) : base(name)
        {
            m_FilePath = new(this);
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var filePath = GetInput(FilePath);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return Task.CompletedTask;
        }
    }
}
