// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections;

[System.Serializable]
internal class SerializedStringTable
{
    [SerializeField] private string[] keys;
    [SerializeField] private int[] values;
    private Hashtable table;
    public Hashtable hashtable { get { SanityCheck(); return table; } }

    public int Length { get { SanityCheck(); return keys.Length; } }

    private void SanityCheck()
    {
        if (keys == null)
        {
            keys = new string[0];
            values = new int[0];
        }
        if (table == null)
        {
            table = new Hashtable();
            for (int i = 0; i < keys.Length; i++) table[keys[i]] = values[i];
        }
    }

    private void SynchArrays()
    {
        keys = new string[table.Count];
        values = new int[table.Count];
        table.Keys.CopyTo(keys, 0);
        table.Values.CopyTo(values, 0);
    }

    public void Set(string key, int value)
    {
        SanityCheck();
        table[key] = value;
        SynchArrays();
    }

    public void Set(string key)
    {
        Set(key, 0);
    }

    public bool Contains(string key)
    {
        SanityCheck();
        return table.Contains(key);
    }

    public int Get(string key)
    {
        SanityCheck();
        if (!table.Contains(key)) return -1;
        return (int)table[key];
    }

    public void Remove(string key)
    {
        SanityCheck();
        if (table.Contains(key)) table.Remove(key);
        SynchArrays();
    }
}
