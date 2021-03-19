using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* PieceCell class
 * This class has one, and only one job: updating the sprite to be displayed in a single cell on the active piece.
 */

public class PieceCell : MonoBehaviour
{
    public int minoNum;
    public bool isGhost = false;
    private SpriteRenderer ref_SpriteRenderer;
    private Piece ref_Piece;
    private GameObject ref_OrchestratorObj;
    private Orchestrator ref_Orchestrator;
    public const float GHOST_ALPHA = 0.4f;



    // Start is called before the first frame update
    void Start()
    {
        ref_SpriteRenderer = GetComponent<SpriteRenderer>();
        ref_Piece = GetComponentInParent<Piece>();
        ref_OrchestratorObj = GameObject.Find("Orchestrator");
        ref_Orchestrator = ref_OrchestratorObj.GetComponent<Orchestrator>(); //Locate the orchestrator to access the list of textures.
    }

    // Update is called once per frame
    void Update()
    {
        //Check to see if this mino even exists in the given piece
        if (minoNum >= ref_Piece.displayMinoes.Length)
        {
            ref_SpriteRenderer.sprite = ref_Orchestrator.minoSprites[0];
            ref_SpriteRenderer.color = Color.clear; //Set own sprite to transparent
        }
        //Mino exists, render it
        else
        {
            int cellState = ref_Piece.texture; //Get cell color        
            ref_SpriteRenderer.sprite = ref_Orchestrator.minoSprites[cellState]; //Set own sprite
            if (isGhost)
            {
                
                ref_SpriteRenderer.color = new Color(1, 1, 1, GHOST_ALPHA); //Set own sprite to "ghosty" visibility
            }
            else
            {
                if (ref_Piece.GetType() == typeof(ActivePiece))
                {
                    float brightness = 0.8f * ((float)ref_Piece.lockDelayTimer / ref_Piece.baseLockDelay) + 0.2f;
                    ref_SpriteRenderer.color = new Color(brightness, brightness, brightness, 1);
                }
                else
                {
                    ref_SpriteRenderer.color = Color.white; //Set own sprite to visible
                }
            }
            float posX = ref_Piece.displayMinoes[minoNum].x * Piece.MINO_SIZE;
            float posY = ref_Piece.displayMinoes[minoNum].y * Piece.MINO_SIZE;
            if (isGhost)
            {
                posY -= (ref_Piece.ghostOffset * Piece.MINO_SIZE);
            }
            transform.localPosition = new Vector3(posX, posY, 0); //Set position in the mino
        }
    }
}
