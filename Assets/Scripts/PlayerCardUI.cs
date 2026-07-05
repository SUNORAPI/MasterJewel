using UnityEngine;
using TMPro;

public class PlayerCardUI : MonoBehaviour
{
    // LRのHPバー方向
    [SerializeField] bool anchorHpFromRight = false;

    TMP_Text numberText;
    TMP_Text pointsText;
    TMP_Text hpText;
    RectTransform hpGreenRect;
    float hpGreenFullWidth;
    float hpGreenFixedEdgeX;
    int playerId = -1;

    void Awake()
    {
        numberText = transform.Find("Number").GetComponent<TMP_Text>();
        pointsText = transform.Find("Points").GetComponent<TMP_Text>();
        hpText = transform.Find("HP").GetComponent<TMP_Text>();
        hpGreenRect = transform.Find("HP_Green").GetComponent<RectTransform>();
        hpGreenFullWidth = hpGreenRect.sizeDelta.x;
        float halfWidth = hpGreenFullWidth / 2f;
        hpGreenFixedEdgeX = anchorHpFromRight
            ? hpGreenRect.anchoredPosition.x + halfWidth
            : hpGreenRect.anchoredPosition.x - halfWidth;

        gameObject.SetActive(false);
    }

    public void SetPlayerId(int id)
    {
        if (playerId == id) return;
        playerId = id;
        gameObject.SetActive(id >= 0);
    }

    void Update()
    {
        if (playerId < 0) return;

        var status = PlayerStatusManager.Instance != null
            ? PlayerStatusManager.Instance.GetStatus(playerId)
            : null;
        if (status == null) return;

        numberText.text = $"P{playerId + 1}";
        pointsText.text = $"{status.Crystals}P";
        int hp = Mathf.Clamp(status.health, 0, 100);
        hpText.text = $"{hp}%";
        float width = hpGreenFullWidth * (hp / 100f);
        float halfWidth = width / 2f;
        float centerX = anchorHpFromRight ? hpGreenFixedEdgeX - halfWidth : hpGreenFixedEdgeX + halfWidth;

        hpGreenRect.sizeDelta = new Vector2(width, hpGreenRect.sizeDelta.y);
        hpGreenRect.anchoredPosition = new Vector2(centerX, hpGreenRect.anchoredPosition.y);
    }
}
