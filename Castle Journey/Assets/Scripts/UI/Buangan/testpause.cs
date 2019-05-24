using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testpause : MonoBehaviour
{
    public GameObject pausemenu;

    void testPauseGame()
    {
        Time.timeScale = 0f;
    }
    void testResumeGame()
    {
        Time.timeScale = 1f;
    }
}
