using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject player; //track player's progress and game state to inform cells, monster, and scrolling.  
    [SerializeField] private GameObject monster; //Track how far along the monster is to determine game state, cell deletion, and player interaction


    [SerializeField] private List<GameObject> activeCells = new List<GameObject>(); //Generate and manage active cells based on monster distance and player progress
    [SerializeField] private GameObject defaultCell; //Prefab to derive all cells.  


    [Header("Scrolling Background")]
    [SerializeField] private GameObject foregroundQuad; //dynamically change the background assets based on player movement
    [SerializeField] private GameObject backgroundQuad;
    [SerializeField] private GameObject wallQuad;
    private Renderer foregroundRenderer;
    private Renderer backgroundRenderer;
    private Renderer wallRenderer;
    float backgroundSpeed = 0.02f; //Scroll speeds of each quad.  
    float wallSpeed = 0.01f;
    float foregroundSpeed = 0.03f;

    private float runDuration = 0f; //Track how long the game has lasted
    private int gameDifficulty = 3; //difficulty rating / difficulty multiplier.  Easy, medium, hard.  
    private float currentBudget = 0f; //Send budget to cells, updated over time.  

    private void clearStaleCells()
    {
        //check if monster has overtaken a cell.  
        //remove from activeCells list and delete the game object if so.  
        //else, do nothing.  
        for (int i = 0; i < activeCells.Count; i++)
        {
            if (monster.transform.position.x - activeCells[i].transform.position.x >= 50) //If the monster has passed this cell, it is no longer reachable and thus should be deleted
            {
                activeCells[i].gameObject.GetComponent<Cell>().destroyCell();
                activeCells.RemoveAt(i);
            }
        }
    }

    private void scrollBackground()
    {
        //scroll foregroundQuad.  foregroundQuad is 3x as long as the backgroundQuad
        //scroll backgroundQuad.  Background Quad is 2x as long as a single cell
        //scroll wallQuad.  Wall Quad is 1x as long as a single cell.  

        foregroundQuad.transform.position = new Vector3(player.transform.position.x, foregroundQuad.transform.position.y, foregroundQuad.transform.position.z);
        backgroundQuad.transform.position = new Vector3(player.transform.position.x, backgroundQuad.transform.position.y, backgroundQuad.transform.position.z);
        wallQuad.transform.position = new Vector3(player.transform.position.x, wallQuad.transform.position.y, wallQuad.transform.position.z);
        
        float deltaX = player.transform.position.x;

        foregroundRenderer.material.mainTextureOffset = new Vector2(deltaX * foregroundSpeed, 0f);
        backgroundRenderer.material.mainTextureOffset = new Vector2(deltaX * backgroundSpeed, 0f);
        wallRenderer.material.mainTextureOffset = new Vector2(deltaX * wallSpeed, 0f);
    }

    private void initializeCells() //create the first 5 cells.  1 behind, 1 where the player is, 3 ahead.  
    {
        GameObject monsterCell = Instantiate<GameObject>(defaultCell, new Vector3(-50f, 0f, 0f), Quaternion.identity); //the cell behind the player.  
        activeCells.Add(monsterCell);
        GameObject playerCell = Instantiate<GameObject>(defaultCell, Vector3.zero, Quaternion.identity); //the cell ahead of the player.  
        activeCells.Add(playerCell);
        GameObject thirdCell = Instantiate<GameObject>(defaultCell, new Vector3(50f, 0f, 0f), Quaternion.identity); //The next three cells ahead of the player.  
        activeCells.Add(thirdCell);
        GameObject fourthCell = Instantiate<GameObject>(defaultCell, new Vector3(100f, 0f, 0f), Quaternion.identity);
        activeCells.Add(fourthCell);
        GameObject fifthCell = Instantiate<GameObject>(defaultCell, new Vector3(150f, 0f, 0f), Quaternion.identity);
        activeCells.Add(fifthCell);



        monsterCell.GetComponent<Cell>().initializeCell(null, playerCell, 1); //generate the first five cells with a budget of one.  
        playerCell.GetComponent<Cell>().initializeCell(monsterCell, thirdCell, 1);
        thirdCell.GetComponent<Cell>().initializeCell(playerCell, fourthCell, 1);
        fourthCell.GetComponent<Cell>().initializeCell(thirdCell, fifthCell, 1);
        fifthCell.GetComponent<Cell>().initializeCell(fourthCell, null, 1);
    }

    private void createNewCells() //If the player is within 100 units of the final cell, then pre-emptively generate a new one, set its budget and randomization, and ensure that it knows which cells it connects to.  Also set the old "final" cell's rightCell
    {
        if (activeCells[activeCells.Count - 1].transform.position.x - player.transform.position.x <= 100)
        {
            //generate a new cell

            //Choose random prefab from the furthest cell's list of valid prefabs
            //then we instantiate that gameobject here.  

            Cell furthestCell = activeCells[activeCells.Count - 1].GetComponent<Cell>();
            if(furthestCell.validRightCellCount > 0) //Generate a cell from the furthest right cell's list of valid "next" cells.  
            {
                GameObject newCell = Instantiate<GameObject>(furthestCell.getRandomValidCell(), new Vector3(activeCells[activeCells.Count - 1].transform.position.x + 50, 0f, 0f), Quaternion.identity);
                activeCells.Add(newCell);
                newCell.GetComponent<Cell>().initializeCell(activeCells[activeCells.Count - 2].GetComponent<Cell>().gameObject, null, currentBudget);
                print("Gave " + newCell.name + " a budget of:  " + currentBudget);
                activeCells[activeCells.Count - 2].GetComponent<Cell>().rightCell = newCell;
            }
            else //generate the default cell if no valid right cells are available.  Should never happen.  
            {
                GameObject newCell = Instantiate<GameObject>(defaultCell, new Vector3(activeCells[activeCells.Count - 1].transform.position.x + 50, 0f, 0f), Quaternion.identity);
                activeCells.Add(newCell);
                newCell.GetComponent<Cell>().initializeCell(activeCells[activeCells.Count - 2].GetComponent<Cell>().gameObject, null, currentBudget);
                activeCells[activeCells.Count - 2].GetComponent<Cell>().rightCell = newCell;
            }
        }
    }

    private Vector3 findSolidGround()
    {
        return activeCells[1].GetComponent<Cell>().floorTiles[0].transform.position + new Vector3(1f, 6f, player.transform.position.z);
    }

    //Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        monster.GetComponent<Monster>().initializeMonster(this.player);
        
        //TODO:  REMOVE WHEN TESTING TRAP GENERATION IS DONE.  
        foregroundRenderer = foregroundQuad.GetComponent<Renderer>();
        backgroundRenderer = backgroundQuad.GetComponent<Renderer>();
        wallRenderer = wallQuad.GetComponent<Renderer>();

        //generate initial cells.  
        initializeCells();

        player.transform.position = findSolidGround();
    }

    //Update is called once per frame
    void Update()
    {
        runDuration = Time.time;
        currentBudget = 10 + (runDuration * gameDifficulty * 0.10f);
        //delete inactive cells / cells fully overtaken by the monster
        clearStaleCells();

        //update scrolling of background based on player movement
        scrollBackground();

        //add new cells if the player is nearing the edge of the generated cells.  There should be 3 ahead of them at any given time.  
        createNewCells();
    }
}
