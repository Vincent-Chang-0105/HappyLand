using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Hint
{
    [TextArea(2, 4)]
    public string hintText;
    public int hintId; // Unique identifier for this hint
    public float displayDuration = 3f; // How long to show the hint (0 = manual dismiss)
    public bool isCompleted = false;
}
