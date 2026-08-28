using System.Collections.Generic;
using UnityEngine;

// PlayerStatusManagerに登録されたプレイヤーの人数に合わせてカードを生成し、
// teamNumberごとに画面の左右へ割り当てる。
public class PlayerCardManager : MonoBehaviour
{
    [SerializeField, Min(0f)] float cardGap = 10f;

    readonly List<PlayerCardUI> teamLCards = new List<PlayerCardUI>();
    readonly List<PlayerCardUI> teamRCards = new List<PlayerCardUI>();
    readonly List<int> teamLPlayerIds = new List<int>();
    readonly List<int> teamRPlayerIds = new List<int>();

    Transform teamLContainer;
    Transform teamRContainer;
    Vector2 teamLBasePosition;
    Vector2 teamRBasePosition;
    int lastPlayerLayoutHash = int.MinValue;
    bool initialized;

    void Start()
    {
        initialized = TryInitialize();
        RefreshCardsIfNeeded();
    }

    void Update()
    {
        if (!initialized) initialized = TryInitialize();
        RefreshCardsIfNeeded();
    }

    bool TryInitialize()
    {
        teamLContainer = transform.Find("Team_L");
        teamRContainer = transform.Find("Team_R");
        if (teamLContainer == null || teamRContainer == null) return false;

        teamLCards.Clear();
        teamRCards.Clear();
        teamLContainer.GetComponentsInChildren(true, teamLCards);
        teamRContainer.GetComponentsInChildren(true, teamRCards);

        if (teamLCards.Count == 0 || teamRCards.Count == 0)
        {
            Debug.LogError("PlayerCardManager: Team_L/Team_Rにカードのテンプレートがありません。", this);
            return false;
        }

        teamLBasePosition = GetAnchoredPosition(teamLCards[0]);
        teamRBasePosition = GetAnchoredPosition(teamRCards[0]);
        return true;
    }

    void RefreshCardsIfNeeded()
    {
        var statusManager = PlayerStatusManager.Instance;
        if (!initialized || statusManager == null) return;

        int layoutHash = CalculatePlayerLayoutHash(statusManager);
        if (layoutHash == lastPlayerLayoutHash) return;
        lastPlayerLayoutHash = layoutHash;

        teamLPlayerIds.Clear();
        teamRPlayerIds.Clear();

        for (int id = 0; id < statusManager.Count; id++)
        {
            var status = statusManager.GetStatus(id);
            if (status == null) continue;

            if (status.teamNumber == 0) teamLPlayerIds.Add(id);
            else if (status.teamNumber == 1) teamRPlayerIds.Add(id);
        }

        EnsureCardCount(teamLCards, teamLPlayerIds.Count, teamLContainer);
        EnsureCardCount(teamRCards, teamRPlayerIds.Count, teamRContainer);
        AssignAndLayoutCards(teamLCards, teamLPlayerIds, teamLBasePosition);
        AssignAndLayoutCards(teamRCards, teamRPlayerIds, teamRBasePosition);
    }

    void EnsureCardCount(List<PlayerCardUI> cards, int requiredCount, Transform container)
    {
        var template = cards[0];
        while (cards.Count < requiredCount)
        {
            var card = Instantiate(template, container);
            card.name = $"{template.name}_{cards.Count + 1}";
            cards.Add(card);
        }
    }

    void AssignAndLayoutCards(List<PlayerCardUI> cards, List<int> playerIds, Vector2 basePosition)
    {
        float spacing = GetVisualCardHeight(cards[0]) + cardGap;

        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            if (card == null) continue;

            if (card.transform is RectTransform rectTransform)
            {
                rectTransform.anchoredPosition = basePosition + Vector2.up * (spacing * i);
            }

            card.SetPlayerId(i < playerIds.Count ? playerIds[i] : -1);
        }
    }

    static float GetVisualCardHeight(PlayerCardUI card)
    {
        if (card != null && card.transform is RectTransform rectTransform)
        {
            return rectTransform.rect.height * Mathf.Abs(rectTransform.localScale.y);
        }

        return 0f;
    }

    static Vector2 GetAnchoredPosition(PlayerCardUI card)
    {
        return card != null && card.transform is RectTransform rectTransform
            ? rectTransform.anchoredPosition
            : Vector2.zero;
    }

    static int CalculatePlayerLayoutHash(PlayerStatusManager statusManager)
    {
        unchecked
        {
            int hash = statusManager.Count;
            for (int id = 0; id < statusManager.Count; id++)
            {
                var status = statusManager.GetStatus(id);
                hash = hash * 31 + (status != null ? status.teamNumber : -2);
            }

            return hash;
        }
    }
}
