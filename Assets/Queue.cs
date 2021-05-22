using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/* Queue class
 * This class is responsible for handling the queue of next pieces to come.
 */

public class Queue : MonoBehaviour
{
    public Queue<BagPiece> nextPieces; //The queue of pieces! 
    public const int INITIAL_QUEUE_SIZE = 800; //In order to keep the queue from doing memory- and time-consuming reallocation, give it plenty of space to work with from the start.
    public const int INITIAL_BAG_LIST_SIZE = 30; //See above.
    public const int NUMBER_OF_PREVIEWS = 5; //The number of next pieces to display.
    public const float END_OF_BAG_INDICATOR_X_OFFSET = 70; //A UI alignment thing.
    public GameObject queuePiecePrefab;
    public GameObject ref_endOfBagIndicator;
    public SpriteRenderer ref_endOfBagIndicatorSprite;
    public SpriteRenderer ref_mirrorIndicatorSprite;
    public QueuePiece[] queuePieceRefs = new QueuePiece[NUMBER_OF_PREVIEWS];
    public Orchestrator ref_Orchestrator;
    public Text ref_IncomingGarbageText;
    public RectTransform ownTransform;
    public int aaaaa = 0; //Please don't ask why this is here. It shouldn't be here. (It was used for testing purposes.) TODO: Remove, replace with countdown timer

    public readonly int[] DROUGHT_FLOOD_PIECES = new int[] { 1, 3, 4, 5, 6 }; //The pieces that drought and flood can add. Basically all tetrominoes except I and T.
    public readonly int[] NICE_PENTOMINOES = new int[] { 12, 13, 14, 17, 18, 19, 21, 24, 25 }; //Pentomino curse gives one of these, then one of *any* pentomino. Includes I5, J5, L5, Pa, Pb, T5, V, Ya, and Yb.
    public readonly int[] VERY_NICE_PSEUDOS = new int[] { 32, 33, 39, 41, 42, 45, 46 }; //Pseudos are classified by their parity for the purpose of pseudo modes. "Very nice" pseudos have 2-2 parity; that is, if placed on a checkerboard, they would cover 2 squares of each color.
    public readonly int[] NICE_PSEUDOS = new int[] { 30, 31, 34, 35, 40, 43, 44, 48, 49, 50, 51, 54, 55 }; //"Nice" pseudos have 3-1 parity; on a checkerboard, they would cover 3 squares of one color and 1 of the other.
    public readonly int[] MEAN_PSEUDOS = new int[] { 36, 37, 38, 47, 52, 53, 56 }; //"Mean" pseudos have 4-0 parity; on a checkerboard, they would cover 4 squares of the same color every time.
    public readonly int[] TETRA_BAG = new int[] { 0, 1, 2, 3, 4, 5, 6 };
    public readonly int[] PENTA_BAG = new int[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27 };
    //public readonly int[] PSEUDO_BAG = new int[] { 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56 };
    public const int PENTOMINOES_START_INDEX = 10;
    public const int PENTOMINOES_END_INDEX = 27;
    public const int PSEUDO_START_INDEX = 30;
    public const int PSEUDO_END_INDEX = 56;
    public const int BIG_START_INDEX = 60;
    public const int BIG_END_INDEX = 66;
    public const int TWIN_START_INDEX = 70;
    public const int TWIN_END_INDEX = 90;
    public const int H_PIECE_INDEX = 110;
    public const int MONOMINO_PIECE_INDEX = 111;
    public const int MIRROR_MONOMINO_PIECE_INDEX = 112;
    public const int BIG_O_PIECE_INDEX = 113;
    public const int BIG_H_PIECE_INDEX = 114;
    public const int DISGUISES_PER_BAG = 3;
    public const int HARDS_PER_BAG = 1;
    public const int FLOATINGS_PER_BAG = 1;
    public const int MONOMINOES_PER_SERENE_BAG = 2;


    // Start is called before the first frame update
    void Start()
    {
        ref_Orchestrator.ref_CurseManager.ResetObject(); //This ensures the Curse Manager is ready to add curses before trying to make it add curses.
        ResetObject();
        //Initialize QueuePieces for display
        for (int i = 0; i < NUMBER_OF_PREVIEWS; i++)
        {
            GameObject newQueuePiece = Instantiate(queuePiecePrefab, transform); //transform argument sets self as parent
            queuePieceRefs[i] = newQueuePiece.GetComponent<QueuePiece>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (aaaaa == 0)
        {
            LateGameInit();
        }
    }

    // aaaaaaaaaaaaaaaaaaaaaaaaa if this function is still here in a final build I've done something terribly wrong, this is supposed to be stuff that might normally be called in Start() but depends on other objects' Start()s.
    public void LateGameInit()
    {
        aaaaa = 1;
        //TESTING ONLY: Set up the queue displays
        UpdateDisplays();
        //ALSO TESTING ONLY: Spawn the first piece
        ref_Orchestrator.SpawnNextPiece();
        ref_Orchestrator.UpdateSpecificCurseDisplays();
        ref_Orchestrator.UpdateScoreMultiplier();
        ref_Orchestrator.UpdateActiveCursesStats();
        /*for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                ref_Orchestrator.ref_Board.cells[x, y] = 1;
            }
        }
        ref_Orchestrator.ref_Board.cells[5, 0] = 0;
        ref_Orchestrator.ref_Board.cells[5, 1] = 0;
        ref_Orchestrator.ref_Board.cells[5, 2] = 0;
        ref_Orchestrator.ref_Board.cells[5, 3] = 0; //This chunk of code was for setting up a board state for testing a tspin quad using a Yb pentomino. It worked first try. Yay!
        ref_Orchestrator.ref_Board.cells[4, 1] = 0;
        ref_Orchestrator.ref_Board.cells[6, 4] = 1;
        ref_Orchestrator.ref_Board.cells[4, 5] = 1;
        ref_Orchestrator.ref_Board.cells[5, 5] = 1;
        ref_Orchestrator.ref_Board.cells[6, 5] = 1;*/

        /*for (int y = 0; y < 2; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                ref_Orchestrator.ref_Board.cells[x, y] = 1;
            }
        }
        ref_Orchestrator.ref_Board.cells[3, 0] = 0;
        ref_Orchestrator.ref_Board.cells[3, 1] = 0;
        ref_Orchestrator.ref_Board.cells[4, 1] = 0;
        ref_Orchestrator.ref_Board.cells[2, 2] = 1;
        ref_Orchestrator.ref_Board.cells[2, 3] = 1; //This chunk of code was for setting up a board state to see if a fin-like tspin worked with a Ya pentomino. It did.
        ref_Orchestrator.ref_Board.cells[2, 4] = 1;
        ref_Orchestrator.ref_Board.cells[3, 4] = 1;
        ref_Orchestrator.ref_Board.cells[4, 4] = 1;
        ref_Orchestrator.ref_Board.cells[5, 4] = 1;*/

        /*for (int y = 0; y < 6; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                ref_Orchestrator.ref_Board.cells[x, y] = 1;
            }
        }
        ref_Orchestrator.ref_Board.cells[2, 0] = 0;
        ref_Orchestrator.ref_Board.cells[3, 0] = 0;
        ref_Orchestrator.ref_Board.cells[2, 1] = 0;
        ref_Orchestrator.ref_Board.cells[3, 1] = 0;
        ref_Orchestrator.ref_Board.cells[2, 2] = 0;
        ref_Orchestrator.ref_Board.cells[3, 2] = 0;
        ref_Orchestrator.ref_Board.cells[2, 3] = 0;
        ref_Orchestrator.ref_Board.cells[3, 3] = 0;
        ref_Orchestrator.ref_Board.cells[2, 4] = 0;
        ref_Orchestrator.ref_Board.cells[3, 4] = 0;
        ref_Orchestrator.ref_Board.cells[2, 5] = 0;
        ref_Orchestrator.ref_Board.cells[3, 5] = 0;
        ref_Orchestrator.ref_Board.cells[4, 2] = 0;
        ref_Orchestrator.ref_Board.cells[4, 3] = 0;
        ref_Orchestrator.ref_Board.cells[5, 2] = 0;
        ref_Orchestrator.ref_Board.cells[5, 3] = 0;
        ref_Orchestrator.ref_Board.cells[1, 6] = 1;
        ref_Orchestrator.ref_Board.cells[1, 7] = 1; //This chunk of code was for setting up a board state for a T-spin sextuple. I knew it would work, I just wanted to do it.
        ref_Orchestrator.ref_Board.cells[1, 8] = 1;
        ref_Orchestrator.ref_Board.cells[2, 8] = 1;*/

        /*for (int y = 0; y < 17; y+= 2)
        {
            for (int x = 0; x < 10; x++)
            {
                ref_Orchestrator.ref_Board.cells[x, y] = 1;
            }
            ref_Orchestrator.ref_Board.cells[3, y] = 0;
        }
        for (int y = 1; y < 17; y += 4)
        {
            ref_Orchestrator.ref_Board.cells[0, y] = 1;
            ref_Orchestrator.ref_Board.cells[5, y] = 1;
            ref_Orchestrator.ref_Board.cells[6, y] = 1;
            ref_Orchestrator.ref_Board.cells[7, y] = 1;
            ref_Orchestrator.ref_Board.cells[8, y] = 1;
            ref_Orchestrator.ref_Board.cells[9, y] = 1; //hey oshi do a t spin triple
        }
        for (int y = 3; y < 17; y += 4)
        {
            ref_Orchestrator.ref_Board.cells[0, y] = 1;
            ref_Orchestrator.ref_Board.cells[1, y] = 1;
            ref_Orchestrator.ref_Board.cells[6, y] = 1;
            ref_Orchestrator.ref_Board.cells[7, y] = 1;
            ref_Orchestrator.ref_Board.cells[8, y] = 1;
            ref_Orchestrator.ref_Board.cells[9, y] = 1;
        }
        ref_Orchestrator.ref_Board.cells[5, 17] = 1;
        ref_Orchestrator.ref_Board.cells[5, 18] = 1;
        ref_Orchestrator.ref_Board.cells[4, 18] = 1;
        ref_Orchestrator.ref_Board.cells[4, 1] = 1;*/
    }

    //ResetObject is a method that appears in several object scripts that resets it to start-of-game values. IMPORTANT: Queue.ResetObject MUST be called AFTER CurseManager.ResetObject, or all the bags that Queue generates will use OLD AND INVALID GAME VARIABLES.
    public void ResetObject()
    {
        nextPieces = new Queue<BagPiece>(INITIAL_QUEUE_SIZE);
        //TODO maybe...? Initialize random seed
        for (int i = 0; i < 20; i++)
        {
            BuildNewBag();
        }
    }

    // AddBag() adds a bag (a list of pieces) to the end of the queue.
    void AddBag(BagPiece[] bag)
    {
        bag[bag.Length - 1].AddProperty(BagPiece.LAST_IN_BAG); //Mark the last piece in the bag as...well, the last piece in the bag.
        foreach (BagPiece piece in bag)
        {
            nextPieces.Enqueue(piece);
        }
    }

    // BuildNewBag() determines which bag type should be generated, then calls an appropriate function to build that particular bag.
    public void BuildNewBag()
    {
        if (PersistantVars.pVars.bagType == BagType.CURSED)
        {
            BuildCursedBag();
        }
        else if (PersistantVars.pVars.bagType == BagType.TETRA)
        {
            BuildSetBag(TETRA_BAG);
        }
        else if (PersistantVars.pVars.bagType == BagType.PENTA)
        {
            BuildSetBag(PENTA_BAG);
        }
        else if (PersistantVars.pVars.bagType == BagType.PSEUDO)
        {
            BuildPseudoBag();
        }
        else if (PersistantVars.pVars.bagType == BagType.CUSTOMBAG)
        {
            //TODO
        }
    }

    // BuildCursedBag() generates a bag of pieces to be added to the queue, in the main "cursed" game mode.
    public void BuildCursedBag()
    {
        //First, we need a new list of curses for this bag. Ask CurseManager to generate that.
        int[] bagCurses = ref_Orchestrator.ref_CurseManager.CreateNewCurses();
        List<BagPiece> bag = new List<BagPiece>(INITIAL_BAG_LIST_SIZE);
        //Add the basic tetrominoes to the list.
        for (int i = 0; i < 7; i++)
        {
            bag.Add(new BagPiece(i));
        }
        //If serenity is active, add two monominoes.
        if (bagCurses[0] < 0)
        {
            int monoIndex = ref_Orchestrator.mirrorMonominoRotation ? MIRROR_MONOMINO_PIECE_INDEX : MONOMINO_PIECE_INDEX; //This takes care of the "mirror monomino teleportation" player setting.
            for (int i = 0; i < MONOMINOES_PER_SERENE_BAG; i++)
            {
                bag.Add(new BagPiece(monoIndex));
            }
        }
        //If pentomino curse is active, add pentominoes. Adds one "nice" pentomino then any other pentomino, and never the same one twice.
        if (IsCurseActiveThisBag(bagCurses, Curse.PENTA))
        {
            int nicePentomino = NICE_PENTOMINOES[Random.Range(0, NICE_PENTOMINOES.Length)];
            int otherPentomino;
            while (true)
            {
                otherPentomino = Random.Range(PENTOMINOES_START_INDEX, PENTOMINOES_END_INDEX + 1);
                if (otherPentomino != nicePentomino)
                {
                    break;
                }
            }
            bag.Add(new BagPiece(nicePentomino));
            bag.Add(new BagPiece(otherPentomino));
        }
        //If h curse is active, add h piece.
        if (IsCurseActiveThisBag(bagCurses, Curse.SMALL_H))
        {
            bag.Add(new BagPiece(H_PIECE_INDEX));
        }
        //If drought is active, add three more tetrominoes (three unique pieces, none of which may be I or T)
        if (IsCurseActiveThisBag(bagCurses, Curse.DROUGHT))
        {
            int[] pieceList = (int[])DROUGHT_FLOOD_PIECES.Clone();
            //Sorta hacky but effective way to pick three unique pieces; just shuffle the list and use the first three. Because we only care about the first three, we can stop the shuffle after three. The shuffle itself is the ever-famous Fisher-Yates shuffle.
            for (int i = 0; i < 3; i++)
            {
                int randomIndex = Random.Range(i, 5);
                int temp = pieceList[i];
                pieceList[i] = pieceList[randomIndex];
                pieceList[randomIndex] = temp;
            }
            //Add the pieces to the bag. (Note: Technically, this loop can be merged into the one above, and also a couple lines from the above loop are unnecessary, but in this case I'm choosing clean and easy-to-read-later code over fast code.
            for (int i = 0; i < 3; i++)
            {
                bag.Add(new BagPiece(pieceList[i]));
            }
        }
        //If pseudo is active, add two pseudo pieces (which can't be two of the same piece)
        if (IsCurseActiveThisBag(bagCurses, Curse.PSEUDO))
        {
            int firstPiece = Random.Range(PSEUDO_START_INDEX, PSEUDO_END_INDEX + 1);
            int secondPiece;
            while (true)
            {
                secondPiece = Random.Range(PSEUDO_START_INDEX, PSEUDO_END_INDEX + 1);
                if (firstPiece != secondPiece)
                {
                    break;
                }
            }
            bag.Add(new BagPiece(firstPiece));
            bag.Add(new BagPiece(secondPiece));
        }
        //If twin is active, add a twinned piece.
        if (IsCurseActiveThisBag(bagCurses, Curse.TWIN))
        {
            int piece = Random.Range(TWIN_START_INDEX, TWIN_END_INDEX + 1);
            bag.Add(new BagPiece(piece));
        }
        //If big is active, add a big piece.
        if (IsCurseActiveThisBag(bagCurses, Curse.BIG))
        {
            int piece = Random.Range(BIG_START_INDEX, BIG_END_INDEX + 1);
            bag.Add(new BagPiece(piece));
        }
        //If big O is active, add a big O piece.
        if (IsCurseActiveThisBag(bagCurses, Curse.BIG_O))
        {
            bag.Add(new BagPiece(BIG_O_PIECE_INDEX));
        }
        //If big H is active, add a big H piece.
        if (IsCurseActiveThisBag(bagCurses, Curse.BIG_H))
        {
            bag.Add(new BagPiece(BIG_H_PIECE_INDEX));
        }
        //Shuffle the bag. This is done by creating a new array, picking a random value from the list, putting it in the array, removing it from the list, and repeating.
        BagPiece[] shuffledBag;
        if (IsCurseActiveThisBag(bagCurses, Curse.FLOOD)) //If Flood is active, make space for the extra Flood pieces generated after shuffle.
        {
            shuffledBag = new BagPiece[bag.Count + 3];
        }
        else
        {
            shuffledBag = new BagPiece[bag.Count];
        }
        int length = bag.Count;
        //The actual shuffle
        for (int i = 0; i < length; i++)
        {
            int index = Random.Range(0, bag.Count);
            shuffledBag[i] = bag[index];
            bag.RemoveAt(index);
        }
        //If flood is active, add three of the same (non-I, non-T) piece. Must be done AFTER shuffle so they stay together in the queue.
        if (IsCurseActiveThisBag(bagCurses, Curse.FLOOD))
        {
            //Hoo boy, you know the off-by-one error potential is bad when you have to break out pencil and paper, but I'm about 80% sure this is all correct. 
            int randomPiece = DROUGHT_FLOOD_PIECES[Random.Range(0, DROUGHT_FLOOD_PIECES.Length)];
            int randomLocation = Random.Range(0, shuffledBag.Length - 2); // Where Length-2 comes from: Start with Length becasue the flood can be inserted before any piece. Now add 1 because the flood can also be inserted at the end of the list as well. Now subtract 3 because the last three elements aren't pieces. Add 1 because Range() is exclusive on the upper bound. And subtract 1 because arrays index from 0. Len+1-3+1-1. Len-2. aaaaaaaaaaaaaaa.
            //Move everything in that randomLocation and onwards forward 3 places to make room for the flood pieces.
            for (int i = shuffledBag.Length - 1; i > randomLocation + 2; i--) // Where randomLocation+2 comes from: You need to clear out indices from randomLocation to randomLocation+2. If randomLocation+2 is clear, you're done.
            {
                shuffledBag[i] = shuffledBag[i - 3];
            }
            //Now add the flood pieces
            shuffledBag[randomLocation] = new BagPiece(randomPiece);
            shuffledBag[randomLocation + 1] = new BagPiece(randomPiece);
            shuffledBag[randomLocation + 2] = new BagPiece(randomPiece);
        }
        //Handle disguise curse: If active, three random pieces are disguised and assigned the wrong color.
        if (IsCurseActiveThisBag(bagCurses, Curse.DISGUISE))
        {
            int i = 0;
            while (i < DISGUISES_PER_BAG)
            {
                int rand = Random.Range(0, shuffledBag.Length);
                if (!shuffledBag[rand].HasAnyCursedProperty())
                {
                    shuffledBag[rand].AddProperty(BagPiece.DISGUISED);
                    i++;
                }
            }
        }
        //Handle hard curse: If active, one random piece is made hard. Hard pieces must be cleared twice. The first time they are cleared, lines above them don't fall.
        if (IsCurseActiveThisBag(bagCurses, Curse.HARD))
        {
            int i = 0;
            while (i < HARDS_PER_BAG)
            {
                int rand = Random.Range(0, shuffledBag.Length);
                if (!shuffledBag[rand].HasAnyCursedProperty())
                {
                    shuffledBag[rand].AddProperty(BagPiece.HARD);
                    i++;
                }
            }
        }
        //Handle floating curse: If active, one random pieces is made floaty. Floaty pieces lock in one mino above where they would otherwise lock in, and cannot be soft-dropped and aren't affected by gravity.
        if (IsCurseActiveThisBag(bagCurses, Curse.FLOATING))
        {
            int i = 0;
            while (i < FLOATINGS_PER_BAG)
            {
                int rand = Random.Range(0, shuffledBag.Length);
                if (!shuffledBag[rand].HasAnyCursedProperty())
                {
                    shuffledBag[rand].AddProperty(BagPiece.FLOATING);
                    i++;
                }
            }
        }
        //Commit the bag to queue
        AddBag(shuffledBag);
    }

    // BuildSetBag() generates a bag with a given set of pieces. Used in extra modes.
    public void BuildSetBag(int[] pieces)
    {
        BagPiece[] bag = new BagPiece[pieces.Length];
        for (int i = 0; i < pieces.Length; i++)
        {
            bag[i] = new BagPiece(pieces[i]);
        }
        //Shuffle using the Fisher-Yates method, a shuffle proven to be the most efficient in both time (runs in O(n)) and memory (runs in-place with only one extra "swap" variable).
        for (int i = 0; i < pieces.Length; i++)
        {
            int selectedElement = Random.Range(i, pieces.Length);
            BagPiece swap = bag[selectedElement];
            bag[selectedElement] = bag[i];
            bag[i] = swap;
        }
        AddBag(bag);
    }

    //BuildPseudoBag generates a bag for use in the Pseudo extra modes. A pseudo bag consists of all seven tetrominoes, two "very nice" pseudos, two "nice" pseudos, and one "mean" pseudo.
    public void BuildPseudoBag()
    {
        BagPiece[] bag = new BagPiece[12];
        //First, add the seven tetrominoes.
        for (int i = 0; i < TETRA_BAG.Length; i++)
        {
            bag[i] = new BagPiece(TETRA_BAG[i]);
        }
        //Add two "very nice" pseudos.
        int firstPiece = VERY_NICE_PSEUDOS[Random.Range(0, VERY_NICE_PSEUDOS.Length)];
        int secondPiece;
        while (true)
        {
            secondPiece = VERY_NICE_PSEUDOS[Random.Range(0, VERY_NICE_PSEUDOS.Length)];
            if (firstPiece != secondPiece)
            {
                break;
            }
        }
        bag[7] = new BagPiece(firstPiece);
        bag[8] = new BagPiece(secondPiece);
        //Add two "nice" pseudos.
        firstPiece = NICE_PSEUDOS[Random.Range(0, NICE_PSEUDOS.Length)];
        while (true)
        {
            secondPiece = NICE_PSEUDOS[Random.Range(0, NICE_PSEUDOS.Length)];
            if (firstPiece != secondPiece)
            {
                break;
            }
        }
        bag[9] = new BagPiece(firstPiece);
        bag[10] = new BagPiece(secondPiece);
        //Add a "mean" pseudo.
        bag[11] = new BagPiece(MEAN_PSEUDOS[Random.Range(0, MEAN_PSEUDOS.Length)]);
        //Shuffle using the Fisher-Yates method, a shuffle proven to be the most efficient in both time (runs in O(n)) and memory (runs in-place with only one extra "swap" variable).
        for (int i = 0; i < bag.Length; i++)
        {
            int selectedElement = Random.Range(i, bag.Length);
            BagPiece swap = bag[selectedElement];
            bag[selectedElement] = bag[i];
            bag[i] = swap;
        }
        AddBag(bag);
    }

    // A function to make the conditionals in BuildCursedBag cleaner.
    public bool IsCurseActiveThisBag(int[] bagCurses, Curse curse)
    {
        return bagCurses[0] >= 0 && (bagCurses[(int)curse] > 0 || PersistantVars.pVars.forcedCurses[(int)curse]); //If notSerenity and (curseActive or curseForcedActive)
    }

    // UpdateDisplays() makes all QueuePiece instances update their displays, as well as update the position of the end-of-bag indicator.
    public void UpdateDisplays()
    {
        int j = 0;
        foreach (BagPiece piece in nextPieces) //Janky loop required because apparently nextPieces[i] doesn't work since it's a queue, yet this loop does...
        {
            //Make the piece update its display.
            queuePieceRefs[j].UpdateDisplay(piece, j, ref_Orchestrator.ref_CurseManager.IsCurseActive(Curse.FOG));
            //Increment loop and check loop condition.
            j++;
            if (j >= NUMBER_OF_PREVIEWS)
            {
                break;
            }
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(ownTransform);
        //Look for end-of-bag piece.
        j = 0;
        foreach (BagPiece piece in nextPieces)
        {
            if (piece.HasProperty(BagPiece.LAST_IN_BAG))
            {
                float scale = ref_Orchestrator.ref_Canvas.transform.localScale.x; //Either I'm doing something horribly wrong (likely), or Unity isn't scaling things like I want it to...
                Vector3 indicatorPosition = queuePieceRefs[j].transform.position;
                indicatorPosition.x += (END_OF_BAG_INDICATOR_X_OFFSET * scale);
                indicatorPosition.y -= (queuePieceRefs[j].ownTransform.sizeDelta.y * scale); //Move it to the bottom of the piece...
                indicatorPosition.y += (Piece.MINO_SIZE * scale); //...but not so far it overlaps the next piece.
                ref_endOfBagIndicator.transform.SetPositionAndRotation(indicatorPosition, Quaternion.identity);
                ref_endOfBagIndicatorSprite.color = Color.white; //Set indicator to visible.
                if (ref_Orchestrator.ref_CurseManager.IsCurseActive(Curse.MIRROR))
                {
                    ref_mirrorIndicatorSprite.color = Color.white; //If mirror is active, also set its indicator to visible.
                }
                else
                {
                    ref_mirrorIndicatorSprite.color = Color.clear; //Else, hide it.
                }
                break;
            }
            //Increment loop and check loop condition.
            j++;
            if (j >= NUMBER_OF_PREVIEWS)
            {
                ref_endOfBagIndicatorSprite.color = Color.clear; //Hide indicator.
                ref_mirrorIndicatorSprite.color = Color.clear;
                break;
            }
        }
    }

    //This method updates the incoming garbage display to warn the player that garbage is coming soon. 
    public void UpdateIncomingGarbageDisplay()
    {
        //Count the amount of incoming garbage.
        int incomingGarbage = 0;
        if (ref_Orchestrator.ref_CurseManager.IsCurseActive(Curse.CHEESE))
        {
            incomingGarbage++;
        }
        if (ref_Orchestrator.ref_CurseManager.IsCurseActive(Curse.CLEAN))
        {
            incomingGarbage += 2;
        }
        if (ref_Orchestrator.ref_CurseManager.IsCurseActive(Curse.SUPERCHEESE))
        {
            incomingGarbage++;
        }
        if (ref_Orchestrator.ref_CurseManager.IsCurseActive(Curse.HYPERCHEESE))
        {
            incomingGarbage++;
        }
        //If no garbage is coming, clear the warning text.
        if (incomingGarbage == 0)
        {
            ref_IncomingGarbageText.text = "";
            return;
        }
        //Set the text to show how much garbage is coming.
        string text = "";
        for (int i = 0; i < incomingGarbage; i++)
        {
            text += ">>>";
        }
        text += "  ";
        //Set the text to show if it's coming within the next three pieces.
        int j = 0;
        foreach (BagPiece piece in nextPieces) //I really hate having to loop like this...Queue<>, why can't you just be [indexed]?
        {
            j++;
            if (piece.HasProperty(BagPiece.LAST_IN_BAG))
            {
                if (j == 1)
                {
                    text += "!!!";
                    break;
                }
                else if (j == 2)
                {
                    text += "!!";
                    break;
                }
                else //if (j == 3)
                {
                    text += "!";
                    break;
                }
            }
            if (j == 3)
            {
                break;
            }
        }
        ref_IncomingGarbageText.text = text;
    }
}

public enum BagType
{
    CURSED, //Standard 7-bag plus whatever curses are active. Note that only the CURSED AddBag function actually makes a call to CurseManager to generate curses. If any other bag type is used, that implicitly means a mode without main-game curse generation.
    TETRA, //Standard 7-bag only.
    PENTA, //Pentominoes only. Bags of 18 with one of each pentomino.
    PSEUDO, //Tetrominoes and pseudo-tetrominoes. Bags of 41, consisting of one of each pseudo-tetromino and two of each normal tetromino.
    CUSTOMBAG //User-defined bag.
}