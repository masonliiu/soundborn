using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class CharacterCatalogPanel : MonoBehaviour
{
    [System.Serializable]
    public class SlotUI
    {
        public Button button;
        public Image portraitImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI levelText;
    }

    [Header("Root")]
    public GameObject root;       

    [Header("Slots")]
    public SlotUI[] slots;         

    [Header("Upgrade")]
    public UpgradePanel upgradePanel;

    private HomeUIController homeUI;
    private int[] characterIndexBySlot;

    private void Awake()
    {
        if (slots == null)
            slots = new SlotUI[0];

        characterIndexBySlot = new int[slots.Length];

        for (int i = 0; i < slots.Length; i++)
        {
            int slotIndex = i;
            var slot = slots[i];
            if (slot != null && slot.button != null)
            {
                slot.button.onClick.AddListener(() => OnClickSlot(slotIndex));
            }
        }

        if (root != null)
            root.SetActive(false);
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDataChanged += HandlePlayerDataChanged;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDataChanged -= HandlePlayerDataChanged;
        }
    }

    private void HandlePlayerDataChanged()
    {
        if (root != null && root.activeSelf)
        {
            Refresh();
        }
    }

    public void Show(HomeUIController home)
    {
        homeUI = home;

        if (root != null)
            root.SetActive(true);

        Refresh();
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }

    public void Refresh()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        var owned = gm.playerData.ownedCharacters;
        if (owned == null) return;

        for (int i = 0; i < characterIndexBySlot.Length; i++)
        {
            characterIndexBySlot[i] = -1;
        }

        var orderedIndices = Enumerable.Range(0, owned.Count)
            .OrderByDescending(i => owned[i].level)
            .ThenBy(i => owned[i].data != null ? owned[i].data.displayName : "")
            .ToList();

        for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
        {
            var slot = slots[slotIndex];
            if (slot == null || slot.button == null)
                continue;

            bool hasChar = slotIndex < orderedIndices.Count;
            slot.button.gameObject.SetActive(hasChar);

            if (!hasChar)
                continue;

            int charIndex = orderedIndices[slotIndex];
            characterIndexBySlot[slotIndex] = charIndex;

            var inst = owned[charIndex];

            if (slot.portraitImage != null &&
                inst.data != null &&
                inst.data.silhouetteSprite != null)
            {
                slot.portraitImage.sprite = inst.data.silhouetteSprite;
            }

            if (slot.nameText != null)
                slot.nameText.text = inst.data != null ? inst.data.displayName : "???";

            if (slot.levelText != null)
                slot.levelText.text = "Lv. " + inst.level.ToString();
        }
    }

    private void OnClickSlot(int slotIndex)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (characterIndexBySlot == null ||
            slotIndex < 0 ||
            slotIndex >= characterIndexBySlot.Length)
            return;

        int charIndex = characterIndexBySlot[slotIndex];
        if (charIndex < 0 ||
            gm.playerData.ownedCharacters == null ||
            charIndex >= gm.playerData.ownedCharacters.Count)
            return;

        if (upgradePanel != null)
        {
            upgradePanel.ShowForCharacter(homeUI, charIndex);
        }
    }

    public void OnClick_Close()
    {
        Hide();
    }
}