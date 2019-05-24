using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public Image[] Hearts;
    public Sprite fullHeart;
    public Sprite emptyHeart;
    
    public int livesToGive;
    public int Health;
    
    //matiin
    public GameObject music;
    public GameObject gameCanvas;
    //nyalain
    public GameObject endgameCanvas;

    // Start is called before the first frame update
    void Start()
    {
        Health = 3;
    }

    // Update is called once per frame
    void Update()
    {
        if(Health > 3)
        {
            Health = 3;
        }
        if(Health <= 0)
        {
            Die();
        }

        for (int i=0; i< Hearts.Length; i++){

            if (i < Health){
             Hearts[i].sprite = fullHeart;
            }
            else {
             Hearts[i].sprite = emptyHeart;
            }

            if(i < 3){
                Hearts[i].enabled = true;
            }
            else {
                Hearts[i].enabled = false;
            }
        }

    }

    public void AddLife(int livesToGive)
    {
        Health += livesToGive;
    }

    public void Damage(int dmg)
    {
        Health -= dmg;
        gameObject.GetComponent<Animation>().Play("PlayerAttackedRed");
    }

    void Die()
    {
        //Restart
        //Application.LoadLevel(Application.loadedLevel);

        //Matiin canvas sama BackGround music
        music.SetActive(false);
        gameCanvas.SetActive(false);
        //Nyalain end Game canvas
        endgameCanvas.SetActive(true);
        //Berhentiin Game seketika
        Time.timeScale = 0f;
    }
}
