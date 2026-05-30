using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class mainMenuController : MonoBehaviour
{
    public void Play()
    {
        // Ensure UIManager will show the level select panel after loading main
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowLevelSelect();
        }

        // Load the main scene (UI is persistent on UIManager)
        SceneManager.LoadScene("main");
    }
}
