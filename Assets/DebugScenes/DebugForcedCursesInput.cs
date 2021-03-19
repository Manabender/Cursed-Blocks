using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/* DebugForcedCursesInput class
 * This class has only one job; listen for input on the forced curses debug menu and update pVars appropriately.
 */

public class DebugForcedCursesInput : MonoBehaviour
{
    public Text[] forceStatusTexts = new Text[27];
    public Text curseManagerStatusText;

    //Is having all this stuff in Update wasteful? Yeah. Do I care? Nah. It's in a debug screen that isn't gonna be accessible by most anyone.
    public void Update()
    {
        for (int i = 0; i < PersistantVars.pVars.NUM_CURSES; i++)
        {
            if (PersistantVars.pVars.forcedCurses[i])
            {
                forceStatusTexts[i].text = "ON";
            }
            else
            {
                forceStatusTexts[i].text = "OFF";
            }
        }
        if (PersistantVars.pVars.enableCurseGeneration)
        {
            curseManagerStatusText.text = "ON";
        }
        else
        {
            curseManagerStatusText.text = "OFF";
        }
    }

    public void ToggleCurse(int id)
    {
        PersistantVars.pVars.forcedCurses[id] = !PersistantVars.pVars.forcedCurses[id];
    }

    //I'm not a fan of needing to do this...
    public void OnPenta(InputAction.CallbackContext context) { if (context.phase == InputActionPhase.Started) { ToggleCurse(0); } }
    public void OnH(InputAction.CallbackContext context) { if (context.phase == InputActionPhase.Started) { ToggleCurse(1); } }
    public void OnDrought(InputAction.CallbackContext context) { if (context.phase == InputActionPhase.Started) { ToggleCurse(2); } }
    public void OnFlood(InputAction.CallbackContext context) { if (context.phase == InputActionPhase.Started) { ToggleCurse(3); } }
    public void OnPseudo(InputAction.CallbackContext context) { if (context.phase == InputActionPhase.Started) { ToggleCurse(4); } }
    public void OnTwin(InputAction.CallbackContext context) { if (context.phase == InputActionPhase.Started) { ToggleCurse(5); } }
    public void OnBig(InputAction.CallbackContext context) { if (context.phase == InputActionPhase.Started) { ToggleCurse(6); } }
    public void OnBigO(InputAction.CallbackContext context) { if (context.phase == InputActionPhase.Started) { ToggleCurse(7); } }
    public void OnBigH(InputAction.CallbackContext context) { if (context.phase == InputActionPhase.Started) { ToggleCurse(8); } }
    public void OnCheese(InputAction.CallbackContext context) { if (context.phase == InputActionPhase.Started) { ToggleCurse(9); } }
    public void OnClean(InputAction.CallbackContext context) { if (context.phase == InputActionPhase.Started) { ToggleCurse(10); } }
    public void OnSupercheese(InputAction.CallbackContext context) { if (context.phase == InputActionPhase.Started) { ToggleCurse(11); } }
    public void OnHypercheese(InputAction.CallbackContext context) { if (context.phase == InputActionPhase.Started) { ToggleCurse(12); } }
    public void OnAntiskim(InputAction.CallbackContext context) { if (context.phase == InputActionPhase.Started) { ToggleCurse(13); } }
    public void OnSpikedHold(InputAction.CallbackContext context) { if (context.phase == InputActionPhase.Started) { ToggleCurse(14); } }
    public void OnDisguise(InputAction.CallbackContext context) { if (context.phase == InputActionPhase.Started) { ToggleCurse(15); } }
    public void OnHard(InputAction.CallbackContext context) { if (context.phase == InputActionPhase.Started) { ToggleCurse(16); } }
    public void OnFloating(InputAction.CallbackContext context) { if (context.phase == InputActionPhase.Started) { ToggleCurse(17); } }
    public void OnFog(InputAction.CallbackContext context) { if (context.phase == InputActionPhase.Started) { ToggleCurse(18); } }
    public void OnClumped(InputAction.CallbackContext context) { if (context.phase == InputActionPhase.Started) { ToggleCurse(19); } }
    public void OnSlowHold(InputAction.CallbackContext context) { if (context.phase == InputActionPhase.Started) { ToggleCurse(20); } }
    public void OnMirror(InputAction.CallbackContext context) { if (context.phase == InputActionPhase.Started) { ToggleCurse(21); } }
    public void OnMist(InputAction.CallbackContext context) { if (context.phase == InputActionPhase.Started) { ToggleCurse(22); } }
    public void OnEphemerealHold(InputAction.CallbackContext context) { if (context.phase == InputActionPhase.Started) { ToggleCurse(23); } }
    public void OnSlippery(InputAction.CallbackContext context) { if (context.phase == InputActionPhase.Started) { ToggleCurse(24); } }
    public void OnGravity(InputAction.CallbackContext context) { if (context.phase == InputActionPhase.Started) { ToggleCurse(25); } }
    public void OnAntigrav(InputAction.CallbackContext context) { if (context.phase == InputActionPhase.Started) { ToggleCurse(26); } }

    public void OnAllOff(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            for (int i = 0; i < PersistantVars.pVars.NUM_CURSES; i++)
            {
                PersistantVars.pVars.forcedCurses[i] = false;
            }
        }
    }

    public void OnCurseManager(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            PersistantVars.pVars.enableCurseGeneration = !PersistantVars.pVars.enableCurseGeneration;
        }
    }

    public void OnReturnToDebugRoot(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            SceneManager.LoadScene("DebugScenes/DebugRoot");
        }
    }

}
