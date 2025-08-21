// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Multiplayer.PlayMode.Editor
{
    internal abstract class MultiplayNode : Node
    {
        protected MultiplayNode(string name) : base(name) {}

        private string GetInputNotSetErrorMessage(string inputName) => $"{inputName} is not set";

        protected void ValidateInputIsSet(NodeInput<string> input, string inputName)
        {
            if (string.IsNullOrEmpty(GetInput(input)))
                throw new InvalidOperationException(GetInputNotSetErrorMessage(inputName));
        }

        protected void ValidateInputIsSet<T>(NodeInput<T> input, string inputName)
        {
            var value = GetInput(input);
            if (value == null || GetInput(input).Equals(default(T)))
                throw new InvalidOperationException(GetInputNotSetErrorMessage(inputName));
        }

        protected static void ValidateNameParameter(string value, string parameterName)
        {
            var regex = new System.Text.RegularExpressions.Regex("^[a-zA-Z0-9-]+$");

            if (!regex.IsMatch(value))
                throw new InvalidOperationException($"{parameterName} must only contain alphanumeric characters and hyphens");
        }
    }
}
