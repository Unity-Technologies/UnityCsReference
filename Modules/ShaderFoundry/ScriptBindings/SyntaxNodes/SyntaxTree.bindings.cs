// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Parser/AST.h")]
    [FoundryAPI]
    internal class SyntaxTree : IDisposable
    {
        IntPtr m_Ptr;
        internal bool IsValid => m_Ptr != IntPtr.Zero;
        // Denotes that the tree's IntPtr was allocated by managed code and so de-allocation should be triggered
        // by managed as well.
        private readonly bool m_IsOwnedByManaged = false;

        public SyntaxTree() : this(Internal_Create())
        {
            m_IsOwnedByManaged = true;
        }

        private SyntaxTree(IntPtr ptr)
        {
            m_Ptr = ptr;
        }

        ~SyntaxTree()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                if (m_IsOwnedByManaged)
                    Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }

        // Sets the root of the tree to the given node. Throws if the tree already has a valid FileRootNode
        // root.
        internal void SetRoot(FileRootNode node)
        {
            // If the syntax tree already has a file root node, throw an exception
            var currentRoot = GetRoot();
            if (currentRoot.IsValid && currentRoot != node)
                throw new InvalidOperationException("A syntax tree may only contain one FileRootNode.");
            SetRootHandle(node.handle);
        }

        // Gets the current root node of the tree. If the root node is a valid FileRootNode, returns it,
        // otherwise returns an invalid FileRootNode.
        internal FileRootNode GetRoot()
        {
            var rootHandle = GetRootHandle();
            if (rootHandle.IsValid && rootHandle.m_NodeType == FileRootNode.InternalNode.kNodeType)
                return new FileRootNode(this, rootHandle);
            return new FileRootNode();
        }

        [NativeMethod(IsThreadSafe = true)] private extern static IntPtr Internal_Create();
        [NativeMethod(IsThreadSafe = true)] private static extern void Internal_Destroy(IntPtr ptr);
        [NativeMethod(IsThreadSafe = true)] internal extern UInt32 AddString(string text);
        [NativeMethod(IsThreadSafe = true)] internal extern string GetString(UInt32 index);
        [NativeMethod(IsThreadSafe = true)] internal extern NodeHandle AllocateByNodeType(NodeType nodeType);
        [NativeMethod(IsThreadSafe = true)] private unsafe extern void* GetNodeUnsafe(NodeHandle handle);
        [NativeMethod(IsThreadSafe = true)] internal extern NodeHandle GetRootHandle();
        [NativeMethod(IsThreadSafe = true)] internal extern void SetRootHandle(NodeHandle handle);

        unsafe internal IntPtr GetNodeIntPtr(NodeHandle handle)
        {
            return (IntPtr)GetNodeUnsafe(handle);
        }

        unsafe internal ref T GetNodeRef<T>(IntPtr ptr) where T : struct
        {
            if (!IsValid)
                throw new NullReferenceException();
            return ref UnsafeUtility.AsRef<T>(ptr.ToPointer());
        }

        internal ISyntaxNode GetSyntaxNode(NodeHandle handle)
        {
            // TODO: Fill out all cases
            switch (handle.m_NodeType)
            {
                case NodeType.FileRoot:
                    return new FileRootNode(this, handle);
                case NodeType.Identifier:
                    return new IdentifierNode(this, handle);
                case NodeType.Text:
                    return new TextNode(this, handle);
                case NodeType.BooleanLiteral:
                    return new BooleanLiteralNode(this, handle);
                case NodeType.CharacterLiteral:
                    return new CharacterLiteralNode(this, handle);
                case NodeType.IntegerLiteral:
                    return new IntegerLiteralNode(this, handle);
                case NodeType.FloatLiteral:
                    return new FloatLiteralNode(this, handle);
                case NodeType.StringLiteral:
                    return new StringLiteralNode(this, handle);
                case NodeType.NamespacedIdentifier:
                    return new NamespacedIdentifierNode(this, handle);
                case NodeType.TypeName:
                    return new TypeNameNode(this, handle);
                // TODO @ SHADERS PLATGRAPH-2907: case NodeType.TemplateSpecifier:
                case NodeType.ArrayDimension:
                    return new ArrayDimensionNode(this, handle);
                case NodeType.AttributeParameterListValue:
                    return new AttributeParameterListValueNode(this, handle);
                case NodeType.AttributeParameter:
                    return new AttributeParameterNode(this, handle);
                case NodeType.Attribute:
                    return new AttributeNode(this, handle);
                case NodeType.Interface:
                    return new InterfaceNode(this, handle);
                case NodeType.InterfaceField:
                    return new InterfaceFieldNode(this, handle);
                case NodeType.Defines:
                    return new DefinesNode(this, handle);
                case NodeType.Define:
                    return new DefineNode(this, handle);
                case NodeType.Include:
                    return new IncludeNode(this, handle);
                case NodeType.Includes:
                    return new IncludesNode(this, handle);
                case NodeType.PackageRequirement:
                    return new PackageRequirementNode(this, handle);
                case NodeType.PackageRequirements:
                    return new PackageRequirementsNode(this, handle);
                case NodeType.Pragma:
                    return new PragmaNode(this, handle);
                case NodeType.Pragmas:
                    return new PragmasNode(this, handle);
                case NodeType.Tag:
                    return new TagNode(this, handle);
                case NodeType.Tags:
                    return new TagsNode(this, handle);
                case NodeType.Struct:
                    return new StructNode(this, handle);
                case NodeType.StructField:
                    return new StructFieldNode(this, handle);
                case NodeType.Function:
                    return new FunctionNode(this, handle);
                case NodeType.FunctionParameter:
                    return new FunctionParameterNode(this, handle);
                case NodeType.Import:
                    return new ImportNode(this, handle);
                case NodeType.Imports:
                    return new ImportsNode(this, handle);
                case NodeType.Block:
                    return new BlockNode(this, handle);
                case NodeType.Namespace:
                    return new NamespaceNode(this, handle);
                case NodeType.BlockSequence:
                    return new BlockSequenceNode(this, handle);
                case NodeType.BlockSequenceElement:
                    return new BlockSequenceElementNode(this, handle);
                case NodeType.LinkOverrideAccessor:
                    return new LinkOverrideAccessorNode(this, handle);
                case NodeType.LinkOverrideElementKeyword:
                    return new LinkOverrideElementKeywordNode(this, handle);
                case NodeType.LinkOverrideElementExpression:
                    return new LinkOverrideElementExpressionNode(this, handle);
                // TODO @ SHADERS PLATGRAPH-2907: case NodeType.LinkOverrideElementConstantExpression:
                case NodeType.LinkOverride:
                    return new LinkOverrideNode(this, handle);
                case NodeType.LinkOverrides:
                    return new LinkOverridesNode(this, handle);
                case NodeType.CustomizationPoint:
                    return new CustomizationPointNode(this, handle);
                case NodeType.CustomizationPointImplementation:
                    return new CustomizationPointImplementationNode(this, handle);
                case NodeType.CustomAttribute:
                    return new CustomAttributeNode(this, handle);
                case NodeType.ConstructorSignature:
                    return new ConstructorSignatureNode(this, handle);
                case NodeType.ConstructorSignatureParameter:
                    return new ConstructorSignatureParameterNode(this, handle);
                case NodeType.Lod:
                    return new LodNode(this, handle);
                // TODO @ SHADERS PLATGRAPH-2907: case NodeType.CopyRule:
                // TODO @ SHADERS PLATGRAPH-2907: case NodeType.PassStageSetting:
                case NodeType.PassStageSetup:
                    return new PassStageSetupNode(this, handle);
                case NodeType.PassStage:
                    return new PassStageNode(this, handle);
                case NodeType.Pass:
                    return new PassNode(this, handle);
                case NodeType.Fallback:
                    return new FallbackNode(this, handle);
                case NodeType.Dependency:
                    return new DependencyNode(this, handle);
                case NodeType.CustomEditor:
                    return new CustomEditorNode(this, handle);
                // TODO @ SHADERS PLATGRAPH-2907: case NodeType.Extensions:
                case NodeType.Template:
                    return new TemplateNode(this, handle);
                case NodeType.BlockShaderInterface:
                    return new BlockShaderInterfaceNode(this, handle);
                case NodeType.RegisterTemplateStatement:
                    return new RegisterTemplateStatementNode(this, handle);
                case NodeType.RegisterTemplatesWithInterface:
                    return new RegisterTemplatesWithInterfaceNode(this, handle);
                // TODO @ SHADERS PLATGRAPH-2907: case NodeType.GeneratorSetting:
                // TODO @ SHADERS PLATGRAPH-2907: case NodeType.BlockShaderSetting:
                // TODO @ SHADERS PLATGRAPH-2907: case NodeType.BlockShaderSettings:
                case NodeType.BlockShader:
                    return new BlockShaderNode(this, handle);
                case NodeType.RenderStateProperty:
                    return new RenderStatePropertyNode(this, handle);
                case NodeType.RenderStateTargetSpecifier:
                    return new RenderStateTargetSpecifierNode(this, handle);
                case NodeType.RenderStateNamedValue:
                    return new RenderStateNamedValueNode(this, handle);
                case NodeType.RenderState:
                    return new RenderStateNode(this, handle);
                case NodeType.RenderStates:
                    return new RenderStatesNode(this, handle);
                case NodeType.Keyword:
                    return new KeywordNode(this, handle);
                case NodeType.Keywords:
                    return new KeywordsNode(this, handle);
                case NodeType.Resource:
                    return new ResourceNode(this, handle);
                case NodeType.OutputLinker:
                    return new OutputLinkerNode(this, handle);
                // TODO @ SHADERS PLATGRAPH-2907: case NodeType.PropertyOrderItem:
                // TODO @ SHADERS PLATGRAPH-2907: case NodeType.PropertyOrderGroup:
                // TODO @ SHADERS PLATGRAPH-2907: case NodeType.PropertyOrder:
                // TODO @ SHADERS PLATGRAPH-2907: case NodeType.BlockInterface:
                default:
                    throw new InvalidOperationException($"Node type {handle.m_NodeType} is not supported.");
            }
        }
        internal IEnumerable<ISyntaxNode> EnumerateSyntaxNodes(NodeListEnumerable list)
        {
            foreach (var handle in list)
                yield return GetSyntaxNode(handle);
        }
        internal IEnumerable<ISyntaxNode> EnumerateSyntaxNodes(NodeList list)
            => EnumerateSyntaxNodes(list.GetEnumerable(this));

        internal IEnumerable<T> EnumerateSyntaxNodes<T>(NodeList list) where T : ISyntaxNode
        {
            foreach (var node in EnumerateSyntaxNodes(list))
                yield return (T)node;
        }

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(SyntaxTree obj) => obj.m_Ptr;
            public static SyntaxTree ConvertToManaged(IntPtr ptr) => new SyntaxTree(ptr);
        }
    }
}
