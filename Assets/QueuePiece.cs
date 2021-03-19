using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* QueuePiece class
 * This class handles the display of a single piece in the next queue. The Queue class tells it what to do, and this class just kinda does it.
 */

public class QueuePiece : Piece
{
    public RectTransform ownTransform;
    private const int QUEUE_DISPLAY_WIDTH = 180;

    // Start is called before the first frame update
    void Start()
    {
        PopulatePieceData();
        ownTransform = transform.GetComponent<RectTransform>();
        //Set up PieceCells for display.
        for (int i = 0; i < 20; i++)
        {
            GameObject newCell = Instantiate(cellPrefab, transform); //transform argument sets self as parent
            PieceCell cellref = newCell.GetComponent<PieceCell>();
            cellref.minoNum = i;
        }
    }

    /*
    // Update is called once per frame
    void Update()
    {
        
    } */
    

    public void UpdateDisplay(BagPiece piece, int numInQueue, bool isFogActive)
    {
        Vector2Int displayOffset = new Vector2Int(0, 0); //As the piece is drawn relative to the center-of-rotation mino, it needs to be shifted right and down so it doesn't collide with other pieces or the board. This indicates how much.
        //^The above was a class field, I don't think it needs to be a class field.
        //Set base properties
        minoes = PIECE_DATA[piece.pieceID].minoes;
        texture = PIECE_DATA[piece.pieceID].DetermineQueueTexture(piece, numInQueue, isFogActive);
        //Calculate displayOffset and displayHeight
        int minX = 0; //We don't need maxX because we don't actually care how far right the piece stretches.
        int minY = 0;
        int maxY = 0;
        foreach (Vector2Int mino in minoes) //Find the leftmost, topmost, and bottommost minoes.
        {
            minX = Mathf.Min(minX, mino.x);
            minY = Mathf.Min(minY, mino.y);
            maxY = Mathf.Max(maxY, mino.y);
        }       
        //Set display height
        float displayHeight = (maxY - minY + 2) * MINO_SIZE; //Add two; one for an off-by-one error, one for padding.
        ownTransform.sizeDelta = new Vector2(QUEUE_DISPLAY_WIDTH, displayHeight);
        //Set the display offset accordingly.
        displayOffset.x = minX * -1;
        displayOffset.y = maxY * -1;
        //Set displayMinoes.
        displayMinoes = new Vector2Int[minoes.Length];
        for (int i = 0; i < minoes.Length; i++)
        {
            displayMinoes[i] = minoes[i] + displayOffset;
        }
    }
}
