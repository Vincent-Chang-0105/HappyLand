using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

[System.Serializable]
public class TicketSprite
{
    public string orderName;
    public Sprite ticketSprite;
}

public class TicketManager : MonoBehaviour
{
    [Header("Ticket System")]
    [SerializeField] private Transform ticketContainer;      // Top area where tickets appear
    [SerializeField] private GameObject ticketPrefab;        // Ticket UI prefab
    [SerializeField] private List<TicketSprite> ticketSprites = new List<TicketSprite>(); // Predefined sprites
    
    [Header("Animation")]
    [SerializeField] private float spawnAnimationDuration = 0.5f;

    // Track active tickets
    private List<GameObject> activeTickets = new List<GameObject>();

    public GameObject SpawnTicket(Order order)
    {
        // Find matching ticket sprite
        Sprite ticketSprite = GetTicketSprite(order.orderName);
        if (ticketSprite == null)
        {
            Debug.LogWarning($"No ticket sprite found for order: {order.orderName}");
            return null;
        }

        // Create ticket object
        GameObject ticket = Instantiate(ticketPrefab, ticketContainer);

        // Setup ticket visual
        Image ticketImage = ticket.GetComponent<Image>();
        if (ticketImage != null)
        {
            ticketImage.sprite = ticketSprite;
        }

        // Animate ticket spawn — preserve prefab scale as the target
        Vector3 targetScale = ticketPrefab.transform.localScale;
        ticket.transform.localScale = Vector3.zero;
        ticket.transform.DOScale(targetScale, spawnAnimationDuration)
               .SetEase(Ease.OutBack);

        // Track ticket
        activeTickets.Add(ticket);

        return ticket;
    }
    
    Sprite GetTicketSprite(string orderName)
    {
        TicketSprite ticketData = ticketSprites.Find(t => t.orderName.Equals(orderName, System.StringComparison.OrdinalIgnoreCase));
        return ticketData?.ticketSprite;
    }
    
    public void RemoveTicket(GameObject ticket)
    {
        if (activeTickets.Contains(ticket))
        {
            activeTickets.Remove(ticket);

            // Animate removal — layout group auto-repositions remaining tickets
            ticket.transform.DOScale(Vector3.zero, 0.3f)
                   .OnComplete(() => Destroy(ticket));
        }
    }
    
    public void ClearAllTickets()
    {
        foreach (GameObject ticket in activeTickets)
        {
            if (ticket != null)
            {
                Destroy(ticket);
            }
        }
        activeTickets.Clear();
    }
    
    public int GetActiveTicketCount()
    {
        return activeTickets.Count;
    }
}