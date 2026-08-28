using UnityEngine;
using TMPro;

public class TeamPointsManager : MonoBehaviour
{
    [Header("Match Rules")]
    [SerializeField, Min(1)] int winningPoints = 1000;
    [SerializeField, Min(1f)] float matchDurationSeconds = 180f;
    [SerializeField] string resultSceneName = "Result";
    [SerializeField] TMP_Text timeText;

    TMP_Text teamPointsLText;
    TMP_Text teamPointsRText;
    RectTransform powerLRect;
    float powerBarFullWidth;
    float powerBarLeftX;
    float elapsedTime;
    int displayedTimeSecond = -1;
    bool matchFinished;

    void Awake()
    {
        teamPointsLText = transform.Find("Team_L/Team_Points/TeamPointsL").GetComponent<TMP_Text>();
        teamPointsRText = transform.Find("Team_R/TeamPoints/TeamPointsR").GetComponent<TMP_Text>();
        powerLRect = transform.Find("Power_L").GetComponent<RectTransform>();
        powerBarFullWidth = transform.Find("Power_R").GetComponent<RectTransform>().sizeDelta.x;
        powerBarLeftX = powerLRect.anchoredPosition.x - powerLRect.sizeDelta.x / 2f;
        if (timeText == null)
        {
            var timeTransform = transform.Find("Time");
            if (timeTransform != null) timeText = timeTransform.GetComponent<TMP_Text>();
        }

        UpdateTimeText();
    }

    void Update()
    {
        if (matchFinished || PlayerStatusManager.Instance == null) return;

        if (Input.GetKeyDown(KeyCode.I))
            PlayerStatusManager.Instance.ForceWinningTeamForResult(0);
        else if (Input.GetKeyDown(KeyCode.O))
            PlayerStatusManager.Instance.ForceWinningTeamForResult(1);

        if (PlayerStatusManager.Instance.ForcedWinningTeam >= 0)
        {
            FinishMatch();
            return;
        }

        elapsedTime = Mathf.Min(elapsedTime + Time.deltaTime, matchDurationSeconds);
        UpdateTimeText();

        int teamLPoints = 0;
        int teamRPoints = 0;
        for (int id = 0; id < PlayerStatusManager.Instance.Count; id++)
        {
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

        if (teamLPoints >= winningPoints
            || teamRPoints >= winningPoints
            || elapsedTime >= matchDurationSeconds)
        {
            FinishMatch();
        }
    }

    void UpdateTimeText()
    {
        if (timeText == null) return;

        var remainingSecond = Mathf.CeilToInt(matchDurationSeconds - elapsedTime);
        if (remainingSecond == displayedTimeSecond) return;

        displayedTimeSecond = remainingSecond;
        var minutes = remainingSecond / 60;
        var seconds = remainingSecond % 60;
        timeText.text = $"{minutes}:{seconds:00}";
    }

    void FinishMatch()
    {
        if (matchFinished) return;

        matchFinished = SceneTransitionController.LoadScene(resultSceneName);
    }
}
