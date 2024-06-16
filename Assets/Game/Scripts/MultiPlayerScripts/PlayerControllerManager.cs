using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.IO;
using Photon.Realtime;
using UnityEngine.UI;
using System.Linq;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerControllerManager : MonoBehaviourPunCallbacks
{
    PhotonView view;
    GameObject controller;

    public int playerTeam;

    private Dictionary<int, int> playerTeams = new Dictionary<int, int>();

    void Awake()
    {
        view = GetComponent<PhotonView>();
    }
    void Start()
    {
        if (view.IsMine)
        {
            CreateController();
        }
    }

    void CreateController()
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Team"))
        {
            playerTeam = (int)PhotonNetwork.LocalPlayer.CustomProperties["Team"];
        }

        AssignPlayerToSpawnArea(playerTeam);
    }

    void AssignPlayerToSpawnArea(int team)
    {
        GameObject spawnArea1 = GameObject.Find("SpawnArea1");
        GameObject spawnArea2 = GameObject.Find("SpawnArea2");

        if (spawnArea1 == null || spawnArea2 == null)
        {
            Debug.LogError("Spawn area not found");
            return;
        }

        Transform spawnPoint = null;

        if (team == 1)
        {
            spawnPoint = spawnArea1.transform.GetChild(Random.Range(0, spawnArea1.transform.childCount));
        }

        if (team == 2)
        {
            spawnPoint = spawnArea2.transform.GetChild(Random.Range(0, spawnArea2.transform.childCount));
        }

        if (spawnPoint != null)
        {
            controller = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "Player"), spawnPoint.position, spawnPoint.rotation, 0, new object[] { view.ViewID });
            Debug.Log("Instantiated player at spawn point");
        }
        else
        {
            Debug.LogError("No available spawn points for team " + team);
        }
    }

    void AssignTeamsToAllPlayers()
    {
        foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.ContainsKey("Team"))
            {
                int team = (int)player.CustomProperties["Team"];
                playerTeams[player.ActorNumber] = team;

                AssignPlayerToSpawnArea(team);
            }
        }
    }

    public void Die()
    {
        PhotonNetwork.Destroy(controller);
        CreateController();
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        AssignTeamsToAllPlayers();
    }
}
