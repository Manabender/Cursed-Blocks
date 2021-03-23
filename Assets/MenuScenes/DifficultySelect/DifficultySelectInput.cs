using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/* DifficultySelectInput class
 * This class has the simple job of handling mouse input on the difficulty select menu.
 */

public class DifficultySelectInput : MonoBehaviour
{
    public void OnEasy()
    {
        PersistantVars.pVars.difficulty = 0;
        StartGame();
    }
    
    public void OnMedium()
    {
        PersistantVars.pVars.difficulty = 1;
        StartGame();
    }
    
    public void OnHard()
    {
        PersistantVars.pVars.difficulty = 2;
        StartGame();
    }

    public void StartGame()
    {
        SceneManager.LoadScene("MainGameScene");
    }
}
