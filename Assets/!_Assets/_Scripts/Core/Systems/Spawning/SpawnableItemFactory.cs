using System.Collections.Generic;
using UnityEngine;

public class SpawnableItemFactory : MonoBehaviour
{
    [Header("Factory Settings")]
    [SerializeField] private List<GameObject> itemPrefabs;
    [SerializeField] private RandomSpawnStrategy spawnStrategy;
    
    public GameObject CreateItem(Vector3 position, Quaternion rotation)
    {
        if (itemPrefabs == null || itemPrefabs.Count == 0) return null;

        GameObject selectedPrefab = itemPrefabs[Random.Range(0, itemPrefabs.Count)];
        GameObject newItem = Instantiate(selectedPrefab, position, rotation);
        spawnStrategy.ApplyToGameObject(newItem);
        
        return newItem;
    }

    public void SetItemPrefabs(List<GameObject> prefabs)
    {
        itemPrefabs = prefabs;
    }
}
