using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneOnClick : MonoBehaviour
{
    // public GameObject backGround;
    // public GameObject camera;
    // public GameObject partikel;

    public void LoadByIndex(int index)
    {
        //AsyncOperator operation = Scenemanager.LoadSceneAsync(index);
        SceneManager.LoadScene(index);
    }
}
