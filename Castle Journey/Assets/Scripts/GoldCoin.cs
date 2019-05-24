﻿using UnityEngine;
using System.Collections;

public class GoldCoin : MonoBehaviour
{
    public int coinValue = 500;
    private Score score;

    // Use this for initialization
	void Start()
	{
        score = FindObjectOfType<Score>();
	}

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag == "Player")
        {
            score.AddScore(coinValue);
            gameObject.SetActive(false);

        }
    }
}