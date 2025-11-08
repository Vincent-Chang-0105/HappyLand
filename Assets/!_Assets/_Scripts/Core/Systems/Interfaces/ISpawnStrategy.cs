using UnityEngine;

public interface ISpawnStrategy
{
    Vector3 GetSpawnPosition(Transform spawnArea, Vector2 areaSize);
    Quaternion GetSpawnRotation();
}