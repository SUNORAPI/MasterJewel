using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerCardUI : MonoBehaviour
{
    TMP_Text numberText;
    TMP_Text pointsText;
    TMP_Text hpText;
    Image hpGaugeImage;
    int playerId = -1;

    void Awake()
    {
        numberText = transform.Find("Number").GetComponent<TMP_Text>();
        pointsText = transform.Find("Points").GetComponent<TMP_Text>();
        hpText = transform.Find("HP").GetComponent<TMP_Text>();
        hpGaugeImage = transform.Find("HPGauge").GetComponent<Image>();
        hpGaugeImage.type = Image.Type.Filled;
        hpGaugeImage.fillMethod = Image.FillMethod.Vertical;
        hpGaugeImage.fillOrigin = (int)Image.OriginVertical.Bottom;
        hpGaugeImage.fillAmount = 1f;

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
        hpGaugeImage.fillAmount = hp / 100f;
    }
}
