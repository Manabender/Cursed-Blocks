using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* IncomingPiece class
 * This class handles the display of the mino locations where the next piece in queue will spawn.
 * Visual indicators are shown there so that the player may avoid placing a piece on top of them, which would end the game.
 */

public class IncomingPiece : Piece
{
    public const int INCOMING_MINO_TEXTURE_ID = 13;

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
        //Permanently set display texture to the incoming mino texture.
        texture = INCOMING_MINO_TEXTURE_ID;
        //Permanently set position to the spawn location.
        int posX = (ref_Orchestrator.ref_Board.width - 1) / 2;
        int posY = ref_Orchestrator.ref_Board.height + 1;
        transform.localPosition = new Vector3((posX * MINO_SIZE) + (MINO_SIZE / 2), (posY * MINO_SIZE) + (MINO_SIZE / 2), PIECE_Z - 1); //Hacky looking coordinates are needed to align the piece to the grid. //Piece Z has minus 1 so it appears on top of the active piece
    }

    //This method updates everything related to the incoming piece.
    public void UpdateIncomingPiece()
    {
        piecePrototype = ref_Orchestrator.ref_Queue.nextPieces.Peek();
        minoes = PIECE_DATA[piecePrototype.pieceID].minoes;
        Vector2Int naturalOffset = PIECE_DATA[piecePrototype.pieceID].rotationOffsets[(int)RotState.NAT];
        displayMinoes = new Vector2Int[minoes.Length];
        for (int i = 0; i < minoes.Length; i++)
        {
            displayMinoes[i] = minoes[i] + naturalOffset; //Natural rotation offset must be added because the piece will spawn with that offset.
        }
    }
}
