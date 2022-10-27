// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation
{
    readonly struct Operator_Internal
    {
        public readonly OperationType Type;
        public readonly string Str;
        public readonly int Precedence;
        public readonly Associativity_Internal Associativity;
        public readonly bool Unary;

        public Operator_Internal(OperationType type, string str, int precedence, Associativity_Internal associativity = Associativity_Internal.None,
                        bool unary = false)
        {
            Type = type;
            Str = str;
            Precedence = precedence;
            Associativity = associativity;
            Unary = unary;
        }
    }
}
