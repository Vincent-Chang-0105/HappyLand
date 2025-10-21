using UnityEngine;

public interface IBoilable
{
    void StartBoiling();
    void StopBoiling();
    void CompleteBoiling();
    bool CanBeBoiled();
    bool IsBoiled();
}