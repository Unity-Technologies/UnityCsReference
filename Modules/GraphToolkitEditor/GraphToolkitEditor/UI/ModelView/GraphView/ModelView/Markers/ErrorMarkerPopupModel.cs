// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolkit.Editor;

/// <summary>
/// Model for the error marker popup containing errors organized by graph scope.
/// </summary>
class ErrorMarkerPopupModel
{
    /// <summary>
    /// Groups errors from a specific subgraph.
    /// </summary>
    public class SubgraphErrorGroup
    {
        readonly List<ErrorMarkerModel> m_Errors = new();
        readonly List<ErrorMarkerModel> m_Warnings = new();
        readonly List<ErrorMarkerModel> m_Infos = new();
        public string GraphPath { get; }

        public IReadOnlyList<ErrorMarkerModel> Errors => m_Errors;

        public IReadOnlyList<ErrorMarkerModel> Warnings => m_Warnings;

        public IReadOnlyList<ErrorMarkerModel> Infos => m_Infos;
        public int Count =>  m_Errors.Count +  m_Warnings.Count + m_Infos.Count;

        public ErrorMarkerModel NavigationTarget =>
            m_Errors.Count > 0 ? m_Errors[0] :
            m_Warnings.Count > 0 ? m_Warnings[0] :
            m_Infos.Count > 0 ? m_Infos[0] : null;

        public SubgraphErrorGroup(string graphPath, List<ErrorMarkerModel> errors)
        {
            GraphPath = graphPath;
            foreach (var errorModel in errors)
            {
                switch (errorModel.ErrorType)
                {
                    case LogType.Assert:
                    case LogType.Exception:
                    case LogType.Error:
                        m_Errors.Add(errorModel);
                        break;
                    case LogType.Warning:
                        m_Warnings.Add(errorModel);
                        break;
                    case LogType.Log:
                        m_Infos.Add(errorModel);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }

    public List<ErrorMarkerModel> CurrentGraphErrors { get; }
    public IReadOnlyList<SubgraphErrorGroup> SubgraphErrors { get; }

    public ErrorMarkerPopupModel(
        IReadOnlyList<ErrorMarkerModel> currentGraphErrors,
        IReadOnlyList<SubgraphErrorGroup> subgraphErrors)
    {
        CurrentGraphErrors = new List<ErrorMarkerModel>(currentGraphErrors);
        SubgraphErrors = new List<SubgraphErrorGroup>(subgraphErrors);
    }
}
