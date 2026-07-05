using System.Collections.Generic;
using UnityEngine;

// PlayerStatusManagerに登録されたプレイヤーをteamNumber順に割当
public class PlayerCardManager : MonoBehaviour
{
    readonly List<PlayerCardUI> teamLCards = new List<PlayerCardUI>();
    readonly List<PlayerCardUI> teamRCards = new List<PlayerCardUI>();

    void Start()
    {
        var teamL = transform.Find("Team_L");
        var teamR = transform.Find("Team_R");
        if (teamL != null) teamL.GetComponentsInChildren(true, teamLCards);
        if (teamR != null) teamR.GetComponentsInChildren(true, teamRCards);
    }

    void Update()
    {
        if (PlayerStatusManager.Instance == null) return;

        int lIndex = 0;
        int rIndex = 0;
        for (int id = 0; id < PlayerStatusManager.Instance.Count; id++){
            var status = PlayerStatusManager.Instance.GetStatus(id);
            if (status == null) continue;
            if (status.teamNumber == 0){
                if (lIndex < teamLCards.Count) teamLCards[lIndex].SetPlayerId(id);
                lIndex++;
            }
            else{
                if (rIndex < teamRCards.Count) teamRCards[rIndex].SetPlayerId(id);
                rIndex++;
            }
        }

        for (int i = lIndex; i < teamLCards.Count; i++) teamLCards[i].SetPlayerId(-1);
        for (int i = rIndex; i < teamRCards.Count; i++) teamRCards[i].SetPlayerId(-1);
    }
}
