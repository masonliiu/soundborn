using UnityEngine;
using UnityEngine.UI;

public class QuestNotificationBadge : MonoBehaviour
{
    [Tooltip("Child GameObject (e.g. red dot image) to show when there are unclaimed quests.")]
    public GameObject badgeRoot;

    private void OnEnable()
    {
        Refresh();
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestsChanged += Refresh;
        }
    }

    private void OnDisable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestsChanged -= Refresh;
        }
    }

    private void Update()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (badgeRoot == null)
            return;

        bool hasUnclaimed = HasUnclaimedQuests();
        if (badgeRoot.activeSelf != hasUnclaimed)
        {
            badgeRoot.SetActive(hasUnclaimed);
        }
    }

    private bool HasUnclaimedQuests()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.playerData == null || gm.playerData.questStates == null)
            return false;

        foreach (var state in gm.playerData.questStates)
        {
            if (state != null && state.isCompleted && !state.isClaimed)
            {
                return true;
            }
        }

        return false;
    }
}


