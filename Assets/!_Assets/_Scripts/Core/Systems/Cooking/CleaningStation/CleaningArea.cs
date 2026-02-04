using UnityEngine;

public class CleaningArea : MonoBehaviour
{
    [Header("VFX")]
    [SerializeField] private ParticleSystem splashVFX;

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Object {other.gameObject.name} entered cleaning area.");
        IWashable washable = other.GetComponent<IWashable>();
        if (washable != null && washable.CanBeWashed())
        {
            Debug.Log($"Object {other.gameObject.name} entered cleaning area and can be washed.");

            // Play splash VFX at object position
            if (splashVFX != null)
            {
                splashVFX.transform.position = other.transform.position;
                splashVFX.Stop();
                splashVFX.Play();
            }

            washable.StartWashing();
        }
    }
}
