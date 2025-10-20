using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PositionBasedSpawnMonitor : MonoBehaviour, ISpawnMonitor
{
    [Header("Monitor Settings")]
    [SerializeField] private float checkInterval = 0.2f;
    
    private List<GameObject> monitoredItems = new List<GameObject>();
    private Dictionary<GameObject, Vector3> itemOriginPositions = new Dictionary<GameObject, Vector3>();
    private IPositionValidator positionValidator;
    private System.Action onItemRemovedCallback;
    private Coroutine monitorCoroutine;
    
    public void Initialize(IPositionValidator validator)
    {
        positionValidator = validator;
    }
    
    public void StartMonitoring(System.Action onItemRemoved)
    {
        onItemRemovedCallback = onItemRemoved;
        
        if (monitorCoroutine != null)
        {
            StopCoroutine(monitorCoroutine);
        }
        
        monitorCoroutine = StartCoroutine(MonitorItems());
    }
    
    public void StopMonitoring()
    {
        if (monitorCoroutine != null)
        {
            StopCoroutine(monitorCoroutine);
            monitorCoroutine = null;
        }
    }
    
    public void RegisterItem(GameObject item)
    {
        if (item != null && !monitoredItems.Contains(item))
        {
            monitoredItems.Add(item);
            itemOriginPositions[item] = item.transform.position;
        }
    }
    
    public void UnregisterItem(GameObject item)
    {
        monitoredItems.Remove(item);
        itemOriginPositions.Remove(item);
    }
    
    private IEnumerator MonitorItems()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);
            CheckItemPositions();
        }
    }
    
    private void CheckItemPositions()
    {
        List<GameObject> itemsToRemove = new List<GameObject>();
        
        foreach (GameObject item in monitoredItems)
        {
            if (item == null)
            {
                itemsToRemove.Add(item);
                continue;
            }
            
            if (itemOriginPositions.TryGetValue(item, out Vector3 origin))
            {
                if (!positionValidator.IsValidPosition(item.transform.position, origin))
                {
                    itemsToRemove.Add(item);
                    onItemRemovedCallback?.Invoke();
                }
            }
        }
        
        foreach (GameObject item in itemsToRemove)
        {
            UnregisterItem(item);
        }
    }
}