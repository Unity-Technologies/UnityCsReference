// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal class ExpressionNotValidException : Exception
    {
        public ExpressionNotValidException(string expression)
            : base(expression) {}

        public ExpressionNotValidException(string message, string expression)
            : base($"{message} : {expression}") {}
    }
}
