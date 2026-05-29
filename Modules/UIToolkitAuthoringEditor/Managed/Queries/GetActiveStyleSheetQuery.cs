// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

class GetActiveStyleSheetQuery : Command<GetActiveStyleSheetQuery>
{
    public static StyleSheet Get()
    {
        var result = default(StyleSheet);
        UICommandQueue.RegisterHandler<QueryPayload>(Handler);
        try
        {
            using var command = GetPooled();
            UICommandQueue.EnqueueCommand(command);
        }
        finally
        {
            UICommandQueue.UnregisterHandler<QueryPayload>(Handler);
        }
        return result;

        void Handler(in CommandContext context)
        {
            result = ((QueryPayload)context.Command).Payload;
        }
    }

    public class QueryPayload : Command<QueryPayload>
    {
        public static QueryPayload GetPooled(object source, StyleSheet ss)
        {
            var cmd = GetPooled();
            cmd.Source = source;
            cmd.Payload = ss;
            return cmd;
        }

        public StyleSheet Payload { get; private set; }

        protected override void Init()
        {
            base.Init();
            Payload = null;
        }

        public override bool Validate() => true;

        public override CommandExecutionStatus Execute() => Payload
            ? CommandExecutionStatus.Success
            : CommandExecutionStatus.ExecutionFailed;
    }

    public override bool Validate() => true;
    public override CommandExecutionStatus Execute() => CommandExecutionStatus.Success;
}
