using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class SceneCommands : MonoBehaviour {
    public void GotoScene(string name) {
        SceneManager.LoadScene(name);
    }
    public void Exit() {
        Application.Quit();
    }
}
