using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Starting Setup")]
    public CharacterData starterPlayer;
    public CharacterData starterEnemy;

    [Header("Runtime Data")]
    public PlayerData playerData = new PlayerData();

    // which enemy the next battle should use
    public CharacterData currentEnemyData;

    private void Awake()
    {
        // singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // if no characters yet, seed with starter
        if (playerData.ownedCharacters.Count == 0 && starterPlayer != null)
        {
            playerData.ownedCharacters.Add(new CharacterInstance(starterPlayer));
            playerData.activeCharacterIndex = 0;
        }

        if (currentEnemyData == null && starterEnemy != null)
        {
            currentEnemyData = starterEnemy;
        }
    }

    public CharacterInstance GetActiveCharacterInstance()
    {
        if (playerData.ownedCharacters.Count == 0)
            return null;

        if (playerData.activeCharacterIndex < 0 ||
            playerData.activeCharacterIndex >= playerData.ownedCharacters.Count)
        {
            playerData.activeCharacterIndex = 0;
        }

        return playerData.ownedCharacters[playerData.activeCharacterIndex];
    }

    public CharacterData GetCurrentEnemyData()
    {
        return currentEnemyData;
    }
}