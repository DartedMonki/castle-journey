using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EndScore : MonoBehaviour
{
    [SerializeField] private Text scoreText;
    
    // Start is called before the first frame update
    void Start()
    {
        scoreText.text = ((int) StaticClass.PreviousScore).ToString();
    }
}
