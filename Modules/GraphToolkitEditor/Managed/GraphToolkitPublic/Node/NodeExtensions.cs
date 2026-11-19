// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.GraphToolkit.Editor;

static class NodeExtensions
{
    // Returns res, or a new result if res is null and an error was found, so error-free nodes allocate nothing.
    public static ErrorsAndWarningsResult CheckNodeErrors(this NodeModel model, ErrorsAndWarningsResult res = null)
    {
        var graphModel = model.GraphModel;
        var inputs = model.InputsByDisplayOrder;
        for (var i = 0; i < inputs.Count; i++)
        {
            var port = inputs[i];
            var wires = port.GetConnectedWires();
            string errorMessage = null;
            for (var j = 0; j < wires.Count; j++)
            {
                var wire = wires[j];
                var fromPort = wire.FromPort;
                var source = fromPort == port ? wire.ToPort : fromPort;
                if (source == null || graphModel.CanAssignTo(port, source))
                    continue;

                if (errorMessage == null)
                    errorMessage = string.Empty;
                else
                    errorMessage += "\n";

                errorMessage += $"Port {port.UniqueName} has connection of unexpected type : {source.PortDataType}";
            }

            if (errorMessage != null)
            {
                res ??= new ErrorsAndWarningsResult();
                res.AddError(errorMessage, port);
            }
        }

        return res;
    }
}
