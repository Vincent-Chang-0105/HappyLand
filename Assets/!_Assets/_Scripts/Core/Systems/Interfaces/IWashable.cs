using UnityEngine;

public interface IWashable
{
    void StartWashing();
    void StopWashing();
    void CompleteWashing();
    bool IsWashed();
    bool CanBeWashed();
}