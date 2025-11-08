using UnityEngine;

public interface IDraggable
{
    void OnDragStart();
    void OnDrag(Vector3 worldPosition);
    void OnDragEnd();
    bool IsDraggable();
}