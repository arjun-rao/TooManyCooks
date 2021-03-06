using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FloatingJoystick : Joystick
{
    public static Joystick S;

    private void Awake()
    {
        if (S == null)
        {
            S = this;
        } else if (S != this)
        {
            Destroy(this);
        }
    }

    protected override void Start()
    {
        base.Start();
        background.gameObject.SetActive(false);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (!GameManager.S.isMobile || GameManager.S.disableJoystick) return;
        background.anchoredPosition = ScreenPointToAnchoredPosition(eventData.position);
        background.gameObject.SetActive(true);
        base.OnPointerDown(eventData);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        if (!GameManager.S.isMobile || GameManager.S.disableJoystick) return;
        background.gameObject.SetActive(false);
        base.OnPointerUp(eventData);
    }
}