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
        SceneManager.LoadScene("MenuScenes/Information/InfoGeneral");
    }

    public void KicktablesClick()
    {
        confirmKicktablesPanel.SetActive(true);
    }

    public void CursesClick()
    {
        confirmCursesPanel.SetActive(true);
    }

    public void KicktablesYes()
    {
        SceneManager.LoadScene("MenuScenes/Information/InfoKicktables");
    }

    public void KicktablesNo()
    {
        confirmKicktablesPanel.SetActive(false);
    }

    public void CursesYes()
    {
        SceneManager.LoadScene("MenuScenes/Information/InfoCurses");
    }

    public void CursesNo()
    {
        confirmCursesPanel.SetActive(false);
    }

    public void GeneralHover()
    {
        tooltipText.text = GENERAL_TEXT;
    }

    public void KicktablesHover()
    {
        tooltipText.text = KICKTABLES_TEXT;
    }

    public void CursesHover()
    {
        tooltipText.text = CURSES_TEXT;
    }

    public void Unhover()
    {
        tooltipText.text = "";
    }
}
