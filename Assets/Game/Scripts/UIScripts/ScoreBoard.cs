using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;
using Photon.Pun;

public class ScoreBoard : MonoBehaviour
{
    public static ScoreBoard instance;
    public Text blueteamText;
    public Text redteamText;

    public int blueTeamScore = 0;
    public int redTeamScore = 0;

    private PhotonView view;

    void Awake()
    {
        instance = this;
        view = GetComponent<PhotonView>();
    }

    public void PlayerDied(int playerTeam)
    {
        if(playerTeam == 2)
        {
            blueTeamScore++;
        }

        if(playerTeam == 1)
        {
            redTeamScore++;
        }

        view.RPC("UpdateScores", RpcTarget.All, blueTeamScore, redTeamScore);
    }
    
    [PunRPC]
    void UpdateScores(int blueScore, int redScore)
    {
        blueTeamScore = blueScore;
        redTeamScore = redScore;

        blueteamText.text = blueTeamScore.ToString();
        redteamText.text = redScore.ToString();
    }
}
