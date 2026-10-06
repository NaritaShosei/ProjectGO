using UnityEngine;

public interface IPrototype<T>
{
    public T Clone();
}
