using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class PlayerUsernameManager : MonoBehaviour
{
    [SerializeField] InputField usernameInput;
    [SerializeField] Text errorMessageText;

    void Start()
    {
        if(PlayerPrefs.HasKey("username"))
        {
            usernameInput.text = PlayerPrefs.GetString("username");
            PhotonNetwork.NickName = PlayerPrefs.GetString("username");
        }
    }

    public void PlayerUsernameInputValueChanged()
    {
        string username = usernameInput.text;

        if(!string.IsNullOrEmpty(username) && username.Length <= 20)
        {
            PhotonNetwork.NickName = username;
            PlayerPrefs.SetString("username", username);
            errorMessageText.text = "";
            MenuManager.instance.OpenMenu("TitleMenu");
        }
        else
        {
            errorMessageText.text = "Username mustn't be empty and should of 20 characters or less.";
        }
    }
}
