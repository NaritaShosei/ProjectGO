using System;
using UnityEngine;

public interface IPoolable 
{
    public Action OnRelease { get; set; }
}
