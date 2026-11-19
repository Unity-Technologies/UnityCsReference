// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

class GetCanvasThemeQuery : Command<GetCanvasThemeQuery>
{
    public static StyleSheet Get(AuthoringContext context)
    {
        var result = default(StyleSheet);
        context.Messages.RegisterHandler<QueryPayload>(Handler);
        try
        {
            using var command = GetPooled();
            context.Messages.Execute(command);
        }
        finally
        {
            context.Messages.UnregisterHandler<QueryPayload>(Handler);
        }
        return result;

        void Handler(in CommandContext commandContext)
        {
            result = ((QueryPayload)commandContext.Command).Payload;
        }
    }

    public class QueryPayload : Command<QueryPayload>
    {
        public static QueryPayload GetPooled(object source, StyleSheet theme)
        {
            var cmd = GetPooled();
            cmd.Source = source;
            cmd.Payload = theme;
            return cmd;
        }

        public static void Execute(AuthoringContext context, object source, StyleSheet theme)
        {
            using var command = GetPooled(source, theme);
            context.Messages.Execute(command);
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
