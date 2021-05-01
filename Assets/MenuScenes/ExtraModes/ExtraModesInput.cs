using System;
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
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_MOVE);
        tooltipText.text = PENTA_SPRINT_TOOLTIP;
    }

    public void HoverPentaUltra()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_MOVE);
        tooltipText.text = PENTA_ULTRA_TOOLTIP;
    }

    public void HoverPentathon()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_MOVE);
        tooltipText.text = PENTATHON_TOOLTIP;
    }

    public void HoverPseudoSprint()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_MOVE);
        tooltipText.text = PSEUDO_SPRINT_TOOLTIP;
    }

    public void HoverPseudoUltra()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_MOVE);
        tooltipText.text = PSEUDO_ULTRA_TOOLTIP;
    }

    public void HoverPseudothon()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_MOVE);
        tooltipText.text = PSEUDOTHON_TOOLTIP;
    }

    public void HoverOverSprint()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_MOVE);
        tooltipText.text = OVERSPRINT_TOOLTIP;
    }

    public void HoverOverUltra()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_MOVE);
        tooltipText.text = OVERULTRA_TOOLTIP;
    }

    public void HoverOverthon()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_MOVE);
        tooltipText.text = OVERTHON_TOOLTIP;
    }

    public void HoverWidePC()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_MOVE);
        tooltipText.text = WIDE_PC_TOOLTIP;
    }

    public void HoverNarrowPC()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_MOVE);
        tooltipText.text = NARROW_PC_TOOLTIP;
    }

    public void HoverNightmare()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_MOVE);
        tooltipText.text = NIGHTMARE_TOOLTIP;
    }

    public void HoverRhythm()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_MOVE);
        tooltipText.text = RHYTHM_TOOLTIP;
    }

    public void HoverCustom()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_MOVE);
        tooltipText.text = CUSTOM_TOOLTIP;
    }

    public void Unhover()
    {
        tooltipText.text = "";
    }

    public void StartCustomMode(ModeGoal goal, BagType bagtype, int width, bool overtuned)
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_SELECT);
        PersistantVars.pVars.goal = goal;
        PersistantVars.pVars.bagType = bagtype;
        PersistantVars.pVars.width = width;
        PersistantVars.pVars.overtuned = overtuned;
        SceneManager.LoadScene("MainGameScene"); //Start game.
    }

    public void OnPentaSprint()
    {
        StartCustomMode(ModeGoal.SPRINT, BagType.PENTA, 12, false);
    }

    public void OnPentaUltra()
    {
        StartCustomMode(ModeGoal.ULTRA, BagType.PENTA, 12, false);
    }

    public void OnPentathon()
    {
        StartCustomMode(ModeGoal.MARATHON, BagType.PENTA, 12, false);
    }

    public void OnPseudoSprint()
    {
        StartCustomMode(ModeGoal.SPRINT, BagType.PSEUDO, 10, false);
    }

    public void OnPseudoUltra()
    {
        StartCustomMode(ModeGoal.ULTRA, BagType.PSEUDO, 10, false);
    }

    public void OnPseudothon()
    {
        StartCustomMode(ModeGoal.MARATHON, BagType.PSEUDO, 10, false);
    }

    public void OnOverSprint()
    {
        StartCustomMode(ModeGoal.SPRINT, BagType.TETRA, 10, true);
    }

    public void OnOverUltra()
    {
        StartCustomMode(ModeGoal.ULTRA, BagType.TETRA, 10, true);
    }

    public void OnOverthon()
    {
        StartCustomMode(ModeGoal.MARATHON, BagType.TETRA, 10, true);
    }

    public void OnWidePC()
    {
        StartCustomMode(ModeGoal.ALLCLEAR, BagType.TETRA, 11, false);
    }

    public void OnNarrowPC()
    {
        StartCustomMode(ModeGoal.ALLCLEAR, BagType.TETRA, 9, false);
    }

    public void OnNightmare()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_SELECT);
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
        StartCustomMode(ModeGoal.SURVIVE, BagType.CURSED, 10, false);
    }

    public void OnNoNightmare()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_SELECT);
        confirmNightmarePanel.SetActive(false);
    }
}
