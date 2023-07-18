/* This class handles the flow of the battle - initialising characters, turn order etc.
 * 
 * Current Debug mapping QWER character actions
 * Alpha0   -enemy attacks in sequence
 * Alpha1-4 -player attacks in sequence
 * Alpha1-4 -player dodges in sequence
 */

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;


public class BattleManager : MonoBehaviour
{
    public static BattleManager instance;
    [Header("Game current state")]
    [Tooltip("the 'Level' the player is currently playing ")]                   public string   battleID = "1";
    [Tooltip("is the entire encounter finished? ")]                                 public bool isInBattle = true;
                                                                                    public bool isPlanningStage = true;
                                                                                    private int CurrentActionPoints;
    [Tooltip("formerly TurnCounter - whether it's enemies or player's turn to act")]public bool isEnemyTurn = false;

    //Data of player characters and enemy characters
    public List <Character> charactersPlayer =  new List<Character>(4);
    public List <Character> charactersEnemy =   new List<Character>(4);
    public List<Transform> characterPositions = new List<Transform>(8);

    //Data of player characters and enemy characters
    // shorthand from characterPositions
    //public List<GameObject> PlayerSprites;
    //public List<GameObject> EnemySprites;

    [SerializeField] Vector3 PlayerSwapPosition;
    public string SceneTranisiton;

    [Header("Object references, prefabs")]
    [Tooltip("this is the VFX spawned when hit lands on player/enemy")]     public GameObject HitMarker;
    [Tooltip("This is the default action to enact if none are assigned")]   public Action DefaultAction;

    
    public int      MaxActionPoints = 4;                        //Amount of actions and movements the player can make
    public int[]    PlayerMovementActions = { 1, 1, 1, 1 };     //each unit has a specific amonut of movement, which works off of charactersPlayer. when a movement is done, it cannot be done a gain for that unit
    public int[]    PlayerMovementDirection = { 0, 0, 0, 0 };   // keeps track of what position units have been prepped to move too, -1 = left, 1 = right, 0 = No Movement prepped.
    
    public int      MaxEnemyActions = 4;                        //Amount of Actions enemy can make
    private int     EnemyActions = 4;
    public float    EnemyAttackDelay = 2f;
    public int      EnemyCounter = 0;
    public int[]    EnemyMovementActions = { 1, 1, 1, 1 };      //each unit has a specific amonut of movement, which works off of charactersPlayer. when a movement is done, it cannot be done a gain for that unit
    public int[]    EnemyMovementDirection = { 1, -1, -1, -1 }; // keeps track of what position units have been prepped to move too, -1 = left, 1 = right, 0 = No Movement prepped.
    public int      PlayerCharacterSelection = 3;
    public bool     OutOfTokens = true;                         //For refilling player and enemy tokens, if this is true during player turn, player actions are refilled and This variable becomes false.

    //Assign instance if none found, otherwise destroy
    private void Awake()
    {
        if(instance == null) instance = this;
        else Destroy(this);
    }

    void Start()
    {
        StartEncounter("1");                        //Initialise health to full and actions from base action values.
        UIManager.instance.RefreshStatusCorners();  //Once character data is fully retrieved, update the current stats to match.
    }   

    //Turn Flow
    void Update()
    {
        if (!isInBattle) return;
        //if (!isPlanningStage) 
        {
            if (isEnemyTurn == false) { PlayerTurn(); PlayerUnitAttacking(); SelectingPlayerUnit(); }           //PreparingPlayerMovements(); 
            if (isEnemyTurn == true) { EnemyTurn(); EnemyAttacks(); PlayerReactions(); SelectingPlayerUnit(); }
        }
    }
    public void ExecuteCharacterAction(int index)=>ExecuteCharacterAction(charactersPlayer[index]);
    public void ExecuteCharacterAction(Character c)    //Execute the selected action of a given Character.
    {
        if(CheckIfValidAttacker(c.position)) c.actionChosen.Perform();
        UIManager.instance.RefreshStatusCorners();
    }
    public void PreviewCharacterAction(Transform position) { if (CheckIfActionValid(GetCharacterBySprite(position).position)) GetCharacterBySprite(position).actionChosen.Preview(true); }
    public void EndPreviewCharacterAction(int position) => GetCharacterByPosition(position).actionChosen.Preview(false);
    public void ToggleActionPreview(Character c, bool state) => c.actionChosen.Preview(state);
    
    //Convenient shorthands for retrieving one kind of data from another - character data from position, sprite by position ETC
    #region utilities
    //Gets character by their ID values
    public Character GetCharacterByID(int ID) 
    {
        for (int i = 0; i < charactersPlayer.Count; i++)
        {
            if (charactersPlayer[i].GetHashCode() == ID) return charactersPlayer[i];
        }
        for (int i = 0; i < charactersEnemy.Count; i++)
        {
            if (charactersEnemy[i].GetHashCode() == ID) return charactersEnemy[i];
        }

        Debug.LogWarning("Failed to retrive character by ID");
        return null;
    }
    public Character GetCharacterByPosition(int position) 
    {
        if(position-1 < charactersPlayer.Count)return charactersPlayer[position-1];
        else return charactersEnemy[position-charactersPlayer.Count-1];
    }   //returns character class given its position
    public Character GetCharacterBySprite(Transform t) 
    {
        for (int i = 0; i < characterPositions.Count; i++)
        {
            if (characterPositions[i].position.Equals(t.position)) 
            {
                i++;
                Debug.Log($"this be position: {i}");
                return GetCharacterByPosition(i);
            }
        }
        Debug.LogWarning("INVALID POSITION REEEE");
        return new Character();

    }       //Returns character object given it's sprite displayer
    public Transform GetSpriteByPosition(int position)
    {
        return characterPositions[position - 1];
    }       //returns the character displaing object given its position

    public int GetPositionByCharacter(Character c)
    {
        for (int i = 0; i < charactersPlayer.Count; i++)
            if (charactersPlayer[i].GetHashCode() == c.GetHashCode()) return charactersPlayer[i].position;

        for (int i = 0; i < charactersEnemy.Count; i++)
            if (charactersEnemy[i].GetHashCode() == c.GetHashCode()) return charactersEnemy[i].position;

        Debug.LogWarning("Character requested is not among players or enemies - returning -1");
        return -1;
    }           //returns the character's position given a character.
    public ActionBase GetActionBaseByName(string name) 
    {
        ScriptableObject temp = (ScriptableObject)Resources.Load("Actions/" + name);
        Debug.LogWarning(temp.name);
        return temp as ActionBase;
    }      //Returns default action given it's name

    public bool CheckIfValidAttacker(int position) 
    {
        if (CheckIfActionValid(position) && !GetCharacterByPosition(position).isDead) return true;
        else return false;
    }          //If the player exists, has an attack equipped and is alive - can attack
    public bool CheckIfActionValid(int position)
    {
        string s;
        try { s= GetCharacterByPosition(position).actionChosen.name; }
        catch { return false; }

        if(s.Length<1) return false;
        Debug.Log(s + " is valid");
        return true;
    }             //if the character exists, has an action equipped, action is valid and can be enacted
    #endregion


    //Call when the encounter begins and ends - wrap the structure for ease of understandability
    public void StartEncounter(string ID)
    {
        UIManager.instance.ToggleEncounterSelection(false); //hide the map screen
        Debug.Log($"Starting encounter: {ID}"); battleID = ID;

        //Initialising Player Characters
        for (int i = 0; i < charactersPlayer.Count; i++)
        {
            //Refill health
            charactersPlayer[i].hpCur = charactersPlayer[i].hpMax;
            charactersPlayer[i].position = i + 1;

            //Initialising action instances from base action Scriptable Objects
            //charactersPlayer[i].actionChosen.Initialise(); ;
            foreach (Action a in charactersPlayer[i].actionsAvalible) a.Initialise();

            //initialising action owners
            charactersPlayer[i].actionChosen.ownerID = charactersPlayer[i].GetHashCode();
            foreach (Action a in charactersPlayer[i].actionsAvalible)
                a.ownerID = charactersPlayer[i].GetHashCode();

            if (charactersPlayer[i].actionChosen == null) charactersPlayer[i].actionChosen = charactersPlayer[i].actionsAvalible[0];
        }

        //if enemies have been reset or deleted, set default values, default attacks
        for (int i = 0; i < charactersEnemy.Count; i++)
        {
            if (charactersEnemy[i].hpMax==0) 
            {
                charactersEnemy[i].actionChosen = DefaultAction;
                charactersEnemy[i].name = "Zombie fairy";
                for (int j = 0; j < charactersEnemy[i].actionsAvalible.Length; j++)
                {
                    charactersEnemy[i].actionsAvalible[j] = DefaultAction;
                }
            }
        }

        //Initialising Enemy Characters
        for (int i = 0; i < charactersEnemy.Count; i++)
        {
            //Refill health
            charactersEnemy[i].hpCur = charactersEnemy[i].hpMax;
            charactersEnemy[i].position = i + 1+ charactersPlayer.Count;

            //Initialising action instances from base action Scriptable Objects
            charactersEnemy[i].actionChosen.Initialise(); ;
            foreach (Action a in charactersEnemy[i].actionsAvalible) a.Initialise();

            //initialising action owners
            charactersEnemy[i].actionChosen.ownerID = charactersEnemy[i].GetHashCode();
            foreach (Action a in charactersEnemy[i].actionsAvalible)
                a.ownerID = charactersEnemy[i].GetHashCode();

            if (charactersEnemy[i].actionChosen == null) charactersEnemy[i].actionChosen = charactersEnemy[i].actionsAvalible[0];
        }
        isEnemyTurn = true;
    }    //Initialise health to full and actions from base action values.
    public void EndEncounter(bool isWon)
    {
        isInBattle = false;
        Debug.Log($"Just finished encounter: {battleID}");
        if (isWon)
        {
            UIManager.instance.ToggleEncounterSelection(true); //show the map screen
            mapManager.instance.NodeUnlockInitialise(battleID);
            // SceneManager.LoadScene(SceneTranisiton);
        }
        else
        {
            Debug.LogWarning("Game Over! Starting over from level 0...");
            // SceneManager.LoadScene(SceneTranisiton); 
        }
    }     //Disable fight behaviours, attacks and turn flow, then toggle map view

    public void RecalculateCharacterPositions() //Rethink where the various characters are after someone's been killed
    {
        for (int i = 0; i < charactersPlayer.Count; i++)
        {
            if (!charactersPlayer[i].isDead) charactersPlayer[i].position = i + 1;
            else i--;
        }
           

        for (int i = 0; i < charactersEnemy.Count; i++)
        {
            if (!charactersPlayer[i].isDead) charactersEnemy[i].position = i + charactersPlayer.Count + 1;   //count increases by 1 natively
            else i--;
        }
    }

    #region turn logic
    void PlayerTurn()   //Refresh action tokens..? -Mick
    {
        // when player turn starts, restore action points to maximum, the value inside MaxActionPoints Variable.
        if (OutOfTokens && isEnemyTurn == false) { CurrentActionPoints = MaxActionPoints; OutOfTokens = false; }

        if (CurrentActionPoints == 0)
        {
            isEnemyTurn = true;
            for(int i = 0; i < 4; i++)
            {
                //PlayerMovementActions[i] = 1;
                //PlayerMovementDirection[i] = 1;
            }
        }
    }

    void SelectingPlayerUnit()
    {
        //This is for selecting character you want to plan movements for
        if (Input.GetKeyDown(KeyCode.Q) && GetCharacterByPosition(1).isDead==false )
        {
            PlayerCharacterSelection = 0;
        }
        else if (Input.GetKeyDown(KeyCode.W) && GetCharacterByPosition(2).isDead == false)
        {
            PlayerCharacterSelection = 1;
        }
        else if (Input.GetKeyDown(KeyCode.E) && GetCharacterByPosition(3).isDead == false)
        {
            PlayerCharacterSelection = 2;
        }
        else if (Input.GetKeyDown(KeyCode.R) && GetCharacterByPosition(4).isDead == false)
        {
            PlayerCharacterSelection = 3;
        }

    }

    public void ChangePlayerMovements(int index,bool isNowMovingLeft) //Left/Right to decide whether moving left/right by ONE tile. Or three if last/first member
    {
        //This actually deciding movements for the player unit selected,
        if (isNowMovingLeft) //if SHOULD NOW be moving right, cause it has been moving left
        {
            if (index == 0) { PlayerMovementDirection[index] = 3; Debug.Log("Player Unit " + index + " will move backwards"); }
            else { PlayerMovementDirection[index] = -1; Debug.Log("Player Unit " + index + " move backwards"); }
        }
        else
        {
            if (index == 3) { PlayerMovementDirection[index] = -3; Debug.Log("Player Unit " + index + " will move backwards"); }
            else { PlayerMovementDirection[index] = 1; Debug.Log("Player Unit " + index + " will move forwards"); }
        }
    }

    void PlayerUnitAttacking()
    {
        if (isEnemyTurn == false)
        {

            if (Input.GetKeyDown(KeyCode.Alpha1) && CheckIfValidAttacker(1) ==true)
            {
                ExecuteCharacterAction(charactersPlayer[0]);
                CurrentActionPoints -= GetCharacterByPosition(1).actionChosen.updatedData.cost;
                if (CheckIfValidAttacker(5) == true)
                {
                    EnemyReactions(0);
                }
            }
            if (Input.GetKeyDown(KeyCode.Alpha2) && CheckIfValidAttacker(2) == true)
            {
                ExecuteCharacterAction(charactersPlayer[1]);
                CurrentActionPoints -= GetCharacterByPosition(2).actionChosen.updatedData.cost;
                if (CheckIfValidAttacker(6) == true)
                {
                    EnemyReactions(1);
                }
            }
            if (Input.GetKeyDown(KeyCode.Alpha3) && CheckIfValidAttacker(3) == true)
            {
                ExecuteCharacterAction(charactersPlayer[2]);
                CurrentActionPoints -= GetCharacterByPosition(2).actionChosen.updatedData.cost;
                if (CheckIfValidAttacker(7) == true)
                {
                    EnemyReactions(2);
                }
            }
            if (Input.GetKeyDown(KeyCode.Alpha4) && CheckIfValidAttacker(4) == true)
            {
                ExecuteCharacterAction(charactersPlayer[3]);
                CurrentActionPoints -= GetCharacterByPosition(3).actionChosen.updatedData.cost;
                if (CheckIfValidAttacker(8) == true)
                {
                    EnemyReactions(3);
                }
            }
        }
    }

    void EnemyReactions(int EnemyIndex)
    {
        Debug.Log("enemy Unit " + EnemyIndex + " needs help");
        EnemyMovementActions[EnemyIndex] = 0;
        SwappingCharacterElements(charactersEnemy, EnemyIndex+4, (EnemyIndex + 4 + EnemyMovementDirection[EnemyIndex]));
        SwappingArrayElements(EnemyMovementActions, EnemyIndex, (EnemyIndex + EnemyMovementDirection[EnemyIndex]));
        SwappingTransformElements(characterPositions, (EnemyIndex + 4), ((EnemyIndex + 4) + (EnemyMovementDirection[EnemyIndex])));
        //SwappingGameObjectElements(EnemySprites, EnemyIndex, (EnemyIndex + EnemyMovementDirection[EnemyIndex]));
        Debug.Log("enemy Unit " + EnemyIndex + " has moved");
        RecalculateCharacterPositions();
    }
    void EnemyTurn()
    {
        if (!OutOfTokens && isEnemyTurn == true) { EnemyActions = MaxEnemyActions; OutOfTokens = true; }

        if (EnemyActions == 0)
        {
            isEnemyTurn = false;
            EnemyCounter = 0;
        }
    }

    // this is set to have each character in a row attack, so each enemy hits once per turn. the max amount of actions for enemies for now is the same as the player
    void EnemyAttacks()
    {
        if (isEnemyTurn == true)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                ExecuteCharacterAction(charactersEnemy[EnemyCounter]);
                EnemyCounter += 1;
                EnemyActions -= 1;
            }
        }
    }

    //this manages moving the player units, so they can move one place to the left or one place to the right, once for each character during the enemies turn
    public void PlayerReactions()
    {
        if (isEnemyTurn == true)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) && PlayerMovementActions[0] == 1)
            {
                PlayerMovementActions[0] = 0;
                SwappingCharacterElements(charactersPlayer, 0, (0 + PlayerMovementDirection[0]));
                SwappingArrayElements(PlayerMovementActions, 0, (0 + PlayerMovementDirection[0]));
                SwappingTransformElements(characterPositions, 0, (0 + PlayerMovementDirection[0]));
                //SwappingGameObjectElements(PlayerSprites , 0, (0 + PlayerMovementDirection[0]));
                SwappingArrayElements(PlayerMovementDirection, 0, (0 + PlayerMovementDirection[0]));
                Debug.Log("Player Unit 1 has moved");
            }
            if (Input.GetKeyDown(KeyCode.Alpha2) && PlayerMovementActions[1] == 1)
            {
                PlayerMovementActions[1] = 0;
                SwappingCharacterElements(charactersPlayer, 1, (1 + PlayerMovementDirection[1]));
                SwappingArrayElements(PlayerMovementActions, 1, (1 + PlayerMovementDirection[1]));
                SwappingTransformElements(characterPositions, 1, (1 + PlayerMovementDirection[1]));
                //SwappingGameObjectElements(PlayerSprites, 1, (1 + PlayerMovementDirection[1]));
                SwappingArrayElements(PlayerMovementDirection, 1, (1 + PlayerMovementDirection[1]));
                Debug.Log("Player Unit 2 has moved");
            }
            if (Input.GetKeyDown(KeyCode.Alpha3) && PlayerMovementActions[2] == 1)
            {
                PlayerMovementActions[2] = 0;
                SwappingCharacterElements(charactersPlayer, 2, (2 + PlayerMovementDirection[2]));
                SwappingArrayElements(PlayerMovementActions, 2, (2 + PlayerMovementDirection[2]));
                SwappingTransformElements(characterPositions, 2, (2 + PlayerMovementDirection[2]));
                //SwappingGameObjectElements(PlayerSprites, 2, (2 + PlayerMovementDirection[2]));
                SwappingArrayElements(PlayerMovementDirection, 2, (2 + PlayerMovementDirection[2]));
                Debug.Log("Player Unit 3 has moved");
            }
            if (Input.GetKeyDown(KeyCode.Alpha4) && PlayerMovementActions[3] == 1)
            {
                PlayerMovementActions[3] = 0;
                SwappingCharacterElements(charactersPlayer, 3, (3 + PlayerMovementDirection[3]));
                SwappingArrayElements(PlayerMovementActions, 3, (3 + PlayerMovementDirection[3]));
                SwappingTransformElements(characterPositions, 3, (3 + PlayerMovementDirection[3]));
                //SwappingGameObjectElements(PlayerSprites, 3, (3 + PlayerMovementDirection[3]));
                SwappingArrayElements(PlayerMovementDirection, 3, (3 + PlayerMovementDirection[3]));
                Debug.Log("Player Unit 4 has moved");
            }
        }
        RecalculateCharacterPositions();
    }
    #endregion


    //used for moving character to different positions on the board
    public void SwappingArrayElements(int[] Array, int Index1, int Index2)
    {
        int temp = Array[Index1];
        Array[Index1] = Array[Index2];
        Array[Index2] = temp;
        //(Array[Index1], Array[Index2]) = (Array[Index1], Array[Index2]);
    }

    public void SwappingCharacterElements(List<Character> Array, int Index1, int Index2)
    {
        Character temp = Array[Index1];
        Array[Index1] = Array[Index2];
        Array[Index2] = temp;
        //(Array[Index1], Array[Index2]) = (Array[Index1], Array[Index2]);
    }

    public void SwappingTransformElements(List<Transform> Array, int Index1, int Index2)
    {
        //swap transform positions; 
        Vector3 temPosition= Array[Index1].position;
        Array[Index1].position = Array[Index2].position;
        Array[Index2].position = temPosition;

        //once swapped, swap reference on transform list.
        Transform temp = Array[Index1];
        Array[Index1] = Array[Index2];
        Array[Index2] = temp;                       

        //(Array[Index1], Array[Index2]) = (Array[Index1], Array[Index2]);
    }

    //Uncalled method?
    public void SwappingGameObjectElements(List<GameObject> Array, int Index1, int Index2)
    {
        PlayerSwapPosition = Array[Index1].transform.position;

        Array[Index1].transform.position = Array[Index2].transform.position;

        Array[Index2].transform.position = PlayerSwapPosition;

        (Array[Index1], Array[Index2]) = (Array[Index1], Array[Index2]);
    }



}
