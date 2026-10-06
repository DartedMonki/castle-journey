using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
        EventSystem.current?.SetSelectedGameObject(null);
        //GameIsPaused = false;
    }

    public void Pausegame()
    {
        gameCanvasUI.SetActive(false);
        //music.SetActive(false);
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        var resume = pauseMenuUI.GetComponentsInChildren<Button>(false);
        var button = System.Array.Find(resume, item => item.name.Contains("Resume"));
        if (button != null)
            EventSystem.current?.SetSelectedGameObject(button.gameObject);
        //GameIsPaused = true;
    }
}
