using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/* HighScoresInput class
 * This class has the relatively simple job of handling mouse input on the high score menu. It also initializes the text on said menu.
 */

public class HighScoresInput : MonoBehaviour
{
    public GameObject ref_againButton;
    public Text[] ref_highScoreLists;

    public readonly string[] HS_TABLE_TITLES = new string[] { "Cursed Blocks - Novice", "Cursed Blocks - Acolyte", "Cursed Blocks - Warlock", "Cursed Blocks - Nightmare!", "Narrow Perfection", "Wide Perfection", "Penta Sprint", "Pseudo Sprint", "Overtuned Sprint", "Penta Ultra", "Pseudo Ultra", "Overtuned Ultra", "Pentathon - Score", "Pseudothon - Score", "Overthon - Score", "Pentathon - Time", "Pseudothon - Time", "Overthon - Time"};

    // Start is called before the first frame update
    void Start()
    {
        InitTableText();
    }

    public void InitTableText()
    {
        for (int i = 0; i < HS_TABLE_TITLES.Length; i++)
        {
            string tableText = HS_TABLE_TITLES[i] + "\n";
            string tableValuesString = PlayerPrefs.GetString(PersistantVars.pVars.HS_TABLE_KEYS[i]);
            int[] tableValues = PersistantVars.pVars.HighScoreStringToInts(tableValuesString);
            for (int j = 0; j < 10; j++)
            {
                tableText += (j + 1).ToString() + ". ";
                if (Array.IndexOf(PersistantVars.pVars.HS_TIME_BASED_TABLES, i) == -1) //Score-based table
                {
                    tableText += tableValues[j].ToString("N0") + "\n"; //"N0" formatting inserts commas every three places.
                }
                else //Time-based table
                {
                    TimeSpan ts = new TimeSpan(0, 0, 0, 0, tableValues[j]);
                    string elapsedTime = string.Format("{0:00}:{1:00}.{2:000}", Math.Floor(ts.TotalMinutes), ts.Seconds, ts.Milliseconds);
                    tableText += elapsedTime + "\n";
                }
            }
            ref_highScoreLists[i].text = tableText;
        }
    }

    public void OnHover()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_MOVE);
    }

    public void OnClick()
    {
        PersistantVars.pVars.PlaySound(SoundEffects.MENU_SELECT);
    }
}
