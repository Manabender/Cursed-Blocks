using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/* Board class
 * Broadly, this class handles the overall state of the game.
 * It is responsible for maintaining the state of all cells, each of which can contain up to one mino.
 * It is also responsible for performing actions on the board overall, such as clearing completed lines or adding garbage.
 * This is implemented as an object class to allow for the potential expansion of multiplayer, or single-player-multiple-boards,
 *  should I wish to add those modes. Two Board objects can exist just as easily as one, and can function independently.
 */

public class Board : MonoBehaviour
{
    public int width = 10; //Width of board in minos
    public int absoluteHeight = 40; //Absolute height in minos; the absolute maximum height that can be reached. Anything pushed above this might be lost.
    public int height = 21; //"Topout" height in minos. Minos can exist above this. Used to detect game-over conditions. (Also, see "legal bs" below)
    public int[,] cells; /* The actual grid of cells, each of which can contain up to one mino.
                          * If cells[x,y] is 0, it has no mino in it. Otherwise, the value is used as a reference into a list of mino textures.
                          * For (most) game mechanics, it only matters if a cell is zero or nonzero. All zeros are "empty" and all nonzeros are "filled".
                          * A full horizontal row of "filled" cells is a line to clear.
                          * Note also that a value of 1 is reserved for "garbage" (in game mechanic terms).
                          * X increases from left to right. Y increases from bottom to top.
                          */
    public int[,] displayCells; //The cells to actually display. Normally identical to cells[,], but the Mist curse can change this.
    public GameObject cellPrefab; //Prototype GameObject that references a Cell prefab (set in the Unity editor).
    public Orchestrator ref_Orchestrator;
    public GameObject ref_Container;
    public GridLayoutGroup ref_GridLayout;
    public GameObject ref_KillLine;
    public int clumpedLines; //The number of filled lines that exist at the moment. If the Clumped curse is active, filled lines won't go away until at least four are all simultaneously filled.
    public int aaaaa = 0; //Please don't ask why this is here. It shouldn't be here. (It was used for testing purposes.)

    public const int CLUMPED_THRESHOLD = 4;
    public const int MIST_CELL_TEXTURE_ID = 14;
    public const int CLUMPED_IGNORE_HEIGHT_THRESHOLD = 16;

    // Start is called before the first frame update
    void Start()
    {
        SetWidth(PersistantVars.pVars.width);
        cells = new int[width, absoluteHeight]; //Array size can't be known until the board size is known, but by the time Start() is called, the size is known and has had the opportunity to be overridden.
        displayCells = new int[width, absoluteHeight];

        //Create Cell objects for every cell.
        for (int y = absoluteHeight - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++) //Loop nesting order matters here; children objects are arranged using a Grid Layout group, so children must be created left-to-right THEN top-to-bottom.
            {
                GameObject newCell = Instantiate(cellPrefab, transform);
                BoardCell cellref = newCell.GetComponent<BoardCell>();
                cellref.cellx = x;
                cellref.celly = y;
                //cells[x, y] = Random.Range(0, 10); //Set random states to test rendering; if you're reading this, either you're me or this line is commented out.
                //cells[x, y] = 0; //I'm pretty sure int arrays in C# initialize to all 0, so this line just serves as a reminder of that.
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateDisplayCells();
    }

    //ResetObject is a method that appears in several object scripts that resets it to start-of-game values.
    public void ResetObject()
    {
        SetWidth(PersistantVars.pVars.width);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < absoluteHeight; y++)
            {
                cells[x, y] = 0;
                displayCells[x, y] = 0;
            }
        }
        clumpedLines = 0;
    }

    public void SetWidth(int w)
    {
        width = w;
        ref_GridLayout.constraintCount = w;
        //Debug.Log(ref_Container.GetComponent<RectTransform>().sizeDelta);
        ref_Container.GetComponent<RectTransform>().sizeDelta = new Vector2(width * Piece.MINO_SIZE, absoluteHeight * Piece.MINO_SIZE);
        ref_KillLine.GetComponent<SpriteRenderer>().size = new Vector2(width * Piece.MINO_SIZE, 4);
    }

    //This method adds one line of garbage. It takes as an argument a list of ints representing the columns which should be empty. Implicitly, all other columns should be filled.
    public void AddGarbage (int[] holes)
    {
        //Check for "Top Out" game over condition; if any mino would rise above the top of the board, game over. Check for this by testing if any cell in the top row is filled.
        for (int i = 0; i < width; i++)
        {
            if (cells[i,absoluteHeight-1] != 0)
            {
                ref_Orchestrator.GameOver();
            }
        }
        Stats.stats.IncStat("Garbage received");
        //Move all rows up one.
        for (int y = absoluteHeight - 1; y > 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                cells[x, y] = cells[x, y - 1];
            }
        }
        //Add the row.
        for (int i = 0; i < width; i++)
        {
            cells[i, 0] = 1;
        }
        //Place holes in the row.
        foreach (int hole in holes)
        {
            cells[hole, 0] = 0;
        }
        PersistantVars.pVars.PlaySound(SoundEffects.GARBAGE_SPAWN);
    }

    public void AddCleanGarbage()
    {
        //Compile a list of cells on the bottom row which are filled.
        List<int> validCells = new List<int>(30);
        for (int i = 0; i < width; i++)
        {
            if (cells[i, 0] == 0)
            {
                validCells.Add(i);
            }
        }
        int[] hole = new int[1];
        //Select one of those cells at random.
        if (validCells.Count != 0)
        {
            hole[0] = validCells[Random.Range(0, validCells.Count)];
        }
        else //Though rare, it is possible that there are no empty cells on the bottom row (due to clumped). In this case, just pick any column at random.
        {
            hole[0] = Random.Range(0, width);
        }
        AddGarbage(hole);
    }

    //This method handles all curses that have a one-off effect at the end of the bag they're in. As of this writing, this includes cheese, clean, supercheese, hypercheese, and mirror.
    public void EndOfBagCurses()
    {
        //Cheese garbage: Adds one line which has one hole, and that hole is guaranteed to appear under a mino if one exists on the bottom row.
        if (ref_Orchestrator.ref_CurseManager.IsCurseActive(Curse.CHEESE))
        {
            //Compile a list of cells on the bottom row which are filled.
            List<int> validCells = new List<int>(30);
            for (int i = 0; i < width; i++)
            {
                if (cells[i,0] != 0)
                {
                    validCells.Add(i);
                }
            }
            int[] hole = new int[1];
            //Select one of those cells at random.
            if (validCells.Count != 0)
            {
                hole[0] = validCells[Random.Range(0, validCells.Count)];
            }
            else //Though rare, it is possible that there are no filled cells on the bottom row (due to perfect clears or antigrav). In this case, just pick any column at random.
            {
                hole[0] = Random.Range(0, width);
            }
            AddGarbage(hole);
        }
        //Clean garbage: Adds two lines which have one hole each, and that hole is guaranteed to appear under an empty cell if one exists on the bottom row.
        if (ref_Orchestrator.ref_CurseManager.IsCurseActive(Curse.CLEAN))
        {
            AddCleanGarbage();
            AddCleanGarbage();
        }
        //Supercheese garbage: Adds one lines with two adjacent holes, and one of those holes is guaranteed to appear under a mino if one exists on the bottom row.
        if (ref_Orchestrator.ref_CurseManager.IsCurseActive(Curse.SUPERCHEESE))
        {
            //Compile a list of options. Scan across each cell except the rightmost, and if either that cell or the cell to its right is occupied, that cell is valid. A valid cell option encompasses that cell itself and the cell to its right.
            List<int> validCells = new List<int>(30);
            for (int i = 0; i < width - 1; i++)
            {
                if (cells[i,0] != 0 || cells [i+1,0] != 0)
                {
                    validCells.Add(i);
                }
            }
            int[] holes = new int[2];
            //Select an option at random.
            if (validCells.Count != 0)
            {
                holes[0] = validCells[Random.Range(0, validCells.Count)];                
            }
            else //Though rare, it is possible that there are no filled cells on the bottom row (due to perfect clears or antigrav). In this case, just pick any column at random.
            {
                holes[0] = Random.Range(0, width - 1);
            }
            holes[1] = holes[0] + 1;
            AddGarbage(holes);
        }
        //Hypercheese garbage: Adds one lines with two non-adjacent holes, and one of those holes is guaranteed to appear under a mino if one exists on the bottom row.
        if (ref_Orchestrator.ref_CurseManager.IsCurseActive(Curse.HYPERCHEESE))
        {
            //Compile a list of cells on the bottom row which are filled.
            List<int> validCells = new List<int>(30);
            for (int i = 0; i < width; i++)
            {
                if (cells[i, 0] != 0)
                {
                    validCells.Add(i);
                }
            }
            int[] holes = new int[2];
            //Select a cell at random for the first hole.
            if (validCells.Count != 0)
            {
                holes[0] = validCells[Random.Range(0, validCells.Count)];
            }
            else //Though rare, it is possible that there are no filled cells on the bottom row (due to perfect clears or antigrav). In this case, just pick any column at random.
            {
                holes[0] = Random.Range(0, width);
            }
            //Select any non-adjacent cell for the second hole.
            while (true)
            {
                holes[1] = Random.Range(0, width);
                int dist = Mathf.Abs(holes[1] - holes[0]);
                if (dist >= 2) //Accept only when the holes are 2 or more apart.
                {
                    break;
                }
            }
            AddGarbage(holes);
        }
        //Mirror: Mirrors the board across the y axis.
        if (ref_Orchestrator.ref_CurseManager.IsCurseActive(Curse.MIRROR))
        {
            PersistantVars.pVars.PlaySound(SoundEffects.PACE_GAINED);
            for (int y = 0; y < absoluteHeight; y++)
            {
                for (int x = 0; x < width / 2; x++) //If width is odd, this should correctly skip the middle column.
                {
                    int temp = cells[x, y];
                    cells[x, y] = cells[width - x - 1, y];
                    cells[width - x - 1, y] = temp;
                }
            }
        }
    }

    // ClearLines() checks the board to see if any completed lines are filled, and removes them from the board if so. It returns the number of lines cleared, plus 200 if a T spin was detected, or plus 100 if a T spin mini was detected.
    public int ClearLines()
    {
        int spinCheckValue = ref_Orchestrator.ref_ActivePiece.SpinCheck(); //The spin check needs the state of the board BEFORE lines are cleared.
        //Check to see how many lines are filled.
        int linesFound = ScanForFullLines();
        bool highLineCleared = false;
        if (linesFound >= 100)
        {
            highLineCleared = true;
            linesFound -= 100;
        }
        //The number of lines "cleared", for scoring purposes, is the number of lines newly filled.
        int scoringLinesCleared = linesFound - clumpedLines;
        clumpedLines = linesFound;
        //Handle Clumped Curse; if active, only delete lines if enough lines are filled. If not active, always delete lines.
        if (!ref_Orchestrator.ref_CurseManager.IsCurseActive(Curse.CLUMPED) || clumpedLines >= CLUMPED_THRESHOLD || highLineCleared)
        {
            DeleteLines();
        }

        if (spinCheckValue == 1 && scoringLinesCleared >= 2)
        {
            spinCheckValue = 2; //Implements the "full spin if 2+ lines cleared" rule that couldn't be implemented in SpinCheck; see that method for why.
        }

        ref_Orchestrator.linesCleared += scoringLinesCleared;

        PlayLineClearSound(scoringLinesCleared + (100 * spinCheckValue));

        return scoringLinesCleared + (100 * spinCheckValue);
    }

    public void PlayLineClearSound(int clear)
    {
        if (clear == 0) { return; }
        else if (clear == 1){PersistantVars.pVars.PlaySound(SoundEffects.LINE_CLEAR1);}
        else if (clear == 2) { PersistantVars.pVars.PlaySound(SoundEffects.LINE_CLEAR2); }
        else if (clear == 3) { PersistantVars.pVars.PlaySound(SoundEffects.LINE_CLEAR3); }
        else if (clear >= 4 && clear < 100) { PersistantVars.pVars.PlaySound(SoundEffects.LINE_CLEAR4); }
        else if (clear == 100) { PersistantVars.pVars.PlaySound(SoundEffects.T_SPIN_MINI0); }
        else if (clear == 101) { PersistantVars.pVars.PlaySound(SoundEffects.T_SPIN_MINI1); }
        else if (clear == 200) { PersistantVars.pVars.PlaySound(SoundEffects.T_SPIN0); }
        else if (clear == 201) { PersistantVars.pVars.PlaySound(SoundEffects.T_SPIN1); }
        else if (clear == 202) { PersistantVars.pVars.PlaySound(SoundEffects.T_SPIN2); }
        else if (clear >= 203) { PersistantVars.pVars.PlaySound(SoundEffects.T_SPIN3); }
    }

    //This is step 1 of the ClearLines method. This finds out how many full lines there are.
    public int ScanForFullLines()
    {
        int linesFound = 0;
        bool highLineFound = false; //Clumped curse won't work if a line sufficiently near the top is filled. This is intended to prevent the curse from unfairly causing a game over.
        for (int y = 0; y < absoluteHeight; y++) //Check each line
        {
            bool filled = true;
            for (int x = 0; x < width; x++) //Check each column within that line
            {
                if (cells[x, y] == 0)
                {
                    filled = false;
                    break;
                }
            }
            if (filled)
            {
                linesFound++;
                if (y >= CLUMPED_IGNORE_HEIGHT_THRESHOLD)
                {
                    highLineFound = true;
                }
            }
        }
        return linesFound + (highLineFound ? 100 : 0);
    }

    //This is step 2 of the ClearLines method. This deletes filled lines as appropriate.
    public void DeleteLines()
    {
        clumpedLines = 0; //Reset clump count
        for (int y = 0; y < absoluteHeight; y++) //Check each line
        {
            bool cleared = true;
            bool garbageFound = false;
            int hardBlocksFound = 0;
            //Check if the line is completely filled TODO: This doesn't need to be done if ScanForFullLines() passes along an array of filled lines. Could get a small efficiency gain by doing that.
            for (int x = 0; x < width; x++)
            {
                if (cells[x, y] == 0)
                {
                    cleared = false;
                    break;
                }
                else if (cells[x, y] == Piece.HARD_TEXTURE_ID)
                {
                    hardBlocksFound++;
                }
                else if (cells[x, y] == Piece.GARBAGE_TEXTURE_ID)
                {
                    garbageFound = true;
                }
            }
            //If the line is completely filled, clear it.
            if (cleared)
            {
                if (garbageFound)
                {
                    Stats.stats.IncStat("Garbage cleared");
                }
                //If no hard block was found and antigrav isn't active, make the lines above fall.
                //Note: There's also a rare edge case to handle with the second condition below: If a row is ENTIRELY filled with hard blocks, clear it anyway.
                if ((hardBlocksFound == 0 || hardBlocksFound == width) && !ref_Orchestrator.ref_CurseManager.IsCurseActive(Curse.ANTIGRAV))
                {
                    for (int yClear = y; yClear < absoluteHeight - 1; yClear++) //Go to < absoluteHeight - 1 to avoid index out of bounds.
                    {
                        for (int xClear = 0; xClear < width; xClear++)
                        {
                            cells[xClear, yClear] = cells[xClear, yClear + 1];
                        }
                    }
                    for (int xClear = 0; xClear < width; xClear++) //There's "nothing" above the top of the board, so this "nothing" falls into the top of the board. Side note: Shoutouts to PPT and PPT2 for NOT doing this.
                    {
                        cells[xClear, absoluteHeight - 1] = 0;
                    }
                    y--; //We just moved everything down one cell, so the next line to check is, likewise, down one cell.
                }
                //If a hard block WAS found or antigrav IS active, we still need to clear the row, but we can't cheat at that by overwriting it with the next row up. So lets actually clear the row.
                else
                {
                    for (int xClear = 0; xClear < width; xClear++)
                    {
                        if (cells[xClear, y] == Piece.HARD_TEXTURE_ID && hardBlocksFound != width) //SUPER rare edge case: If a row is totally filled with hard minoes AND antigrav is active, they still all need to be cleared completely. The extra "&& hardBlocksFound != width" check implements that.
                        {
                            cells[xClear, y] = Piece.COLOR_PINK; //Make the hard blocks no-longer-hard so they clear next time.
                        }
                        else
                        {
                            cells[xClear, y] = 0;
                        }
                    }
                }
            }
        }
    }

    //This method checks the board to see if it is completely clear of minoes. It returns 2 in case of an All Clear (every cell is empty), 1 in case of a Color Clear (every cell is either empty or has garbage in it), or 0 otherwise.
    public int CheckFullClears()
    {
        bool allClear = true; //If we find a garbage mino, set this to false but keep going. If we search every cell and don't find a non-garbage mino, we either have an All Clear if this is true, or a Color Clear if this is false.
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < absoluteHeight; y++)
            {
                if (cells[x, y] >= 2)
                {
                    return 0;
                }
                else if (cells[x, y] == 1)
                {
                    allClear = false;
                }
            }
        }
        if (allClear)
        {
            PersistantVars.pVars.PlaySound(SoundEffects.PERFECT_CLEAR);
            return 2;
        }
        else
        {
            PersistantVars.pVars.PlaySound(SoundEffects.PERFECT_CLEAR);
            return 1;
        }
    }

    public void UpdateDisplayCells()
    {
        //If mist isn't active, just set display cells to actual cells.
        if (!ref_Orchestrator.ref_CurseManager.IsCurseActive(Curse.MIST) || ref_Orchestrator.gameover)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < absoluteHeight; y++)
                {
                    displayCells[x, y] = cells[x, y];
                }
            }
        }
        else //if Mist IS active...
        {
            //Scan first through each column.
            for (int x = 0; x < width; x++)
            {
                bool blockFound = false;
                //In that column, go from top to bottom.
                for (int y = absoluteHeight - 1; y >= 0; y--)
                {
                    if (blockFound)
                    {
                        displayCells[x, y] = MIST_CELL_TEXTURE_ID;
                    }
                    else
                    {
                        displayCells[x, y] = cells[x, y];
                        if (cells[x,y] != 0)
                        {
                            blockFound = true; //Arranged in such a way that only the first mino found from the top is visible.
                        }
                    }
                }
            }
        }
    }
}

/* LEGAL BS
 * Apparently a judge has ruled that a board with the exact dimensions of 10x20 is a copyrightable element of a certain popular block-stacking game. 
 * If this winds up in court -- well, first of all, WHAT THE !@#$ IS WRONG WITH YOU, US LAW AND [insert name of suing party],
 * but I'm sure I could get nine out of ten mathematicians to agree that 21 is not 20.
 */
