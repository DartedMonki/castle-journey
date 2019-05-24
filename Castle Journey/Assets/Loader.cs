using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Loader : MonoBehaviour
{
    //perlu dimatiin
    public GameObject menuCanvas;
    public GameObject menuBG;
    public GameObject partikel;
    //perlu dinyalain
    public GameObject loadingCanvas;
    public Slider slider;
    public GameObject loadBG;

    public void loadScene(int index)
    {
        StartCoroutine(LoadAsyncronously(index));
    }
    IEnumerator LoadAsyncronously(int index)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(index);
        
        //fungsi ngaktifin
        menuCanvas.SetActive(false);
        menuBG.SetActive(false);
        partikel.SetActive(false);
        loadingCanvas.SetActive(true);
        loadBG.SetActive(true);

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / .9f);
            slider.value = progress;
            yield return null;
        }
    }
}
