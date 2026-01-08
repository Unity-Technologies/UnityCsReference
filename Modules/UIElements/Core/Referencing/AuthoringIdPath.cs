// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements;

/// <summary>
/// Struct representing a path of authoring IDs used to identify VisualElements.
/// </summary>
[Serializable]
public struct AuthoringIdPath : IEquatable<AuthoringIdPath>
{
    const int k_RootAuthoringId = 0;

    [SerializeField]
    int[] m_PathIds;

    /// <summary>
    /// The path of authoring IDs.
    /// </summary>
    public ReadOnlySpan<int> path => m_PathIds ?? Array.Empty<int>();

    internal bool isEmpty => path.Length == 0;

    internal bool isRootReference => path.Length == 1 && path[0] == 0;

    /// <summary>
    /// Creates an empty AuthoringIdPath.
    /// </summary>
    public AuthoringIdPath()
    {
    }

    /// <summary>
    /// Creates an AuthoringIdPath from the given path IDs.
    /// </summary>
    /// <param name="pathIds">The list of authoring-IDs to locate the element.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public AuthoringIdPath(params int[] pathIds)
    {
        if (pathIds == null)
            throw new ArgumentNullException(nameof(pathIds));

        m_PathIds = pathIds;
    }

    /// <summary>
    /// Checks for equality between two AuthoringIdPath instances.
    /// </summary>
    /// <param name="other">The <see cref="AuthoringIdPath"/> to compare against.</param>
    /// <returns><see langword="true"/> if <paramref name="other"/> has a matching <see cref="path"/>.</returns>
    public bool Equals(AuthoringIdPath other)
    {
        if (path.Length != other.path.Length)
            return false;

        for (int i = 0; i < path.Length; i++)
            if (path[i] != other.path[i])
                return false;
        return true;
    }

    /// <summary>
    /// Returns hash code for the AuthoringIdPath. The hash code is computed based on the path IDs.
    /// </summary>
    /// <returns>The hascode generated from the path.</returns>
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

    static int AggregateHashCode(ReadOnlySpan<int> path)
    {
        var hash = new HashCode();
        foreach (var item in path)
        {
            hash.Add(item);
        }
        return hash.ToHashCode();
    }

    /// <summary>
    /// Returns a string representation of the AuthoringIdPath.
    /// </summary>
    /// <returns>The string version of the path.</returns>
    public override string ToString()
    {
        if (path.Length == 0)
            return "AuthoringIdPath []";
        return "AuthoringIdPath [" + string.Join(",", m_PathIds) + "]";
    }
}
