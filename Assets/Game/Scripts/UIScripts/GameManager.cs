using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public bool isMenuOpened = false;
    public GameObject menuUI;
    public GameObject scoreUI;

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape) && isMenuOpened == false)
        {
            scoreUI.SetActive(false);
            menuUI.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            isMenuOpened = true;
            AudioListener.pause = true;
        }
        else if(Input.GetKeyDown(KeyCode.Escape) && isMenuOpened == true)
        {
            scoreUI.SetActive(true);
            menuUI.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            isMenuOpened = false;
            AudioListener.pause = false;
        }
    }
    public void LeaveGame()
    {
        Debug.Log("Game Leave");
        Application.Quit();
    }
}
