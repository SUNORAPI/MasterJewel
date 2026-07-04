using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUIManager : MonoBehaviour
{
    [SerializeField] GameObject HP_Prefab;
    [SerializeField] Transform LeftContainer;//左のUI管理
    [SerializeField] Transform RightContainer;//右のUI管理
    [SerializeField] int maxHP = 100;

    readonly List<(int playerId, Slider slider)> bars = new List<(int, Slider)>();

    void Start()
    {
        var registrars = FindObjectsByType<PlayerRegistrar>(FindObjectsSortMode.None).OrderBy(r => r.PlayerId).ToList();

        foreach(var registrar in registrars)
        {
            int id = registrar.PlayerId;
            var status = PlayerStatusManager.Instance.GetStatus(id);
            if(status == null) continue;

            //チームナンバーで左右を判定
            Transform parent = (status.teamNumber == 0) ? LeftContainer : RightContainer;

            GameObject instance = Instantiate(HP_Prefab, parent);
            Slider slider = instance.GetComponentInChildren<Slider>();
            TMP_Text nameText = instance.GetComponentInChildren<TMP_Text>();

            slider.maxValue = maxHP;
            slider.value = status.health;
            if(nameText != null) nameText.text = $"Player{id + 1} HP";

            bars.Add((id, slider));
        }
    }

    void Update()
    {
        foreach(var(playerId, slider) in bars)
        {
            var status = PlayerStatusManager.Instance.GetStatus(playerId);
            if(status != null) slider.value = status.health;
        }
    }
}
