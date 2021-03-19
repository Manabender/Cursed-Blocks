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
    public readonly int[] NICE_PENTOMINOES = new int[] { 12, 13, 14, 17, 18, 19, 21 }; //Pentomino curse gives one of these, then one of *any* pentomino. Includes I5, J5, L5, Pa, Pb, T5, and V.
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
    public const int BIG_O_PIECE_INDEX = 112;
    public const int BIG_H_PIECE_INDEX = 113;
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

    // BuildNewBag() generates a bag of pieces to be added to the queue.
    public void BuildNewBag()
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
            for (int i = 0; i < MONOMINOES_PER_SERENE_BAG; i++)
            {
                bag.Add(new BagPiece(MONOMINO_PIECE_INDEX));
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

    //A function to make the conditionals in BuildNewBag cleaner.
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