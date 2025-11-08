using UnityEngine;

[System.Serializable]
public class RadiusPositionValidator : IPositionValidator
{
    [SerializeField] private float validRadius = 2f;
    
    public bool IsValidPosition(Vector3 position, Vector3 referencePoint)
    {
        float distance = Vector3.Distance(position, referencePoint);
        return distance <= validRadius;
    }
    
    public float ValidRadius 
    { 
        get => validRadius; 
        set => validRadius = value; 
    }
}