using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PauseScript : MonoBehaviour
{
    public static bool GameIsPaused = false;

    public GameObject pauseMenuUI;
    //public GameObject GameUI;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameIsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }
    public void Resume()
    {
        //audioSource.Play(0);
        pauseMenuUI.SetActive(false);
        //GameUI(true);
        Time.timeScale = 1f;
        GameIsPaused = false;
    }

    void Pause()
    {
        //audioSource.Pause();
        //GameUI(false);
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        GameIsPaused = true;
    }
}