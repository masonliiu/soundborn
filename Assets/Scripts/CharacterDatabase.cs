using UnityEngine;

[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Game/Character Database")]
public class CharacterDatabase : ScriptableObject
{
    public CharacterData[] allCharacters;

    public CharacterData GetById(string id)
    {
        if (string.IsNullOrEmpty(id) || allCharacters == null) return null;
        for (int i = 0; i < allCharacters.Length; i++)
        {
            var c = allCharacters[i];
            if (c != null && c.characterId == id)
                return c;
        }
        return null;
    }

    public CharacterData GetByDisplayName(string name)
    {
        if (string.IsNullOrEmpty(name) || allCharacters == null) return null;
        for (int i = 0; i < allCharacters.Length; i++)
        {
            var c = allCharacters[i];
            if (c != null && c.displayName == name)
                return c;
        }
        return null;
    }
}


