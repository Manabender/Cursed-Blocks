using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/* ExtraModesInput class
 * This class has the simple job of handling mouse input on the extra modes menu.
 */

public class ExtraModesInput : MonoBehaviour
{
    public Text tooltipText;
    public GameObject confirmNightmarePanel;

    public const string PENTA_SPRINT_TOOLTIP = "Using only pentominoes, clear 40 lines as fast as possible.";
    public const string PENTA_ULTRA_TOOLTIP = "Using only pentominoes, score as high as possible in two minutes.";
    public const string PENTATHON_TOOLTIP = "Using only pentominoes, score as high as possible under Marathon rules.";
    public const string PSEUDO_SPRINT_TOOLTIP = "Using predominantly pseudos, clear 40 lines as fast as possible.";
    public const string PSEUDO_ULTRA_TOOLTIP = "Using predominantly pseudos, score as high as possible in two minutes.";
    public const string PSEUDOTHON_TOOLTIP = "Using predominantly pseudos, score high under Marathon rules.";
    public const string OVERSPRINT_TOOLTIP = "Handling settings (DAS, etc) are locked at MAX! Clear 40 lines.";
    public const string OVERULTRA_TOOLTIP = "Handling settings (DAS, etc) are locked at MAX! Score as high as possible.";
    public const string OVERTHON_TOOLTIP = "Handling settings (DAS, etc) are locked at MAX! Survive for 150 lines.";
    public const string WIDE_PC_TOOLTIP = "Make consecutive ALL CLEARS with normal pieces, on an 11-wide board.";
    public const string NARROW_PC_TOOLTIP = "Make consecutive ALL CLEARS with normal pieces, on a 9-wide board.";
    public const string NIGHTMARE_TOOLTIP = "All curses. All the time. You will die. Very quickly. Trust me, don't bother.";
    public const string RHYTHM_TOOLTIP = "Play to the beat! Pieces lock in at regular intervals. Score and/or survive.";
    public const string CUSTOM_TOOLTIP = "Play as you please. Customize various game settings to your liking.";

    public void HoverPentaSprint()
    {
        tooltipText.text = PENTA_SPRINT_TOOLTIP;
    }

    public void HoverPentaUltra()
    {
        tooltipText.text = PENTA_ULTRA_TOOLTIP;
    }

    public void HoverPentathon()
    {
        tooltipText.text = PENTATHON_TOOLTIP;
    }

    public void HoverPseudoSprint()
    {
        tooltipText.text = PSEUDO_SPRINT_TOOLTIP;
    }

    public void HoverPseudoUltra()
    {
        tooltipText.text = PSEUDO_ULTRA_TOOLTIP;
    }

    public void HoverPseudothon()
    {
        tooltipText.text = PSEUDOTHON_TOOLTIP;
    }

    public void HoverOverSprint()
    {
        tooltipText.text = OVERSPRINT_TOOLTIP;
    }

    public void HoverOverUltra()
    {
        tooltipText.text = OVERULTRA_TOOLTIP;
    }

    public void HoverOverthon()
    {
        tooltipText.text = OVERTHON_TOOLTIP;
    }

    public void HoverWidePC()
    {
        tooltipText.text = WIDE_PC_TOOLTIP;
    }

    public void HoverNarrowPC()
    {
        tooltipText.text = NARROW_PC_TOOLTIP;
    }

    public void HoverNightmare()
    {
        tooltipText.text = NIGHTMARE_TOOLTIP;
    }

    public void HoverRhythm()
    {
        tooltipText.text = RHYTHM_TOOLTIP;
    }

    public void HoverCustom()
    {
        tooltipText.text = CUSTOM_TOOLTIP;
    }

    public void Unhover()
    {
        tooltipText.text = "";
    }

    public void OnPentaSprint()
    {
        //TODO
    }

    public void OnPentaUltra()
    {
        //TODO
    }

    public void OnPentathon()
    {
        //TODO
    }

    public void OnPseudoSprint()
    {
        //TODO
    }

    public void OnPseudoUltra()
    {
        //TODO
    }

    public void OnPseudothon()
    {
        //TODO
    }

    public void OnOverSprint()
    {
        //TODO
    }

    public void OnOverUltra()
    {
        //TODO
    }

    public void OnOverthon()
    {
        //TODO
    }

    public void OnWidePC()
    {
        //TODO
    }

    public void OnNarrowPC()
    {
        //TODO
    }

    public void OnNightmare()
    {
        confirmNightmarePanel.SetActive(true);
    }

    public void OnRhythm()
    {
        //TODO
    }

    public void OnCustom()
    {
        //TODO
    }

    public void OnYesNightmare()
    {
        PersistantVars.pVars.difficulty = 3;
        SceneManager.LoadScene("MainGameScene"); //Start game.
    }

    public void OnNoNightmare()
    {
        confirmNightmarePanel.SetActive(false);
    }
}
