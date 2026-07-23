using UnityEngine;

public interface ISpawnable
{
    void Spawn(Vector3 position, Quaternion rotation);
    void Despawn();
}