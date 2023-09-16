using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private GameObject panelMenu;
    

    public void OnMenuToggleClick()
    {
        panelMenu.SetActive(!panelMenu.activeSelf);
    }

    public void OnResetBDamaClick()
    {

    }

    public void OnResetTargetLocationClick()
    {

    }

    public void OnFreeCameraToggleClick()
    {

    }

    private void Awake()
    {
        Instance = this;
    }

}
