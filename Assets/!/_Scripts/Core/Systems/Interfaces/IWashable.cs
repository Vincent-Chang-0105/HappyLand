using UnityEngine;

public interface IWashable
{
    void StartWashing();
    void CompleteWashing();
    bool IsWashed();
    bool CanBeWashed();
}