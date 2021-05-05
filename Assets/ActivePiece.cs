using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* ActivePiece class
 * This class stores data about the active piece. This includes its location, rotation, and other values used in computing how its rotation states affect its end location.
 * This class also handles piece spawning. Original design was to have a PieceSpawner object handle that, but it seemed wasteful to keep destroying and recreating Piece objects.
 * Instead, now the piece "spawns itself" by changing its properties to those of the next piece, instead of destroying itself and having a spawner create another piece with those properties.
 */

public class ActivePiece : Piece
{
    private Vector2Int position; //Piece's position on the board. The lower left corner of the board is 0,0.
    private RotState rotation; //Piece's rotation state. 0 is unrotated, 1 is 90deg clockwise, 2 is 180deg, 3 is 270deg clockwise (or 90deg counterclockwise).
    public Vector2Int[] preRotatedMinoes; //The position of each mino relative to the piece's board position, taking into account rotation but NOT rotation offset. Used for CanPieceRotate().
    public Vector2Int[] rotatedMinoes; //The actual, current position of each mino relative to the piece's board position, taking into account rotation and rotation offset, in that order.
    public Vector2Int[] trueMinoes; //The absolute position of each mino in the board. This is simply rotatedMinoes + position.
    public Vector2Int[] rotationOffsets; //An offset applied to all minoes on a per-rotationState basis. This is applied after the rotation itself.
    public Vector2Int[,][] kicktable; //Kicktables define location offsets to try to place a piece being rotated. Indices are [currentRotState,destinationRotState][kickAttempt]
    public Vector2Int[] spinCheckOffsets; //For pieces that can be spun, the locations that the game checks are occupied to determine whether a spin was performed or not.
    public int[] spinCheckValues; //The number of "points" scored if the corresponding location is occupied. The total is check against the next two numbers.
    public int miniThreshold; //The number of points that the spin test must score to be a mini spin.
    public int fullThreshold; //The number of points that the spin test must score to be a full spin.
    public int baseFinalLock; //The amount of time, in game frames, that a piece will wait while resting on the floor before it locks in on its own, except this timer ignores inputs.
    public int finalLockTimer; //The actual timer which ticks down (see above). DOES NOT reset on move or rotate; only when a new piece is spawned.
    public bool locking; //Are the lock timers ticking? The answer to this question is also the same as "Is the piece blocked from moving down?"
    public bool lastMoveWasSpin; //Was the most recent movement on the piece a rotation input? This is used in detecting Tspins.
    public bool lastSpinWasSuper; //Was the most recent rotation made with a "super" kick? A "super" kick is defined by having a y offset of -2 or lower and an x offset of anything except 0.

    public int T_TETROMINO_ID = 2;
    public int BASE_LOCK_DELAY_SETTING = 500;
    public int BASE_FINAL_LOCK_SETTING = 7500;


    public RotState Rotation
    {
        get => rotation;
        set { rotation = value; UpdateDerivedMinoes(); UpdateGhostOffset(); } //trueMinoes and ghost piece offset need to be updated whenever rotation is updated.
    }

    public Vector2Int Position
    {
        get => position;
        set { position = value; UpdateDerivedMinoes(); UpdateGhostOffset(); } //trueMinoes and ghost piece offset need to be updated whenever position is updated.
    }

    // Start is called before the first frame update
    void Start()
    {
        PopulatePieceData();
        
        //Set up PieceCells for display.
        for (int i = 0; i < 20; i++)
        {
            GameObject newCell = Instantiate(cellPrefab, transform); //transform argument sets self as parent
            PieceCell cellref = newCell.GetComponent<PieceCell>();
            cellref.minoNum = i;
        }
        //Set up PieceCells for ghost piece display.
        for (int i = 0; i < 20; i++)
        {
            GameObject newCell = Instantiate(cellPrefab, transform); //transform argument sets self as parent
            PieceCell cellref = newCell.GetComponent<PieceCell>();
            cellref.minoNum = i;
            cellref.isGhost = true;
        }
        baseLockDelay = BASE_LOCK_DELAY_SETTING;
        baseFinalLock = BASE_FINAL_LOCK_SETTING;
    }

    // Update is called once per frame
    void Update()
    {
        displayMinoes = rotatedMinoes; //Keep visual position in line with actual piece position. TODO: Maybe don't have to do this on update?
        transform.localPosition = new Vector3(Position.x * MINO_SIZE + MINO_SIZE / 2, Position.y * MINO_SIZE + MINO_SIZE / 2, PIECE_Z); //Hacky looking coordinates are needed to align the piece to the grid.
    }

    //ResetObject is a method that appears in several object scripts that resets it to start-of-game values.
    //NOTE: This ResetObject must be called AFTER Queue's. Otherwise it will spawn from an old queue.
    public void ResetObject()
    {
        ref_Orchestrator.SpawnNextPiece();
    }

    //This method generates both A) rotatedMinoes, which is the position of each mino within the piece taking rotation and rotation offset into account, and B) trueMinoes, which is the absolute position of each mino on the board.
    void UpdateDerivedMinoes()
    {
        preRotatedMinoes = new Vector2Int[minoes.Length];
        rotatedMinoes = new Vector2Int[minoes.Length];
        trueMinoes = new Vector2Int[minoes.Length];
        for (int i = 0; i < minoes.Length; i++)
        {
            /*if (Rotation == RotState.NAT) //No rotation
            {
                preRotatedMinoes[i] = minoes[i];
            }
            else if (Rotation == RotState.CWI) //Clockwise rotation translates [x,y] to [y,-x]
            {
                preRotatedMinoes[i].x = minoes[i].y;
                preRotatedMinoes[i].y = minoes[i].x * -1;
            }
            else if (Rotation == RotState.TWO) //180 rotation translates [x,y] to [-x,-y]
            {
                preRotatedMinoes[i] = minoes[i] * -1;
            }
            else //Counterclockwise rotation translates [x,y] to [-y,x]
            {
                preRotatedMinoes[i].x = minoes[i].y * -1;
                preRotatedMinoes[i].y = minoes[i].x;
            }*/
            preRotatedMinoes[i] = RotateVector(minoes[i], Rotation);
            rotatedMinoes[i] = preRotatedMinoes[i] + rotationOffsets[(int)Rotation]; //Apply rotation offset for rotated mino locations.
            trueMinoes[i] = rotatedMinoes[i] + Position; //Apply piece location for true mino locations.
        }

        //Update locking value; piece is locking if it cannot move down.
        bool wasLocking = locking;
        locking = !CanPieceMove(Vector2Int.down);
        if (locking && !wasLocking && !ref_Orchestrator.hardDropping)
        {
            PersistantVars.pVars.PlaySound(SoundEffects.PIECE_LAND);
        }
        //Reset lock delay.
        lockDelayTimer = baseLockDelay;
    }

    //This method searches below the mino to find where the ghost piece should be rendered.
    void UpdateGhostOffset()
    {
        int tryDrop = 1;
        while (true)
        {
            if (!CanPieceMove(new Vector2Int(0,(tryDrop*-1))))
            {
                break;
            }
            tryDrop++;
        }
        ghostOffset = tryDrop - 1;
        //Bump the ghost up one if the piece is floaty.
        if (texture == FLOATING_TEXTURE_ID && CanPieceMove(Vector2Int.up))
        {
            ghostOffset--;
        }
    }

    //This method spawns a new piece. Or, more accurately, it re-initializes all of the active piece's properties.
    public void SpawnPiece(BagPiece piece)
    {
        int spawnX = (ref_Orchestrator.ref_Board.width - 1) / 2; //Spawn the piece in the middle of the board width-wise, biased left on even widths.
        int spawnY = ref_Orchestrator.ref_Board.height + 1; //Design choice: Spawn the piece 1 mino higher to accomodate large pieces, among other reasons.
        position = new Vector2Int(spawnX, spawnY); //Note: It's OK to use the variable name instead of the Position setter to avoid an extra unnecessary call of UpdateDerivedMinoes(), which will be called when the Rotation setter is used a few lines down.      
        minoes = PIECE_DATA[piece.pieceID].minoes;
        texture = PIECE_DATA[piece.pieceID].DetermineTexture(piece);
        rotationOffsets = PIECE_DATA[piece.pieceID].rotationOffsets;
        kicktable = PIECE_DATA[piece.pieceID].kicktable;
        spinCheckOffsets = PIECE_DATA[piece.pieceID].spinCheckOffsets;
        spinCheckValues = PIECE_DATA[piece.pieceID].spinCheckValues;
        miniThreshold = PIECE_DATA[piece.pieceID].miniThreshold;
        fullThreshold = PIECE_DATA[piece.pieceID].fullThreshold;
        Rotation = RotState.NAT;
        piecePrototype = piece;
        lockDelayTimer = baseLockDelay;
        finalLockTimer = baseFinalLock;
        lastMoveWasSpin = false;
        //Check for "Blockout" gameover condition; if the spawning piece has any of its minoes overlapping with a mino on the board, the game ends.
        foreach (Vector2Int mino in trueMinoes)
        {
            if (ref_Orchestrator.ref_Board.cells[mino.x, mino.y] != 0)
            {
                ref_Orchestrator.GameOver();
            }
        }
    }

    //This method immediately drops the piece as far as it can go.
    public void HardDrop()
    {
        while (CanPieceMove(Vector2Int.down))
        {
            MovePiece(Vector2Int.down);
            ref_Orchestrator.score += ref_Orchestrator.hardDropScoreGain * ref_Orchestrator.DropScoreMultiplier(); //Award points for each cell dropped
        }
    }

    public void HandleLockDelay()
    {
        lockDelayTimer--;
        finalLockTimer--;
        if (lockDelayTimer <= 0)
        {
            PersistantVars.pVars.PlaySound(SoundEffects.PIECE_LOCK_DELAY_EXPIRED);
            LockPiece();
        }
        else if (finalLockTimer <= 0)
        {
            PersistantVars.pVars.PlaySound(SoundEffects.PIECE_LOCK_FORCED);
            LockPiece();
        }
    }

    //This method locks in the current piece, so that its minoes stop being part of the piece and start being part of the board. //TODO: This method is getting pretty bloated; can I break it up at all?
    public void LockPiece()
    {
        //Handle Slippery Curse: If active, and the piece is placed where it is able to move both left and right (IE, not against a wall of some kind), it slips one square randomly left or right. Then, if it is able to fall further, it does.
        if (ref_Orchestrator.ref_CurseManager.IsCurseActive(Curse.SLIPPERY) && CanPieceMove(Vector2Int.left) && CanPieceMove(Vector2Int.right))
        {
            if (Random.Range(0,2) == 1)
            {
                MovePiece(Vector2Int.left);
            }
            else
            {
                MovePiece(Vector2Int.right);
            }
            PersistantVars.pVars.PlaySound(SoundEffects.PIECE_INITIAL_ROTATION);
            while (CanPieceMove(Vector2Int.down))
            {
                MovePiece(Vector2Int.down);
            }
        }
        //Handle Floating Curse; if the piece is floaty, move it up one before locking it in. Extra note: There is the possibility of getting a mino above a floaty piece. It is concievably possible that, for example, a J5 or L5 pentomino overhangs up far into the upper part of the board and the floaty piece can be moved under it. Or possibly even the floaty piece could be kicked down. Regardless, to handle this edge case, check if the piece can move up.
        //Further extra note: That possibility is now significantly more likely. As of v1.0.3, floaty pieces are subject to gravity and can intentionally be wedged under something on the board to stop it from floating.
        if (texture == FLOATING_TEXTURE_ID && CanPieceMove(Vector2Int.up))
        {
            MovePiece(Vector2Int.up);
        }        
        //Place the piece's minoes on the board
        for (int i = 0; i < minoes.Length; i++)
        {
            ref_Orchestrator.ref_Board.cells[trueMinoes[i].x, trueMinoes[i].y] = texture;
        }

        //Score points!
        int linesClearedValue = ref_Orchestrator.ref_Board.ClearLines();
        int spinType = linesClearedValue / 100; //0 for no spin, 1 for mini, 2 for full tspin
        int linesCleared = linesClearedValue % 100; //Actual number of lines cleared
        int scoreGain;
        if (spinType == 0)
        {
            scoreGain = ref_Orchestrator.lineClearScoreGain[linesCleared];
        }
        else if (spinType == 1)
        {
            scoreGain = ref_Orchestrator.tSpinMiniScoreGain[linesCleared];
        }
        else //if (spinType == 2)
        {
            scoreGain = ref_Orchestrator.tSpinScoreGain[linesCleared];
        }

        //Handle B2B
        if (spinType == 0 && linesCleared >= 1 && linesCleared <= 3) //Conditions for RESETTING B2B.
        {
            ref_Orchestrator.backToBack = -1;
        }
        else if (spinType != 0 && linesCleared >= 1) //Conditions for INCREMENTING B2B in case of TSPIN WITH AT LEAST ONE LINE CLEARED
        {
            ref_Orchestrator.backToBack++;
        }
        else if (spinType == 0 && linesCleared >= 4) //Conditions for INCREMENTING B2B in case of QUADRUPLE OR GREATER LINE CLEAR
        {
            ref_Orchestrator.backToBack++;
        }
        //else: B2B does not change.

        //Add B2B bonus if necessary.
        if (linesCleared >= 1 && ref_Orchestrator.backToBack >= 1)
        {
            PersistantVars.pVars.PlaySound(SoundEffects.B2B);
            scoreGain = scoreGain * 3 / 2;
        }

        //Multiply by score multiplier.
        scoreGain *= ref_Orchestrator.scoreMultiplier;

        //Now that B2B is accounted for, add the points.
        ref_Orchestrator.score += scoreGain;

        //Handle combo
        if (linesCleared >= 1)
        {
            ref_Orchestrator.combo++;
            ref_Orchestrator.score += (ref_Orchestrator.comboScoreGain * ref_Orchestrator.combo * ref_Orchestrator.scoreMultiplier); //Award points for combo.
            PlayComboSound();
        }
        else
        {
            ref_Orchestrator.combo = -1; //No lines were cleared, reset combo.
        }

        //Check for full clears
        int fullClear = ref_Orchestrator.ref_Board.CheckFullClears();
        if (fullClear == 2)
        {
            ref_Orchestrator.score += ref_Orchestrator.allClearScoreGain * ref_Orchestrator.scoreMultiplier;
            //Update allclear count. Only relevant in allclear modes.
            ref_Orchestrator.allClears++;
            ref_Orchestrator.piecesPlacedSinceLastAllClear = 0;
        }
        else if (fullClear == 1)
        {
            ref_Orchestrator.score += ref_Orchestrator.colorClearScoreGain * ref_Orchestrator.scoreMultiplier;
        }
        else
        {
            ref_Orchestrator.piecesPlacedSinceLastAllClear++;
        }

        //Tell Orchestrator to update relevant text fields.
        ref_Orchestrator.SetClearTypeAndFullClearText(linesClearedValue, fullClear);

        //Check for "Lockout" gameover condition; if the piece locks completely above the kill line (completely above the board's height, not to be confused with its absolute height), the game ends.
        bool validMinoFound = false;
        foreach (Vector2Int mino in trueMinoes)
        {
            if (mino.y < ref_Orchestrator.ref_Board.height)
            {
                validMinoFound = true;
                break; //Once a single mino below the kill line is found, we're good. Game doesn't end.
            }
        }
        if (!validMinoFound)
        {
            ref_Orchestrator.GameOver();
        }

        //Anti-skim curse: If a non-spin single was cleared, add garbage.
        if (ref_Orchestrator.ref_CurseManager.IsCurseActive(Curse.ANTISKIM) && spinType == 0 && linesCleared == 1)
        {
            ref_Orchestrator.ref_Board.AddCleanGarbage();
        }

        //Spawn the next piece.
        ref_Orchestrator.SpawnNextPiece();
        //SpawnPiece(Random.Range(0, 7)); //Used for testing purposes.
    }

    //I expanded this out into this obviously-repetitive list in order to avoid typecasting from SoundEffects to int back to SoundEffects. That could be done to condense the entire function to a couple lines, but this is much clearer.
    public void PlayComboSound()
    {
        if (ref_Orchestrator.combo == 0) { return; }
        else if (ref_Orchestrator.combo == 1) { PersistantVars.pVars.PlaySound(SoundEffects.COMBO1); }
        else if (ref_Orchestrator.combo == 2) { PersistantVars.pVars.PlaySound(SoundEffects.COMBO2); }
        else if (ref_Orchestrator.combo == 3) { PersistantVars.pVars.PlaySound(SoundEffects.COMBO3); }
        else if (ref_Orchestrator.combo == 4) { PersistantVars.pVars.PlaySound(SoundEffects.COMBO4); }
        else if (ref_Orchestrator.combo == 5) { PersistantVars.pVars.PlaySound(SoundEffects.COMBO5); }
        else if (ref_Orchestrator.combo == 6) { PersistantVars.pVars.PlaySound(SoundEffects.COMBO6); }
        else if (ref_Orchestrator.combo == 7) { PersistantVars.pVars.PlaySound(SoundEffects.COMBO7); }
        else if (ref_Orchestrator.combo == 8) { PersistantVars.pVars.PlaySound(SoundEffects.COMBO8); }
        else if (ref_Orchestrator.combo == 9) { PersistantVars.pVars.PlaySound(SoundEffects.COMBO9); }
        else if (ref_Orchestrator.combo == 10) { PersistantVars.pVars.PlaySound(SoundEffects.COMBO10); }
        else if (ref_Orchestrator.combo == 11) { PersistantVars.pVars.PlaySound(SoundEffects.COMBO11); }
        else if (ref_Orchestrator.combo == 12) { PersistantVars.pVars.PlaySound(SoundEffects.COMBO12); }
        else if (ref_Orchestrator.combo == 13) { PersistantVars.pVars.PlaySound(SoundEffects.COMBO13); }
        else if (ref_Orchestrator.combo == 14) { PersistantVars.pVars.PlaySound(SoundEffects.COMBO14); }
        else if (ref_Orchestrator.combo == 15) { PersistantVars.pVars.PlaySound(SoundEffects.COMBO15); }
        else if (ref_Orchestrator.combo == 16) { PersistantVars.pVars.PlaySound(SoundEffects.COMBO16); }
        else if (ref_Orchestrator.combo == 17) { PersistantVars.pVars.PlaySound(SoundEffects.COMBO17); }
        else if (ref_Orchestrator.combo == 18) { PersistantVars.pVars.PlaySound(SoundEffects.COMBO18); }
        else if (ref_Orchestrator.combo == 19) { PersistantVars.pVars.PlaySound(SoundEffects.COMBO19); }
        else if (ref_Orchestrator.combo >= 20) { PersistantVars.pVars.PlaySound(SoundEffects.COMBO20); }

    }

    //This method, named MovePiece, moves the piece. Simple names are the best names.
    public void MovePiece(Vector2Int dir)
    {
        Position += dir;
        lastMoveWasSpin = false;
    }

    //This method checks a location, relative to the piece's current position, for anything that would block a movement.
    //Only the final location is checked, and typically, pieces are only moved one unit at a time, so the parameter is a "direction".
    public bool CanPieceMove(Vector2Int dir)
    {
        int boardAbsoluteHeight = ref_Orchestrator.ref_Board.absoluteHeight;
        int boardWidth = ref_Orchestrator.ref_Board.width;
        for (int i = 0; i < trueMinoes.Length; i++)
        {
            Vector2Int destination = trueMinoes[i] + dir;
            //If the destination is outside the board boundaries, the move is invalid.
            if (destination.x < 0 || destination.x >= boardWidth || destination.y < 0 || destination.y >= boardAbsoluteHeight)
            {
                return false;
            }
            //If the destination is inside another mino, the move is invalid.
            if (ref_Orchestrator.ref_Board.cells[destination.x, destination.y] != 0)
            {
                return false;
            }
        }

        return true; //If no conflicts were found, the move is valid.
    }

    //This method rotates the piece, as well as shifting it if necessary.
    public void RotatePiece(RotState rotateBy, Vector2Int offset)
    {
        rotation = (RotState)(((int)Rotation + (int)rotateBy) % 4); //Note: It's OK to use the variable name instead of the Rotation setter to avoid an extra unnecessary call of UpdateDerivedMinoes().
        Position += offset;
        lastMoveWasSpin = true;
        if (offset.y <= -2 && offset.x != 0)
        {
            lastSpinWasSuper = true;
        }
        else
        {
            lastSpinWasSuper = false;
        }
    }

    //This method checks to see if the piece can be rotated in a particular manner. A rotation is valid if and only if all of the destination cells are empty.
    public bool CanPieceRotate(RotState rotateBy, Vector2Int offset)
    {
        int boardAbsoluteHeight = ref_Orchestrator.ref_Board.absoluteHeight;
        int boardWidth = ref_Orchestrator.ref_Board.width;
        RotState targetRotation = (RotState)(((int)Rotation + (int)rotateBy) % 4);

        for (int i = 0; i < trueMinoes.Length; i++)
        {
            //Determine where this mino is trying to go.

            Vector2Int destination;// = new Vector2Int(0, 0);

            //Move the mino according to the new desired rotation.
            /*if (rotateBy == RotState.CWI)
            {
                destination.x = preRotatedMinoes[i].y;
                destination.y = preRotatedMinoes[i].x * -1;
            }
            else if (rotateBy == RotState.TWO)
            {
                destination = preRotatedMinoes[i] * -1;
            }
            else if (rotateBy == RotState.CCW)
            {
                destination.x = preRotatedMinoes[i].y * -1;
                destination.y = preRotatedMinoes[i].x;
            }
            else
            {
                Debug.LogError("CanPieceRotate was passed a rotateBy that wasn't expected. What are you doing?");
            }*/
            destination = RotateVector(preRotatedMinoes[i], rotateBy);
            //Debug.Log("After internal rotate:" + destination);
            destination += rotationOffsets[(int)targetRotation]; //Move the mino according to its new would-be rotation offset
            //Debug.Log("After Rotation Offsets:" + destination);
            destination += position; //Move the mino according to where the piece actually is
            //Debug.Log("After position:" + destination);
            destination += offset; //Move the mino according to the desired offset (kick)
            //Debug.Log("After offset:" + destination);

            //If the destination is outside the board boundaries, the move is invalid.
            if (destination.x < 0 || destination.x >= boardWidth || destination.y < 0 || destination.y >= boardAbsoluteHeight)
            {
                return false;
            }

            //If the destination is inside another mino, the move is invalid.
            if (ref_Orchestrator.ref_Board.cells[destination.x, destination.y] != 0)
            {
                return false;
            }
        }

        return true; //If no conflicts were found, the move is valid.
    }

    //This method iterates through the applicable kicktable to find the first valid kick, if one exists. If the whole kicktable is invalid, it returns the vector (-9999,-9999).
    public Vector2Int CheckKicktable(RotState rotateBy)
    {
        RotState targetRotation = (RotState)(((int)Rotation + (int)rotateBy) % 4);
        foreach (Vector2Int kick in kicktable[(int)Rotation, (int)targetRotation])
        {
            if (CanPieceRotate(rotateBy, kick))
            {
                return kick;
            }
        }
        return new Vector2Int(-9999, -9999);
    }

    //This method tests for Tspins. The exact definition is complex, and honestly, best described by the code itself. It returns 2 in case of a full spin, 1 in case of a mini, and 0 in case of no spin.
    public int SpinCheck()
    {
        //Is the piece eligible to be spun? If no, no spin.
        if (spinCheckOffsets == null)
        {
            //Debug.Log("No spin because piece isn't T");
            return 0;
        }
        //Was last move spin? If no, no spin.
        if (!lastMoveWasSpin)
        {
            //Debug.Log("No spin because last move wasn't a rotate");
            return 0;
        }
        //Does 3point check pass? If no, no spin.
        int cornerTest = SpinCornerTest(spinCheckOffsets, spinCheckValues, miniThreshold, fullThreshold);
        if (cornerTest == 0)
        {
            //Debug.Log("No spin because corner test failed");
            return 0;
        }
        //Was last spin super? (y decreases by at least 2, x changes by nonzero) If yes, full spin.
        if (lastSpinWasSuper)
        {
            //Debug.Log("Full spin because last rotate was super");
            return 2;
        }
        //Does froNt2point check pass? If yes, full spin.
        if (cornerTest == 2)
        {
            //Debug.Log("Full spin because front corner test passed");
            return 2;
        }
        //Were 2+ lines cleared? If yes, full spin. //Note: This is not part of guideline; I just feel like Tspin mini doubles are just as hard as (actually much harder than) Tspin full doubles, and I don't want to come up with a point table for Tspin minis 3 through 5.
        /*if (linesCleared >= 2)
        {
            Debug.Log("Full spin because 2+ lines were cleared");
            return 2;
        }*/ //This check had to be taken out because the corner test needs the state of the board BEFORE lines are removed. Lines are removed as they are detected as cleared, so if this condition is met, Board.ClearLines will handle it.
        //Else mini spin.
        //Debug.Log("Mini spin");
        return 1;
    }

    //This method tests the four "corners" of a T piece to see if they are occupied, and returns the check score.
    //A check score of exactly 7 means both back corners and one front corner are occupied; this is the "mini" spin condition as defined by guideline.
    //A check score of 8 or more means both front corners and one or both back corners are occupied; this is the "full" spin condition.
    //This is derived by making each front corner worth 3 and each back corner worth 2. This "scoring system" isn't part of guideline; it's just a way I came up with to condense the spin check result into a single number.
    //NOTE MADE LATER: This method has now been generalized to allow for other scoring methods. It must now also be supplied with a point table and two thresholds. It returns 2 if the full threshold is met, 1 if the mini threshold is met, and 0 else.
    public int SpinCornerTest(Vector2Int[] checkMinoOffsets, int[] pointValues, int miniThreshold, int fullThreshold)
    {
        Vector2Int[] checkMinoes = (Vector2Int[])checkMinoOffsets.Clone();
        for (int i = 0; i < checkMinoes.Length; i++)
        {
            checkMinoes[i] = RotateVector(checkMinoes[i], Rotation); //Rotate the list of checkMinoOffsets according to the piece's rotation. This is necessary even with the basic T tetromino shape as the list places the "front" corners in the first two indices and the "back" corners in the last two.
        }
        //Shift the offset locations by the current rotation offset.
        for (int i = 0; i < checkMinoes.Length; i++)
        {
            checkMinoes[i] += rotationOffsets[(int)Rotation];
        }
        int score = 0;
        for (int i = 0; i < checkMinoes.Length; i++)
        {
            if (IsCellOccupied(Position + checkMinoes[i]))
            {
                score += pointValues[i];
            }
        }

        //Debug.Log(score);
        if (score >= fullThreshold)
        {
            return 2;
        }
        else if (score >= miniThreshold)
        {
            return 1;
        }
        return 0; //else
    }

    //This method checks to see if a cell on the board is either occupied by another mino, or outside the board bounds, and returns true in either case, or false else.
    public bool IsCellOccupied(Vector2Int cell)
    {
        if (cell.x < 0 || cell.x >= ref_Orchestrator.ref_Board.width || cell.y < 0 || cell.y >= ref_Orchestrator.ref_Board.absoluteHeight)
        {
            return true;
        }
        if (ref_Orchestrator.ref_Board.cells[cell.x, cell.y] != 0)
        {
            return true;
        }
        return false;
    }

    //This method returns the Vector2Int passed to it, after rotating it by a specified multiple of 90 degrees about the origin (0, 0).
    public static Vector2Int RotateVector(Vector2Int vector, RotState rotateBy)
    {
        Vector2Int rotatedVector = new Vector2Int(0, 0);
        if (rotateBy == RotState.NAT)
        {
            rotatedVector = vector;
        }
        else if (rotateBy == RotState.CWI)
        {
            rotatedVector.x = vector.y;
            rotatedVector.y = vector.x * -1;
        }
        else if (rotateBy == RotState.TWO)
        {
            rotatedVector = vector * -1;
        }
        else //if (rotateBy == RotState.CCW)
        {
            rotatedVector.x = vector.y * -1;
            rotatedVector.y = vector.x;
        }
        return rotatedVector;
    }
}


