using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RankingItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private Image bdamaImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI distanceText;

    public void UpdateUI(int rank, BDama bdama, float distance)
    {
        rankText.text = $"{rank + 1}位";
        bdamaImage.color = bdama.meshRenderer.material.color;
        scoreText.text = $"({bdama.player.score.Value})";
        distanceText.text = $"{distance:0}m";
    }
}
