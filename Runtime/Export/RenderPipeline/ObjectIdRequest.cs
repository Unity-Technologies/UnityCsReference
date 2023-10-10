// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License




namespace UnityEngine.Rendering
{
    public class ObjectIdRequest
    {
       /// <remarks>
       /// A destination RenderTexture must be available in order for the request to be completed successfully
       /// </remarks>
       public RenderTexture destination { get; set; }
       public int mipLevel { get; set; }
       public CubemapFace face { get; set; }
       public int slice { get; set; }

       /// <remarks>
       /// Will be null until the request has been completed
       /// </remarks>
       public ObjectIdResult result { get; internal set; }

       public ObjectIdRequest(
           RenderTexture destination,
           int mipLevel = 0,
           CubemapFace face = CubemapFace.Unknown,
           int slice = 0)
       {
           this.destination = destination;
           this.mipLevel = mipLevel;
           this.face = face;
           this.slice = slice;
       }
    }

    public class ObjectIdResult
    {
        // idToObjectMapping[index] is a buffer mapping indices to objects, corresponding to the encoded colors in the destination RenderTexture.
        // DecodeIdFromColor can be used to decode the RGBA colors in the destination RenderTexture to an index that can be looked up in this buffer.
        //
        // In order to use this information on the GPU, some sort of texture/buffer should be filled in C# (e.g. `colors[IdToObjectMapping.length]`) that
        // contains the per-object information required in the shader for each object. The shader then loads the RGBA pixel from the destination render texture,
        // decodes it to an index index (using the same math as in DecodeIdFromColor), then uses index to look up the per-object property stored in the buffer.
        // The per-object property buffer is "tightly" packed only containing relevant objects for the current camera view (so at most a few 100s or 1000s).
       public Object[]  idToObjectMapping { get; }

       internal ObjectIdResult(Object[] idToObjectMapping)
       {
           this.idToObjectMapping = idToObjectMapping;
       }

       public static int DecodeIdFromColor(Color color)
       {
           // This logic must be the inverse of the logic in `ColorRGBA32 PickingEncodeIndex(UInt32 index)` in picking.cpp
           return (int)(color.r * 255) +
                 ((int)(color.g * 255) <<  8) +
                 ((int)(color.b * 255) << 16) +
                 ((int)(color.a * 255) << 24);
       }
    }
}



