using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Facebook.Unity;
using UnityEngine.UI;
using System.Collections.Generic;

public class FacebookManager : MonoBehaviour
{
    public Text userIdText;

    private void Awake()
    {
        if (!FB.IsInitialized)
        {
            FB.Init();
        }
        else
        {
            FB.ActivateApp();
        }
    }

    public void LogIn()
    {
        FB.LogInWithReadPermissions(callback:OnLogIn);
    }

    private void OnLogIn(ILoginResult result)
    {
        if (FB.IsLoggedIn)
        {
            AccessToken Token = AccessToken.CurrentAccessToken;
            userIdText.text = Token.UserId;
        }
        else
            Debug.Log("Canceled Login");
    }
}
