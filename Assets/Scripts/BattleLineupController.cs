using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class BattleLineupController : MonoBehaviour
{
    [Header("Navigation")]
    public Button exitButton;
    public string homeSceneName = "HomeScene";

    [Header("Scene References")]
    public GameObject preBattleRoot; 
    public GameObject bottomBarRoot;   
    public Button battleButton;        
    public TextMeshProUGUI hintText;  

    [Header("Lineup Slots (4)")]
    public Image[] slotImages;        
    public Button[] slotButtons;       

    [Header("Roster Scroll")]
    public BattleTeamPanel teamPanel; 

    [Header("Battle")]
    public BattleController battleController;

    private readonly List<int> selected = new List<int>(4);
    private readonly HashSet<int> selectedSet = new HashSet<int>();

    private void Start()
    {
        Debug.Log("[BattleLineupController] Start() called");
        
        if (preBattleRoot != null) preBattleRoot.SetActive(true);
        if (bottomBarRoot != null) bottomBarRoot.SetActive(false);

        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(() => SceneManager.LoadScene(homeSceneName));
        }
        
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int slot = i;
            if (slotButtons[slot] != null)
                slotButtons[slot].onClick.AddListener(() => RemoveAtSlot(slot));
        }

        if (battleButton != null)
            battleButton.onClick.AddListener(ConfirmAndStartBattle);

        if (teamPanel != null)
            teamPanel.SetClickHandler(OnClickRosterIndex);

        LoadFromPlayerData();
        RefreshUI();
    }

    private void LoadFromPlayerData()
    {
        Debug.Log("[BattleLineupController] LoadFromPlayerData() called");
        
        var gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[BattleLineupController] LoadFromPlayerData: GameManager.Instance is NULL!");
            return;
        }

        selected.Clear();
        selectedSet.Clear();

        var arr = gm.playerData.activeLineupIndices;
        if (arr == null || arr.Length != 4)
        {
            Debug.LogWarning("[BattleLineupController] LoadFromPlayerData: activeLineupIndices is invalid!");
            return;
        }

        Debug.Log($"[BattleLineupController] LoadFromPlayerData: activeLineupIndices = [{arr[0]}, {arr[1]}, {arr[2]}, {arr[3]}]");
        Debug.Log($"[BattleLineupController] LoadFromPlayerData: ownedCharacters.Count = {gm.playerData.ownedCharacters.Count}");

        foreach (var idx in arr)
        {
            if (idx >= 0 && idx < gm.playerData.ownedCharacters.Count && selected.Count < 4)
            {
                selected.Add(idx);
                selectedSet.Add(idx);
                Debug.Log($"[BattleLineupController] LoadFromPlayerData: Added character index {idx} to selected lineup");
            }
        }

        Debug.Log($"[BattleLineupController] LoadFromPlayerData: Loaded {selected.Count} characters into lineup");
    }

    private void OnClickRosterIndex(int index)
    {
        Debug.Log($"[BattleLineupController] OnClickRosterIndex({index}) called");

        if (selectedSet.Contains(index))
        {
            RemoveIndex(index);
        }
        else
        {
            if (selected.Count >= 4)
            {
                if (hintText != null) hintText.text = "Lineup is full (4). Tap one to remove.";
                Debug.LogWarning("[BattleLineupController] OnClickRosterIndex: Lineup is full!");
                return;
            }

            selected.Add(index);
            selectedSet.Add(index);
            Debug.Log($"[BattleLineupController] OnClickRosterIndex: Added character {index} to lineup. Total: {selected.Count}");
        }

        RefreshUI();
    }

    private void RemoveAtSlot(int slot)
    {
        Debug.Log($"[BattleLineupController] RemoveAtSlot({slot}) called");
        
        if (slot < 0 || slot >= selected.Count)
        {
            Debug.LogWarning($"[BattleLineupController] RemoveAtSlot: Invalid slot index {slot}!");
            return;
        }
        
        int idx = selected[slot];
        RemoveIndex(idx);
        RefreshUI();
    }

    private void RemoveIndex(int index)
    {
        Debug.Log($"[BattleLineupController] RemoveIndex({index}) called");
        selected.Remove(index);
        selectedSet.Remove(index);
    }

    private void RefreshUI()
    {
        var gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[BattleLineupController] RefreshUI: GameManager.Instance is NULL!");
            return;
        }

        if (slotImages == null || slotImages.Length < 4)
        {
            Debug.LogError($"[BattleLineupController] RefreshUI: slotImages array is invalid! Null: {slotImages == null}, Length: {(slotImages != null ? slotImages.Length.ToString() : "N/A")}");
            return;
        }

        Debug.Log($"[BattleLineupController] RefreshUI: Updating UI for {selected.Count} selected characters");

        for (int i = 0; i < 4; i++)
        {
            if (i >= slotImages.Length)
            {
                Debug.LogError($"[BattleLineupController] RefreshUI: Slot {i} is out of bounds!");
                continue;
            }

            if (slotImages[i] == null)
            {
                Debug.LogError($"[BattleLineupController] RefreshUI: slotImages[{i}] is NULL!");
                continue;
            }

            if (i < selected.Count)
            {
                int charIndex = selected[i];
                if (charIndex >= 0 && charIndex < gm.playerData.ownedCharacters.Count)
                {
                    var inst = gm.playerData.ownedCharacters[charIndex];
                    if (inst != null && inst.data != null)
                    {
                        slotImages[i].sprite = inst.data.silhouetteSprite;
                        slotImages[i].color = Color.white;
                        Debug.Log($"[BattleLineupController] RefreshUI: Slot {i}: Set sprite for {inst.data.displayName}");
                    }
                    else
                    {
                        Debug.LogWarning($"[BattleLineupController] RefreshUI: Slot {i}: Character instance or data is NULL!");
                        slotImages[i].sprite = null;
                        slotImages[i].color = new Color(1f, 1f, 1f, 0.15f);
                    }
                }
                else
                {
                    Debug.LogWarning($"[BattleLineupController] RefreshUI: Slot {i}: Invalid character index {charIndex}!");
                    slotImages[i].sprite = null;
                    slotImages[i].color = new Color(1f, 1f, 1f, 0.15f);
                }
            }
            else
            {
                slotImages[i].sprite = null;
                slotImages[i].color = new Color(1f, 1f, 1f, 0.15f);
                Debug.Log($"[BattleLineupController] RefreshUI: Slot {i}: Empty slot");
            }
        }

        if (battleButton != null)
            battleButton.interactable = (selected.Count > 0);

        if (hintText != null)
            hintText.text = $"Selected: {selected.Count}/4";

        if (teamPanel != null)
            teamPanel.SetSelected(selectedSet);
    }

    private void ConfirmAndStartBattle()
    {
        Debug.Log("[BattleLineupController] ConfirmAndStartBattle() called");
        Debug.Log($"[BattleLineupController] ConfirmAndStartBattle: Selected characters: {selected.Count}");
        
        var gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[BattleLineupController] ConfirmAndStartBattle: GameManager.Instance is NULL!");
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            gm.playerData.activeLineupIndices[i] = (i < selected.Count) ? selected[i] : -1;
            Debug.Log($"[BattleLineupController] ConfirmAndStartBattle: activeLineupIndices[{i}] = {gm.playerData.activeLineupIndices[i]}");
        }

        if (selected.Count > 0)
        {
            gm.playerData.activeCharacterIndex = selected[0];
            Debug.Log($"[BattleLineupController] ConfirmAndStartBattle: activeCharacterIndex = {gm.playerData.activeCharacterIndex}");
        }

        Debug.Log($"[BattleLineupController] ConfirmAndStartBattle: Final activeLineupIndices = [{gm.playerData.activeLineupIndices[0]}, {gm.playerData.activeLineupIndices[1]}, {gm.playerData.activeLineupIndices[2]}, {gm.playerData.activeLineupIndices[3]}]");

        if (preBattleRoot != null) preBattleRoot.SetActive(false);
        if (bottomBarRoot != null) bottomBarRoot.SetActive(true);

        if (battleController != null)
        {
            Debug.Log("[BattleLineupController] ConfirmAndStartBattle: Calling battleController.StartBattleNow() with final lineup");
            battleController.ResetBattleState();
            battleController.StartBattleNow();
        }
        else
        {
            Debug.LogError("[BattleLineupController] ConfirmAndStartBattle: battleController is NULL!");
        }
    }
}
