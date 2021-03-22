using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* HoldPiece class
 * This class handles everything related to the hold piece, both in functionality and display.
 */

public class HoldPiece : Piece
{
    //public BagPiece heldPiece; //The held piece! //NOTE: This actually was not necessary. The identity of the held piece is now stored in the superclass field "piecePrototype".
    public int cooldown; //The number of pieces that must be dropped before hold can be used again. Usually set to 1 whenever hold is used.
    public int spikes; //The number of times that hold has been used while the Spiked Hold curse is active. Once this gets high enough, it resets to 0 and garbage is added.
    public int ephemereal; //The number of pieces that have gone by without using hold while the Ephemereal Hold curse is active. If this gets high enough, the hold piece disappears.

    public const int SPIKED_HOLD_THRESHOLD = 3; //The number of times that hold must be used while under the Spiked Hold curse to trigger adding garbage.
    public const int EPHEMEREAL_HOLD_THRESHOLD = 4; //The number of times that hold must go unused while under the Ephemereal Hold curse to trigger removing the hold piece.

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
        ResetObject();
    }

    // Update is called once per frame
    /*void Update()
    {
        
    }*/

    //ResetObject is a method that appears in several object scripts that resets it to start-of-game values.
    public void ResetObject()
    {
        piecePrototype = new BagPiece(0, BagPiece.EMPTY_HOLD); //Initialize hold with a "hold is empty" piece.
        cooldown = 0;
        spikes = 0;
        ephemereal = 0;
        UpdateDisplay();
    }

    //This method performs the actual "hold" action. The argument is the piece entering hold. The method sends the piece leaving hold (if any) to Orchestrator.SpawnPiece().
    public void HoldPieceAction(BagPiece incomingPiece)
    {
        if (cooldown <= 0)
        {
            //Check for spiked hold. Note: This must be done before the actual hold action. Were it the other way around, it would be possible to spawn a piece then push a mino up inside that spawned piece with the garbage.
            if (ref_Orchestrator.ref_CurseManager.IsCurseActive(Curse.SPIKED_HOLD))
            {
                spikes++;
                if (spikes >= SPIKED_HOLD_THRESHOLD)
                {
                    spikes = 0;
                    ref_Orchestrator.ref_Board.AddCleanGarbage();
                }
                UpdateSpikeDisplay();
            }
            //Now handle the actual hold action.
            if (piecePrototype.HasProperty(BagPiece.EMPTY_HOLD)) //If there is no hold piece...
            {
                ref_Orchestrator.SpawnNextPiece(); //...just spawn the next piece.
            }
            else //If there is a hold piece...
            {
                ref_Orchestrator.SpawnPiece(piecePrototype, true); //...Spawn that hold piece. //true argument means that the piece to be spawned DID come from hold.
            }
            piecePrototype = incomingPiece; //Place the incoming piece into hold.
            cooldown = 1 + (ref_Orchestrator.ref_CurseManager.IsCurseActive(Curse.SLOW_HOLD) ? 1 : 0); //Sets cooldown to 1 normally, or 2 if slow hold curse is active.
            UpdateDisplay();
        }
    }

    public void UpdateDisplay()
    {
        if (piecePrototype.HasProperty(BagPiece.EMPTY_HOLD)) //If there isn't actually a hold piece...
        {
            displayMinoes = new Vector2Int[0]; //...display no minoes...
            return; //...and that's literally it.
        }
        Vector2Int displayOffset = new Vector2Int(0, 0); //As the piece is drawn relative to the center-of-rotation mino, it needs to be shifted left so it doesn't collide with other pieces or the board. This indicates how much.
        //Set base properties
        minoes = PIECE_DATA[piecePrototype.pieceID].minoes;
        if (cooldown > 0)
        {
            texture = 1; //If hold is on cooldown, show the held piece appropriately.
        }
        else
        {
            texture = PIECE_DATA[piecePrototype.pieceID].DetermineTexture(piecePrototype);
        }
        //Calculate displayOffset and displayHeight
        int maxX = 0;
        int maxY = 0;
        foreach (Vector2Int mino in minoes) //Find the rightmost and topmost minoes.
        {
            maxX = Mathf.Max(maxX, mino.x);
            maxY = Mathf.Max(maxY, mino.y);
        }
        //Set the display offset accordingly.
        displayOffset.x = maxX * -1;
        displayOffset.y = maxY * -1;
        //Set displayMinoes.
        displayMinoes = new Vector2Int[minoes.Length];
        for (int i = 0; i < minoes.Length; i++)
        {
            displayMinoes[i] = minoes[i] + displayOffset;
        }
    }

    //This method updates the display for Spiked Hold.
    public void UpdateSpikeDisplay()
    {
        if (!ref_Orchestrator.ref_CurseManager.IsCurseActive(Curse.SPIKED_HOLD)) //If curse is inactive, disable the display.
        {
            ref_Orchestrator.ref_SpikedHoldText.text = "";
        }
        else
        {
            string text = "Spiked ";
            for (int i = 0; i < spikes; i++)
            {
                text += "(*)";
            }
            for (int i = spikes; i < SPIKED_HOLD_THRESHOLD; i++)
            {
                text += "( )";
            }
            ref_Orchestrator.ref_SpikedHoldText.text = text;
        }
    }

    //This method updates the display for Ephemereal Hold.
    public void UpdateEphemerealDisplay()
    {
        if (!ref_Orchestrator.ref_CurseManager.IsCurseActive(Curse.EPHEMEREAL_HOLD)) //If curse is inactive, disable the display.
        {
            ref_Orchestrator.ref_EphemerealHoldText.text = "";
        }
        else
        {
            string text = "Ephemereal [";
            for (int i = 0; i < ephemereal; i++)
            {
                text += "*";
            }
            for (int i = ephemereal; i < EPHEMEREAL_HOLD_THRESHOLD; i++)
            {
                text += ".";
            }
            text += "]";
            ref_Orchestrator.ref_EphemerealHoldText.text = text;
        }
    }
}
