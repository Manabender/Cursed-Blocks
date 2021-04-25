using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

/* Persistant Vars class
 * This class is used to store variables that need to be maintained across scenes. It is not destroyed on load.
 */

public class PersistantVars : MonoBehaviour
{
    public static PersistantVars pVars; //A static reference to self that other objects can easily use to access the variables here.

    public int NUM_CURSES; //The same as in CurseManager.

    public bool[] forcedCurses; //A list of curses (by index into the Curse enum) to treat as always active. Used only for debugging and testing.
    public bool enableCurseGeneration; //Should the Curse Manager automatically generate curses, as in a normal game? Used only for debugging and testing.
    public int difficulty; //Difficulty level to use.
    public ModeGoal goal; //The goal of the game mode selected.
    public BagType bagType; //The bag type of the game mode selected.
    public int width; //The width of the board for the game mode selected.
    public bool overtuned; //Was an overtuned mode selected? This causes all handling settings to be treated as to 0, except SDG, which is treated as max.

    //Awake is like Start, but even earlier.
    void Awake()
    {
        //If this is the first copy of PersistantVars, set up a self-reference that other objects can use. Otherwise, destroy self to ensure there is only one.
        if (pVars == null)
        {
            pVars = this;
        }
        else //if (pVars != this) //The sample code I found has the extra "if" on the "else", but is it really necessary...?
        {
            Destroy(gameObject);
            return;
        }

        //Initialize persistant vars (Note: forcedCurses is initialized in Start)
        NUM_CURSES = Enum.GetNames(typeof(Curse)).Length;
        forcedCurses = new bool[NUM_CURSES];
        enableCurseGeneration = true;
        difficulty = 1;
        goal = ModeGoal.SURVIVE;
        bagType = BagType.CURSED;
        width = 10;
        overtuned = false;

        //Don't destroy on load. That's kinda what "persistant" means.
        DontDestroyOnLoad(gameObject);
    }
}
