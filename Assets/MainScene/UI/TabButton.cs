using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TabButton : MonoBehaviour
{
    [SerializeField] private Transform tabContents;
    [SerializeField] private GameObject tabTarget;
    private Button button;

    private void Awake()
    {
        TryGetComponent(out button);
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        for (int i = 0; i < tabContents.childCount; i++)
        {
            var child = tabContents.GetChild(i).gameObject;
            child.SetActive(child == tabTarget);
        }
    }
}
