using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Score : MonoBehaviour
{
    public float score = 0.0f;
    public Text scoreText;
    
    // Start is called before the first frame update
    void Start()
    {
        scoreText.text = "SCORE";
    }

    // Update is called once per frame
    void Update()
    {
        scoreText.text = "SCORE\n" + ((int)score).ToString();
    }

}
