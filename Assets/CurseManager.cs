using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

/* CurseManager class
 * This class handles everything related to setting up curses. It also handles the display of the curse queue.
 * Actual curse effect functionality is implemented in other classes where their functionality is relevant. For example, curses that add pieces to a bag are handled in Queue.BuildNewBag().
 */

public class CurseManager : MonoBehaviour
{   
    public int NUM_CURSES; //The total number of curses defined in the Curse enum. Variable name is formatted as a CONSTANT because it essentially is constant.
    public List<int[]> activeCurses; //The arrays of active curses. The list itself contains several arrays. The first element of the list contains the current bag's curses, the second element contains the next bag, and so on.
                                     //Each array consists of one int per curse. If the value is 0, the curse is inactive. If the value is postive, the curse is active for a number of bags equal to the value.
                                     //When Serenity is active, the first (index 0) value is negated and has 1 subtracted. Which means -1 means curse is inactive, -2 means it will be active for 1 bag, -3 means it will be active for 2 bags, etc.
    public List<GameObject> curseRows; //The array of visual curse rows showing which curses are active and upcoming.
    public int difficulty; //The difficulty level, with 0 being the easiest. Higher difficulty level means higher initial values for pretty much everything below.
    public int cursePoints; //Also called CP. This is the amount of "mana" the game has to use in order to add curses to the curse queue. Each curse has its own CP cost. Under no circumstance can the game spend more CP than it has; IE, CP will never go negative.
    public int cpRegen; //The amount of CP the game gains per bag. This amount, itself, increases by...
    public int cpRegenRamping; //...this. This is the amount by which CP regen increases per bag. This means that, slowly, the game will gain the ability to add more and stronger curses concurrently.
    public int cpFloor; //When it goes to try to add curses, if the game has less CP than this, it will usually choose not to. It has a small chance to do so anyway, however.
    public int cpFloorRamping; //The amount by which the CP floor increases per bag.
    public float cpFloorIgnoreChance; //The chance (from 0 to 1) that the game will choose to add a curse even if its CP is lower than the floor. It can only take this chance if it has already added at least one curse normally this bag, however, and it can only take the chance once per bag.
    public int baseCostCap; //The game can only add curses with a base cost less than or equal to this. This prevents very strong curses (like antigrav) from appearing early.
    public int baseCostCapRamping; //The amount by which the base cost cap increases per bag.
    public int maxCostCap; //In order to prevent the game from spending an excessive amount of CP on a single very-long-duration curse, the total amount of CP spent on a single curse is capped at this.
    public int maxCostCapRamping; //The amount by which the max cost cap increases per bag.
    public int serenityTimer; //The number of bags until serenity begins. During serenity, all curses are suspended and a "monomino" "blessing" is active, adding two monominoes per bag to help plug holes.
    public int serenityReset; //The value that the serenity timer will reset to when serenity expires.
    public int serenityResetRamping; //The amount by which serenity reset increases each time it occurs. Thus, serenity becomes less frequent as the game continues.
    public int serenityDuration; //The number of bags that serenity lasts. Intended to always be 2, at least as of writing this.
    public Orchestrator ref_Orchestrator;
    public const int INITIAL_LIST_SIZE = 20;
    public GameObject curseRowPrefab;
    public GameObject serenitySpritePrefab;
    public Sprite blankCurseTileSprite;

    //The following are constants (well, kind of) that set initial values for most of the game variables above. They are arrays; these handle various difficulty levels. The easiest level uses all index 0, the next uses all index 1, and so on.
    //The values for the last index are sort of a joke. They correspond to Nightmare difficulty, which just starts all curses with a 999999999 duration, effectively keeping them permanently enabled.
    public static readonly int[] INITIAL_CURSE_POINTS = { 150, 180, 210, 99999 };
    public static readonly int[] INITIAL_CP_REGEN = { 50, 60, 70, 99999 };
    public static readonly int[] INITIAL_CP_REGEN_RAMPING = { 3, 4, 5, 9 };
    public static readonly int[] INITIAL_CP_FLOOR = { 250, 300, 350, 99999 };
    public static readonly int[] INITIAL_CP_FLOOR_RAMPING = { 5, 6, 7, 9 };
    public static readonly float[] INITIAL_CP_FLOOR_IGNORE_CHANCE = { 0.08f, 0.10f, 0.12f, 0.99f };
    public static readonly int[] INITIAL_BASE_COST_CAP = { 40, 60, 60, 999 };
    public static readonly int[] INITIAL_BASE_COST_CAP_RAMPING = { 1, 1, 2, 9 };
    public static readonly int[] INITIAL_MAX_COST_CAP = { 150, 220, 300, 999999 };
    public static readonly int[] INITIAL_MAX_COST_CAP_RAMPING = { 1, 2, 3, 9 };
    public static readonly int[] INITIAL_SERENITY_RESET = { 8, 10, 12, 999999999 };
    public static readonly int[] INITIAL_SERENITY_RESET_RAMPING = { 1, 1, 1, 999999 };
    public static readonly int[] INITIAL_SERENITY_DURATION = { 2, 2, 2, 9 };

    public const int DIFFICULTY_NIGHTMARE = 3;

    public CurseData[] CURSE_DATA;

    //Awake is like Start, but even earlier.
    void Awake()
    {
        NUM_CURSES = Enum.GetNames(typeof(Curse)).Length;
        PopulateCurseData();
    }

    //This method generates a new array to append to activeCurses. Or, determines what curses will be active in the next generated bag (note that 20ish bags are already generated at any time and this adds another one on the end of that)
    //This method is called from Queue.AddBag and returns to it the list of curses active in that bag, which it uses to determine what pieces to put in that bag.
    public int[] CreateNewCurses()
    {        
        int[] curses = new int[NUM_CURSES];

        if (PersistantVars.pVars.enableCurseGeneration)
        {
            if (activeCurses.Count != 0) //Set curse durations to the same as they were last bag (they'll be decremented later if serenity isn't active). If there was no last bag, the array starts as all 0 anyway, so just do nothing.
            {
                for (int i = 0; i < NUM_CURSES; i++)
                {
                    curses[i] = activeCurses[activeCurses.Count - 1][i];
                }
            }

            if (activeCurses.Count == 0 && difficulty == DIFFICULTY_NIGHTMARE) //Initialize all curse durations to "effectively forever" if playing on Nightmare.
            {
                for (int i = 0; i < NUM_CURSES; i++)
                {
                    curses[i] = 999999999;
                }
            }

            //Handle Serenity
            serenityTimer--;
            if (serenityTimer < 0) //If serenity is active...
            {
                if (serenityTimer == -1) //Checks if serenity JUST STARTED THIS BAG.
                {
                    curses[0] = (curses[0] * -1) - 1; //Sets the "serenity active" pseudo-flag.
                }
                if (-1 * serenityTimer > serenityDuration) //Checks if serenity is FINISHED. (This isn't an elseif so that, if serenityDuration is 0, serenity effectively does not happen. Because, if serenityDuration is 0, both this block and the above block are executed, meaning that serenity starts then immediately stops, effectively meaning it may as well have not started in the first place.)
                {
                    curses[0] = (curses[0] * -1) - 1; //Unsets the "serenity active" pseudo-flag.
                    serenityReset += serenityResetRamping; //Ramp serenity reset value (serenity occurs less often over time).
                    serenityTimer = serenityReset - 1; //Reset serenity timer. //Extra -1 because we already decremented the timer for this bag and that decrement should carry over.
                }
            }

            //Core logic for determining what curses to add this bag.
            if (serenityTimer >= 0) //This isn't simply an "else" because, in the block above, serenityTimer is reset back to positive if serenity is finished. (Honestly, the somewhat sketchy logic comes down to how the function was written over time. I started with just the base curse-adding functionality with little regard to serenity, then added serenity later.)
            {
                //Decrement all active timers.
                for (int i = 0; i < NUM_CURSES; i++)
                {
                    curses[i] = Mathf.Max(0, curses[i] - 1);
                }

                //Ramp all values by their ramping rates
                cursePoints += cpRegen;
                cpRegen += cpRegenRamping;
                cpFloor += cpFloorRamping;
                baseCostCap += baseCostCapRamping;
                maxCostCap += maxCostCapRamping;

                //Add curses! Maybe. Keep going until under the CP floor.
                while (cursePoints > cpFloor)
                {
                    int curseAdded = AddCurse(curses); //Need to pass the working curses array so that AddCurse knows what curses are already active, so that it doesn't try to add an already-active curse.
                    if (curseAdded != 0)
                    {
                        curses[curseAdded % 1000] = curseAdded / 1000;
                        //To mix things up a bit, there is a small chance now that, if the game's available CP has gone under the CP floor, it'll add one more curse.
                        if (cursePoints <= cpFloor && cpFloorIgnoreChance > UnityEngine.Random.value)
                        {
                            curseAdded = AddCurse(curses);
                            if (curseAdded != 0)
                            {
                                curses[curseAdded % 1000] = curseAdded / 1000;
                            }
                        }
                    }
                    else //If the value returned from AddCurse is 0, there was no valid curse to add. So stop trying to add more.
                    {
                        break;
                    }
                }

            }

        }

        /*string debugString = "";
        for (int i = 0; i < NUM_CURSES; i++)
        {
            debugString += curses[i];
            debugString += ", ";
        }
        Debug.Log(debugString);*/

        activeCurses.Add(curses);

        AddVisualCurseRow(curses); //Add the visual curse row for this new set of curses

        return curses;
    }

    //This method selects a curse to add. It returns a value that contains both a curse index and a duration; the index is (value mod 1000), and the duration is floor(value / 1000). Alternately, it returns 0 if there is no valid curse to add.
    public int AddCurse(int[] curses)
    {
        List<int> availableCurses = new List<int>(NUM_CURSES); //List of curses that are valid to add.
        for (int i = 0; i < NUM_CURSES; i++) //Iterate through all curses to find which are valid.
        {
            //To be a valid curse, these conditions must be met: 1) The curse can't already be active. 2) The curse's base cost must be less or equal to base cost cap. 3) The curse's base cost must be less or equal to available CP.
            if (curses[i] == 0 && CURSE_DATA[i].cost <= baseCostCap && CURSE_DATA[i].cost <= cursePoints)
            {
                availableCurses.Add(i);
            }
        }
        if (availableCurses.Count == 0) //No valid curses to add, abort.
        {
            return 0;
        }

        int curseToAdd = availableCurses[UnityEngine.Random.Range(0, availableCurses.Count)]; //Pick a random curse to add.

        //Now that a curse has been selected, determine how long to apply it for.
        int maxCP = Mathf.Min(cursePoints, maxCostCap); //You can't spend more than you have, nor can you spend more than you're currently allowed to.
        int maxDuration = maxCP / CURSE_DATA[curseToAdd].cost; //The maximum possible duration allowed within the above constraints.
        int duration = Mathf.Min(UnityEngine.Random.Range(1, maxDuration + 1), UnityEngine.Random.Range(1, maxDuration + 1)); //Duration is set to the lower of two random rolls between 1 and maxDuration. This is done to favor shorter durations, as having several short duration curses is generally more interesting that a few long ones.
        cursePoints -= (duration * CURSE_DATA[curseToAdd].cost); //Deduct cost from available CP.

        return (duration * 1000) + curseToAdd;
    }

    //This method handles adding a new visual row of curse tiles to indicate which curses are active or upcoming.
    public void AddVisualCurseRow(int[] curses)
    {
        //Check if serenity is active
        if (curses[0] < 0)
        {
            GameObject newCurseRow = Instantiate(serenitySpritePrefab, transform); //transform argument sets self as parent
            curseRows.Add(newCurseRow);
        }
        else
        {
            //Add a new row!
            GameObject newCurseRow = Instantiate(curseRowPrefab, transform); //transform argument sets self as parent
            curseRows.Add(newCurseRow);

            //Get references to each curse tile. NOTE: The first object is actually just a left border, so the index [1] tile corresponds to the index [0] curse.
            SpriteRenderer[] curseTiles = newCurseRow.GetComponentsInChildren<SpriteRenderer>();        
        
            for (int i = 0; i < NUM_CURSES; i++)
            {
                if (curses[i] == 0) //Curse tiles default to being shown. Check if they shouldn't be.
                {
                    curseTiles[i + 1].sprite = blankCurseTileSprite;
                }
            }
        }
    }
    
    //This method advances the curse array queue. Or, basically, it just deletes the first array.
    public void NextCurseBag()
    {
        if (PersistantVars.pVars.goal != ModeGoal.SURVIVE) //The curse list only exists in the actual cursed mode. No need to do anything in other modes.
        {
            return;
        }
        PersistantVars.pVars.PlaySound(SoundEffects.LEVEL_UP);
        activeCurses.RemoveAt(0);
        Destroy(curseRows[0]);
        curseRows.RemoveAt(0);
    }

    //This method is used is various places to ask if a particular curse is active at the moment. As one might predict, it returns true if so and false if not.
    public bool IsCurseActive(Curse curse)
    {
        if (PersistantVars.pVars.goal == ModeGoal.SURVIVE) //Curses are only relevant in the actual cursed mode. All curses are off in other modes.
        {
            return activeCurses[0][0] >= 0 && (activeCurses[0][(int)curse] > 0 || PersistantVars.pVars.forcedCurses[(int)curse]); //If notSerenity and (curseActive or curseForcedActive)
        }
        return false;
        /*if (activeCurses[0][0] >= 0 && activeCurses[0][(int)curse] > 0) //If serenity is not active AND if the specified curse has a duration of 1 or more...
        {
            return true;
        }
        if (PersistantVars.pVars.forcedCurses[(int)curse]) //If the curse is forced via debug...
        {
            return true;
        }
        return false;*/
    }

    //This method calculates the current "curse level", which is the cost of all active curses divided by 10. This is used as a bonus to score multiplier.
    public int CurrentCurseLevel()
    {
        int total = 0;
        for (int i = 0; i < NUM_CURSES; i++)
        {
            if ( IsCurseActive( (Curse)i ) )
            {
                total += (CURSE_DATA[i].cost / 10);
            }
        }
        return total;
    }

    //ResetObject is a method that appears in several object scripts that resets it to start-of-game values. IMPORTANT: CurseManager.ResetObject MUST be called BEFORE Queue.ResetObject, or all the bags that Queue generates will use OLD AND INVALID GAME VARIABLES.
    public void ResetObject()
    {
        difficulty = PersistantVars.pVars.difficulty;
        activeCurses = new List<int[]>(INITIAL_LIST_SIZE);
        curseRows = new List<GameObject>(INITIAL_LIST_SIZE);
        cursePoints = INITIAL_CURSE_POINTS[difficulty];
        cpRegen = INITIAL_CP_REGEN[difficulty];
        cpRegenRamping = INITIAL_CP_REGEN_RAMPING[difficulty];
        cpFloor = INITIAL_CP_FLOOR[difficulty];
        cpFloorRamping = INITIAL_CP_FLOOR_RAMPING[difficulty];
        cpFloorIgnoreChance = INITIAL_CP_FLOOR_IGNORE_CHANCE[difficulty];
        baseCostCap = INITIAL_BASE_COST_CAP[difficulty];
        baseCostCapRamping = INITIAL_BASE_COST_CAP_RAMPING[difficulty];
        maxCostCap = INITIAL_MAX_COST_CAP[difficulty];
        maxCostCapRamping = INITIAL_MAX_COST_CAP_RAMPING[difficulty];
        serenityReset = INITIAL_SERENITY_RESET[difficulty];
        serenityTimer = serenityReset;
        serenityResetRamping = INITIAL_SERENITY_RESET_RAMPING[difficulty];
        serenityDuration = INITIAL_SERENITY_DURATION[difficulty];
    }

    //This method populates the CURSE_DATA array with...well, data.
    void PopulateCurseData()
    {
        CURSE_DATA = new CurseData[NUM_CURSES]; //Initialize array to correct size.

        CURSE_DATA[(int)Curse.PENTA] = new CurseData(
            "Penta",
            "Adds two pentominoes per bag",
            50);

        CURSE_DATA[(int)Curse.SMALL_H] = new CurseData(
            "h",
            "Adds an h piece to each bag",
            40);

        CURSE_DATA[(int)Curse.DROUGHT] = new CurseData(
            "Drought",
            "Adds three pieces per bag, but never I or T",
            50);

        CURSE_DATA[(int)Curse.FLOOD] = new CurseData(
            "Flood",
            "Adds three indentical pieces in a row to bag",
            70);

        CURSE_DATA[(int)Curse.PSEUDO] = new CurseData(
            "Pseudo",
            "Adds two pseudo-tetrominoes per bag",
            50);

        CURSE_DATA[(int)Curse.TWIN] = new CurseData(
            "Twin",
            "Adds a twinned piece to each bag",
            70);

        CURSE_DATA[(int)Curse.BIG] = new CurseData(
            "Big",
            "Adds a BIG piece to each bag",
            120);

        CURSE_DATA[(int)Curse.BIG_O] = new CurseData(
            "Big O",
            "[O] no, a piece with a hole in it",
            100);

        CURSE_DATA[(int)Curse.BIG_H] = new CurseData(
            "Big h",
            "hhhhhhhhhhh",
            100);

        CURSE_DATA[(int)Curse.CHEESE] = new CurseData(
            "Cheese Garbage",
            "Adds a row of cheesey garbage each bag",
            50);

        CURSE_DATA[(int)Curse.CLEAN] = new CurseData(
            "Clean Garbage",
            "Adds two rows of clean garbage each bag",
            50);

        CURSE_DATA[(int)Curse.SUPERCHEESE] = new CurseData(
            "Supercheese",
            "Cheesy garbage with a 2-wide gap",
            90);

        CURSE_DATA[(int)Curse.HYPERCHEESE] = new CurseData(
            "Hypercheese",
            "Cheesy garbage with two 1-wide gaps",
            150);

        CURSE_DATA[(int)Curse.ANTISKIM] = new CurseData(
            "Anti-skim",
            "Adds one clean garbage when you clear a single",
            100);

        CURSE_DATA[(int)Curse.SPIKED_HOLD] = new CurseData(
            "Spiked Hold",
            "Adds garbage every three holds",
            50);

        CURSE_DATA[(int)Curse.DISGUISE] = new CurseData(
            "Disguise",
            "Some pieces are colored wrong",
            20);

        CURSE_DATA[(int)Curse.HARD] = new CurseData(
            "Hard",
            "Hard pieces have to be cleared twice",
            150);

        CURSE_DATA[(int)Curse.FLOATING] = new CurseData(
            "Floating",
            "Floaty pieces lock in 1 above the floor",
            90);

        CURSE_DATA[(int)Curse.FOG] = new CurseData(
            "Fog",
            "Hides all but one next piece",
            40);

        CURSE_DATA[(int)Curse.CLUMPED] = new CurseData(
            "Clumped",
            "Lines don't clear until you fill four of them",
            80);

        CURSE_DATA[(int)Curse.SLOW_HOLD] = new CurseData(
            "Slow Hold",
            "Can't use hold twice in a row",
            50);

        CURSE_DATA[(int)Curse.MIRROR] = new CurseData(
            "Mirror",
            "Board is mirrored at end of bag",
            60);

        CURSE_DATA[(int)Curse.MIST] = new CurseData(
            "Mist",
            "Hides all but the topmost mino in each column",
            70);

        CURSE_DATA[(int)Curse.EPHEMERAL_HOLD] = new CurseData(
            "Ephemeral Hold",
            "Held piece disappears if not used 4 times",
            40);

        CURSE_DATA[(int)Curse.SLIPPERY] = new CurseData(
            "Slippery",
            "Pieces slip sideways unless placed next to a wall",
            70);

        CURSE_DATA[(int)Curse.GRAVITY] = new CurseData(
            "20G",
            "Who turned up the gravity?",
            90);

        CURSE_DATA[(int)Curse.ANTIGRAV] = new CurseData(
            "Antigrav",
            "Pieces don't fall when lines under them clear",
            200);
    }
}

//CurseData struct. Each individual curse appears in a large CURSE_DATA list, with each element being a CurseData.
public struct CurseData
{
    public string name; //The name of the curse.
    public string description; //A short description of the curse which appears on the pause screen to inform the player of what it does.
    public int cost; //The CP cost of this curse to apply it for one bag.

    public CurseData(string n, string desc, int c)
    {
        name = n;
        description = desc;
        cost = c;
    }
}

//Curse enum, to associate curse IDs with their names in the code.
public enum Curse
{
    PENTA, //0
    SMALL_H, //Because a single letter just doesn't feel right as an enumerated member
    DROUGHT,
    FLOOD,
    PSEUDO,
    TWIN, //5
    BIG,
    BIG_O,
    BIG_H,
    CHEESE,
    CLEAN, //10
    SUPERCHEESE,
    HYPERCHEESE,
    ANTISKIM,
    SPIKED_HOLD,
    DISGUISE, //15
    HARD,
    FLOATING,
    FOG,
    CLUMPED,
    SLOW_HOLD, //20
    MIRROR,
    MIST,
    EPHEMERAL_HOLD,
    SLIPPERY,
    GRAVITY, //25 //Unfortunately, "20G" isn't a valid name.
    ANTIGRAV
}