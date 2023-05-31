// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.ShaderFoundry
{
    internal sealed partial class ShaderContainer
    {
        static void AddDefaultTypes(ShaderContainer container)
        {
            ShaderType.Void(container);

            string[] scalarTypes =
            {
                "bool", "int", "uint", "half", "float", "double"
            };

            foreach (var s in scalarTypes)
            {
                var scalarType = ShaderType.Scalar(container, s);

                for (int dim = 1; dim <= 4; dim++)
                    ShaderType.Vector(container, scalarType, dim);

                for (int rows = 1; rows <= 4; rows++)
                    for (int cols = 1; cols <= 4; cols++)
                        ShaderType.Matrix(container, scalarType, rows, cols);
            }

            ShaderType.Texture(container, "Texture1D");
            ShaderType.Texture(container, "Texture1DArray");
            ShaderType.Texture(container, "Texture2D");
            ShaderType.Texture(container, "Texture2DArray");
            ShaderType.Texture(container, "Texture3D");
            ShaderType.Texture(container, "TextureCube");
            ShaderType.Texture(container, "TextureCubeArray");
            ShaderType.Texture(container, "Texture2DMS");
            ShaderType.Texture(container, "Texture2DMSArray");
            ShaderType.SamplerState(container, "SamplerState");


            // Unity wrapped resource types
            ShaderType BuildExternallyDeclaredType(ShaderContainer container, string typeName)
            {
                var builder = new ShaderType.StructBuilder(container, typeName);
                builder.DeclaredExternally();
                return builder.Build();
            }

            container._UnityTexture2D = BuildExternallyDeclaredType(container, "UnityTexture2D");
            container._UnityTexture2DArray = BuildExternallyDeclaredType(container, "UnityTexture2DArray");
            container._UnityTextureCube = BuildExternallyDeclaredType(container, "UnityTextureCube");
            container._UnityTexture3D = BuildExternallyDeclaredType(container, "UnityTexture3D");
            container._UnitySamplerState = BuildExternallyDeclaredType(container, "UnitySamplerState");
        }
    }
}
