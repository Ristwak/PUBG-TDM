using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class UsernameDisplay : MonoBehaviour
{
    public Text userNameText;
    public Text teamText;
    public PhotonView view;

    void Start()
    {
        if (view.IsMine)
        {
            // don't want to display the username
            gameObject.SetActive(false);
        }
        userNameText.text = view.Owner.NickName;

        // show team number
        if (view.Owner.CustomProperties.ContainsKey("Team"))
        {
            int team = (int)view.Owner.CustomProperties["Team"];
            teamText.text = "Team " + team;
        }
    }
}
