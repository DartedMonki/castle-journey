using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    public int LoadLevel;
    public Score score;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            StaticClass.PreviousScore = score.score;
            SceneManager.LoadScene(LoadLevel);
        }
    }

}
