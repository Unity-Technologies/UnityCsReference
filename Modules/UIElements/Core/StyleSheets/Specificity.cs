// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements;

[Serializable]
internal struct Specificity : IEquatable<Specificity>, IComparable<Specificity>
{
    const int k_Mask = 0xFF;
    const int k_TypeScoreOffset = 0;
    const int k_ClassScoreOffset = 8;
    const int k_IdScoreOffset = 16;

    [SerializeField]
    int m_Score = 0;

    public byte idScore
    {
        get => GetValueFromMask(m_Score, k_IdScoreOffset);
        set => SetValueWithMask(ref m_Score, value, k_IdScoreOffset);
    }

    public byte classScore
    {
        get => GetValueFromMask(m_Score, k_ClassScoreOffset);
        set => SetValueWithMask(ref m_Score, value, k_ClassScoreOffset);
    }

    public byte typeScore
    {
        get => GetValueFromMask(m_Score, k_TypeScoreOffset);
        set => SetValueWithMask(ref m_Score, value, k_TypeScoreOffset);
    }

    public bool isUniversal => idScore == 0 && classScore == 0 && typeScore == 0;

    public Specificity(int score)
    {
        m_Score = score;
    }

    public Specificity(byte idScore, byte classScore, byte typeScore)
    {
        this.idScore = idScore;
        this.classScore = classScore;
        this.typeScore = typeScore;
    }

    private static byte GetValueFromMask(int value, int offset)
    {
        return (byte)((value & (k_Mask << offset)) >> offset);
    }

    private static void SetValueWithMask(ref int score, byte value, int offset)
    {
        score = (score & ~(k_Mask << offset)) | (value << offset);
    }

    public static implicit operator int(Specificity specificity)
    {
        return specificity.m_Score;
    }

    public static implicit operator Specificity(int specificityScore)
    {
        return new Specificity(specificityScore);
    }

    public override string ToString()
    {
        return $"{idScore}-{classScore}-{typeScore}";
    }

    public bool Equals(Specificity other)
    {
        return m_Score == other.m_Score;
    }

    public override bool Equals(object obj)
    {
        return obj is Specificity other && Equals(other);
    }

    public override int GetHashCode()
    {
        return m_Score;
    }

    public int CompareTo(Specificity other)
    {
        return m_Score.CompareTo(other.m_Score);
    }
}
