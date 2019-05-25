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

    public void Share()
    {
        FB.ShareLink(contentTitle: "Castle Journey", contentURL:new System.Uri("https://github.com/DartedMonki/Castle_Journey"), contentDescription:"Here's a Description About Game", callback:OnShare);
    }

    private void OnShare(IShareResult result)
    {
        if(result.Cancelled || !string.IsNullOrEmpty(result.Error))
        {
            Debug.Log("ShareLink Error: " + result.Error);
        }
        else if (!string.IsNullOrEmpty(result.PostId))
        {
            Debug.Log(result.PostId);
        }
        else
        {
            Debug.Log("Share Succeed");
        }
    }
}
