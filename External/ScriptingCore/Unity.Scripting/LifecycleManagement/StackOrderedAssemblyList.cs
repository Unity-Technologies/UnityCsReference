using System.Collections;
using System.Reflection;

namespace Unity.Scripting.LifecycleManagement;

internal sealed class StackOrderedAssemblyList : IReadOnlyList<Assembly>
{
    private int _count;
    private readonly List<ReadOnlyAssemblyList> _assemblyStacks = new();

    public int Count => _count;
    public int StackCount => _assemblyStacks.Count;

    public Assembly this[int index]
    {
        get
        {
            if (index < 0 || index >= _count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            foreach (var assemblyStack in _assemblyStacks)
            {
                if (index < assemblyStack.Count)
                {
                    return assemblyStack[index];
                }
                index -= assemblyStack.Count;
            }

            // TODO: for .net core replace with throw new UnreachableException();
            throw new InvalidOperationException("Unreachable");
        }
    }

    public void PushStack(ReadOnlyAssemblyList assemblies)
    {
        _count += assemblies.Count;
        _assemblyStacks.Add(assemblies);
    }

    public void PopStack()
    {
        if (_assemblyStacks.Count == 0)
        {
            throw new InvalidOperationException("Nothing to pop, collection is empty");
        }

        int lastElement = _assemblyStacks.Count - 1;
        _count -= _assemblyStacks[lastElement].Count;
        _assemblyStacks.RemoveAt(lastElement);
    }

    public void Clear()
    {
        _assemblyStacks.Clear();
        _count = 0;
    }

    public IEnumerator<Assembly> GetEnumerator()
    {
        foreach (var assemblyStack in _assemblyStacks)
        {
            foreach (var assembly in assemblyStack)
            {
                yield return assembly;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool TryGetAssembly(string assemblyName, out Assembly assembly)
    {
        foreach (var assemblyStack in _assemblyStacks)
        {
            if (assemblyStack.TryGetAssembly(assemblyName, out assembly))
            {
                return true;
            }
        }

        assembly = default!;
        return false;
    }

    public bool Contains(string assemblyName)
    {
        foreach (var assemblyStack in _assemblyStacks)
        {
            if (assemblyStack.Contains(assemblyName))
            {
                return true;
            }
        }
        return false;
    }
}
