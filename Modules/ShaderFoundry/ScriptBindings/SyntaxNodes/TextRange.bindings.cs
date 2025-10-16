// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/TextRange.h")]
    [FoundryAPI]
    struct Position : IEquatable<Position>
    {
        public UInt32 line;
        public UInt32 character;

        // IEquatable Interface
        public override bool Equals(object obj) => obj is Position other && this.Equals(other);
        public bool Equals(Position other) => line == other.line && character == other.character;
        public override int GetHashCode() => (line, character).GetHashCode();
        public static bool operator ==(Position lhs, Position rhs) => lhs.Equals(rhs);
        public static bool operator !=(Position lhs, Position rhs) => !lhs.Equals(rhs);
    }

    [NativeHeader("Modules/ShaderFoundry/Public/TextRange.h")]
    [FoundryAPI]
    struct TextRange : IEquatable<TextRange>
    {
        public Position startPosition;
        public Position endPosition;

        public TextRange(uint startLine, uint startCharacter, uint endLine, uint endCharacter)
        {
            startPosition = new Position { line = startLine, character = startCharacter };
            endPosition = new Position { line = endLine, character = endCharacter };
        }

        // IEquatable Interface
        public override bool Equals(object obj) => obj is TextRange other && this.Equals(other);
        public bool Equals(TextRange other) => startPosition == other.startPosition && endPosition == other.endPosition;
        public override int GetHashCode() => (startPosition, endPosition).GetHashCode();
        public static bool operator ==(TextRange lhs, TextRange rhs) => lhs.Equals(rhs);
        public static bool operator !=(TextRange lhs, TextRange rhs) => !lhs.Equals(rhs);
    }
}
