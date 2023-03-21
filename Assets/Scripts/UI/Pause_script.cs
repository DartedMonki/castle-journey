using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pause_script : MonoBehaviour
{
    //public static bool GameIsPaused = false;

    public GameObject pauseMenuUI;
    public GameObject gameCanvasUI;
    //public GameObject music;

    public void Resumegame()
    {
        gameCanvasUI.SetActive(true);
        pauseMenuUI.SetActive(false);
        //music.SetActive(true);
        Time.timeScale = 1f;
        //GameIsPaused = false;
    }

    public void Pausegame()
    {
        gameCanvasUI.SetActive(false);
        //music.SetActive(false);
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        //GameIsPaused = true;
    }
}
