using UnityEngine;

public interface IPooledObject
{
    void OnObjectSpawn();
    string PoolTag { get; set; }
}