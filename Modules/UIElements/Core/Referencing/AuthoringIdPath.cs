// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements;

[Serializable]
struct AuthoringIdPath : IEquatable<AuthoringIdPath>
{
    const int k_RootAuthoringId = 0;

    [SerializeField]
    int[] m_PathIds;

    public int[] path => m_PathIds ?? Array.Empty<int>();

    internal bool isEmpty => path.Length == 0;

    internal bool isRootReference => path.Length == 1 && path[0] == 0;

    public AuthoringIdPath()
    {
    }

    public AuthoringIdPath(params int[] pathIds)
    {
        if (pathIds == null)
            throw new ArgumentNullException(nameof(pathIds));

        m_PathIds = pathIds;
    }

    public bool Equals(AuthoringIdPath other)
    {
        if (path.Length != other.path.Length)
            return false;

        for (int i = 0; i < path.Length; i++)
            if (path[i] != other.path[i])
                return false;
        return true;
    }

    public override int GetHashCode()
    {
        return path.Length switch
        {
            0 => 0,
            1 => path[0].GetHashCode(),
            2 => HashCode.Combine(path[0], path[1]),
            3 => HashCode.Combine(path[0], path[1], path[2]),
            4 => HashCode.Combine(path[0], path[1], path[2], path[3]),
            5 => HashCode.Combine(path[0], path[1], path[2], path[3], path[4]),
            6 => HashCode.Combine(path[0], path[1], path[2], path[3], path[4], path[5]),
            7 => HashCode.Combine(path[0], path[1], path[2], path[3], path[4], path[5], path[6]),
            8 => HashCode.Combine(path[0], path[1], path[2], path[3], path[4], path[5], path[6], path[7]),
            _ => AggregateHashCode(path)
        };
    }

    static int AggregateHashCode(int[] path)
    {
        var hash = new HashCode();
        foreach (var item in path)
        {
            hash.Add(item);
        }
        return hash.ToHashCode();
    }

    public override string ToString()
    {
        if (path.Length == 0)
            return "AuthoringIdPath []";
        return "AuthoringIdPath [" + string.Join(",", path) + "]";
    }
}
