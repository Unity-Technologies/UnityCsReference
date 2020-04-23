namespace UnityEngine.UIElements
{
    public interface ITransform
    {
        Vector3 position { get; set; }
        Quaternion rotation { get; set; }
        Vector3 scale { get; set; }
        Matrix4x4 matrix { get; }
    }
}
