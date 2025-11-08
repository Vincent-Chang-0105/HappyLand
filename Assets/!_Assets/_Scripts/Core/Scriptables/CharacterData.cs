using UnityEngine;

[CreateAssetMenu(fileName = "New Character", menuName = "Dialogue/Character Data")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    public Sprite characterSprite;
    public Color characterColor = Color.white;
}
