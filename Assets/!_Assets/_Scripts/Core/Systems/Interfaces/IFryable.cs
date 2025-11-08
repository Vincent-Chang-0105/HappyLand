using UnityEngine;

public interface IFryable
{
    void StartFrying();
    void StopFrying();
    void CompleteFrying();
    bool CanBeFried();
    bool IsFried();
}