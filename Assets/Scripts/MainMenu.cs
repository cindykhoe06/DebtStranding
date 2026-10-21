using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    void Start()
    {
        // Make sure the mouse cursor is visible and free in the menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void PlayGame()
    {
        // Loads the museum map (make sure the name matches your scene file exactly!)
        SceneManager.LoadScene("Main Scene");
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit(); // Works in the exported build
    }
}