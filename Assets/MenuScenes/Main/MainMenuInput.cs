using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/* MainMenuInput class
 * This class has the relatively simple job of handling mouse input on the main menu. It updates the "tooltip" in response to hovering over a button, and loads the appropriate scene when a button is clicked.
 */

public class MainMenuInput : MonoBehaviour
{
    public Text tooltipText;
    public GameObject confirmQuitPanel;

    public const string CURSED_BLOCKS_TOOLTIP = "The main game mode. Stack to survive against ever-increasing curses.";
    public const string EXTRA_MODES_TOOLTIP = "A collection of additional game modes, each cursed in their own way.";
    public const string SETTINGS_TOOLTIP = "Tweak the controls and whatnot to your liking.";
    public const string HIGH_SCORES_TOOLTIP = "Check your best records in each mode.";
    public const string INFORMATION_TOOLTIP = "Review game information, from the most basic to the incredibly-detailed.";
    public const string QUIT_TOOLTIP = "Close the game.";

    public void HoverCursedBlocks(BaseEventData _)
    {
        tooltipText.text = CURSED_BLOCKS_TOOLTIP;
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_MOVE);
    }

    public void HoverExtraModes(BaseEventData _)
    {
        tooltipText.text = EXTRA_MODES_TOOLTIP;
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_MOVE);
    }

    public void HoverSettings(BaseEventData _)
    {
        tooltipText.text = SETTINGS_TOOLTIP;
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_MOVE);
    }

    public void HoverHighScores(BaseEventData _)
    {
        tooltipText.text = HIGH_SCORES_TOOLTIP;
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_MOVE);
    }

    public void HoverInformation(BaseEventData _)
    {
        tooltipText.text = INFORMATION_TOOLTIP;
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_MOVE);
    }

    public void HoverQuit(BaseEventData _)
    {
        tooltipText.text = QUIT_TOOLTIP;
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_MOVE);
    }

    public void Unhover(BaseEventData _)
    {
        tooltipText.text = "";
    }

    public void OnCursedBlocks()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_SELECT);
        SceneManager.LoadScene("MenuScenes/DifficultySelect/DifficultySelect");
    }

    public void OnExtraModes()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_SELECT);
        SceneManager.LoadScene("MenuScenes/ExtraModes/ExtraModes");
    }

    public void OnSettings()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_SELECT);
        SceneManager.LoadScene("MenuScenes/Settings/Settings");
    }

    public void OnHighScores()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_SELECT);
        SceneManager.LoadScene("MenuScenes/HighScores/HighScores");
    }

    public void OnInformation()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_SELECT);
        SceneManager.LoadScene("MenuScenes/Information/Information");
    }

    public void OnQuit()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_SELECT);
        confirmQuitPanel.SetActive(true);
    }

    public void OnConfirmQuit()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_SELECT);
        Application.Quit();
    }

    public void OnConfirmNoQuit()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_SELECT);
        confirmQuitPanel.SetActive(false);
    }
}
