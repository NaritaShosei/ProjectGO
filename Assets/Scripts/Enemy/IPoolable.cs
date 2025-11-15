using System;
using UnityEngine;

public interface IPoolable 
{
    public ObjectPoolManager PoolManager { get; set; }
    public Action OnRelease { get; set; }
}
