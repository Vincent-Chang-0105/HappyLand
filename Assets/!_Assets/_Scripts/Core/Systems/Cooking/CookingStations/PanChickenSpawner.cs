using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class PanChickenSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private List<GameObject> chickenPrefabs;
    [SerializeField] private Transform spawnArea; // The red pan transform
    [SerializeField] private Vector2 spawnAreaSize = new Vector2(2f, 1f);
    
    [Header("Chicken Management")]
    [SerializeField] private int initialChickenCount = 5; // Number to spawn on start
    [SerializeField] private int maxChickens = 5; // Maximum chickens to maintain
    [SerializeField] private float refillDelay = 0.5f; // Delay before spawning replacement
    [SerializeField] private float checkInterval = 0.2f; // How often to check for missing chickens
    
    [Header("Spawn Randomization")]
    [SerializeField] private bool randomRotation = true;
    [SerializeField] private Vector2 rotationRange = new Vector2(0f, 360f);
    [SerializeField] private bool randomScale = false;
    [SerializeField] private Vector2 scaleRange = new Vector2(0.8f, 1.2f);
    
    [Header("Detection Settings")]
    [SerializeField] private float panRadius = 2f; // How far from pan center chickens can be before considered "removed"
    [SerializeField] private LayerMask chickenLayer = -1; // Layer mask for chicken detection
    
    private List<GameObject> spawnedChickens = new List<GameObject>();
    private bool isActive = true;
    private Coroutine refillCoroutine;
    
    private void Start()
    {
        InitializeChickens();
        StartMonitoring();
    }
    
    private void InitializeChickens()
    {
        // Spawn initial chickens instantly
        for (int i = 0; i < initialChickenCount; i++)
        {
            SpawnChicken();
        }
        
        //Debug.Log($"Spawned {initialChickenCount} chickens on start");
    }
    
    private void StartMonitoring()
    {
        if (refillCoroutine != null)
        {
            StopCoroutine(refillCoroutine);
        }
        
        refillCoroutine = StartCoroutine(MonitorChickens());
    }
    
    private IEnumerator MonitorChickens()
    {
        while (isActive)
        {
            yield return new WaitForSeconds(checkInterval);
            
            // Clean up destroyed chickens and check positions
            CheckAndRefillChickens();
        }
    }
    
    private void CheckAndRefillChickens()
    {
        // Remove null references (destroyed chickens)
        spawnedChickens.RemoveAll(chicken => chicken == null);
        
        // Check which chickens are still in the pan area
        List<GameObject> chickensInPan = new List<GameObject>();
        
        foreach (GameObject chicken in spawnedChickens)
        {
            if (chicken != null && IsChickenInPan(chicken))
            {
                chickensInPan.Add(chicken);
            }
        }
        
        // Update the list to only include chickens still in pan
        int chickensRemovedFromPan = spawnedChickens.Count - chickensInPan.Count;
        
        if (chickensRemovedFromPan > 0)
        {
            // Update spawned chickens list to only include chickens in pan
            spawnedChickens = chickensInPan;
            
            //Debug.Log($"{chickensRemovedFromPan} chicken(s) removed from pan. Chickens in pan: {chickensInPan.Count}");
            
            // Spawn replacements with delay
            StartCoroutine(RefillChickensWithDelay(chickensRemovedFromPan));
        }
    }
    
    private bool IsChickenInPan(GameObject chicken)
    {
        if (chicken == null || spawnArea == null) return false;
        
        // Check if chicken is within pan radius
        float distance = Vector3.Distance(chicken.transform.position, spawnArea.position);
        return distance <= panRadius;
    }
    
    private IEnumerator RefillChickensWithDelay(int chickenCount)
    {
        yield return new WaitForSeconds(refillDelay);
        
        // Spawn replacement chickens
        for (int i = 0; i < chickenCount; i++)
        {
            if (spawnedChickens.Count < maxChickens)
            {
                SpawnChicken();
                yield return new WaitForSeconds(0.1f); // Small delay between spawns
            }
        }
    }
    
    public void SpawnChicken()
    {
        if (chickenPrefabs == null || chickenPrefabs.Count == 0 || spawnArea == null) return;
        
        // Don't spawn if we're at max capacity
        if (spawnedChickens.Count >= maxChickens) return;
        
        // Calculate random position within spawn area
        Vector3 randomPosition = GetRandomSpawnPosition();

        // Calculate random rotation
        Quaternion randomRotation = GetRandomRotation();
        
        // Spawn chicken
        GameObject chickenPrefab = chickenPrefabs[Random.Range(0, chickenPrefabs.Count)];
        GameObject newChicken = Instantiate(chickenPrefab, randomPosition, randomRotation);
        
        // Apply random scale if enabled
        if (randomScale)
        {
            float randomScaleValue = Random.Range(scaleRange.x, scaleRange.y);
            newChicken.transform.localScale = Vector3.one * randomScaleValue;
        }
        
        // Add to spawned list
        spawnedChickens.Add(newChicken);
        
        //Debug.Log($"Spawned chicken at {randomPosition}. Total in pan: {spawnedChickens.Count}");
    }
    
    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 basePosition = spawnArea.position;
        
        // Random offset within spawn area
        float randomX = Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
        float randomY = Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
        
        return basePosition + new Vector3(randomX, randomY, 0);
    }
    
    private Quaternion GetRandomRotation()
    {
        if (!randomRotation) return Quaternion.identity;
        
        float randomZ = Random.Range(rotationRange.x, rotationRange.y);
        return Quaternion.Euler(0, 0, randomZ);
    }
    
    #region Public Methods
    
    public void SetMaxChickens(int newMax)
    {
        maxChickens = newMax;
        
        // If we now have too many chickens, remove excess
        while (spawnedChickens.Count > maxChickens)
        {
            GameObject excessChicken = spawnedChickens[spawnedChickens.Count - 1];
            spawnedChickens.RemoveAt(spawnedChickens.Count - 1);
            
            if (excessChicken != null)
            {
                Destroy(excessChicken);
            }
        }
        
        //Debug.Log($"Max chickens set to: {maxChickens}");
    }
    
    public void ForceRefill()
    {
        // Manually trigger a refill check
        CheckAndRefillChickens();
    }
    
    public void ClearAllChickens()
    {
        foreach (GameObject chicken in spawnedChickens)
        {
            if (chicken != null)
            {
                Destroy(chicken);
            }
        }
        spawnedChickens.Clear();
        //Debug.Log("Cleared all chickens from pan");
    }
    
    public void RestockPan()
    {
        ClearAllChickens();
        InitializeChickens();
        //Debug.Log("Restocked pan with fresh chickens");
    }
    
    public int GetChickenCount()
    {
        // Remove null references before counting
        spawnedChickens.RemoveAll(chicken => chicken == null);
        return spawnedChickens.Count;
    }
    
    public void PauseSpawning()
    {
        isActive = false;
        
        if (refillCoroutine != null)
        {
            StopCoroutine(refillCoroutine);
        }
    }
    
    public void ResumeSpawning()
    {
        isActive = true;
        StartMonitoring();
    }
    
    #endregion
    
    private void OnDrawGizmosSelected()
    {
        if (spawnArea != null)
        {
            // Draw spawn area
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(spawnArea.position, new Vector3(spawnAreaSize.x, spawnAreaSize.y, 0.1f));
            
            // Draw pan detection radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(spawnArea.position, panRadius);
        }
    }
    
    private void OnDestroy()
    {
        // Clean up coroutines
        if (refillCoroutine != null)
        {
            StopCoroutine(refillCoroutine);
        }
    }
    
    // For debugging - shows current chicken count in inspector
    private void OnValidate()
    {
        // Ensure max chickens is not less than initial count
        if (maxChickens < initialChickenCount)
        {
            maxChickens = initialChickenCount;
        }
    }
}