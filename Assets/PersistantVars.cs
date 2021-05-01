using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Text.RegularExpressions;

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
    public AudioSource ref_Audio;
    public AudioClip[] audioClips;

    public readonly string[] HS_TABLE_KEYS = new string[] { "cursed0", "cursed1", "cursed2", "cursed3", "narrowpc", "widepc", "pentasprint", "pseudosprint", "oversprint", "pentaultra", "pseudoultra", "overultra", "pentathonscore", "pseudothonscore", "overthonscore", "pentathontime", "pseudothontime", "overthontime" };
    public readonly int[] HS_TIME_BASED_TABLES = new int[] { 6, 7, 8, 15, 16, 17 }; //Indices of time-based tables of the above array.

    public const float DEFAULT_VOLUME = 0.2f;
    public Regex HS_REGEX = new Regex("\\d+");

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
        ref_Audio = GetComponent<AudioSource>();

        //Set pseudo-global audio volume
        if (PlayerPrefs.HasKey("volume"))
        {
            ref_Audio.volume = PlayerPrefs.GetFloat("volume");
        }
        else
        {
            ref_Audio.volume = DEFAULT_VOLUME;
        }

        InitHighScores();

        //Don't destroy on load. That's kinda what "persistant" means.
        DontDestroyOnLoad(gameObject);
    }

    //This method creates PlayerPrefs of high score tables if necessary.
    public void InitHighScores()
    {
        for (int i = 0; i < HS_TABLE_KEYS.Length; i++)
        {
            if (!PlayerPrefs.HasKey(HS_TABLE_KEYS[i]))
            {
                if (Array.IndexOf(HS_TIME_BASED_TABLES, i) == -1)
                {
                    PlayerPrefs.SetString(HS_TABLE_KEYS[i], "0/0/0/0/0/0/0/0/0/0/");
                }
                else
                {
                    PlayerPrefs.SetString(HS_TABLE_KEYS[i], "9999999/9999999/9999999/9999999/9999999/9999999/9999999/9999999/9999999/9999999/");
                }
            }
        }
    }

    public int[] HighScoreStringToInts(string highscores)
    {
        MatchCollection matches = HS_REGEX.Matches(highscores);
        int[] ints = new int[10];
        for (int i = 0; i < 10; i++)
        {
            ints[i] = int.Parse(matches[i].ToString());
        }
        return ints;
    }

    public string HighScoreIntsToString(int[] highscores)
    {
        string str = "";
        foreach (int score in highscores)
        {
            str += score.ToString();
            str += "/";
        }
        return str;
    }

    public void PlaySound(SoundEffects sound)
    {
        ref_Audio.PlayOneShot(audioClips[(int)sound]);
    }
}

//Reference list for sound effects. To play a sound, the code says "PlaySound(SoundEffects.[SOME_SOUND_NAME])" and the PlaySound function will convert that to an integer index into an AudioClip[] array.
public enum SoundEffects
{
    ALARM_CLOSE_TO_TOP, //0
    ALARM_NEXT_PIECE_WILL_KILL,
    B2B,
    COMBO1,
    COMBO2,
    COMBO3, //5
    COMBO4,
    COMBO5,
    COMBO6,
    COMBO7,
    COMBO8, //10
    COMBO9,
    COMBO10,
    COMBO11,
    COMBO12,
    COMBO13, //15
    COMBO14,
    COMBO15,
    COMBO16,
    COMBO17,
    COMBO18, //20
    COMBO19,
    COMBO20,
    GAME_END_DEATH,
    GAME_END_EXCELLENT,
    GAME_END_GAMEOVER, //25
    GAME_ERROR,
    GAME_PAUSE,
    GAME_START_GO,
    GAME_START_READY,
    GAME_START_START, //30
    GARBAGE_FLY_IMPACT,
    GARBAGE_FLY_RECEIVE,
    GARBAGE_FLY_SEND,
    GARBAGE_SPAWN,
    HURRY_UP, //35
    LEVEL_UP,
    LINE_CLEAR1,
    LINE_CLEAR2,
    LINE_CLEAR3,
    LINE_CLEAR4, //40
    LINE_CLEAR_COLLAPSE,
    MENU_BACK,
    MENU_CHANGE_SETTING,
    MENU_MOVE,
    MENU_SELECT_OPTION, //45
    MENU_SELECT,
    PACE_GAINED,
    PACE_LOST,
    PERFECT_CLEAR,
    PIECE_HOLD, //50
    PIECE_INITIAL_HOLD,
    PIECE_INITIAL_ROTATION,
    PIECE_INITIAL_SKIP,
    PIECE_LAND,
    PIECE_LOCK_FORCED, //55
    PIECE_HARD_DROP,
    PIECE_LOCK_DELAY_EXPIRED,
    PIECE_MOVE,
    PIECE_ROTATE_T_SLOT,
    PIECE_ROTATE_T_SLOT_MINI, //60
    PIECE_ROTATE,
    PIECE_SKIP,
    PIECE_STEP,
    PIECE_WALL_KICK,
    T_SPIN0, //65
    T_SPIN1,
    T_SPIN2,
    T_SPIN3,
    T_SPIN_MINI0,
    T_SPIN_MINI1, //70
    T_SPIN_MINI2
}