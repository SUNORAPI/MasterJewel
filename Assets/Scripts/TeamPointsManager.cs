using UnityEngine;
using TMPro;

public class TeamPointsManager : MonoBehaviour
{
    TMP_Text teamPointsLText;
    TMP_Text teamPointsRText;
    RectTransform powerLRect;
    float powerBarFullWidth;
    float powerBarLeftX;

    void Awake()
    {
        teamPointsLText = transform.Find("Team_L/Team_Points/TeamPointsL").GetComponent<TMP_Text>();
        teamPointsRText = transform.Find("Team_R/TeamPoints/TeamPointsR").GetComponent<TMP_Text>();
        powerLRect = transform.Find("Power_L").GetComponent<RectTransform>();
        powerBarFullWidth = transform.Find("Power_R").GetComponent<RectTransform>().sizeDelta.x;
        powerBarLeftX = powerLRect.anchoredPosition.x - powerLRect.sizeDelta.x / 2f;
    }

    void Update()
    {
        if (PlayerStatusManager.Instance == null) return;
        int teamLPoints = 0;
        int teamRPoints = 0;
        for (int id = 0; id < PlayerStatusManager.Instance.Count; id++){
            var status = PlayerStatusManager.Instance.GetStatus(id);
            if (status == null) continue;
            if (status.teamNumber == 0) teamLPoints += status.Crystals;
            else teamRPoints += status.Crystals;
        }

        teamPointsLText.text = teamLPoints.ToString();
        teamPointsRText.text = teamRPoints.ToString();
        int total = teamLPoints + teamRPoints;
        float ratio = total > 0 ? (float)teamLPoints / total : 0.5f;
        float width = powerBarFullWidth * ratio;
        powerLRect.sizeDelta = new Vector2(width, powerLRect.sizeDelta.y);
        powerLRect.anchoredPosition = new Vector2(powerBarLeftX + width / 2f, powerLRect.anchoredPosition.y);
    }
}
