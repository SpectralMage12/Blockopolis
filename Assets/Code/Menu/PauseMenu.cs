using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }
    [SerializeField] private GameObject pauseMenu;

    private void Awake() {
        if(Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }

    private void Start() {
        HidePauseMenu();
    }

    public void DisplayPauseMenu(object sender, EventArgs e) {
        pauseMenu.SetActive(true);
    }

    public void HidePauseMenu(object sender, EventArgs e) {
        HidePauseMenu();
    }

    public void HidePauseMenu() {
        pauseMenu.SetActive(false);
    }

    public void ReturnToTitleScreen(object sender, EventArgs e) {
        SceneManager.LoadScene(sceneName: MenuProperties.menuSceneName);
    }
}
