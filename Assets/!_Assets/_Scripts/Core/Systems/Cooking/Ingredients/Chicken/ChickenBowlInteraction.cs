using UnityEngine;

public class ChickenBowlInteraction : MonoBehaviour
{
    [Header("Bowl Detection Settings")]
    [SerializeField] private LayerMask bowlLayer = -1;
    [SerializeField] private float bowlDropRadius = 1f;

    private bool isInBowl = false;
    private Bowl currentBowl = null;
    private Collider2D col2D;

    public bool IsInBowl => isInBowl;
    public Bowl CurrentBowl => currentBowl;

    private void Awake()
    {
        col2D = GetComponent<Collider2D>();
    }

    public void CheckForBowlDrop(bool isWashed)
    {
        if (isInBowl || !isWashed) return;

        Collider2D[] bowls = Physics2D.OverlapCircleAll(transform.position, bowlDropRadius, bowlLayer);

        Bowl closestBowl = null;
        float closestDistance = float.MaxValue;

        foreach (Collider2D bowlCollider in bowls)
        {
            Bowl bowlComponent = bowlCollider.GetComponent<Bowl>();
            if (bowlComponent != null && bowlComponent.CanAcceptIngredient(gameObject))
            {
                float distance = Vector3.Distance(transform.position, bowlCollider.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestBowl = bowlComponent;
                }
            }
        }

        if (closestBowl != null)
        {
            EnterBowl(closestBowl);
        }
    }

    public void EnterBowl(Bowl bowl)
    {
        if (isInBowl) return;

        // Cancel any pending teleport
        CancelInvoke(nameof(TeleportToNearestBowl));

        currentBowl = bowl;
        isInBowl = true;

        if (col2D != null)
        {
            col2D.enabled = false; // Disable collider to prevent further interactions
        }

        bowl.AddIngredient(gameObject);

    }

    public void ExitBowl()
    {
        if (!isInBowl || currentBowl == null) return;

        currentBowl.RemoveIngredient(gameObject);
        currentBowl = null;
        isInBowl = false;

        if (col2D != null)
        {
            col2D.enabled = true;
        }

    }

    /// <summary>
    /// Resets bowl state without calling Bowl.RemoveIngredient (avoids double-remove).
    /// Use when the bowl has already removed the ingredient externally (e.g., CookingStation transfer).
    /// </summary>
    public void ForceExitBowl()
    {
        currentBowl = null;
        isInBowl = false;

        if (col2D != null)
        {
            col2D.enabled = true;
        }
    }

    public void TeleportToNearestBowl()
    {
        // Don't teleport if already in bowl
        if (isInBowl) return;

        Bowl[] allBowls = FindObjectsOfType<Bowl>();

        Bowl nearestBowl = null;
        float nearestDistance = float.MaxValue;

        foreach (Bowl bowl in allBowls)
        {
            if (bowl.CanAcceptIngredient(gameObject))
            {
                float distance = Vector3.Distance(transform.position, bowl.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestBowl = bowl;
                }
            }
        }

        if (nearestBowl != null)
        {
            EnterBowl(nearestBowl);
        }
        else
        {
        }
    }

    public Bowl FindBowlByTag(string bowlTag)
    {
        GameObject bowlObj = GameObject.FindGameObjectWithTag(bowlTag);
        return bowlObj != null ? bowlObj.GetComponent<Bowl>() : null;
    }

    public void TeleportToBowlWithTag(string bowlTag)
    {
        Bowl targetBowl = FindBowlByTag(bowlTag);
        if (targetBowl != null && targetBowl.CanAcceptIngredient(gameObject))
        {
            // Exit current bowl if in one
            if (isInBowl)
            {
                ExitBowl();
            }

            // Unparent from any container
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }

            EnterBowl(targetBowl);
        }
        else
        {
            TeleportToNearestBowl();
        }
    }

    private void OnDestroy()
    {
        CancelInvoke(nameof(TeleportToNearestBowl));
    }

    private void OnDrawGizmosSelected()
    {
        // Draw bowl drop radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, bowlDropRadius);

        // Draw line to current bowl
        if (currentBowl != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentBowl.transform.position);
        }
    }
}
