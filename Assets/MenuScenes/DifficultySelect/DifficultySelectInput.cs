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

    public void OnHover()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_MOVE);
    }

    public void StartGame()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_SELECT);
        PersistantVars.pVars.goal = ModeGoal.SURVIVE;
        PersistantVars.pVars.bagType = BagType.CURSED;
        PersistantVars.pVars.width = 10;
        PersistantVars.pVars.overtuned = false;
        SceneManager.LoadScene("MainGameScene");
    }
}
