using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ContainerSpawner : MonoBehaviour
{
    [Header("Spawn Configuration")]
    [SerializeField] private Transform spawnArea;
    [SerializeField] private Vector2 spawnAreaSize = new Vector2(2f, 1f);
    [SerializeField] private int initialItemCount = 5;
    [SerializeField] private int maxItems = 5;
    [SerializeField] private float refillDelay = 0.5f;
    
    [Header("Dependencies")]
    [SerializeField] private SpawnableItemFactory itemFactory;
    [SerializeField] private RandomSpawnStrategy spawnStrategy;
    [SerializeField] private RadiusPositionValidator positionValidator;
    [SerializeField] private PositionBasedSpawnMonitor spawnMonitor;
    
    [SerializeField] private List<GameObject> spawnedItems = new List<GameObject>();
    
    private void Start()
    {
        InitializeDependencies();
        SpawnInitialItems();
        StartMonitoring();
    }
    
    private void InitializeDependencies()
    {
        // Initialize monitor with validator
        if (spawnMonitor != null && positionValidator != null)
        {
            spawnMonitor.Initialize(positionValidator);
        }
    }
    
    private void SpawnInitialItems()
    {
        for (int i = 0; i < initialItemCount; i++)
        {
            SpawnItem();
        }
    }
    
    private void StartMonitoring()
    {
        spawnMonitor.StartMonitoring(OnItemRemoved);
    }
    
    private void SpawnItem()
    {
        if (spawnedItems.Count >= maxItems || itemFactory == null || spawnArea == null) 
            return;
        
        Vector3 spawnPosition = spawnStrategy.GetSpawnPosition(spawnArea, spawnAreaSize);
        Quaternion spawnRotation = spawnStrategy.GetSpawnRotation();
        
        GameObject newItem = itemFactory.CreateItem(spawnPosition, spawnRotation);
        
        if (newItem != null)
        {
            spawnedItems.Add(newItem);
            spawnMonitor.RegisterItem(newItem);
        }
    }
    
    private void OnItemRemoved()
    {
        // Clean up null references
        spawnedItems.RemoveAll(item => item == null);
        
        // Spawn replacement after delay
        StartCoroutine(SpawnReplacementWithDelay());
    }
    
    private IEnumerator SpawnReplacementWithDelay()
    {
        yield return new WaitForSeconds(refillDelay);
        
        if (spawnedItems.Count < maxItems)
        {
            SpawnItem();
        }
    }
    
    private void OnDestroy()
    {
        spawnMonitor?.StopMonitoring();
    }
}