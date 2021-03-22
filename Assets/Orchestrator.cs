using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/* Orchestrator class
 * This class is intended to serve these main purposes:
 * 1. It handles all input.
 * 2. It serves as a communications middleman for any objects that need to talk to other objects.
 *  This way, each object only needs to maintain a reference to the orchestrator, and it can communicate with all other objects through the orchestrator.
 * 3. It maintains global variables that don't really fit anywhere else.
 */

/* A note on inputs... (written pretty early in development)
 * Unity doesn't seem to have the capability to tie into the processor interrupt that occurs on a keyboard input. This...is sort of a shame.
 * High-level players tune their handling (DAS, primarily) down to the millisecond, and I want to offer the same level of precision.
 * To accomplish this, I've set the Input System to update with FixedUpdate, and changed the FixedUpdate interval to a mere 2ms (where it is 20ms by default).
 * I really hope Unity can handle that speed without having a hernia. Time and testing will tell.
 * Unity CAN read the precise time down to the microsecond that an input was made, and this COULD be used for sub-frame precision, and indeed, Tetr.io DOES do that.
 * That is my backup plan. If it turns out that Unity cannot handle extremely rapid FixedUpdates, I'll switch to that method.
 * But, if this simple solution exists, I will take it.
 */

public class Orchestrator : MonoBehaviour
{
    public Sprite[] minoSprites; //Mino textures to be displayed, placed in a handy indexable list.
    //Object and component references, defined in Unity editor
    public Board ref_Board;
    public ActivePiece ref_ActivePiece;
    public Queue ref_Queue;
    public Canvas ref_Canvas;
    public HoldPiece ref_HoldPiece;
    public IncomingPiece ref_IncomingPiece;
    public CurseManager ref_CurseManager;
    public PlayerInput ref_PlayerInput;
    public GameObject ref_BoardContainer;
    public GameObject ref_PauseTextObject;
    public Text ref_PauseText;
    public Text ref_ScoreDisplay;
    public Text ref_ComboText;
    public Text ref_ClearTypeText;
    public Text ref_FullClearText;
    public Text ref_B2BText;
    public Text ref_GameoverText;
    public Text ref_AntiskimText;
    public Text ref_SpikedHoldText;
    public Text ref_AntigravText;
    public Text ref_SlowHoldText;
    public Text ref_EphemerealHoldText;
    public Text ref_SlipperyText;
    public Text ref_ClumpedText;
    public Text ref_ScoreInfoText;
    //Various """global""" variables
    public float gravity = 1.0f; //Gravity is the rate at which the active piece falls, measured in minoes per second. "Instant" ("20G") gravity is at least 11000 under default board height.
    public float partialFallProgress = 0.0f; //This is what fraction of a mino the active piece has already fallen. Once this exceeds 1, the active piece falls by 1 and this decrements by 1.
    public long score; //The player's score, of course.
    public int combo; //The number (minus 1, because convention) of consecutive pieces which have cleared a line.
    public int backToBack; //The number (minus 1) of consecutive "Power Clears". Clearing at least one line with a T-spin (mini or full), or clearing at least four lines with a single piece, will each increment B2B. Clearing one, two, or three lines with a single piece without a T-spin resets B2B to -1.
    public int bagNumber; //How many bags have we been through? Which one are we in the middle of?
    public int scoreMultiplier; //All score gain is multiplied by this. It goes up every bag and is also increased for each active curse. (Note that harddrop and softdrop is multiplied instead by a constant.)
    public bool gameover; //Is the game in a finished state?
    public bool paused; //Is the game paused?
    //Score tuning variables
    public int softDropScoreGain; //The number of points gained for each space the active piece falls while soft-dropped.
    public int hardDropScoreGain; //The number of points gained for each space the active piece falls while hard-dropped.
    public int[] lineClearScoreGain; //The number of points gained for line clears. Index 0 is a throwaway, index 1 is for a single, index 2 is for a double, etc.
    public int[] tSpinScoreGain; //The number of points gained for tspins. Index 0 is for 0 line tspin, 1 for tspin single, 2 for tspin double, etc.
    public int[] tSpinMiniScoreGain; //The number of points gained for tspin minis. Index 0 is for 0 line tspin mini, index 1 is for a tspin mini single. More than that is a full tspin by my definition.
    public int comboScoreGain; //The number of extra points gained for combos, per combo.
    public int allClearScoreGain; //The number of points awarded for clearing the entire board of all minoes.
    public int colorClearScoreGain; //The number of points awarded for clearing the board of all non-garbage minoes.
    //Text timers; clear certain text after some time.
    public float clearTypeTextTimer;
    public float fullClearTextTimer;
    //Input-handling variables
    public int dasLeftTimer = -1; //Time, in game frames, until das kicks in.
    public int dasRightTimer = -1;
    public bool softDropHeld = false;
    public bool moveLeftHeld = false;
    public bool moveRightHeld = false;
    public bool moveLeftMostRecent = false; //Was LEFT the most recent move key pressed? (moveRightMostRecent is implied to be the opposite.)
    public float softDropGravity; //Gravity used when soft-drop is held.
    public int das; //DAS, short for Delayed Auto-Shift, is the time (in game frames, which are 2ms each) a left/right key must be pressed before Auto-Shift kicks in.
    public int arr; //ARR, short for Auto-Repeat Rate, is the time (in game frames) between Auto-Shifts, where the active piece moves repeatedly while a left/right key is held.
    public int rotateDCD; //DCD, short for DAS Cut Delay, resets the active DAS timer(s) to the DCD value when another input is made. Separate DCDs exist for rotation, hold, and harddrop.
    public int harddropDCD;
    public int holdDCD;
    public bool resetDASOnDirChange; //User option which, if enabled, resets one direction's DAS timer if the other direction is pressed or released.

    public const int BASE_SCORE_MULTIPLIER = 20;
    public const int DROP_SCORE_MULTIPLIER = 21;

    public readonly string[] CLEAR_NAMES = new string[] { "", "Single", "Double", "Triple", "Quadruple", "Pentuple", "Sextuple", "Septuple", "Octuple", "WHAT?", "HOW?", "CHEATER?", "HACKER?" };
    public const string T_SPIN_MINI_TEXT = "T-spin mini";
    public const string T_SPIN_TEXT = "T-spin";
    public const string ALL_CLEAR_TEXT = "---ALL CLEAR!!!---";
    public const string COLOR_CLEAR_TEXT = "---COLOR CLEAR!!---";
    public const string COMBO_TEXT = "Combo: ";
    public const string BACK_TO_BACK_TEXT = "Back-to-Back x";
    public const string GAME_OVER_TEXT = "GAME OVER\nPRESS {0} TO RESET";
    public const string PAUSED_TEXT = "PAUSED\nPRESS {0} TO RESUME";
    public const string ANTISKIM_TEXT = "Avoid Singles";
    public const string ANTIGRAV_TEXT = "Antigrav";
    public const string SLOW_HOLD_TEXT = "Slow hold";
    public const string SLIPPERY_TEXT = "Slippery";
    public const string CLUMPED_TEXT = "Clumped";
    public const float CLEAR_TYPE_TIMER_LENGTH = 2.0f; //Time (in seconds) for clear type text to stay on screen.
    public const float FULL_CLEAR_TIMER_LENGTH = 3.0f; //Time (in seconds) for full clear text to stay on screen.

    // Start is called before the first frame update
    void Start()
    {
        //Load mino textures.
        minoSprites = Resources.LoadAll<Sprite>("MinoTextures");
        //Initialize game variables
        softDropScoreGain = 1; //TODO: Set by mode
        hardDropScoreGain = 2; //TODO: Set by mode
        lineClearScoreGain = new int[] { 0, 100, 300, 500, 800, 1200, 1800, 2700, 4000, 6000, 9000, 15000, 25000 }; //A few extra entries just to protect against out-of-bounds errors. Hopefully. Only up to index 8 should be needed.
        tSpinScoreGain = new int[] { 400, 800, 1200, 1600, 2400, 3600, 5400, 9000, 15000, 25000 }; //Only up to index 6 should be necessary, but see above.
        tSpinMiniScoreGain = new int[] { 100, 200, 1200, 1600, 2400, 3600, 5400, 9000, 15000, 25000 }; //Again, see above. Only up to index 1 should ever be used.
        comboScoreGain = 50;
        allClearScoreGain = 3000;
        colorClearScoreGain = 1000;       
        //User handling settings
        //Does the PlayerPrefs exist? If not, create it.
        if (!PlayerPrefs.HasKey("DAS"))
        {
            CreatePlayerPrefs();
        }
        softDropGravity = PlayerPrefs.GetFloat("SDG");
        das = PlayerPrefs.GetInt("DAS");
        arr = PlayerPrefs.GetInt("ARR");
        rotateDCD = PlayerPrefs.GetInt("rotateDCD");
        harddropDCD = PlayerPrefs.GetInt("harddropDCD");
        holdDCD = PlayerPrefs.GetInt("holdDCD");
        resetDASOnDirChange = PlayerPrefs.GetInt("interruptDAS") == 1; //A somewhat odd way of typcasting an int to a bool...
        //User keybind settings
        InputActionMap map = ref_PlayerInput.actions.FindActionMap("Piece");
        map.FindAction("Shift left").ChangeBinding(0).WithPath(PlayerPrefs.GetString("keyLeft"));
        map.FindAction("Shift right").ChangeBinding(0).WithPath(PlayerPrefs.GetString("keyRight"));
        map.FindAction("Soft drop").ChangeBinding(0).WithPath(PlayerPrefs.GetString("keySoftDrop"));
        map.FindAction("Hard drop").ChangeBinding(0).WithPath(PlayerPrefs.GetString("keyHardDrop"));
        map.FindAction("Rotate CW").ChangeBinding(0).WithPath(PlayerPrefs.GetString("keyCW"));
        map.FindAction("Rotate CCW").ChangeBinding(0).WithPath(PlayerPrefs.GetString("keyCCW"));
        map.FindAction("Rotate 180").ChangeBinding(0).WithPath(PlayerPrefs.GetString("key180"));
        map.FindAction("Hold").ChangeBinding(0).WithPath(PlayerPrefs.GetString("keyHold"));
        map.FindAction("Pause").ChangeBinding(0).WithPath(PlayerPrefs.GetString("keyPause"));
        map.FindAction("Reset").ChangeBinding(0).WithPath(PlayerPrefs.GetString("keyReset"));
        //Initialize object variables
        ResetObject();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateText();
    }

    //FixedUpdate is a method called by Unity at a standard interval. As of writing this, that interval is every 2ms. Yes only two. A tiny interval is required for precision controls.
    //I'm using this method as a "framerate" of sorts for all game functionality. Like older games, ALL game events in this game only happen on frame updates. Unity, of course,
    // can detach "game frames" from "visual frames". Game frames run nominally at 500 FPS. Visual frames run at whatever Unity decides to run them at.
    private void FixedUpdate()
    {
        if (!paused && ref_Queue.aaaaa == 1) //If the game hasn't started yet, these methods reference things that haven't yet been properly initialized. So skip them until the game starts. TODO: Reference gamestarted variable instead of aaaaa.
        {
            HandleDAS();
            HandleGravity();
        }
    }

    //ResetObject is a method that appears in several object scripts that resets it to start-of-game values.
    public void ResetObject()
    {
        //Game variables
        score = 0;
        combo = -1;
        backToBack = -1;
        bagNumber = 1;
        scoreMultiplier = 21;
        gameover = false;
        paused = false;
        //Clear text fields
        ref_ComboText.text = "";
        ref_ClearTypeText.text = "";
        ref_FullClearText.text = "";
        ref_B2BText.text = "";
        ref_GameoverText.text = "";
        ref_AntiskimText.text = "";
        ref_SpikedHoldText.text = "";
        clearTypeTextTimer = 0;
        fullClearTextTimer = 0;
        //Input-handling variables NOTE: Problems might be caused if ResetObject is called while game buttons are pressed.
        dasLeftTimer = -1;
        dasRightTimer = -1;
        softDropHeld = false;
        moveLeftHeld = false;
        moveRightHeld = false;
        moveLeftMostRecent = false;
    }

    //This method creates a new PlayerPrefs to use with default settings.
    public void CreatePlayerPrefs()
    {
        PlayerPrefs.SetInt("DAS", 90);
        PlayerPrefs.SetInt("ARR", 15);
        PlayerPrefs.SetFloat("SDG", 20.0f);
        PlayerPrefs.SetInt("rotateDCD", 0);
        PlayerPrefs.SetInt("harddropDCD", 0);
        PlayerPrefs.SetInt("holdDCD", 0);
        PlayerPrefs.SetInt("interruptDAS", 0);
        InputActionMap map = ref_PlayerInput.actions.FindActionMap("Piece");
        PlayerPrefs.SetString("keyLeft", map.FindAction("Shift left").bindings[0].path);
        PlayerPrefs.SetString("keyRight", map.FindAction("Shift right").bindings[0].path);
        PlayerPrefs.SetString("keySoftDrop", map.FindAction("Soft drop").bindings[0].path);
        PlayerPrefs.SetString("keyHardDrop", map.FindAction("Hard drop").bindings[0].path);
        PlayerPrefs.SetString("keyCW", map.FindAction("Rotate CW").bindings[0].path);
        PlayerPrefs.SetString("keyCCW", map.FindAction("Rotate CCW").bindings[0].path);
        PlayerPrefs.SetString("key180", map.FindAction("Rotate 180").bindings[0].path);
        PlayerPrefs.SetString("keyHold", map.FindAction("Hold").bindings[0].path);
        PlayerPrefs.SetString("keyPause", map.FindAction("Pause").bindings[0].path);
        PlayerPrefs.SetString("keyReset", map.FindAction("Reset").bindings[0].path);
        PlayerPrefs.Save();
    }

    //This method handles the Auto-Repeat movement of the piece. Pressing a move left/right key moves the piece, and holding it continues to move it.
    //The move-on-press is handled by the input methods, while the move-on-hold is handled here.
    private void HandleDAS()
    {
        if (gameover)
        {
            return;
        }
        if (moveLeftHeld) //If left is held, process the left timer.
        {
            dasLeftTimer--;
            if (dasLeftTimer <= 0) //If the timer has expired, reset it, and possibly move the piece.
            {
                dasLeftTimer = 0;
                if (moveLeftMostRecent) //Only move the piece if move left was most recent.
                {
                    dasLeftTimer = arr;
                    if (arr == 0) //Move the piece as far as it can if ARR is 0. Otherwise, just move it 1.
                    {
                        while (ref_ActivePiece.CanPieceMove(Vector2Int.left))
                        {
                            ref_ActivePiece.MovePiece(Vector2Int.left);
                        }
                    }
                    else
                    {
                        if (ref_ActivePiece.CanPieceMove(Vector2Int.left)) //Move the piece if possible.
                        {
                            ref_ActivePiece.MovePiece(Vector2Int.left);
                        }
                    }
                }
            }
        }
        if (moveRightHeld)
        {
            dasRightTimer--;
            if (dasRightTimer <= 0) //If the timer has expired, reset it, and possibly move the piece.
            {
                dasRightTimer = 0;
                if (!moveLeftMostRecent) //Only move the piece if move right was most recent.
                {
                    dasRightTimer = arr;
                    if (arr == 0) //Move the piece as far as it can if ARR is 0. Otherwise, just move it 1.
                    {
                        while (ref_ActivePiece.CanPieceMove(Vector2Int.right))
                        {
                            ref_ActivePiece.MovePiece(Vector2Int.right);
                        }
                    }
                    else
                    {
                        if (ref_ActivePiece.CanPieceMove(Vector2Int.right)) //Move the piece if possible.
                        {
                            ref_ActivePiece.MovePiece(Vector2Int.right);
                        }
                    }
                }
            }
        }
    }

    //This method processes the force of gravity on the piece, pulling it towards the bottom of the board. It is called once every game frame (2ms).
    private void HandleGravity()
    {
        if (gameover)
        {
            return;
        }
        /*if (ref_CurseManager.IsCurseActive(Curse.GRAVITY))
        {
            ref_ActivePiece.locking = true;
        }*/
        if (!ref_ActivePiece.locking) //If the piece isn't locking, make it fall according to gravity.
        {
            //Determine how far to fall.
            float distanceToFall;
            if (ref_ActivePiece.texture == Piece.FLOATING_TEXTURE_ID) //Side-effect of floating curse pieces: They do not fall, neither under gravity nor softdrop. They can only be harddropped.
            {
                distanceToFall = 0;
            }
            else if (ref_CurseManager.IsCurseActive(Curse.GRAVITY)) //Handle "20g" gravity curse; if active, piece falls as far as it can.
            {
                distanceToFall = 999999999;
            }
            else if (softDropHeld && softDropGravity > gravity)
            {
                distanceToFall = softDropGravity * Time.fixedDeltaTime;
            }
            else
            {
                distanceToFall = gravity * Time.fixedDeltaTime;
            }
            //Update partialFallProgress accordingly.
            partialFallProgress += distanceToFall;
            //Fall.
            while (partialFallProgress >= 1.0 && ref_ActivePiece.CanPieceMove(Vector2Int.down))
            {
                ref_ActivePiece.MovePiece(Vector2Int.down);
                partialFallProgress -= 1.0f;
                if (softDropHeld)
                {
                    score += softDropScoreGain * DROP_SCORE_MULTIPLIER;
                }
            }
        }
        else //If the piece is locking, handle that.
        {
            partialFallProgress = 0.0f;
            ref_ActivePiece.HandleLockDelay();
            /*if (ref_CurseManager.IsCurseActive(Curse.GRAVITY))
            {
                while (ref_ActivePiece.CanPieceMove(Vector2Int.down))
                {
                    ref_ActivePiece.MovePiece(Vector2Int.down);
                }
            }*/
        }
    }

    //This method keeps the text fields up to date. It is called on every frame via Update().
    public void UpdateText()
    {
        //Handle score display.
        ref_ScoreDisplay.text = score.ToString("N0"); //The N0 argument formats the number to have commas every three digits.
        //Handle combo display.
        if (combo <= 0)
        {
            ref_ComboText.text = "";
        }
        else
        {
            ref_ComboText.text = COMBO_TEXT + combo;
        }
        //Handle clear type display.
        clearTypeTextTimer -= Time.deltaTime;
        if (clearTypeTextTimer < 0)
        {
            ref_ClearTypeText.text = "";
        }
        //Handle full clear display.
        fullClearTextTimer -= Time.deltaTime;
        if (fullClearTextTimer < 0)
        {
            ref_FullClearText.text = "";
        }
        if (backToBack == -1)
        {
            ref_B2BText.text = "";
        }
        else
        {
            ref_B2BText.text = BACK_TO_BACK_TEXT + backToBack;
        }
    }

    //This method sets the text and timers for Clear Type and Full Clear text. It is called from ActivePiece.LockPiece().
    public void SetClearTypeAndFullClearText(int clearValue, int fullClearType)
    {
        if (clearValue != 0) //Don't update or reset timer if nothing was cleared and no spins were done.
        {
            clearTypeTextTimer = CLEAR_TYPE_TIMER_LENGTH;
            string text = "";
            //Add tspin text if necessary
            if (clearValue / 100 == 1)
            {
                text = T_SPIN_MINI_TEXT;
            }
            else if (clearValue / 100 == 2)
            {
                text = T_SPIN_TEXT;
            }
            //Add text for number of lines cleared
            int linesCleared = clearValue % 100;
            text += " " + CLEAR_NAMES[linesCleared];
            //Actually set the text.
            ref_ClearTypeText.text = text;
        }

        if (fullClearType != 0) //Don't update or reset timer if no full clear was done.
        {
            fullClearTextTimer = FULL_CLEAR_TIMER_LENGTH;
            if (fullClearType == 1)
            {
                ref_FullClearText.text = COLOR_CLEAR_TEXT;
            }
            else //if (fullClearType == 2)
            {
                ref_FullClearText.text = ALL_CLEAR_TEXT;
            }
        }
    }

    //This method is called when any gameover condition is detected and handles the transition to the post-game screen. //TODO: Actually add that post-game screen, in this first "blocks but not cursed yet" build, it doesn't exist.
    public void GameOver()
    {
        gameover = true;
        string resetKeybind = gameObject.GetComponent<PlayerInput>().actions.FindAction("Reset").GetBindingDisplayString(); //Determine reset keybind so it can be displayed.
        ref_GameoverText.text = string.Format(GAME_OVER_TEXT, resetKeybind);
    }

    //The next several functions, named "On[something]", are input-handling functions, invoked by the Player Input component.
    public void OnShiftLeft(InputAction.CallbackContext context)
    {
        //Debug.Log("heard left input");
        if (gameover)
        {
            return;
        }
        if (context.phase == InputActionPhase.Started)
        {
            if (ref_ActivePiece.CanPieceMove(Vector2Int.left)) //Move the piece if possible.
            {
                ref_ActivePiece.MovePiece(Vector2Int.left);
            }
            if (moveRightHeld && resetDASOnDirChange) //Reset opposite DAS timer if user desires.
            {
                dasRightTimer = das;
            }
            dasLeftTimer = das; //Start DAS timer.
            moveLeftHeld = true;
            moveLeftMostRecent = true;
        }
        else if (context.phase == InputActionPhase.Canceled)
        {
            if (moveRightHeld && resetDASOnDirChange) //Reset opposite DAS timer if user desires.
            {
                dasRightTimer = das;
            }
            moveLeftHeld = false;
            moveLeftMostRecent = false; //Now move-right is either the most recent held key because it's the only held key, or neither are held (in which case this variable is irrelevant).
        }
        //Debug.Log(context);
    }

    public void OnShiftRight(InputAction.CallbackContext context)
    {
        //Debug.Log("heard right input");
        if (gameover)
        {
            return;
        }
        if (context.phase == InputActionPhase.Started)
        {
            if (ref_ActivePiece.CanPieceMove(Vector2Int.right))
            {
                ref_ActivePiece.MovePiece(Vector2Int.right);
            }
            if (moveLeftHeld && resetDASOnDirChange) //Reset opposite DAS timer if user desires.
            {
                dasLeftTimer = das;
            }
            dasRightTimer = das; //Start DAS timer.
            moveRightHeld = true;
            moveLeftMostRecent = false;
        }
        else if (context.phase == InputActionPhase.Canceled)
        {
            if (moveLeftHeld && resetDASOnDirChange) //Reset opposite DAS timer if user desires.
            {
                dasLeftTimer = das;
            }
            moveRightHeld = false;
            moveLeftMostRecent = true; //Now move-left is either the most recent held key because it's the only held key, or neither are held (in which case this variable is irrelevant).
        }
    }

    public void OnSoftDrop(InputAction.CallbackContext context)
    {
        if (gameover)
        {
            return;
        }
        if (context.phase == InputActionPhase.Started)
        {
            softDropHeld = true;
        }
        else if (context.phase == InputActionPhase.Canceled)
        {
            softDropHeld = false;
        }
    }

    public void OnHardDrop(InputAction.CallbackContext context)
    {
        if (gameover)
        {
            return;
        }
        if (context.phase == InputActionPhase.Started)
        {
            ref_ActivePiece.HardDrop();
            ref_ActivePiece.LockPiece();
            if (moveLeftHeld)
            {
                dasLeftTimer = Mathf.Max(harddropDCD, dasLeftTimer);
            }
            if (moveRightHeld)
            {
                dasRightTimer = Mathf.Max(harddropDCD, dasRightTimer);
            }
        }
    }

    public void OnRotateCW(InputAction.CallbackContext context)
    {
        if (gameover)
        {
            return;
        }
        if (context.phase == InputActionPhase.Started)
        {
            Vector2Int kick = ref_ActivePiece.CheckKicktable(RotState.CWI); //Check for the first valid kick.
            if (kick.x != -9999) //Stop if the kick is invalid.
            {
                ref_ActivePiece.RotatePiece(RotState.CWI, kick);
            }
            
            if (moveLeftHeld)
            {
                dasLeftTimer = Mathf.Max(rotateDCD, dasLeftTimer);
            }
            if (moveRightHeld)
            {
                dasRightTimer = Mathf.Max(rotateDCD, dasRightTimer);
            }
        }
    }

    public void OnRotateCCW(InputAction.CallbackContext context)
    {
        if (gameover)
        {
            return;
        }
        if (context.phase == InputActionPhase.Started)
        {
            Vector2Int kick = ref_ActivePiece.CheckKicktable(RotState.CCW); //Check for the first valid kick.
            if (kick.x != -9999) //Stop if the kick is invalid.
            {
                ref_ActivePiece.RotatePiece(RotState.CCW, kick);
            }

            if (moveLeftHeld)
            {
                dasLeftTimer = Mathf.Max(rotateDCD, dasLeftTimer);
            }
            if (moveRightHeld)
            {
                dasRightTimer = Mathf.Max(rotateDCD, dasRightTimer);
            }
        }
    }

    public void OnRotate180(InputAction.CallbackContext context)
    {
        if (gameover)
        {
            return;
        }
        if (context.phase == InputActionPhase.Started)
        {
            Vector2Int kick = ref_ActivePiece.CheckKicktable(RotState.TWO); //Check for the first valid kick.
            if (kick.x != -9999) //Stop if the kick is invalid.
            {
                ref_ActivePiece.RotatePiece(RotState.TWO, kick);
            }

            if (moveLeftHeld)
            {
                dasLeftTimer = Mathf.Max(rotateDCD, dasLeftTimer);
            }
            if (moveRightHeld)
            {
                dasRightTimer = Mathf.Max(rotateDCD, dasRightTimer);
            }
        }
    }

    public void OnHold(InputAction.CallbackContext context)
    {
        if (gameover)
        {
            return;
        }
        if (context.phase == InputActionPhase.Started)
        {
            ref_HoldPiece.HoldPieceAction(ref_ActivePiece.piecePrototype);
            if (moveLeftHeld)
            {
                dasLeftTimer = Mathf.Max(holdDCD, dasLeftTimer);
            }
            if (moveRightHeld)
            {
                dasRightTimer = Mathf.Max(holdDCD, dasRightTimer);
            }
        }
    }

    public void OnReset(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            SceneManager.LoadScene("MainGameScene"); //Is reloading the entire scene to reset wasteful? Lil' bit. Is it reliable? You betcha.
            /*ResetObject();
            ref_HoldPiece.ResetObject();
            ref_Board.ResetObject();
            ref_ActivePiece.ResetObject();
            ref_CurseManager.ResetObject();
            ref_Queue.ResetObject(); //IMPORTANT: Queue.ResetObject MUST be called AFTER CurseManager.ResetObject, or all the bags that Queue generates will use OLD AND INVALID GAME VARIABLES.
            */
        }
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            if (!paused) //This whole thing could be condensed down to "paused = !paused" and etc., but this is much clearer.
            {
                paused = true;
                ref_BoardContainer.SetActive(false);
                ref_PauseTextObject.SetActive(true);
                string pauseKeybind = gameObject.GetComponent<PlayerInput>().actions.FindAction("Pause").GetBindingDisplayString();
                ref_PauseText.text = string.Format(PAUSED_TEXT, pauseKeybind);
            }
            else
            {
                paused = false;
                ref_BoardContainer.SetActive(true);
                ref_PauseTextObject.SetActive(false);
            }
        }
    }

    public void OnDebug(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            SceneManager.LoadScene("DebugScenes/DebugRoot");
        }
    }

    //This method spawns a new piece from the front of the queue, and makes the queue and incoming piece update as well.
    public void SpawnNextPiece()
    {
        SpawnPiece(ref_Queue.nextPieces.Dequeue(), false); //false argument for "wasHeld" parameter, meaning the piece did NOT come from hold.
        ref_Queue.UpdateDisplays();
        ref_IncomingPiece.UpdateIncomingPiece();
    }

    //This method spawns a new piece (by calling ActivePiece.SpawnPiece()) and handles everything else that needs to occur "between" pieces.
    public void SpawnPiece(BagPiece piece, bool wasHeld)
    {        
        //If this bag is empty, and the piece came from the next queue, generate the next bag.
        if (piece.HasProperty(BagPiece.LAST_IN_BAG) && !wasHeld)
        {
            ref_Queue.BuildNewBag();
            ref_Board.EndOfBagCurses(); //It's important that this come BEFORE CurseManager.NextCurseBag(). We need this bag's curses before we discard them.
            ref_CurseManager.NextCurseBag();
            UpdateSpecificCurseDisplays();
            bagNumber++;
            UpdateScoreMultiplier();
        }
        ref_ActivePiece.SpawnPiece(piece); //Has to happen after EndOfBagCurses() in case garbage gets pushed up into the spawning piece.
        //If the piece didn't come from hold, decrement the hold cooldown.
        if (!wasHeld)
        {
            ref_HoldPiece.cooldown--;
            ref_HoldPiece.UpdateDisplay(); //Hold piece needs to be updated in case the texture needs to be changed to reflect hold cooldown.
        }
        partialFallProgress = 1.0f; //According to guideline, a newly-spawning piece immediately falls by one mino if it is able. This implements that behavior.
        ref_Queue.UpdateIncomingGarbageDisplay(); //This needs to be updated every piece.
        //Handle Ephemereal Hold curse; if active, increase the ephemeral hold count, and if it's high enough, remove the hold piece.
        if (ref_CurseManager.IsCurseActive(Curse.EPHEMEREAL_HOLD) && !wasHeld)
        {
            ref_HoldPiece.ephemereal++;
            if (ref_HoldPiece.ephemereal >= HoldPiece.EPHEMEREAL_HOLD_THRESHOLD)
            {
                ref_HoldPiece.piecePrototype = new BagPiece(0, BagPiece.EMPTY_HOLD);
                ref_HoldPiece.UpdateDisplay();
            }
        }
        else //If the curse isn't active, OR if the piece was held, reset ephemereal count to 0. This single "else" handily takes care of both cases!
        {
            ref_HoldPiece.ephemereal = 0;
        }
        if (ref_HoldPiece.piecePrototype.HasProperty(BagPiece.EMPTY_HOLD)) //Reset the count if there is no hold piece.
        {
            ref_HoldPiece.ephemereal = 0;
        }
        ref_HoldPiece.UpdateEphemerealDisplay();
    }

    //This method determines what the current score multiplier is and updates the display accordingly.
    public void UpdateScoreMultiplier()
    {
        int curseBonus = ref_CurseManager.CurrentCurseLevel();
        scoreMultiplier = BASE_SCORE_MULTIPLIER + bagNumber + curseBonus;
        ref_ScoreInfoText.text = "Bag " + bagNumber + "\nCurse LV " + curseBonus + "\nScore multiplier " + scoreMultiplier;
    }

    //Several curses have a specific on-screen visual to indicate they are active. This method shows or hides them as appropriate.
    public void UpdateSpecificCurseDisplays()
    {
        if (ref_CurseManager.IsCurseActive(Curse.ANTISKIM))
        {
            ref_AntiskimText.text = ANTISKIM_TEXT;
        }
        else
        {
            ref_AntiskimText.text = "";
        }
        ref_HoldPiece.UpdateSpikeDisplay(); //This method checks for the presence or absence of the spiked hold curse. No need to do it here, just call it and let HoldPiece handle it.
        if (ref_CurseManager.IsCurseActive(Curse.ANTIGRAV))
        {
            ref_AntigravText.text = ANTIGRAV_TEXT;
        }
        else
        {
            ref_AntigravText.text = "";
        }
        if (ref_CurseManager.IsCurseActive(Curse.SLOW_HOLD))
        {
            ref_SlowHoldText.text = SLOW_HOLD_TEXT;
        }
        else
        {
            ref_SlowHoldText.text = "";
        }
        if (ref_CurseManager.IsCurseActive(Curse.SLIPPERY))
        {
            ref_SlipperyText.text = SLIPPERY_TEXT;
        }
        else
        {
            ref_SlipperyText.text = "";
        }
        if (ref_CurseManager.IsCurseActive(Curse.CLUMPED))
        {
            ref_ClumpedText.text = CLUMPED_TEXT;
        }
        else
        {
            ref_ClumpedText.text = "";
        }
    }
}