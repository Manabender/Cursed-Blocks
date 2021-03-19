using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Piece class
 * This class is a superclass of all specific Piece-like objects, including ActivePiece (the piece being controlled),
 *  QueuePiece (pieces displayed in the next queue), and HoldPiece (the piece displayed in hold).
 * This class serves two purposes. First, it holds the piece database. I chose to put it here because Piece.cs could be swapped out to implement new piece sets or rotation systems.
 * Second, it maintains a few fields common to all pieces, like their list of minoes.
 */



public class Piece : MonoBehaviour
{
    public BagPiece piecePrototype; //The raw BagPiece (containing id and special properties) of this piece.
    public Vector2Int[] minoes; //The position of each mino within the piece itself, relative to its position and in its natural rotation state.
    public Vector2Int[] displayMinoes; //The position in which each mino is to be displayed.
    public int texture; //The index into Mino Textures to use to display this piece's minoes.
    public int ghostOffset; //The amount of space below the piece that it can fall; IE, how far down the ghost piece should be drawn in relation to the active piece. Only used in ActivePiece subclass, but has to be here because PieceCell references the Piece superclass.
    public int baseLockDelay = 1; //The amount of time, in game frames (2 ms each), that a piece must wait while resting on the floor before it locks in on its own. Only used in ActivePiece subclass, but has to be here because PieceCell references the Piece superclass. (Initialized to 1 to avoid div0 errors, even though they shouldn't be an issue anyway)
    public int lockDelayTimer = 1; //The actual timer which ticks down (see above). Resets any time the piece makes a valid move or rotate. Only used in ActivePiece subclass, but has to be here because PieceCell references the Piece superclass.    
    public GameObject cellPrefab;
    public Orchestrator ref_Orchestrator;
    public const float MINO_SIZE = 20;
    public const float PIECE_Z = -1;

    public readonly PieceData[] PIECE_DATA = new PieceData[120]; //Initialize the piece database. It is populated in PopulatePieceData() which is called in Start()

    // Disguised pieces replace this's index with this's value
    //                                                               0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20
    public static readonly int[] DISGUISE_REPLACEMENTS = new int[] { 0, 1, 5, 7, 9, 2, 8, 3, 6, 4, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
    public const int GARBAGE_TEXTURE_ID = 1;
    public const int BLANK_TEXTURE_ID = 10;
    public const int FLOATING_TEXTURE_ID = 11;
    public const int HARD_TEXTURE_ID = 12;

    // PieceData struct: All the piece data is stored in one PIECE_DATA array, with each element being a PieceData.
    public struct PieceData
    {
        public readonly Vector2Int[] minoes; //The location of the minoes of the piece, relative to an implict center at [0,0]
        public readonly Vector2Int[] rotationOffsets; //A global offset applied to all minoes on a per-rotationState basis.
        public readonly Vector2Int[,][] kicktable; //The kicktable for this piece. Kicktables define location offsets to try to place a piece being rotated. Indices are [currentRotState,destinationRotState][kickAttempt]
        public readonly int texture; //The index into Mino Textures to use to display this piece's minoes.
        public readonly Vector2Int[] spinCheckOffsets; //For pieces that can be spun, the locations that the game checks are occupied to determine whether a spin was performed or not.
        public readonly int[] spinCheckValues; //The number of "points" scored if the corresponding location is occupied. The total is check against the next two numbers.
        public readonly int miniThreshold; //The number of points that the spin test must score to be a mini spin.
        public readonly int fullThreshold; //The number of points that the spin test must score to be a full spin.

        public PieceData(Vector2Int[] minoes, Vector2Int[] rotationOffsets, Vector2Int[,][] kicktable, int texture)
        {
            this.minoes = minoes;
            this.rotationOffsets = rotationOffsets;
            this.kicktable = kicktable;
            this.texture = texture;
            spinCheckOffsets = null;
            spinCheckValues = null;
            miniThreshold = 0;
            fullThreshold = 0;
        }

        public PieceData(Vector2Int[] minoes, Vector2Int[] rotationOffsets, Vector2Int[,][] kicktable, int texture, Vector2Int[] spinCheckOffsets, int[] spinCheckValues, int miniThreshold, int fullThreshold)
        {
            this.minoes = minoes;
            this.rotationOffsets = rotationOffsets;
            this.kicktable = kicktable;
            this.texture = texture;
            this.spinCheckOffsets = spinCheckOffsets;
            this.spinCheckValues = spinCheckValues;
            this.miniThreshold = miniThreshold;
            this.fullThreshold = fullThreshold;
        }

        //If the piece has any special properties, this method overwrites the texture with that property's texture.
        public int DetermineTexture(BagPiece piece)
        {
            if (piece.HasProperty(BagPiece.DISGUISED))
            {
                return DISGUISE_REPLACEMENTS[texture];
            }
            else if (piece.HasProperty(BagPiece.HARD))
            {
                return HARD_TEXTURE_ID;
            }
            else if (piece.HasProperty(BagPiece.FLOATING))
            {
                return FLOATING_TEXTURE_ID;
            }

            return texture;
        }

        //An extension of the above method that allows queue pieces to be invisible during the Fog curse.
        public int DetermineQueueTexture(BagPiece piece, int numInQueue, bool isFogActive)
        {
            if (isFogActive && numInQueue > 0)
            {
                return BLANK_TEXTURE_ID;
            }

            return DetermineTexture(piece);
        }
    }

    //Evenbox offsets are used by pieces with an even-sized "bounding box". It allows me to simulate a center of rotation that's in the corner of a mino instead of on a mino. UL means the center of rotation/spawn is in the upper left corner of the even-sized bounding box, LL means lower left.
    private readonly Vector2Int[] ROTATION_OFFSETS_EVENBOX_UL = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, -1) };
    private readonly Vector2Int[] ROTATION_OFFSETS_EVENBOX_LL = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(1, 0) };
    private readonly Vector2Int[] ROTATION_OFFSETS_NONE = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 0), new Vector2Int(0, 0), new Vector2Int(0, 0) };
    private readonly Vector2Int[] ROTATION_OFFSETS_RIGHT_SPAWN = new Vector2Int[] { new Vector2Int(1, 0), new Vector2Int(1, 0), new Vector2Int(1, 0), new Vector2Int(1, 0) }; //This offset is for wider pieces that spawn with their center of rotation biased right instead of left.
    private readonly Vector2Int[,][] KICKTABLE_SRS_MAIN = new Vector2Int[4, 4][];
    private readonly Vector2Int[,][] KICKTABLE_SRS_I = new Vector2Int[4, 4][];
    private readonly Vector2Int[,][] KICKTABLE_SRS_PLUS_I = new Vector2Int[4, 4][];
    private readonly Vector2Int[,][] KICKTABLE_SRS_O = new Vector2Int[4, 4][];
    private readonly Vector2Int[,][] KICKTABLE_BIG_SRS_MAIN = new Vector2Int[4, 4][];
    private readonly Vector2Int[,][] KICKTABLE_BIG_SRS_I = new Vector2Int[4, 4][];
    private readonly Vector2Int[,][] KICKTABLE_BIG_SRS_PLUS_I = new Vector2Int[4, 4][];
    private readonly Vector2Int[,][] KICKTABLE_SRS_3X3 = new Vector2Int[4, 4][];
    private readonly Vector2Int[,][] KICKTABLE_SRS_2X4_3X4 = new Vector2Int[4, 4][];
    private readonly Vector2Int[,][] KICKTABLE_I5 = new Vector2Int[4, 4][];
    private readonly Vector2Int[,][] KICKTABLE_PSUEDO_I = new Vector2Int[4, 4][];
    private readonly Vector2Int[,][] KICKTABLE_PENTA_P_A = new Vector2Int[4, 4][];
    private readonly Vector2Int[,][] KICKTABLE_PENTA_P_B = new Vector2Int[4, 4][];
    private readonly Vector2Int[,][] KICKTABLE_BIG_H = new Vector2Int[4, 4][];
    private readonly Vector2Int[,][] KICKTABLE_TWIN = new Vector2Int[4, 4][];
    private readonly Vector2Int[,][] KICKTABLE_MONO = new Vector2Int[4, 4][];
    private readonly Vector2Int[] STANDARD_SPIN_CHECK_OFFSETS = new Vector2Int[] { new Vector2Int(-1, 1), new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1) };
    private readonly int[] STANDARD_SPIN_CHECK_VALUES = new int[] { 3, 3, 2, 2 };
    private const int STANDARD_SPIN_CHECK_MINI_THRESHOLD = 7;
    private const int STANDARD_SPIN_CHECK_FULL_THRESHOLD = 8;
    private readonly Vector2Int[] BIG_T_SPIN_CHECK_OFFSETS = new Vector2Int[] { new Vector2Int(-1, 1), new Vector2Int(2, 1), new Vector2Int(2, -2), new Vector2Int(-1, -2) };
    private readonly Vector2Int[] PSEUDO_T_SPIN_CHECK_OFFSETS = new Vector2Int[] { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0) };
    private readonly int[] PSEUDO_T_SPIN_CHECK_VALUES = new int[] { 2, 2, 1, 1 };
    private const int PSEUDO_T_SPIN_CHECK_MINI_THRESHOLD = 3;
    private const int PSEUDO_T_SPIN_CHECK_FULL_THRESHOLD = 4;

    public const int COLOR_RED = 2;
    public const int COLOR_ORANGE = 3;
    public const int COLOR_YELLOW = 4;
    public const int COLOR_GREEN = 5;
    public const int COLOR_CYAN = 6;
    public const int COLOR_BLUE = 7;
    public const int COLOR_PURPLE = 8;
    public const int COLOR_PINK = 9;

    protected void PopulatePieceData()
    {
        // Set up kicktables in a slightly more readable format. The first index is the start rotation state, the second index is the end rotation state.

        // This kicktable is standard SRS taken from Guideline. It is well established, if imperfect, and is burned heavily into most hardcore tetromino-stacker players' memory.
        KICKTABLE_SRS_MAIN[(int)RotState.NAT, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, -2), new Vector2Int(-1, -2) };
        KICKTABLE_SRS_MAIN[(int)RotState.CWI, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, 2), new Vector2Int(1, 2) };
        KICKTABLE_SRS_MAIN[(int)RotState.CWI, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, 2), new Vector2Int(1, 2) };
        KICKTABLE_SRS_MAIN[(int)RotState.TWO, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, -2), new Vector2Int(-1, -2) };
        KICKTABLE_SRS_MAIN[(int)RotState.TWO, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(0, -2), new Vector2Int(1, -2) };
        KICKTABLE_SRS_MAIN[(int)RotState.CCW, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(0, 2), new Vector2Int(-1, 2) };
        KICKTABLE_SRS_MAIN[(int)RotState.CCW, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(0, 2), new Vector2Int(-1, 2) };
        KICKTABLE_SRS_MAIN[(int)RotState.NAT, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(0, -2), new Vector2Int(1, -2) };
        // This 180 kicktable is taken, with permission, from TETR.IO. Thanks, osk! As there is little precident or documented design philosophy behind 180 kicks, I'm just going to use this kicktable for every piece (except big pieces, which use the same kicktable but doubled).
        KICKTABLE_SRS_MAIN[(int)RotState.NAT, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, 0), new Vector2Int(-1, 0) };
        KICKTABLE_SRS_MAIN[(int)RotState.TWO, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(-1, -1), new Vector2Int(1, -1), new Vector2Int(-1, 0), new Vector2Int(1, 0) };
        KICKTABLE_SRS_MAIN[(int)RotState.CWI, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 2), new Vector2Int(1, 1), new Vector2Int(0, 2), new Vector2Int(0, 1) };
        KICKTABLE_SRS_MAIN[(int)RotState.CCW, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, 2), new Vector2Int(-1, 1), new Vector2Int(0, 2), new Vector2Int(0, 1) };

        // This kicktable is standard SRS taken from Guideline. It is well established, if imperfect, and is burned heavily into most hardcore tetromino-stacker players' memory.
        KICKTABLE_SRS_I[(int)RotState.NAT, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-2, 0), new Vector2Int(1, 0), new Vector2Int(-2, -1), new Vector2Int(1, 2) };
        KICKTABLE_SRS_I[(int)RotState.CWI, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(-1, 0), new Vector2Int(2, 1), new Vector2Int(-1, -2) };
        KICKTABLE_SRS_I[(int)RotState.CWI, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(2, 0), new Vector2Int(-1, 2), new Vector2Int(2, -1) };
        KICKTABLE_SRS_I[(int)RotState.TWO, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-2, 0), new Vector2Int(1, -2), new Vector2Int(-2, 1) };
        KICKTABLE_SRS_I[(int)RotState.TWO, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(-1, 0), new Vector2Int(2, 1), new Vector2Int(-1, -2) };
        KICKTABLE_SRS_I[(int)RotState.CCW, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-2, 0), new Vector2Int(1, 0), new Vector2Int(-2, -1), new Vector2Int(1, 2) };
        KICKTABLE_SRS_I[(int)RotState.CCW, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-2, 0), new Vector2Int(1, -2), new Vector2Int(-2, 1) };
        KICKTABLE_SRS_I[(int)RotState.NAT, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(2, 0), new Vector2Int(-1, 2), new Vector2Int(2, -1) };
        // This 180 kicktable is taken, with permission, from TETR.IO. Thanks, osk! As there is little precident or documented design philosophy behind 180 kicks, I'm just going to use this kicktable for every piece (except big pieces, which use the same kicktable but doubled).
        KICKTABLE_SRS_I[(int)RotState.NAT, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, 0), new Vector2Int(-1, 0) };
        KICKTABLE_SRS_I[(int)RotState.TWO, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(-1, -1), new Vector2Int(1, -1), new Vector2Int(-1, 0), new Vector2Int(1, 0) };
        KICKTABLE_SRS_I[(int)RotState.CWI, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 2), new Vector2Int(1, 1), new Vector2Int(0, 2), new Vector2Int(0, 1) };
        KICKTABLE_SRS_I[(int)RotState.CCW, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, 2), new Vector2Int(-1, 1), new Vector2Int(0, 2), new Vector2Int(0, 1) };

        // This is the "SRS+" kicktable for the I piece, created by osk for TETR.IO. It's basically the same as normal SRS, except that it's actually mirror-symmetrical (that is, if rotating in one direction on a particular board produces a specific result, rotating the opposite direction on a mirrored board always produces the same [mirrored] result.)
        KICKTABLE_SRS_PLUS_I[(int)RotState.NAT, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-2, 0), new Vector2Int(-2, -1), new Vector2Int(1, 2) };
        KICKTABLE_SRS_PLUS_I[(int)RotState.CWI, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(2, 0), new Vector2Int(-1, -2), new Vector2Int(2, 1) };
        KICKTABLE_SRS_PLUS_I[(int)RotState.CWI, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(2, 0), new Vector2Int(-1, 2), new Vector2Int(2, -1) };
        KICKTABLE_SRS_PLUS_I[(int)RotState.TWO, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-2, 0), new Vector2Int(1, 0), new Vector2Int(-2, 1), new Vector2Int(1, -2) };
        KICKTABLE_SRS_PLUS_I[(int)RotState.TWO, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(-1, 0), new Vector2Int(2, 1), new Vector2Int(-1, -2) };
        KICKTABLE_SRS_PLUS_I[(int)RotState.CCW, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-2, 0), new Vector2Int(1, 2), new Vector2Int(-2, -1) };
        KICKTABLE_SRS_PLUS_I[(int)RotState.CCW, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-2, 0), new Vector2Int(1, -2), new Vector2Int(-2, 1) };
        KICKTABLE_SRS_PLUS_I[(int)RotState.NAT, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(2, 0), new Vector2Int(2, -1), new Vector2Int(-1, 2) };
        // This 180 kicktable is taken, with permission, from TETR.IO. Thanks, osk! As there is little precident or documented design philosophy behind 180 kicks, I'm just going to use this kicktable for every piece (except big pieces, which use the same kicktable but doubled).
        KICKTABLE_SRS_PLUS_I[(int)RotState.NAT, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, 0), new Vector2Int(-1, 0) };
        KICKTABLE_SRS_PLUS_I[(int)RotState.TWO, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(-1, -1), new Vector2Int(1, -1), new Vector2Int(-1, 0), new Vector2Int(1, 0) };
        KICKTABLE_SRS_PLUS_I[(int)RotState.CWI, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 2), new Vector2Int(1, 1), new Vector2Int(0, 2), new Vector2Int(0, 1) };
        KICKTABLE_SRS_PLUS_I[(int)RotState.CCW, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, 2), new Vector2Int(-1, 1), new Vector2Int(0, 2), new Vector2Int(0, 1) };

        //O piece has no kicks. O piece needs no kicks.
        for (int i = 0; i <= 3; i++)
        {
            for (int j = 0; j <= 3; j++)
            {
                KICKTABLE_SRS_O[i, j] = new Vector2Int[] { new Vector2Int(0, 0) };
            }
        }

        // The next three kicktables are big SRS kicktables, used by big (double-size) pieces. It's literally just the standard SRS kicktables with everything doubled.
        KICKTABLE_BIG_SRS_MAIN[(int)RotState.NAT, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-2, 0), new Vector2Int(-2, 2), new Vector2Int(0, -4), new Vector2Int(-2, -4) };
        KICKTABLE_BIG_SRS_MAIN[(int)RotState.CWI, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(2, -2), new Vector2Int(0, 4), new Vector2Int(2, 4) };
        KICKTABLE_BIG_SRS_MAIN[(int)RotState.CWI, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(2, -2), new Vector2Int(0, 4), new Vector2Int(2, 4) };
        KICKTABLE_BIG_SRS_MAIN[(int)RotState.TWO, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-2, 0), new Vector2Int(-2, 2), new Vector2Int(0, -4), new Vector2Int(-2, -4) };
        KICKTABLE_BIG_SRS_MAIN[(int)RotState.TWO, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(2, 2), new Vector2Int(0, -4), new Vector2Int(2, -4) };
        KICKTABLE_BIG_SRS_MAIN[(int)RotState.CCW, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-2, 0), new Vector2Int(-2, -2), new Vector2Int(0, 4), new Vector2Int(-2, 4) };
        KICKTABLE_BIG_SRS_MAIN[(int)RotState.CCW, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-2, 0), new Vector2Int(-2, -2), new Vector2Int(0, 4), new Vector2Int(-2, 4) };
        KICKTABLE_BIG_SRS_MAIN[(int)RotState.NAT, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(2, 2), new Vector2Int(0, -4), new Vector2Int(2, -4) };
        // This 180 kicktable is taken, with permission, from TETR.IO. Thanks, osk! As there is little precident or documented design philosophy behind 180 kicks, I'm just going to use this kicktable for every piece (except big pieces, which use the same kicktable but doubled).
        KICKTABLE_BIG_SRS_MAIN[(int)RotState.NAT, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 2), new Vector2Int(2, 2), new Vector2Int(-2, 2), new Vector2Int(2, 0), new Vector2Int(-2, 0) };
        KICKTABLE_BIG_SRS_MAIN[(int)RotState.TWO, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, -2), new Vector2Int(-2, -2), new Vector2Int(2, -2), new Vector2Int(-2, 0), new Vector2Int(2, 0) };
        KICKTABLE_BIG_SRS_MAIN[(int)RotState.CWI, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(2, 4), new Vector2Int(2, 2), new Vector2Int(0, 4), new Vector2Int(0, 2) };
        KICKTABLE_BIG_SRS_MAIN[(int)RotState.CCW, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-2, 0), new Vector2Int(-2, 4), new Vector2Int(-2, 2), new Vector2Int(0, 4), new Vector2Int(0, 2) };

        KICKTABLE_BIG_SRS_I[(int)RotState.NAT, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-4, 0), new Vector2Int(2, 0), new Vector2Int(-4, -2), new Vector2Int(2, 4) };
        KICKTABLE_BIG_SRS_I[(int)RotState.CWI, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(4, 0), new Vector2Int(-2, 0), new Vector2Int(4, 2), new Vector2Int(-2, -4) };
        KICKTABLE_BIG_SRS_I[(int)RotState.CWI, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-2, 0), new Vector2Int(4, 0), new Vector2Int(-2, 4), new Vector2Int(4, -2) };
        KICKTABLE_BIG_SRS_I[(int)RotState.TWO, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(-4, 0), new Vector2Int(2, -4), new Vector2Int(-4, 2) };
        KICKTABLE_BIG_SRS_I[(int)RotState.TWO, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(4, 0), new Vector2Int(-2, 0), new Vector2Int(4, 2), new Vector2Int(-2, -4) };
        KICKTABLE_BIG_SRS_I[(int)RotState.CCW, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-4, 0), new Vector2Int(2, 0), new Vector2Int(-4, -2), new Vector2Int(2, 4) };
        KICKTABLE_BIG_SRS_I[(int)RotState.CCW, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(-4, 0), new Vector2Int(2, -4), new Vector2Int(-4, 2) };
        KICKTABLE_BIG_SRS_I[(int)RotState.NAT, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-2, 0), new Vector2Int(4, 0), new Vector2Int(-2, 4), new Vector2Int(4, -2) };
        // This 180 kicktable is taken, with permission, from TETR.IO. Thanks, osk! As there is little precident or documented design philosophy behind 180 kicks, I'm just going to use this kicktable for every piece (except big pieces, which use the same kicktable but doubled).
        KICKTABLE_BIG_SRS_I[(int)RotState.NAT, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 2), new Vector2Int(2, 2), new Vector2Int(-2, 2), new Vector2Int(2, 0), new Vector2Int(-2, 0) };
        KICKTABLE_BIG_SRS_I[(int)RotState.TWO, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, -2), new Vector2Int(-2, -2), new Vector2Int(2, -2), new Vector2Int(-2, 0), new Vector2Int(2, 0) };
        KICKTABLE_BIG_SRS_I[(int)RotState.CWI, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(2, 4), new Vector2Int(2, 2), new Vector2Int(0, 4), new Vector2Int(0, 2) };
        KICKTABLE_BIG_SRS_I[(int)RotState.CCW, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-2, 0), new Vector2Int(-2, 4), new Vector2Int(-2, 2), new Vector2Int(0, 4), new Vector2Int(0, 2) };

        KICKTABLE_BIG_SRS_PLUS_I[(int)RotState.NAT, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(-4, 0), new Vector2Int(-4, -2), new Vector2Int(2, 4) };
        KICKTABLE_BIG_SRS_PLUS_I[(int)RotState.CWI, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-2, 0), new Vector2Int(4, 0), new Vector2Int(-2, -4), new Vector2Int(4, 2) };
        KICKTABLE_BIG_SRS_PLUS_I[(int)RotState.CWI, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-2, 0), new Vector2Int(4, 0), new Vector2Int(-2, 4), new Vector2Int(4, -2) };
        KICKTABLE_BIG_SRS_PLUS_I[(int)RotState.TWO, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-4, 0), new Vector2Int(2, 0), new Vector2Int(-4, 2), new Vector2Int(2, -4) };
        KICKTABLE_BIG_SRS_PLUS_I[(int)RotState.TWO, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(4, 0), new Vector2Int(-2, 0), new Vector2Int(4, 2), new Vector2Int(-2, -4) };
        KICKTABLE_BIG_SRS_PLUS_I[(int)RotState.CCW, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(-4, 0), new Vector2Int(2, 4), new Vector2Int(-4, -2) };
        KICKTABLE_BIG_SRS_PLUS_I[(int)RotState.CCW, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(-4, 0), new Vector2Int(2, -4), new Vector2Int(-4, 2) };
        KICKTABLE_BIG_SRS_PLUS_I[(int)RotState.NAT, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-2, 0), new Vector2Int(4, 0), new Vector2Int(4, -2), new Vector2Int(-2, 4) };
        // This 180 kicktable is taken, with permission, from TETR.IO. Thanks, osk! As there is little precident or documented design philosophy behind 180 kicks, I'm just going to use this kicktable for every piece (except big pieces, which use the same kicktable but doubled).
        KICKTABLE_BIG_SRS_PLUS_I[(int)RotState.NAT, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 2), new Vector2Int(2, 2), new Vector2Int(-2, 2), new Vector2Int(2, 0), new Vector2Int(-2, 0) };
        KICKTABLE_BIG_SRS_PLUS_I[(int)RotState.TWO, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, -2), new Vector2Int(-2, -2), new Vector2Int(2, -2), new Vector2Int(-2, 0), new Vector2Int(2, 0) };
        KICKTABLE_BIG_SRS_PLUS_I[(int)RotState.CWI, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(2, 4), new Vector2Int(2, 2), new Vector2Int(0, 4), new Vector2Int(0, 2) };
        KICKTABLE_BIG_SRS_PLUS_I[(int)RotState.CCW, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-2, 0), new Vector2Int(-2, 4), new Vector2Int(-2, 2), new Vector2Int(0, 4), new Vector2Int(0, 2) };

        // This is an extended SRS kicktable used for pieces that fit in a 3x3 bounding box. It includes a copy of all kicks with a nonzero x offset, where the copy negates that offset. These copies are added to the end of the table.
        // This is based on the assumption that a 3x3 piece can be conceptualized as two 3x2 pieces that are overlapping eachother and facing opposite directions. This is not a perfect assumption, but it should work well enough.
        KICKTABLE_SRS_3X3[(int)RotState.NAT, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, -2), new Vector2Int(-1, -2), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, -2) };
        KICKTABLE_SRS_3X3[(int)RotState.CWI, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(-1, 2) };
        KICKTABLE_SRS_3X3[(int)RotState.CWI, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(-1, 2) };
        KICKTABLE_SRS_3X3[(int)RotState.TWO, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, -2), new Vector2Int(-1, -2), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, -2) };
        KICKTABLE_SRS_3X3[(int)RotState.TWO, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(0, -2), new Vector2Int(1, -2), new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(-1, -2) };
        KICKTABLE_SRS_3X3[(int)RotState.CCW, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(0, 2), new Vector2Int(-1, 2), new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(1, 2) };
        KICKTABLE_SRS_3X3[(int)RotState.CCW, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(0, 2), new Vector2Int(-1, 2), new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(1, 2) };
        KICKTABLE_SRS_3X3[(int)RotState.NAT, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(0, -2), new Vector2Int(1, -2), new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(-1, -2) };
        // This 180 kicktable is taken, with permission, from TETR.IO. Thanks, osk! As there is little precident or documented design philosophy behind 180 kicks, I'm just going to use this kicktable for every piece (except big pieces, which use the same kicktable but doubled).
        KICKTABLE_SRS_3X3[(int)RotState.NAT, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, 0), new Vector2Int(-1, 0) };
        KICKTABLE_SRS_3X3[(int)RotState.TWO, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(-1, -1), new Vector2Int(1, -1), new Vector2Int(-1, 0), new Vector2Int(1, 0) };
        KICKTABLE_SRS_3X3[(int)RotState.CWI, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 2), new Vector2Int(1, 1), new Vector2Int(0, 2), new Vector2Int(0, 1) };
        KICKTABLE_SRS_3X3[(int)RotState.CCW, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, 2), new Vector2Int(-1, 1), new Vector2Int(0, 2), new Vector2Int(0, 1) };

        // This is an extended SRS kicktable used for pieces that fit in a 2x4 bounding box. (It is also used for 3x4 pieces because they're particularly rare) The kicks and the logic behind them for Natural to CCW are as follows:
        // 0,0: The no-kick. Natural rotations are always preferred when possible, obviously.
        // 1,0: The bump-left kick. This is the same as kick 2 in standard SRS. Typically used if there is an obstruction below.
        // 1,1: The bump-up kick. This is the same as kick 3 in standard SRS. Typically used if there is a flat(ish) floor the piece is on.
        // 1,2: The extended-bump-up kick. Typically used if the longer side of the piece (the one that protrudes 2 from the center of rotation) ends up pointing down.
        // 0,-2,  1,-2,  0,-3,  1,-3,  2,-3: The "into trench" kicks. These are the reverse of kicks that pull a piece out of a narrow well that it can't bump down into. The first two of these kicks are the same as kicks 4 and 5 in standard SRS. The others account for the fact that pulling up 2 isn't always enough.
        // -1,0,  -2,0: The reverse of "off the wall" kicks for vertical to horizontal. Without these kicks, pieces may not rotate when against the edge of the board.
        KICKTABLE_SRS_2X4_3X4[(int)RotState.NAT, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(-1, 2), new Vector2Int(0, -2), new Vector2Int(-1, -2), new Vector2Int(0, -3), new Vector2Int(-1, -3), new Vector2Int(-2, -3), new Vector2Int(1, 0), new Vector2Int(2, 0) };
        KICKTABLE_SRS_2X4_3X4[(int)RotState.CWI, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(1, -2), new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(0, 3), new Vector2Int(1, 3), new Vector2Int(2, 3), new Vector2Int(-1, 0), new Vector2Int(-2, 0) };
        KICKTABLE_SRS_2X4_3X4[(int)RotState.CWI, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(1, -2), new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(0, 3), new Vector2Int(1, 3), new Vector2Int(2, 3), new Vector2Int(-1, 0), new Vector2Int(-2, 0) };
        KICKTABLE_SRS_2X4_3X4[(int)RotState.TWO, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(-1, 2), new Vector2Int(0, -2), new Vector2Int(-1, -2), new Vector2Int(0, -3), new Vector2Int(-1, -3), new Vector2Int(-2, -3), new Vector2Int(1, 0), new Vector2Int(2, 0) };
        KICKTABLE_SRS_2X4_3X4[(int)RotState.TWO, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, 2), new Vector2Int(0, -2), new Vector2Int(1, -2), new Vector2Int(0, -3), new Vector2Int(1, -3), new Vector2Int(2, -3), new Vector2Int(-1, 0), new Vector2Int(-2, 0) };
        KICKTABLE_SRS_2X4_3X4[(int)RotState.CCW, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(-1, -2), new Vector2Int(0, 2), new Vector2Int(-1, 2), new Vector2Int(0, 3), new Vector2Int(-1, 3), new Vector2Int(-2, 3), new Vector2Int(1, 0), new Vector2Int(2, 0) };
        KICKTABLE_SRS_2X4_3X4[(int)RotState.CCW, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(-1, -2), new Vector2Int(0, 2), new Vector2Int(-1, 2), new Vector2Int(0, 3), new Vector2Int(-1, 3), new Vector2Int(-2, 3), new Vector2Int(1, 0), new Vector2Int(2, 0) };
        KICKTABLE_SRS_2X4_3X4[(int)RotState.NAT, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, 2), new Vector2Int(0, -2), new Vector2Int(1, -2), new Vector2Int(0, -3), new Vector2Int(1, -3), new Vector2Int(2, -3), new Vector2Int(-1, 0), new Vector2Int(-2, 0) };
        // This 180 kicktable is taken, with permission, from TETR.IO. Thanks, osk! As there is little precident or documented design philosophy behind 180 kicks, I'm just going to use this kicktable for every piece (except big pieces, which use the same kicktable but doubled).
        KICKTABLE_SRS_2X4_3X4[(int)RotState.NAT, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, 0), new Vector2Int(-1, 0) };
        KICKTABLE_SRS_2X4_3X4[(int)RotState.TWO, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(-1, -1), new Vector2Int(1, -1), new Vector2Int(-1, 0), new Vector2Int(1, 0) };
        KICKTABLE_SRS_2X4_3X4[(int)RotState.CWI, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 2), new Vector2Int(1, 1), new Vector2Int(0, 2), new Vector2Int(0, 1) };
        KICKTABLE_SRS_2X4_3X4[(int)RotState.CCW, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, 2), new Vector2Int(-1, 1), new Vector2Int(0, 2), new Vector2Int(0, 1) };

        // This is a custom kicktable for the I5 pentomino. It is based broadly on the I4 kicktable. It attempts to place the center of the rotated piece on each of the pre-rotated pieces minoes, then it tries to rotate around the outermost minoes.
        KICKTABLE_I5[(int)RotState.NAT, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(-2, 0), new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(2, 2), new Vector2Int(-2, -2) };
        KICKTABLE_I5[(int)RotState.CWI, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 2), new Vector2Int(0, -2), new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(2, 2), new Vector2Int(-2, -2) };
        KICKTABLE_I5[(int)RotState.CWI, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 2), new Vector2Int(0, -2), new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(-2, 2), new Vector2Int(2, -2) };
        KICKTABLE_I5[(int)RotState.TWO, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-2, 0), new Vector2Int(2, 0), new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(-2, 2), new Vector2Int(2, -2) };
        KICKTABLE_I5[(int)RotState.TWO, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(-2, 0), new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(2, 2), new Vector2Int(-2, -2) };
        KICKTABLE_I5[(int)RotState.CCW, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 2), new Vector2Int(0, -2), new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(2, 2), new Vector2Int(-2, -2) };
        KICKTABLE_I5[(int)RotState.CCW, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 2), new Vector2Int(0, -2), new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(-2, 2), new Vector2Int(2, -2) };
        KICKTABLE_I5[(int)RotState.NAT, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-2, 0), new Vector2Int(2, 0), new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(-2, 2), new Vector2Int(2, -2) };
        // The I5 pentomino doesn't need a 180 kicktable, since a 180 rotation just places it exactly back where it was.
        KICKTABLE_I5[(int)RotState.NAT, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0) };
        KICKTABLE_I5[(int)RotState.TWO, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0) };
        KICKTABLE_I5[(int)RotState.CWI, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0) };
        KICKTABLE_I5[(int)RotState.CCW, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0) };

        // This is a custom kicktable for the pseudo-I piece, which is just four minoes in a diagonal line. No other kicktable seems appropriate, so instead, I'm just going with a simple solution: Try the natural, then try a rotation around each mino.
        KICKTABLE_PSUEDO_I[(int)RotState.NAT, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(0, 1), new Vector2Int(0, -3), new Vector2Int(0, 3) };
        KICKTABLE_PSUEDO_I[(int)RotState.CWI, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(0, 3), new Vector2Int(0, -3) };
        KICKTABLE_PSUEDO_I[(int)RotState.CWI, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(-3, 0), new Vector2Int(3, 0) };
        KICKTABLE_PSUEDO_I[(int)RotState.TWO, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(3, 0), new Vector2Int(-3, 0) };
        KICKTABLE_PSUEDO_I[(int)RotState.TWO, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(0, 3), new Vector2Int(0, -3) };
        KICKTABLE_PSUEDO_I[(int)RotState.CCW, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(0, 1), new Vector2Int(0, -3), new Vector2Int(0, 3) };
        KICKTABLE_PSUEDO_I[(int)RotState.CCW, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(3, 0), new Vector2Int(-3, 0) };
        KICKTABLE_PSUEDO_I[(int)RotState.NAT, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(-3, 0), new Vector2Int(3, 0) };
        // The pseudo-I piece doesn't need a 180 kicktable, since a 180 rotation just places it exactly back where it was.
        KICKTABLE_PSUEDO_I[(int)RotState.NAT, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0) };
        KICKTABLE_PSUEDO_I[(int)RotState.TWO, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0) };
        KICKTABLE_PSUEDO_I[(int)RotState.CWI, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0) };
        KICKTABLE_PSUEDO_I[(int)RotState.CCW, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0) };

        // This kicktable is standard SRS, with one extra kick at the start. It is used for the P pentominoes to simulate them having evenbox around their "O piece".
        KICKTABLE_PENTA_P_A[(int)RotState.NAT, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, -2), new Vector2Int(-1, -2) };
        KICKTABLE_PENTA_P_A[(int)RotState.CWI, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, 2), new Vector2Int(1, 2) };
        KICKTABLE_PENTA_P_A[(int)RotState.CWI, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, 2), new Vector2Int(1, 2) };
        KICKTABLE_PENTA_P_A[(int)RotState.TWO, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, -2), new Vector2Int(-1, -2) };
        KICKTABLE_PENTA_P_A[(int)RotState.TWO, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(0, -2), new Vector2Int(1, -2) };
        KICKTABLE_PENTA_P_A[(int)RotState.CCW, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(0, 2), new Vector2Int(-1, 2) };
        KICKTABLE_PENTA_P_A[(int)RotState.CCW, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(0, 2), new Vector2Int(-1, 2) };
        KICKTABLE_PENTA_P_A[(int)RotState.NAT, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(0, -2), new Vector2Int(1, -2) };
        // This 180 kicktable is taken, with permission, from TETR.IO. Thanks, osk! As there is little precident or documented design philosophy behind 180 kicks, I'm just going to use this kicktable for every piece (except big pieces, which use the same kicktable but doubled).
        KICKTABLE_PENTA_P_A[(int)RotState.NAT, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, 0), new Vector2Int(-1, 0) };
        KICKTABLE_PENTA_P_A[(int)RotState.TWO, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(-1, -1), new Vector2Int(1, -1), new Vector2Int(-1, 0), new Vector2Int(1, 0) };
        KICKTABLE_PENTA_P_A[(int)RotState.CWI, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 2), new Vector2Int(1, 1), new Vector2Int(0, 2), new Vector2Int(0, 1) };
        KICKTABLE_PENTA_P_A[(int)RotState.CCW, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, 2), new Vector2Int(-1, 1), new Vector2Int(0, 2), new Vector2Int(0, 1) };

        // This kicktable is standard SRS, with one extra kick at the start. It is used for the P pentominoes to simulate them having evenbox around their "O piece".
        KICKTABLE_PENTA_P_B[(int)RotState.NAT, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, -2), new Vector2Int(-1, -2) };
        KICKTABLE_PENTA_P_B[(int)RotState.CWI, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, 2), new Vector2Int(1, 2) };
        KICKTABLE_PENTA_P_B[(int)RotState.CWI, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, 2), new Vector2Int(1, 2) };
        KICKTABLE_PENTA_P_B[(int)RotState.TWO, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, -2), new Vector2Int(-1, -2) };
        KICKTABLE_PENTA_P_B[(int)RotState.TWO, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(0, -2), new Vector2Int(1, -2) };
        KICKTABLE_PENTA_P_B[(int)RotState.CCW, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(0, 2), new Vector2Int(-1, 2) };
        KICKTABLE_PENTA_P_B[(int)RotState.CCW, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(0, 2), new Vector2Int(-1, 2) };
        KICKTABLE_PENTA_P_B[(int)RotState.NAT, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(0, -2), new Vector2Int(1, -2) };
        // This 180 kicktable is taken, with permission, from TETR.IO. Thanks, osk! As there is little precident or documented design philosophy behind 180 kicks, I'm just going to use this kicktable for every piece (except big pieces, which use the same kicktable but doubled).
        KICKTABLE_PENTA_P_B[(int)RotState.NAT, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, 0), new Vector2Int(-1, 0) };
        KICKTABLE_PENTA_P_B[(int)RotState.TWO, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(-1, -1), new Vector2Int(1, -1), new Vector2Int(-1, 0), new Vector2Int(1, 0) };
        KICKTABLE_PENTA_P_B[(int)RotState.CWI, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 2), new Vector2Int(1, 1), new Vector2Int(0, 2), new Vector2Int(0, 1) };
        KICKTABLE_PENTA_P_B[(int)RotState.CCW, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, 2), new Vector2Int(-1, 1), new Vector2Int(0, 2), new Vector2Int(0, 1) };


        // This is a custom kicktable for the big h piece. The kicks, in order, are: 1) Natural rotation, 2) Rotate around center mino, 3) Rotate around the empty 2x2 box, 4) Rotate around the 3-way junction mino, 5) Rotate around the 2-way junction mino
        KICKTABLE_BIG_H[(int)RotState.NAT, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(1, -1), new Vector2Int(-2, -1), new Vector2Int(1, 2) };
        KICKTABLE_BIG_H[(int)RotState.CWI, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(-1, 1), new Vector2Int(2, 1), new Vector2Int(-1, -2) };
        KICKTABLE_BIG_H[(int)RotState.CWI, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(-1, 2), new Vector2Int(2, -1) };
        KICKTABLE_BIG_H[(int)RotState.TWO, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, -2), new Vector2Int(-2, 1) };
        KICKTABLE_BIG_H[(int)RotState.TWO, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(-1, 1), new Vector2Int(2, 1), new Vector2Int(-1, -2) };
        KICKTABLE_BIG_H[(int)RotState.CCW, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(1, -1), new Vector2Int(-2, -1), new Vector2Int(1, 2) };
        KICKTABLE_BIG_H[(int)RotState.CCW, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, -2), new Vector2Int(-2, 1) };
        KICKTABLE_BIG_H[(int)RotState.NAT, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(-1, 2), new Vector2Int(2, -1) };
        // This 180 kicktable is taken, with permission, from TETR.IO. Thanks, osk! As there is little precident or documented design philosophy behind 180 kicks, I'm just going to use this kicktable for every piece (except big pieces, which use the same kicktable but doubled).
        KICKTABLE_BIG_H[(int)RotState.NAT, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, 0), new Vector2Int(-1, 0) };
        KICKTABLE_BIG_H[(int)RotState.TWO, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(-1, -1), new Vector2Int(1, -1), new Vector2Int(-1, 0), new Vector2Int(1, 0) };
        KICKTABLE_BIG_H[(int)RotState.CWI, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 2), new Vector2Int(1, 1), new Vector2Int(0, 2), new Vector2Int(0, 1) };
        KICKTABLE_BIG_H[(int)RotState.CCW, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, 2), new Vector2Int(-1, 1), new Vector2Int(0, 2), new Vector2Int(0, 1) };

        // This is a custom kicktable for twin pieces. Because these are very unique beasts, this kicktable is designed only to ensure that pieces can kick off of obstructions.
        // Clockwise kicks send the piece left, then right, then up. Counterclockwise kicks send the piece right, then left, then up.
        // TODO: Twin pieces could use better kicktables. Initially I conceived of "twin SRS", which had first a 0,0 kick around the whole piece's center, then the 5 SRS kicks around one of the tetromino's center of rotation, then the 5 SRS kicks around the other center, then 0,5 just for safety.
        // I didn't implement this because honestly I was too lazy to compute those kicktables.
        KICKTABLE_TWIN[(int)RotState.NAT, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-2, 0), new Vector2Int(-3, 0), new Vector2Int(-4, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(4, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3), new Vector2Int(0, 4) };
        KICKTABLE_TWIN[(int)RotState.CWI, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(4, 0), new Vector2Int(-1, 0), new Vector2Int(-2, 0), new Vector2Int(-3, 0), new Vector2Int(-4, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3), new Vector2Int(0, 4) };
        KICKTABLE_TWIN[(int)RotState.CWI, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-2, 0), new Vector2Int(-3, 0), new Vector2Int(-4, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(4, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3), new Vector2Int(0, 4) };
        KICKTABLE_TWIN[(int)RotState.TWO, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(4, 0), new Vector2Int(-1, 0), new Vector2Int(-2, 0), new Vector2Int(-3, 0), new Vector2Int(-4, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3), new Vector2Int(0, 4) };
        KICKTABLE_TWIN[(int)RotState.TWO, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-2, 0), new Vector2Int(-3, 0), new Vector2Int(-4, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(4, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3), new Vector2Int(0, 4) };
        KICKTABLE_TWIN[(int)RotState.CCW, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(4, 0), new Vector2Int(-1, 0), new Vector2Int(-2, 0), new Vector2Int(-3, 0), new Vector2Int(-4, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3), new Vector2Int(0, 4) };
        KICKTABLE_TWIN[(int)RotState.CCW, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-2, 0), new Vector2Int(-3, 0), new Vector2Int(-4, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(4, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3), new Vector2Int(0, 4) };
        KICKTABLE_TWIN[(int)RotState.NAT, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(4, 0), new Vector2Int(-1, 0), new Vector2Int(-2, 0), new Vector2Int(-3, 0), new Vector2Int(-4, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, 3), new Vector2Int(0, 4) };
        // This 180 kicktable is taken, with permission, from TETR.IO. Thanks, osk! As there is little precident or documented design philosophy behind 180 kicks, I'm just going to use this kicktable for every piece (except big pieces, which use the same kicktable but doubled).
        KICKTABLE_TWIN[(int)RotState.NAT, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, 0), new Vector2Int(-1, 0) };
        KICKTABLE_TWIN[(int)RotState.TWO, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(-1, -1), new Vector2Int(1, -1), new Vector2Int(-1, 0), new Vector2Int(1, 0) };
        KICKTABLE_TWIN[(int)RotState.CWI, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 2), new Vector2Int(1, 1), new Vector2Int(0, 2), new Vector2Int(0, 1) };
        KICKTABLE_TWIN[(int)RotState.CCW, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, 2), new Vector2Int(-1, 1), new Vector2Int(0, 2), new Vector2Int(0, 1) };

        // This is a kicktable that implements teleportation for monominoes. They are intended to teleport up to two squares away in order to plug holes. (Monominoes are a "blessed" piece so to speak, and appear during serenity periods)
        KICKTABLE_MONO[(int)RotState.NAT, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(2, 0), new Vector2Int(2, -1), new Vector2Int(2, -2), new Vector2Int(1, -2), new Vector2Int(0, -2), new Vector2Int(0, 0) };
        KICKTABLE_MONO[(int)RotState.CWI, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(-2, 0), new Vector2Int(-2, -1), new Vector2Int(-2, -2), new Vector2Int(-1, -2), new Vector2Int(0, -2), new Vector2Int(0, 0) };
        KICKTABLE_MONO[(int)RotState.CWI, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(2, 0), new Vector2Int(2, -1), new Vector2Int(2, -2), new Vector2Int(1, -2), new Vector2Int(0, -2), new Vector2Int(0, 0) };
        KICKTABLE_MONO[(int)RotState.TWO, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(-2, 0), new Vector2Int(-2, -1), new Vector2Int(-2, -2), new Vector2Int(-1, -2), new Vector2Int(0, -2), new Vector2Int(0, 0) };
        KICKTABLE_MONO[(int)RotState.TWO, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(2, 0), new Vector2Int(2, -1), new Vector2Int(2, -2), new Vector2Int(1, -2), new Vector2Int(0, -2), new Vector2Int(0, 0) };
        KICKTABLE_MONO[(int)RotState.CCW, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(-2, 0), new Vector2Int(-2, -1), new Vector2Int(-2, -2), new Vector2Int(-1, -2), new Vector2Int(0, -2), new Vector2Int(0, 0) };
        KICKTABLE_MONO[(int)RotState.CCW, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(2, 0), new Vector2Int(2, -1), new Vector2Int(2, -2), new Vector2Int(1, -2), new Vector2Int(0, -2), new Vector2Int(0, 0) };
        KICKTABLE_MONO[(int)RotState.NAT, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(-2, 0), new Vector2Int(-2, -1), new Vector2Int(-2, -2), new Vector2Int(-1, -2), new Vector2Int(0, -2), new Vector2Int(0, 0) };
        KICKTABLE_MONO[(int)RotState.NAT, (int)RotState.TWO] = new Vector2Int[] { new Vector2Int(0, -2), new Vector2Int(0, 0) };
        KICKTABLE_MONO[(int)RotState.TWO, (int)RotState.NAT] = new Vector2Int[] { new Vector2Int(0, -2), new Vector2Int(0, 0) };
        KICKTABLE_MONO[(int)RotState.CWI, (int)RotState.CCW] = new Vector2Int[] { new Vector2Int(0, -2), new Vector2Int(0, 0) };
        KICKTABLE_MONO[(int)RotState.CCW, (int)RotState.CWI] = new Vector2Int[] { new Vector2Int(0, -2), new Vector2Int(0, 0) };

        //// Piece shape key: # = mino, @ = center-of-rotation mino, * = center-of-rotation without a mino on it
        
        // Piece ID quick reference:
        // Tetrominoes: 0-6
        // Pentominoes: 10-27
        // Pseudo-tetrominoes: 30-56
        // Big pieces: 60-66
        // Twin pieces: 70-90
        // h piece: 110
        // Monomino: 111
        // Big O piece: 112
        // Big h piece: 113

        // ----- Basic Tetrominoes ; indices 0-6

        /*  I piece
         *
         *   #@##
         */
        PIECE_DATA[0] = new PieceData(
            new Vector2Int[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) },
            ROTATION_OFFSETS_EVENBOX_UL,
            KICKTABLE_SRS_PLUS_I,
            COLOR_CYAN
        );

        /*  O piece
         *
         *   ##
         *   @#
         */
        PIECE_DATA[1] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(1, 1) },
            ROTATION_OFFSETS_EVENBOX_LL,
            KICKTABLE_SRS_O,
            COLOR_YELLOW
        );

        /*  T piece
         *
         *    #
         *   #@#
         */
        PIECE_DATA[2] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(1, 0) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_MAIN,
            COLOR_PURPLE,
            STANDARD_SPIN_CHECK_OFFSETS,
            STANDARD_SPIN_CHECK_VALUES,
            STANDARD_SPIN_CHECK_MINI_THRESHOLD,
            STANDARD_SPIN_CHECK_FULL_THRESHOLD
        );

        /*  J piece
         *  
         *   #
         *   #@# 
         */
        PIECE_DATA[3] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(1, 0) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_MAIN,
            COLOR_BLUE
        );

        /*  L piece
         *  
         *     #
         *   #@# 
         */
        PIECE_DATA[4] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(1, 1), new Vector2Int(1, 0) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_MAIN,
            COLOR_ORANGE
        );

        /*  S piece
         *  
         *    ##
         *   #@ 
         */
        PIECE_DATA[5] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(1, 1), new Vector2Int(0, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_MAIN,
            COLOR_GREEN
        );

        /*  Z piece
         *  
         *   ##
         *    @# 
         */
        PIECE_DATA[6] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_MAIN,
            COLOR_RED
        );

        // ----- Pentominoes; there are 18 of these and they occupy indices 10-27
        // Of the 12 free pentominoes, six are non-chrial and only appear once in the list. The other six are chiral and have two entries for each mirroring (similar to J+L and S+Z tetrominoes).
        // All 12 free pentominoes have "canon" one-letter names. Chiral pentominoes' names are further appended with "a" or "b" to differentiate the two versions.
        //  Extra note: The Penta curse gives one pentomino from a smaller list of "nice" (easier to fit into a clean stack) pentominoes, and one pentomino from the full list.
        //  "Nice" pentominoes are labelled...well, "nice".

        /*  Fa pentomino
         *
         *   #
         *   #@#
         *    #
         */
        PIECE_DATA[10] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, -1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_3X3,
            COLOR_BLUE
        );

        /*  Fb pentomino
         *
         *     #
         *   #@#
         *    #
         */
        PIECE_DATA[11] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(1, 1), new Vector2Int(0, -1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_3X3,
            COLOR_ORANGE
        );

        /*  I5 pentomino (nice)
         *
         *   ##@##
         */
        PIECE_DATA[12] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(-1, 0), new Vector2Int(-2, 0) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_I5,
            COLOR_CYAN
        );

        /*  L5 pentomino (nice)
         *
         *      #
         *   ##@#
         */
        PIECE_DATA[13] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(-1, 0), new Vector2Int(-2, 0) },
            ROTATION_OFFSETS_RIGHT_SPAWN,
            KICKTABLE_SRS_2X4_3X4,
            COLOR_ORANGE
        );

        /*  J5 pentomino (nice)
         *
         *   #
         *   #@##
         */
        PIECE_DATA[14] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(-1, 0), new Vector2Int(-1, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_2X4_3X4,
            COLOR_BLUE
        );

        /*  Na pentomino
         *
         *   ##
         *    @##
         */
        PIECE_DATA[15] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1), new Vector2Int(-1, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_2X4_3X4,
            COLOR_RED
        );

        /*  Nb pentomino
         *
         *     ##
         *   ##@
         */
        PIECE_DATA[16] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-2, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },
            ROTATION_OFFSETS_RIGHT_SPAWN,
            KICKTABLE_SRS_2X4_3X4,
            COLOR_GREEN
        );

        /*  Pa pentomino (nice)
         *
         *   ##
         *   #@#
         */
        PIECE_DATA[17] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_PENTA_P_A,
            COLOR_YELLOW
        );

        /*  Pb pentomino (nice)
         *
         *    ##
         *   #@#
         */
        PIECE_DATA[18] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(0, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_PENTA_P_B,
            COLOR_YELLOW
        );

        /*  T5 pentomino (nice)
         *
         *    #
         *    #
         *   #@#
         */
        PIECE_DATA[19] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(0, 2) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_MAIN, //It's not a mistake that T5 uses SRS main and not SRS 3x3. The T5 pentomino can count for T-spins, and rotating around its junction mino is the most intuitive rotation system. This negates the need for the 3x3 additions.
            COLOR_PURPLE,
            STANDARD_SPIN_CHECK_OFFSETS,
            STANDARD_SPIN_CHECK_VALUES,
            STANDARD_SPIN_CHECK_MINI_THRESHOLD,
            STANDARD_SPIN_CHECK_FULL_THRESHOLD
        );

        /*  U pentomino
         *
         *   # #
         *   #@#
         */
        PIECE_DATA[20] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(-1, 1), new Vector2Int(1, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_MAIN,
            COLOR_PINK
        );

        //Quick kicktable note: I don't particularly like how I handle kicktables for pieces that are symmetrical across a diagonal, like the V and W pentominoes. They break the mirror symmetry that SRS otherwise typically has.
        //Unfortunately there is no easy fix for this. I considered just cramming both the forward and reverse kicktables (example: the NAT->CWI and the CWI->NAT) into one huge kicktable, but that kicktable would be so big as to be too permissive.
        //Another possible fix would be to consider them chiral and split them into two mirrored copies, even though they aren't actually chiral. I don't like this option either since it would overrepresent them in the list.
        //Perhaps there's a better kicktable to use with diagonal symmetry in mind. But I feel the SRS 3x3 kicktable should work well enough. TODO: Maybe look into this.

        /*  V pentomino (nice)
         *
         *   ###
         *   #*
         *   #
         */
        PIECE_DATA[21] = new PieceData(
            new Vector2Int[] { new Vector2Int(-1, -1), new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_3X3,
            COLOR_BLUE
        );

        /*  W pentomino
         *
         *    ##
         *   #@
         *   #
         */
        PIECE_DATA[22] = new PieceData(
            new Vector2Int[] { new Vector2Int(-1, -1), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_3X3,
            COLOR_RED
        );

        /*  X pentomino (this piece is evil)
         *
         *    #
         *   #@#
         *    #
         */
        PIECE_DATA[23] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_O, //(this is why it's evil)
            COLOR_PINK
        );

        /*  Ya pentomino
         *
         *     #
         *   ##@#
         */
        PIECE_DATA[24] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-2, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) },
            ROTATION_OFFSETS_RIGHT_SPAWN,
            KICKTABLE_SRS_2X4_3X4,
            COLOR_PURPLE,
            STANDARD_SPIN_CHECK_OFFSETS,
            STANDARD_SPIN_CHECK_VALUES,
            STANDARD_SPIN_CHECK_MINI_THRESHOLD,
            STANDARD_SPIN_CHECK_FULL_THRESHOLD
        );

        /*  Yb pentomino
         *
         *    #
         *   #@##
         */
        PIECE_DATA[25] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_2X4_3X4,
            COLOR_PURPLE,
            STANDARD_SPIN_CHECK_OFFSETS,
            STANDARD_SPIN_CHECK_VALUES,
            STANDARD_SPIN_CHECK_MINI_THRESHOLD,
            STANDARD_SPIN_CHECK_FULL_THRESHOLD
        );

        /*  Z5 pentomino
         *
         *     #
         *   #@#
         *   #
         */
        PIECE_DATA[26] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(1, 0), new Vector2Int(1, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_3X3,
            COLOR_ORANGE
        );

        /*  S5 pentomino
         *
         *   #
         *   #@#
         *     #
         */
        PIECE_DATA[27] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(1, 0), new Vector2Int(1, -1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_3X3,
            COLOR_BLUE
        );

        // ----- Pseudo-Tetrominoes (also known as Tetrakings or Tetraplets); there are 27 of these and they occupy indices 30-56. (Technically, there are 34 pseudo-tetrominoes, but 7 of them are also normal tetrominoes so they don't count)
        // Of the 17 free pentominoes, 7 are non-chrial and only appear once in the list. The other 10 are chiral and have two entries for each mirroring (similar to J+L and S+Z tetrominoes).
        // Unlike tetrominoes or pentominoes, pseudo-tetrominoes do not have "canon" names. I've made my own up. Most chiral pentominoes' names are further appended with "a" or "b" to differentiate the two versions.

        /*  Bumped I-a
         *
         *     #
         *   #@ #
         */
        PIECE_DATA[30] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(2, 0), new Vector2Int(1, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_2X4_3X4,
            COLOR_CYAN
        );

        /*  Bumped I-b
         *
         *    #
         *   # @#
         */
        PIECE_DATA[31] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-2, 0), new Vector2Int(1, 0), new Vector2Int(-1, 1) },
            ROTATION_OFFSETS_RIGHT_SPAWN,
            KICKTABLE_SRS_2X4_3X4,
            COLOR_CYAN
        );

        /*  Slash-a
         *
         *   #
         *    @#
         *      #
         */
        PIECE_DATA[32] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 1), new Vector2Int(1, 0), new Vector2Int(2, -1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_2X4_3X4,
            COLOR_CYAN
        );

        /*  Slash-b
         *
         *      #
         *    @#
         *   #
         */
        PIECE_DATA[33] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(2, 1), new Vector2Int(1, 0), new Vector2Int(-1, -1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_2X4_3X4,
            COLOR_CYAN
        );

        /*  Sickle-a
         *
         *     #
         *    @#
         *   #
         */
        PIECE_DATA[34] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 1), new Vector2Int(1, 0), new Vector2Int(-1, -1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_3X3,
            COLOR_ORANGE
        );

        /*  Sickle-b
         *
         *   #
         *   #@
         *     #
         */
        PIECE_DATA[35] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 1), new Vector2Int(-1, 0), new Vector2Int(1, -1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_3X3,
            COLOR_BLUE
        );

        /*  Pseudo O (this piece is very evil)
         *
         *    #
         *   #*#
         *    #
         */
        PIECE_DATA[36] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(1, 0) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_O, //(this is why it's evil)
            COLOR_YELLOW
        );

        /*  Pseudo Z
         *
         *    # #
         *   # @
         */
        PIECE_DATA[37] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-2, 0), new Vector2Int(-1, 1), new Vector2Int(1, 1) },
            ROTATION_OFFSETS_RIGHT_SPAWN,
            KICKTABLE_SRS_2X4_3X4,
            COLOR_RED
        );

        /*  Pseudo S
         *
         *   # #
         *    @ #
         */
        PIECE_DATA[38] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(2, 0), new Vector2Int(-1, 1), new Vector2Int(1, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_2X4_3X4,
            COLOR_GREEN
        );

        /*  Bracket-s
         *
         *   #  #
         *    @#
         */
        PIECE_DATA[39] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(2, 1), new Vector2Int(-1, 1), new Vector2Int(1, 0) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_2X4_3X4,
            COLOR_PINK
        );

        /*  Y4s
         *
         *   # #
         *    @
         *    #
         */
        PIECE_DATA[40] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 1), new Vector2Int(1, 1), new Vector2Int(0, -1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_3X3,
            COLOR_PINK
        );

        /*  Y4d
         *
         *    #
         *    @#
         *   #
         */
        PIECE_DATA[41] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, -1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_3X3,
            COLOR_PINK
        );

        /*  Bracket-d
         *
         *    ##
         *   #*
         *   #
         */
        PIECE_DATA[42] = new PieceData(
            new Vector2Int[] { new Vector2Int(-1, -1), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_3X3,
            COLOR_PINK
        );

        /*  Hook-a
         *
         *   #
         *   #*#
         *    #
         */
        PIECE_DATA[43] = new PieceData(
            new Vector2Int[] { new Vector2Int(-1, 1), new Vector2Int(-1, 0), new Vector2Int(0, -1), new Vector2Int(1, 0) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_3X3,
            COLOR_GREEN
        );

        /*  Hook-b
         *
         *     #
         *   #*#
         *    #
         */
        PIECE_DATA[44] = new PieceData(
            new Vector2Int[] { new Vector2Int(1, 1), new Vector2Int(-1, 0), new Vector2Int(0, -1), new Vector2Int(1, 0) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_3X3,
            COLOR_RED
        );

        /*  Skew-a
         *
         *     ##
         *   #@
         */
        PIECE_DATA[45] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(1, 1), new Vector2Int(2, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_2X4_3X4,
            COLOR_GREEN
        );

        /*  Skew-b
         *
         *   ##
         *     @#
         */
        PIECE_DATA[46] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 1), new Vector2Int(-2, 1) },
            ROTATION_OFFSETS_RIGHT_SPAWN,
            KICKTABLE_SRS_2X4_3X4,
            COLOR_RED
        );

        /*  Pseudo T
         *
         *   # #
         *    @
         *     #
         */
        PIECE_DATA[47] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 1), new Vector2Int(1, 1), new Vector2Int(1, -1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_3X3,
            COLOR_PURPLE,
            PSEUDO_T_SPIN_CHECK_OFFSETS,
            PSEUDO_T_SPIN_CHECK_VALUES,
            PSEUDO_T_SPIN_CHECK_MINI_THRESHOLD,
            PSEUDO_T_SPIN_CHECK_FULL_THRESHOLD
        );

        /*  Bent I-a
         *
         *     ##
         *    @
         *   #
         */
        PIECE_DATA[48] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(-1, -1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_2X4_3X4,
            COLOR_BLUE
        );

        /*  Bent I-b
         *
         *   ##
         *     @
         *      #
         */
        PIECE_DATA[49] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 1), new Vector2Int(-2, 1), new Vector2Int(1, -1) },
            ROTATION_OFFSETS_RIGHT_SPAWN,
            KICKTABLE_SRS_2X4_3X4,
            COLOR_ORANGE
        );

        /*  Almost I-a
         *
         *      #
         *   #@#
         */
        PIECE_DATA[50] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(2, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_2X4_3X4,
            COLOR_CYAN
        );

        /*  Almost I-b
         *
         *   #
         *    #@#
         */
        PIECE_DATA[51] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(-2, 1) },
            ROTATION_OFFSETS_RIGHT_SPAWN,
            KICKTABLE_SRS_2X4_3X4,
            COLOR_CYAN
        );

        /*  Pseudo L
         *
         *   #
         *    @ #
         *     #
         */
        PIECE_DATA[52] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(2, 0) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_2X4_3X4,
            COLOR_ORANGE
        );

        /*  Pseudo J
         *
         *      #
         *   # @
         *    #
         */
        PIECE_DATA[53] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 1), new Vector2Int(-1, -1), new Vector2Int(-2, 0) },
            ROTATION_OFFSETS_RIGHT_SPAWN,
            KICKTABLE_SRS_2X4_3X4,
            COLOR_BLUE
        );

        /*  Claw-a
         *
         *   # #
         *   #@
         */
        PIECE_DATA[54] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(1, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_MAIN,
            COLOR_YELLOW
        );

        /*  Claw-b
         *
         *   # #
         *    @#
         */
        PIECE_DATA[55] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 1), new Vector2Int(1, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_MAIN,
            COLOR_YELLOW
        );

        /*  Pseudo I
         *
         *      #
         *     #
         *    @
         *   #
         */
        PIECE_DATA[56] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(-1, -1), new Vector2Int(1, 1), new Vector2Int(2, 2) },
            ROTATION_OFFSETS_EVENBOX_LL,
            KICKTABLE_PSUEDO_I,
            COLOR_CYAN
        );

        // ----- Big Tetrominoes
        // Like normal tetrominoes, there are 7 of these, and they occupy indices 60-66.

        /*  Big I
         *
         *   ########
         *   ###@####
         */
        PIECE_DATA[60] = new PieceData(
            new Vector2Int[] { new Vector2Int(-3, 0), new Vector2Int(-2, 0), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(4, 0), new Vector2Int(-3, 1), new Vector2Int(-2, 1), new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(3, 1), new Vector2Int(4, 1)},
            ROTATION_OFFSETS_EVENBOX_UL,
            KICKTABLE_BIG_SRS_PLUS_I,
            COLOR_CYAN
        );

        /*  Big T
         *  
         *     ##
         *     ##
         *   ##@###
         *   ######
         */
        PIECE_DATA[61] = new PieceData(
            new Vector2Int[] { new Vector2Int(-2, -1), new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1), new Vector2Int(2, -1), new Vector2Int(3, -1), new Vector2Int(-2, 0), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(0, 2), new Vector2Int(1, 2) },
            ROTATION_OFFSETS_EVENBOX_UL,
            KICKTABLE_BIG_SRS_MAIN,
            COLOR_PURPLE,
            BIG_T_SPIN_CHECK_OFFSETS,
            STANDARD_SPIN_CHECK_VALUES,
            STANDARD_SPIN_CHECK_MINI_THRESHOLD,
            STANDARD_SPIN_CHECK_FULL_THRESHOLD
        );

        /*  Big O
         *  
         *   ####
         *   ####
         *   #@##
         *   ####
         */
        PIECE_DATA[62] = new PieceData(
            new Vector2Int[] { new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1), new Vector2Int(2, -1), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(-1, 2), new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2) },
            ROTATION_OFFSETS_EVENBOX_LL,
            KICKTABLE_SRS_O,
            COLOR_YELLOW
        );

        /*  Big J
         *  
         *   ##
         *   ##
         *   ##@###
         *   ######
         */
        PIECE_DATA[63] = new PieceData(
            new Vector2Int[] { new Vector2Int(-2, -1), new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1), new Vector2Int(2, -1), new Vector2Int(3, -1), new Vector2Int(-2, 0), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(-2, 1), new Vector2Int(-1, 1), new Vector2Int(-2, 2), new Vector2Int(-1, 2) },
            ROTATION_OFFSETS_EVENBOX_UL,
            KICKTABLE_BIG_SRS_MAIN,
            COLOR_BLUE
        );

        /*  Big L
         *  
         *       ##
         *       ##
         *   ##@###
         *   ######
         */
        PIECE_DATA[64] = new PieceData(
            new Vector2Int[] { new Vector2Int(-2, -1), new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1), new Vector2Int(2, -1), new Vector2Int(3, -1), new Vector2Int(-2, 0), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(2, 1), new Vector2Int(3, 1), new Vector2Int(2, 2), new Vector2Int(3, 2) },
            ROTATION_OFFSETS_EVENBOX_UL,
            KICKTABLE_BIG_SRS_MAIN,
            COLOR_ORANGE
        );

        /*  Big S
         *  
         *     ####
         *     ####
         *   ##@#
         *   ####
         */
        PIECE_DATA[65] = new PieceData(
            new Vector2Int[] { new Vector2Int(-2, -1), new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1), new Vector2Int(-2, 0), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(3, 1), new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2), new Vector2Int(3, 2) },
            ROTATION_OFFSETS_EVENBOX_UL,
            KICKTABLE_BIG_SRS_MAIN,
            COLOR_GREEN
        );

        /*  Big Z
         *  
         *   ####
         *   ####
         *     @###
         *     ####
         */
        PIECE_DATA[66] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, -1), new Vector2Int(1, -1), new Vector2Int(2, -1), new Vector2Int(3, -1), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(-2, 1), new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(-2, 2), new Vector2Int(-1, 2), new Vector2Int(0, 2), new Vector2Int(1, 2) },
            ROTATION_OFFSETS_EVENBOX_UL,
            KICKTABLE_BIG_SRS_MAIN,
            COLOR_RED
        );

        // ----- Twin pieces; there are currently 21 of these (3 for each tetromino) and they occupy indices 70-90. Space is reserved for 40 of them in total if I want to add more.
        // There are WAY too many ways to stick two of the same piece together, so I've chosen only three for each piece. (Except O piece. There are actually ONLY three ways to jam them together.)
        // 

        /*  Twin I 1
         *  
         *   ####
         *     @###
         */
        PIECE_DATA[70] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(-2, 1), new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1) },
            ROTATION_OFFSETS_EVENBOX_LL,
            KICKTABLE_TWIN,
            COLOR_CYAN
        );

        /*  Twin I 2
         *  
         *     ####
         *   ##@#
         */
        PIECE_DATA[71] = new PieceData(
            new Vector2Int[] { new Vector2Int(-2, 0), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(3, 1) },
            ROTATION_OFFSETS_EVENBOX_LL,
            KICKTABLE_TWIN,
            COLOR_CYAN
        );

        /*  Twin I 3
         *  
         *   ###@####
         */
        PIECE_DATA[72] = new PieceData(
            new Vector2Int[] { new Vector2Int(-3, 0), new Vector2Int(-2, 0), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(4, 0) },
            ROTATION_OFFSETS_EVENBOX_UL,
            KICKTABLE_TWIN,
            COLOR_CYAN
        );

        /*  Twin O 1
         *  
         *   ##
         *   #@##
         *     ##
         */
        PIECE_DATA[73] = new PieceData(
            new Vector2Int[] { new Vector2Int(1, -1), new Vector2Int(2, -1), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_TWIN,
            COLOR_YELLOW
        );

        /*  Twin O 2
         *  
         *     ##
         *   #@##
         *   ##
         */
        PIECE_DATA[74] = new PieceData(
            new Vector2Int[] { new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(1, 1), new Vector2Int(2, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_TWIN,
            COLOR_YELLOW
        );

        /*  Twin O 3
         *  
         *   ####
         *   #@##
         */
        PIECE_DATA[75] = new PieceData(
            new Vector2Int[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1) },
            ROTATION_OFFSETS_EVENBOX_LL,
            KICKTABLE_TWIN,
            COLOR_YELLOW
        );

        /*  Twin T 1
         *  
         *   #  #
         *   #@##
         *   #  #
         */
        PIECE_DATA[76] = new PieceData(
            new Vector2Int[] { new Vector2Int(-1, -1), new Vector2Int(2, -1), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(-1, 1), new Vector2Int(2, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_TWIN,
            COLOR_PURPLE
        );

        /*  Twin T 2
         *  
         *    ##
         *   #@##
         *    ##
         */
        PIECE_DATA[77] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, -1), new Vector2Int(1, -1), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_TWIN,
            COLOR_PURPLE
        );

        /*  Twin T 3
         *  
         *     #
         *    ###
         *   #@#
         *    #
         */
        PIECE_DATA[78] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(1, 2) },
            ROTATION_OFFSETS_EVENBOX_LL,
            KICKTABLE_TWIN,
            COLOR_PURPLE
        );

        /*  Twin J 1
         *  
         *    ###
         *    @#
         *   ###   
         */
        PIECE_DATA[79] = new PieceData(
            new Vector2Int[] { new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_TWIN,
            COLOR_BLUE
        );

        /*  Twin J 2
         *  
         *    #
         *    ###
         *   #@#
         *     #
         */
        PIECE_DATA[80] = new PieceData(
            new Vector2Int[] { new Vector2Int(1, -1), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(0, 2) },
            ROTATION_OFFSETS_EVENBOX_LL,
            KICKTABLE_TWIN, //Interesting note: since this piece has 4-way point symmetry, and rotates around that symmetry, it'll only ever use the 0,0 kick.
            COLOR_BLUE
        );

        /*  Twin J 3
         *  
         *   #  
         *   ##@###
         *        #
         */
        PIECE_DATA[81] = new PieceData(
            new Vector2Int[] { new Vector2Int(3, -1), new Vector2Int(-2, 0), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(-2, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_TWIN,
            COLOR_BLUE
        );

        /*  Twin L 1
         *  
         *   ###
         *    @#
         *    ###   
         */
        PIECE_DATA[82] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, -1), new Vector2Int(1, -1), new Vector2Int(2, -1), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_TWIN,
            COLOR_ORANGE
        );

        /*  Twin L 2
         *  
         *     #
         *   ###
         *    @##
         *    #
         */
        PIECE_DATA[83] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(1, 2) },
            ROTATION_OFFSETS_EVENBOX_LL,
            KICKTABLE_TWIN, //Interesting note: since this piece has 4-way point symmetry, and rotates around that symmetry, it'll only ever use the 0,0 kick.
            COLOR_ORANGE
        );

        /*  Twin L 3
         *  
         *        #  
         *   ##@###
         *   #
         */
        PIECE_DATA[84] = new PieceData(
            new Vector2Int[] { new Vector2Int(-2, -1), new Vector2Int(-2, 0), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(3, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_TWIN,
            COLOR_ORANGE
        );

        /*  Twin S 1
         *  
         *    ####  
         *   ##@#
         */
        PIECE_DATA[85] = new PieceData(
            new Vector2Int[] { new Vector2Int(-2, 0), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_TWIN,
            COLOR_GREEN
        );

        /*  Twin S 2
         *  
         *   #
         *   ###  
         *    @##
         *      #
         */
        PIECE_DATA[86] = new PieceData(
            new Vector2Int[] { new Vector2Int(2, -1), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(-1, 2) },
            ROTATION_OFFSETS_EVENBOX_LL,
            KICKTABLE_TWIN,
            COLOR_GREEN
        );

        /*  Twin S 3
         *  
         *   # #
         *   #@##  
         *    # #
         */
        PIECE_DATA[87] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, -1), new Vector2Int(2, -1), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(-1, 1), new Vector2Int(1, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_TWIN,
            COLOR_GREEN
        );

        /*  Twin Z 1
         *  
         *   ####  
         *    #@##
         */
        PIECE_DATA[88] = new PieceData(
            new Vector2Int[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(-2, 1), new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_TWIN,
            COLOR_RED
        );

        /*  Twin Z 2
         *  
         *      #
         *    ###  
         *   #@#
         *   #
         */
        PIECE_DATA[89] = new PieceData(
            new Vector2Int[] { new Vector2Int(-1, -1), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(2, 2) },
            ROTATION_OFFSETS_EVENBOX_LL,
            KICKTABLE_TWIN,
            COLOR_RED
        );

        /*  Twin Z 3
         *  
         *    # #
         *   #@##  
         *   # #
         */
        PIECE_DATA[90] = new PieceData(
            new Vector2Int[] { new Vector2Int(-1, -1), new Vector2Int(1, -1), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1), new Vector2Int(2, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_TWIN,
            COLOR_RED
        );

        // ----- Special pieces: These occupy indices from 110 onwards.

        /*  h piece
         *  
         *   #
         *   #@#
         *   # #
         */
        PIECE_DATA[110] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(-1, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_3X3,
            COLOR_PINK
        );

        /*  Monomino
         *  
         *   @
         */
        PIECE_DATA[111] = new PieceData(
            new Vector2Int[] { new Vector2Int(0, 0) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_MONO,
            COLOR_PINK
        );

        /*  Big (but not BIG) O piece
         *  
         *   ###
         *   #*#
         *   ###
         */
        PIECE_DATA[112] = new PieceData(
            new Vector2Int[] { new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1), new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1) },
            ROTATION_OFFSETS_NONE,
            KICKTABLE_SRS_O,
            COLOR_YELLOW
        );

        /*  Big h piece
         *  
         *   #
         *   ####
         *   #* #
         *   #  #
         */
        PIECE_DATA[113] = new PieceData(
            new Vector2Int[] { new Vector2Int(-1, -1), new Vector2Int(2, -1), new Vector2Int(-1, 0), new Vector2Int(2, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(-1, 2) },
            ROTATION_OFFSETS_EVENBOX_LL,
            KICKTABLE_BIG_H,
            COLOR_PINK
        );
    }
}

//Structure for piece identification. 
public struct BagPiece
{
    public int pieceID;
    public int pieceProperties; //A list of bit flags for special properties of the piece. Those bit flag definitions follow.
    public const int LAST_IN_BAG = 0x01;
    public const int DISGUISED = 0x02;
    public const int FLOATING = 0x04;
    public const int HARD = 0x08;
    public const int EMPTY_HOLD = 0x10; //A hacky way of indicating that the piece in hold doesn't actually exist. I ran into problems checking if the hold piece's piecePrototype was null, so instead I decided to make it never null and just have this property if hold is empty.

    public BagPiece(int id, int props)
    {
        pieceID = id;
        pieceProperties = props;
    }

    //Most (All?) new pieces are going to be created with no properties, so making property optional seems wise.
    public BagPiece(int id)
    {
        pieceID = id;
        pieceProperties = 0;
    }

    public bool HasProperty(int property)
    {
        if ((pieceProperties & property) != 0) //Bitwise AND comparison; note that if HasProperty() is passed a value with multiple bits set, it will return true if ANY of them are present.
        {
            return true;
        }
        return false;
    }

    public bool HasAnyCursedProperty()
    {
        return HasProperty(DISGUISED | FLOATING | HARD);
    }

    public void AddProperty(int property)
    {
        pieceProperties |= property; //Bitwise OR assignment
    }
}

public enum RotState
{
    NAT, //Natural
    CWI, //ClockWIse just to make is three characters like the rest
    TWO, //180 is TWO rotations (in either direction)
    CCW  //CounterClockWise
}