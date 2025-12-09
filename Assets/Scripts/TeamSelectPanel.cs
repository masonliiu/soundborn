using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TeamSelectPanel : MonoBehaviour
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

    private HomeUIController homeUI;

    private void Awake()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            int index = i;
            var slot = slots[i];
            if (slot != null && slot.button != null)
            {
                slot.button.onClick.AddListener(() => OnClickSlot(index));
            }
        }

        if (root != null)
            root.SetActive(false);
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

    private void Refresh()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        var owned = gm.playerData.ownedCharacters;

        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (slot == null || slot.button == null)
                continue;

            bool hasChar = i < owned.Count;
            slot.button.gameObject.SetActive(hasChar);

            if (!hasChar)
                continue;

            var inst = owned[i];

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

    private void OnClickSlot(int index)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        gm.SetActiveCharacterIndex(index);

        if (homeUI != null)
            homeUI.Refresh();

        Hide();
    }

    public void OnClick_Close()
    {
        Hide();
    }
}