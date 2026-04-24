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

    [Header("Tower UI")]
    public TextMeshProUGUI floorText;

    private readonly List<int> selected = new List<int>(4);
    private readonly HashSet<int> selectedSet = new HashSet<int>();

    private void Start()
    {
        if (preBattleRoot != null) preBattleRoot.SetActive(true);
        if (bottomBarRoot != null) bottomBarRoot.SetActive(false);

        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(() => SceneManager.LoadScene(homeSceneName));
        }

        for (int i = 0; slotButtons != null && i < slotButtons.Length; i++)
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
        RefreshFloorText();
    }

    private void LoadFromPlayerData()
    {
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
            Debug.LogWarning("[BattleLineupController] LoadFromPlayerData: activeLineupIndices is invalid! Resetting to -1.");
            gm.playerData.activeLineupIndices = new int[4] { -1, -1, -1, -1 };
            arr = gm.playerData.activeLineupIndices;
        }

        foreach (var idx in arr)
        {
            if (idx >= 0 && idx < gm.playerData.ownedCharacters.Count && selected.Count < 4)
            {
                selected.Add(idx);
                selectedSet.Add(idx);
            }
        }

        if (selected.Count == 0 && gm.playerData.ownedCharacters.Count > 0)
        {
            int idx = Mathf.Clamp(gm.playerData.activeCharacterIndex, 0, gm.playerData.ownedCharacters.Count - 1);
            selected.Add(idx);
            selectedSet.Add(idx);
        }
    }

    private void OnClickRosterIndex(int index)
    {
        if (selectedSet.Contains(index))
        {
            RemoveIndex(index);
        }
        else
        {
            if (selected.Count >= 4)
            {
                if (hintText != null) hintText.text = "Lineup is full (4). Tap one to remove.";
                return;
            }

            selected.Add(index);
            selectedSet.Add(index);

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnTeamChanged();
            }
        }

        RefreshUI();
    }

    private void RemoveAtSlot(int slot)
    {
        if (slot < 0 || slot >= selected.Count)
        {
            return;
        }

        int idx = selected[slot];
        RemoveIndex(idx);
        RefreshUI();
    }

    private void RemoveIndex(int index)
    {
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

        for (int i = 0; i < 4; i++)
        {
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
            }
        }

        if (battleButton != null)
            battleButton.interactable = (selected.Count > 0);

        if (hintText != null)
            hintText.text = $"Selected: {selected.Count}/4";

        if (teamPanel != null)
            teamPanel.SetSelected(selectedSet);
    }

    private void RefreshFloorText()
    {
        var gm = GameManager.Instance;
        if (gm == null || floorText == null)
            return;
        floorText.text = $"Floor {gm.GetFloorLabel(gm.playerData.towerCurrentFloor)}";
    }

    private void ConfirmAndStartBattle()
    {
        if (selected.Count == 0)
        {
            if (hintText != null) hintText.text = "Select at least 1 hero to start.";
            return;
        }

        var gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[BattleLineupController] ConfirmAndStartBattle: GameManager.Instance is NULL!");
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            gm.playerData.activeLineupIndices[i] = (i < selected.Count) ? selected[i] : -1;
        }

        gm.playerData.activeCharacterIndex = selected[0];

        gm.SetEnemyFromTowerFloor();

        if (preBattleRoot != null) preBattleRoot.SetActive(false);
        if (bottomBarRoot != null) bottomBarRoot.SetActive(true);

        if (battleController != null)
        {
            battleController.ResetBattleState();
            battleController.StartBattleNow();
        }
        else
        {
            Debug.LogError("[BattleLineupController] ConfirmAndStartBattle: battleController is NULL!");
        }
    }
}
