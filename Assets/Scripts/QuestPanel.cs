using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Simple vertical list displaying active quests and allowing reward claim.
/// </summary>
public class QuestPanel : MonoBehaviour
{
    [Header("UI")]
    public Transform contentRoot;
    public QuestRowUI rowPrefab;

    private void OnEnable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestsChanged += Refresh;
        }
        Refresh();
    }

    private void OnDisable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestsChanged -= Refresh;
        }
    }

    public void Refresh()
    {
        if (contentRoot == null || rowPrefab == null || GameManager.Instance == null) return;

        foreach (Transform child in contentRoot)
        {
            Destroy(child.gameObject);
        }

        var pd = GameManager.Instance.playerData;
        if (pd == null || pd.questStates == null || QuestManager.Instance == null) return;

        foreach (var state in pd.questStates)
        {
            if (state == null) continue;
            var def = GetDefinition(state.questId);
            if (def == null) continue;

            var row = Instantiate(rowPrefab, contentRoot);
            row.Bind(def, state);
        }
    }

    private QuestData GetDefinition(string questId)
    {
        if (QuestManager.Instance == null || string.IsNullOrEmpty(questId)) return null;
        var defs = QuestManager.Instance.initialQuests;
        if (defs == null) return null;
        for (int i = 0; i < defs.Length; i++)
        {
            var q = defs[i];
            if (q != null && q.questId == questId)
                return q;
        }
        return null;
    }
}


