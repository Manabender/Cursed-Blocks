using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/* DebugRootInput class
 * This class has only one job; listen for input on the debug root menu and load the scene requested by the player.
 * I guess it also has a second job, setting the "current difficulty" text.
 */

public class DebugRootInput : MonoBehaviour
{
    public Text diffText;

    public void Start()
    {
        diffText.text = "SELECTED DIFFICULTY: " + PersistantVars.pVars.difficulty;
    }

    public void OnStartGame(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            SceneManager.LoadScene("MainGameScene");
        }
    }

    public void OnForcedCursesMenu(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            SceneManager.LoadScene("DebugScenes/DebugForcedCurses");
        }
    }

    public void OnPlayerSettingsMenu(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            SceneManager.LoadScene("DebugScenes/DebugPlayerSettings");
        }
    }

    public void OnDifficultySelectMenu(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            SceneManager.LoadScene("DebugScenes/DebugDifficultySelect");
        }
    }
}
