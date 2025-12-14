using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleLineupController : MonoBehaviour
{
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
        if (preBattleRoot != null) preBattleRoot.SetActive(true);
        if (bottomBarRoot != null) bottomBarRoot.SetActive(false);

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
        var gm = GameManager.Instance;
        if (gm == null) return;

        selected.Clear();
        selectedSet.Clear();

        var arr = gm.playerData.activeLineupIndices;
        if (arr == null || arr.Length != 4) return;

        foreach (var idx in arr)
        {
            if (idx >= 0 && idx < gm.playerData.ownedCharacters.Count && selected.Count < 4)
            {
                selected.Add(idx);
                selectedSet.Add(idx);
            }
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
        }

        RefreshUI();
    }

    private void RemoveAtSlot(int slot)
    {
        if (slot < 0 || slot >= selected.Count) return;
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
        if (gm == null) return;

        for (int i = 0; i < 4; i++)
        {
            if (slotImages == null || i >= slotImages.Length) continue;

            if (i < selected.Count)
            {
                var inst = gm.playerData.ownedCharacters[selected[i]];
                slotImages[i].sprite = inst.data != null ? inst.data.silhouetteSprite : null;
                slotImages[i].color = Color.white;
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
            hintText.text = $"Select up to 4. Selected: {selected.Count}/4";

        if (teamPanel != null)
            teamPanel.SetSelected(selectedSet);
    }

    private void ConfirmAndStartBattle()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        for (int i = 0; i < 4; i++)
            gm.playerData.activeLineupIndices[i] = (i < selected.Count) ? selected[i] : -1;

        if (selected.Count > 0)
            gm.playerData.activeCharacterIndex = selected[0];

        if (preBattleRoot != null) preBattleRoot.SetActive(false);
        if (bottomBarRoot != null) bottomBarRoot.SetActive(true);

        if (battleController != null)
            battleController.StartBattleNow();
    }
}