using UnityEngine;

public class CleaningArea : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Object {other.gameObject.name} entered cleaning area.");
        IWashable washable = other.GetComponent<IWashable>();
        if (washable != null && washable.CanBeWashed())
        {
            Debug.Log($"Object {other.gameObject.name} entered cleaning area and can be washed.");
            washable.StartWashing();
        }
    }
}
