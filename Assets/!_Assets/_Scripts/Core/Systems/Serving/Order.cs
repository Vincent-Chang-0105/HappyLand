using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Order", menuName = "Order")]
public class Order : ScriptableObject
{
    [Header("Order Information")]
    public string orderName;
    public string orderDescription;
    public List<string> requiredIngredients = new List<string>();
    public List<string> requiredSteps = new List<string>();

    [Header("Rewards")]
    public int correctOrderReward = 50;
    public int incorrectOrderPenalty = -10;
    public float timeLimit = 60f; // Time limit in seconds

    [Header("Visuals")]
    public Sprite orderSprite;
    public Color orderColor = Color.white;

    [System.NonSerialized]
    public float orderStartTime;
    [System.NonSerialized]
    public bool isCompleted = false;
    [System.NonSerialized]
    public bool isCorrect = false;

    public float TimeRemaining => Mathf.Max(0, timeLimit - (Time.time - orderStartTime));
    public bool IsExpired => TimeRemaining <= 0;

    public void StartOrder()
    {
        orderStartTime = Time.time;
        isCompleted = false;
        isCorrect = false;
    }

    public void CompleteOrder(bool correct)
    {
        isCompleted = true;
        isCorrect = correct;
    }

    public int GetReward()
    {
        if (!isCompleted) return 0;
        return isCorrect ? correctOrderReward : incorrectOrderPenalty;
    }
}
