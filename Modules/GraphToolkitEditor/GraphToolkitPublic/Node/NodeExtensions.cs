// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.GraphToolkit.Editor;

static class NodeExtensions
{
    public static void CheckNodeErrors(this NodeModel model, ErrorsAndWarningsResult res)
    {
        foreach (var port in model.InputsByDisplayOrder)
        {
            string errorMessage = null;
            foreach (var source in port.GetConnectedPorts())
            {
                if (source == null || ConnectionTypeValidForPortType(port, source))
                    continue;

                if (errorMessage == null)
                    errorMessage = string.Empty;
                else
                    errorMessage += "\n";

                errorMessage += $"Port {port.UniqueName} has connection of unexpected type : {source.PortDataType}";
            }

            if (errorMessage != null)
            {
                res.AddError(errorMessage, port);
            }
        }
    }

    static bool ConnectionTypeValidForPortType(PortModel port, PortModel source)
    {
        return port.DataTypeHandle == source.DataTypeHandle || port.DataTypeHandle.IsAssignableFrom(source.DataTypeHandle);
    }
}
