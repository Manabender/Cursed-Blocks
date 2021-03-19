using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/* DebugDifficultySelectInput class
 * This class has only one job; listen for input on the debug difficulty select menu, set the selected difficulty, then return to debug root.
 */

public class DebugDifficultySelectInput : MonoBehaviour
{
    public void OnSelectEasy(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            SelectDifficulty(0);
        }
    }

    public void OnSelectMedium(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            SelectDifficulty(1);
        }
    }

    public void OnSelectHard(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            SelectDifficulty(2);
        }
    }

    public void OnSelectNightmare(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            SelectDifficulty(3);
        }
    }

    public void SelectDifficulty(int diff)
    {
        PersistantVars.pVars.difficulty = diff;
        SceneManager.LoadScene("DebugScenes/DebugRoot");
    }
}
