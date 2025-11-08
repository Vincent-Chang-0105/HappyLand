using UnityEngine;

public interface IPositionValidator
{
    bool IsValidPosition(Vector3 position, Vector3 referencePoint);
}