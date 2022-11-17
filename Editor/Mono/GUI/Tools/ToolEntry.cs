// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.EditorTools
{
    struct ToolEntry
    {
        public enum Scope
        {
            // Built-in override tools as specified by the active EditorToolContext. Can be global or component tools.
            BuiltinView = Tool.View,
            BuiltinMove = Tool.Move,
            BuiltinRotate = Tool.Rotate,
            BuiltinScale = Tool.Scale,
            BuiltinRect = Tool.Rect,
            BuiltinTransform = Tool.Transform,

            // Additional built-in tools as specified by the active EditorToolContext. Can be global or component tools.
            BuiltinAdditional,

            // User defined global tools not associated with any context.
            CustomGlobal,

            // Component tools applicable to the current selection.
            Component
        }

        public Scope scope;
        public Type variantGroup;
        public int priority;
        public List<EditorTool> tools;
        public bool componentTool;

        public ToolEntry(EditorTypeAssociation meta, Scope scope)
        {
            this.scope = scope;
            variantGroup = meta.variantGroup;
            priority = meta.priority;
            componentTool = meta.targetBehaviour != null && meta.targetBehaviour != typeof(NullTargetKey);
            tools = new List<EditorTool>();
        }

        public override int GetHashCode()
        {
            var hash = scope.GetHashCode();
            hash = Tuple.CombineHashCodes(hash, priority);
            return variantGroup != null
                ? Tuple.CombineHashCodes(hash, variantGroup.GetHashCode())
                : Tuple.CombineHashCodes(hash, tools[0].GetType().GetHashCode());
        }
    }
}
