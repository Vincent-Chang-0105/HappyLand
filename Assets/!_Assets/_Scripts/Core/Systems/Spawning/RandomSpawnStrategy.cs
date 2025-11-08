using UnityEngine;

[System.Serializable]
public class RandomSpawnStrategy : ISpawnStrategy
{
    [Header("Randomization Settings")]
    [SerializeField] private bool randomRotation = true;
    [SerializeField] private Vector2 rotationRange = new Vector2(0f, 360f);
    [SerializeField] private bool randomScale = false;
    [SerializeField] private Vector2 scaleRange = new Vector2(0.8f, 1.2f);
    
    public Vector3 GetSpawnPosition(Transform spawnArea, Vector2 areaSize)
    {
        Vector3 basePosition = spawnArea.position;
        
        float randomX = Random.Range(-areaSize.x / 2f, areaSize.x / 2f);
        float randomY = Random.Range(-areaSize.y / 2f, areaSize.y / 2f);
        
        return basePosition + new Vector3(randomX, randomY, 0);
    }
    
    public Quaternion GetSpawnRotation()
    {
        if (!randomRotation) return Quaternion.identity;
        
        float randomZ = Random.Range(rotationRange.x, rotationRange.y);
        return Quaternion.Euler(0, 0, randomZ);
    }
    
    public void ApplyToGameObject(GameObject obj)
    {
        if (randomScale && obj != null)
        {
            float randomScaleValue = Random.Range(scaleRange.x, scaleRange.y);
            obj.transform.localScale = Vector3.one * randomScaleValue;
        }
    }
}