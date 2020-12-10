﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoteButton : MonoBehaviour
{
    public static VoteButton S;
    private void Awake()
    {
        if (S == null)
        {
            S = this;
        } else if (S != this)
        {
            Destroy(this);
        }
        this.gameObject.SetActive(false);
    }
}
