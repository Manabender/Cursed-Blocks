using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* BoardCell class
 * This class has one, and only one job: updating the sprite to be displayed in a single cell on the board.
 */

public class BoardCell : MonoBehaviour
{
    public int cellx;
    public int celly;
    private SpriteRenderer ref_SpriteRenderer;
    private Board ref_Board;
    private GameObject ref_OrchestratorObj;
    private Orchestrator ref_Orchestrator;



    // Start is called before the first frame update
    void Start()
    {
        ref_SpriteRenderer = GetComponent<SpriteRenderer>();
        ref_Board = GetComponentInParent<Board>();
        ref_OrchestratorObj = GameObject.Find("Orchestrator");
        ref_Orchestrator = ref_OrchestratorObj.GetComponent<Orchestrator>(); //Locate the orchestrator to access the list of textures.
    }

    // Update is called once per frame
    void Update()
    {
        //Get cell state
        int cellState = ref_Board.displayCells[cellx, celly];
        //Set own sprite
        ref_SpriteRenderer.sprite = ref_Orchestrator.minoSprites[cellState];
    }
}
