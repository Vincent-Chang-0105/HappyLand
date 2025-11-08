using UnityEngine;

public interface ISpawnMonitor
{
    void StartMonitoring(System.Action onItemRemoved);
    void StopMonitoring();
    void RegisterItem(GameObject item);
    void UnregisterItem(GameObject item);
}