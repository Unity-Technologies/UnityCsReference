// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    internal class BaseVariableBuilder : BaseBuilder
    {
        protected List<AttributeNode> m_Attributes = new List<AttributeNode>();
        protected TypeNameNode m_TypeName;
        protected IdentifierNode m_Name;
        public TextRange SourceRange { get; set; }
        public IEnumerable<AttributeNode> Attributes => m_Attributes;
        public void AddAttribute(AttributeNode node) => m_Attributes.Add(Validate(node));
        public TypeNameNode TypeName
        {
            get => m_TypeName;
            set => m_TypeName = Validate(value);
        }
        public IdentifierNode Name
        {
            get => m_Name;
            set => m_Name = Validate(value);
        }

        public BaseVariableBuilder(SyntaxTree syntaxTree, TypeNameNode typeName, IdentifierNode name)
            : base(syntaxTree)
        {
            TypeName = typeName;
            Name = name;
        }
    }
}
