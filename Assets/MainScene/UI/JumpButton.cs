using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class JumpButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public bool pressing = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        pressing = true;
    }


    public void OnPointerUp(PointerEventData eventData)
    {
        pressing = false;
    }

}
