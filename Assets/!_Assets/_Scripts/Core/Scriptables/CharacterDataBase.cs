using UnityEngine;

[CreateAssetMenu(fileName = "Character Database", menuName = "Dialogue/Character Database")]
public class CharacterDatabase : ScriptableObject
{
    [System.Serializable]
    public class CharacterEntry
    {
        public CharacterID id;
        public CharacterData characterData;
    }
    
    [SerializeField] private CharacterEntry[] characters;
    
    public CharacterData GetCharacterData(CharacterID id)
    {
        var entry = System.Array.Find(characters, c => c.id == id);
        return entry?.characterData;
    }
}
