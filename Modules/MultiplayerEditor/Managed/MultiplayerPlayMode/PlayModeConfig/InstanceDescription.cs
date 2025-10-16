// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.ObjectModel;
using UnityEditor.Build.Profile;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    /// <summary>
    /// Base class for all instance descriptions.
    /// An instance description contains information about how to build and run an instance.
    /// </summary>
    [Serializable]
    abstract class InstanceDescription
    {
        public string Name;

        // Free Running Instances Running Mode state
        private RunModeState m_RunModeState = RunModeState.ScenarioControl;

        internal RunModeState RunModeState
        {
            get => m_RunModeState;
            set => m_RunModeState = value;
        }

        //[TODO]: we should remove this because one instance could have multiple nodes
        /// <summary>
        /// The nodeID that corresponds to this instance description.
        /// Useful to get the node for querying the status.
        /// </summary>
        [field: SerializeField]
        internal string CorrespondingNodeId { get; set; }

        /// <summary>
        /// The nodeIDs that correspond to this instance description.
        /// Useful to get the nodes for querying the status.
        /// </summary>
        [field: SerializeField, HideInInspector]
        private string[] m_Nodes;

        internal void SetCorrespondingNodes(params string[] nodeIds)
        {
            m_Nodes = nodeIds;
        }

        internal void SetCorrespondingNodes(params Node[] nodes)
        {
            var names = new string[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                names[i] = nodes[i]?.Name;
            }
            SetCorrespondingNodes(names);
        }

        internal ReadOnlyCollection<string> GetCorrespondingNodes()
        {
            m_Nodes ??= Array.Empty<string>();
            return Array.AsReadOnly(m_Nodes);
        }

        internal abstract string InstanceTypeName { get; }

        internal abstract string BuildTargetType { get; }

        internal abstract string MultiplayerRole { get; }
    }

    interface IBuildableInstanceDescription
    {
        /// <summary>
        /// The build profile that this instance will be based on.
        /// </summary>
        BuildProfile BuildProfile { get; set; }
    }
}
