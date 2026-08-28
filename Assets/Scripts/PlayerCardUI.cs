using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerCardUI : MonoBehaviour
{
    const float MaxJGaugePoints = 10f;

    TMP_Text numberText;
    TMP_Text pointsText;
    TMP_Text hpText;
    Image hpGaugeImage;
    Image jGaugeImage;
    int playerId = -1;

    void Awake()
    {
        numberText = transform.Find("Number").GetComponent<TMP_Text>();
        pointsText = transform.Find("Points").GetComponent<TMP_Text>();
        hpText = transform.Find("HP").GetComponent<TMP_Text>();
        hpGaugeImage = transform.Find("HPGauge").GetComponent<Image>();
        jGaugeImage = transform.Find("JGauge").GetComponent<Image>();
        ConfigureVerticalGauge(hpGaugeImage);
        ConfigureVerticalGauge(jGaugeImage);
        hpGaugeImage.fillAmount = 1f;
        jGaugeImage.fillAmount = 0f;
    }

    public void SetPlayerId(int id)
    {
        bool shouldBeVisible = id >= 0;
        if (playerId == id)
        {
            if (gameObject.activeSelf != shouldBeVisible) gameObject.SetActive(shouldBeVisible);
            return;
        }

        playerId = id;
        gameObject.SetActive(shouldBeVisible);
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
        jGaugeImage.fillAmount = Mathf.Clamp01(status.Crystals / MaxJGaugePoints);
    }

    static void ConfigureVerticalGauge(Image gaugeImage)
    {
        gaugeImage.type = Image.Type.Filled;
        gaugeImage.fillMethod = Image.FillMethod.Vertical;
        gaugeImage.fillOrigin = (int)Image.OriginVertical.Bottom;
    }
}
