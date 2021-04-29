using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/* InformationInput class
 * This class has the relatively simple job of handling mouse input on the root information menu. It updates the "tooltip" in response to hovering over a button, and loads the appropriate scene when a button is clicked.
 */

public class InformationInput : MonoBehaviour
{
    public Text tooltipText;
    public GameObject confirmKicktablesPanel;
    public GameObject confirmCursesPanel;

    public const string GENERAL_TEXT = "Basic information specific to Cursed Blocks.";
    public const string KICKTABLES_TEXT = "Info on how pieces rotate, especially those not seen in typical games.";
    public const string CURSES_TEXT = "Detailed info on each curse.";

    public void GeneralClick()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_SELECT);
        SceneManager.LoadScene("MenuScenes/Information/InfoGeneral");
    }

    public void KicktablesClick()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_SELECT);
        confirmKicktablesPanel.SetActive(true);
    }

    public void CursesClick()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_SELECT);
        confirmCursesPanel.SetActive(true);
    }

    public void KicktablesYes()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_SELECT);
        SceneManager.LoadScene("MenuScenes/Information/InfoKicktables");
    }

    public void KicktablesNo()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_SELECT);
        confirmKicktablesPanel.SetActive(false);
    }

    public void CursesYes()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_SELECT);
        SceneManager.LoadScene("MenuScenes/Information/InfoCurses");
    }

    public void CursesNo()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_SELECT);
        confirmCursesPanel.SetActive(false);
    }

    public void GeneralHover()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_MOVE);
        tooltipText.text = GENERAL_TEXT;
    }

    public void KicktablesHover()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_MOVE);
        tooltipText.text = KICKTABLES_TEXT;
    }

    public void CursesHover()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_MOVE);
        tooltipText.text = CURSES_TEXT;
    }

    public void Unhover()
    {
        tooltipText.text = "";
    }

    public void OtherHover()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_MOVE);
    }
}
