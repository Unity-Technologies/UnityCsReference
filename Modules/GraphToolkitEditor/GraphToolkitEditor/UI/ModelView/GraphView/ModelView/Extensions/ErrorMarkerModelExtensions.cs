// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.Editor.Implementation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor;

static class ErrorMarkerModelExtensions
{
    static object GetTarget(this ErrorMarkerModel model, RootView rootView)
    {
        if (rootView is GraphView graphView)
        {
            GraphElementModel parent = model.GetParentModel(graphView.GraphModel);
            if (parent is IUserNodeModelImp userNodeModel)
                return userNodeModel.Node;
            if (parent is PortModel portModel)
                return portModel;
        }
        return null;
    }

    public static string GetEntryPrefix(this ErrorMarkerModel model)
    {
        switch (model.ErrorType)
        {
            case LogType.Error:
            case LogType.Assert:
            case LogType.Exception:
                return L10n.Tr("Error");
            case LogType.Warning:
                return L10n.Tr("Warning");
            case LogType.Log:
                return L10n.Tr("Log");
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public static bool TryAppendAction(this ErrorMarkerModel model, DropdownMenu menu, RootView rootView, string prefix = "")
    {
        if (model.Action == null)
            return false;

        menu.AppendAction(
            $"{prefix}" + model.Action.Description,
            _ => model.Action.Action(model.UserData ?? model.GetTarget(rootView)));

        return true;
    }
}
