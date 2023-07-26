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
using UnityEngine.UIElements;


public class BattleManager : MonoBehaviour
{
    public enum BattleStage {planning, playerAct,playerMove, enemyAct};
    public static BattleManager instance;
    [Header("Game current state")]
    [Tooltip("the 'Level' the player is currently playing ")]                     public string battleID = "1";
    [Tooltip("is the entire encounter finished? ")]                                 public bool isInBattle = true;
                                                                             public BattleStage curStage = BattleStage.planning;
                                                                              [SerializeField] private int CurrentActionPoints;

    //Data of player characters and enemy characters
    public List <Character> charactersPlayer =  new List<Character>(4);
    public List <Character> charactersEnemy =   new List<Character>(4);
    public List<Transform> characterPositions = new List<Transform>(8);

    Vector3 PlayerSwapPosition; 
    public string SceneTranisiton;

    [Header("Object references, prefabs")]
    [Tooltip("this is the VFX spawned when hit lands on player/enemy")]     public GameObject HitMarker;
    [Tooltip("This is the default action to enact if none are assigned")]   public Action DefaultAction;

    
    public int      ActionsPerCharacter = 1;                        //Amount of actions and movements the player can make
    public int[]    PlayerMovementActions = { 1, 1, 1, 1 };     //each unit has a specific amonut of movement, which works off of charactersPlayer. when a movement is done, it cannot be done a gain for that unit
    public int[]    PlayerMovementDirection = { 0, 0, 0, 0 };   //keeps track of what position units have been prepped to move too, -1 = left, 1 = right, 0 = No Movement prepped.
    
    public int      ActionsPerEnemy = 1;                        //Amount of Actions enemy can make
    public float    EnemyAttackDelay = 2f;
    public int      EnemyCounter = 1;
    public int[]    EnemyMovementActions = { 1, 1, 1, 1 };      //each unit has a specific amonut of movement, which works off of charactersPlayer. when a movement is done, it cannot be done a gain for that unit
    public int[]    EnemyMovementDirection = { 1, -1, -1, -1 }; // keeps track of what position units have been prepped to move too, -1 = left, 1 = right, 0 = No Movement prepped.
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
        StartCoroutine(BattleFlow());
        UIManager.instance.RefreshStatusCorners();  //Once character data is fully retrieved, update the current stats to match.
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) SwitchBetweenPlanActPhases();         
    }
    public void SwitchBetweenPlanActPhases() 
    {
        if (curStage != BattleStage.planning) curStage = BattleStage.planning;
        else curStage = BattleStage.playerAct;

        if (curStage == BattleStage.planning)
        {
            UIManager.instance.SelectedArrow.SetActive(true);
            UIManager.instance.MovePreviewArrow.SetActive(false);

            UIManager.instance.AnimatorTrigger("SwitchToPlan");
        }
        else
        {
            UIManager.instance.LoadActionDescriptions();   //Load details of actions chosen in plan phase and displays them on execute actions panel
            UIManager.instance.SelectedArrow.SetActive(false);

            UIManager.instance.AnimatorTrigger("SwitchToAct");
        }
    }

    //////////////////    MAIN Turn Flow   ////////////////////////////////
    public IEnumerator BattleFlow() 
    {
    startOfRound:
        while (!isInBattle || curStage == BattleStage.planning) 
        {
            yield return new WaitForSeconds(0.5f);
            //Debug.Log($" Standing by in plan");
        } 
        while (isInBattle && curStage != BattleStage.planning) 
        {
            //Show targetted position for actions chosen, enact one of the selected actions
            if (curStage == BattleStage.playerAct)
            {
                Debug.Log($" entered act");
                StartPlayerTurn();

                //Wait for player to choose an action
                while (curStage == BattleStage.playerAct)
                {
                    yield return new WaitForEndOfFrame();
                    PlayerAttacking();                          //Keybind attacks, supplementing the UI ones enabled
                    //Debug.Log($" standing by in Act");
                }
            }
            //show where enemies are aiming and planning to move, hold until player chooses a movement
            else if (curStage == BattleStage.playerMove)
            {
                Debug.Log($" entered move");

                //Switch Animator to show the prepared moves panel
                UIManager.instance.AnimatorTrigger("SwitchToMove");
                
                //Wait for player to choose a move
                while (curStage == BattleStage.playerMove)
                {
                    yield return new WaitForEndOfFrame();
                    PreviewEnemyBehaviour();                //Preview what the enemy is about to do, where to move
                    PlayerMoves();                          //Keybind moves, supplementing the UI ones enabled
                    Debug.Log($" standing by in Move");;
                }
            }
            else if(curStage == BattleStage.enemyAct) 
            {
                Debug.Log($" entered enemy actions");
                UIManager.instance.HideMoveArrow();    
                //Switch Animator to show nothing 

                EnemyAct();
                yield return new WaitForSeconds(EnemyAttackDelay);
                EnemyMove(EnemyCounter);

                //perform the enemy attack and enemy move
                while (curStage == BattleStage.enemyAct)
                {
                    yield return new WaitForSeconds(0.5f);
                    Debug.Log($" standing by in enemy actions"); ;
                }
            }


            if (curStage == BattleStage.planning) goto startOfRound;
        }

        //Battle would naturally end here
    }

    #region targetting and action previews
    public void PreviewCharacterAction(int position) 
    { 
        if (CheckIfActionValid(position)) 
        {
            if(!GetCharacterByPosition(position).CheckIsThisPlayer())   //if not player than preview only if on move stage
            {
                if (curStage == BattleStage.playerMove) GetCharacterByPosition(position).actionChosen.Preview(true);     
            }
            else if(curStage == BattleStage.playerAct || curStage == BattleStage.playerMove) 
                GetCharacterByPosition(position).actionChosen.Preview(true);                    //if player then preview in act and move stages

        }  
    }        
    public void PreviewCharacterAction(Transform position) 
    {
        if (CheckIfActionValid(GetCharacterBySprite(position).position))
        {
            PreviewCharacterAction(GetPositionByCharacter(GetCharacterBySprite(position)));
        }
    }   //redirects to PreviewCharacterAction(int)
    public void EndPreviewCharacterAction(int position) => GetCharacterByPosition(position).actionChosen.Preview(false);
    public void ToggleActionPreview(Character c, bool state) => c.actionChosen.Preview(state);
    
    public void PreviewEnemyBehaviour() 
    {
        if (CheckIfActionValid(GetCharacterByPosition(4+EnemyCounter).position) && curStage == BattleStage.playerMove)  //if the pleyer is moving, preview what the enemy is going to do
        { 
            GetCharacterByPosition(4 + EnemyCounter).actionChosen.Preview(true);
            UIManager.instance.ShowMoveArrow(GetSpriteByPosition(4 + EnemyCounter), EnemyCounter - 1);
        } 
    }
    #endregion


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
                return GetCharacterByPosition(i);
            }
        }
        Debug.LogWarning("INVALID POSITION REEEE");
        return new Character();

    }       //Returns character object given it's sprite displayer
    public int GetPositionBySprite(Transform t) 
    {
        for (int i = 0; i < characterPositions.Count; i++)
        {
            if (characterPositions[i].position.Equals(t.position))
            {
                i++;
                Debug.Log($"this be position: {i}");
                return i+1;
            }
        }
        Debug.LogWarning("INVALID POSITION REEEE");
        return 1;

    }

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
        if (CheckIfActionValid(position) && !GetCharacterByPosition(position).isDead) { Debug.Log(GetCharacterByPosition(position).name+" is a valid attacker"); return true; }
        else return false;
    }          //If the player exists, has an attack equipped and is alive - can attack
    public bool CheckIfActionValid(int position)
    {
        string s;
        try { s= GetCharacterByPosition(position).actionChosen.name; }
        catch { return false; }

        if(s.Length<1) return false;
        //Debug.Log(s + " is valid");
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
        EnemyCounter = 1;
    }    //Initialise health to full and actions from base action values.
    public void EndEncounter(bool isWon)
    {
        isInBattle = false;
        StopCoroutine(BattleFlow());
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
    void StartPlayerTurn()   //Refresh action tokens..? -Mick
    {
        // when player turn starts, restore action points to maximum, the value inside ActionsPerCharacter Variable.
        if (OutOfTokens) { CurrentActionPoints = ActionsPerCharacter; OutOfTokens = false; }

        if (CurrentActionPoints == 0)
        {
            for(int i = 0; i < 4; i++)
            {
                //PlayerMovementActions[i] = 1;
                //PlayerMovementDirection[i] = 1;
            }
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

    void PlayerAttacking()
    {
            if (Input.GetKeyDown(KeyCode.Alpha1) && CheckIfValidAttacker(1) == true)    CharacterAct(1);
            if (Input.GetKeyDown(KeyCode.Alpha2) && CheckIfValidAttacker(2) == true)    CharacterAct(2);
            if (Input.GetKeyDown(KeyCode.Alpha3) && CheckIfValidAttacker(3) == true)    CharacterAct(3);
            if (Input.GetKeyDown(KeyCode.Alpha4) && CheckIfValidAttacker(4) == true)    CharacterAct(4);     
    }
    public void CharacterAct(int position) //Execute the character's action and trigger a corresponding enemy reaction (player 3, triggers enemy 3)
    {
        Character c = GetCharacterByPosition(position);
        if (CheckIfValidAttacker(c.position) == false) return;

        if (c.CheckIsThisPlayer())                  //if it's player character acting
        {
            if (CurrentActionPoints < c.actionChosen.updatedData.cost) { curStage = BattleStage.playerMove; return; }
            CurrentActionPoints -= c.actionChosen.updatedData.cost;
        }

        c.actionChosen.Perform();
        if (CurrentActionPoints < c.actionChosen.updatedData.cost && curStage == BattleStage.playerAct) curStage = BattleStage.playerMove;   //prgress to move stage if action points exhausted

        UIManager.instance.RefreshStatusCorners();
    }


    //this manages moving the player units, so they can move one place to the left or one place to the right, once for each character during the enemies turn
    public void PlayerMoves()
    {
            if (Input.GetKeyDown(KeyCode.Alpha1) && PlayerMovementActions[0] == 1) PlayerMove(1);
            if (Input.GetKeyDown(KeyCode.Alpha2) && PlayerMovementActions[1] == 1) PlayerMove(2);
            if (Input.GetKeyDown(KeyCode.Alpha3) && PlayerMovementActions[2] == 1) PlayerMove(3);
            if (Input.GetKeyDown(KeyCode.Alpha4) && PlayerMovementActions[3] == 1) PlayerMove(4);

        RecalculateCharacterPositions();
    }
    public void PlayerMove(int position)
    {
        int i = position - 1;
        PlayerMovementActions[i] = 0;
        Debug.Log($"moving at {position}");
        SwappingCharacterElements(charactersPlayer, i, (i + PlayerMovementDirection[i]));
        SwappingArrayElements(PlayerMovementActions, i, (i + PlayerMovementDirection[i]));
        SwappingTransformElements(characterPositions, i, (i + PlayerMovementDirection[i]));
        SwappingArrayElements(PlayerMovementDirection, i, (i + PlayerMovementDirection[i]));
        Debug.Log($"Player Unit {position} has moved!");

        //after the player action is performed, proceed to the enemy action
        curStage = BattleStage.enemyAct;
        UIManager.instance.AnimatorTrigger("SwitchToAct");
    }

    // this is set to have each character in a row attack, so each enemy hits once per turn. the max amount of actions for enemies for now is the same as the player
    void EnemyAct()
    {
        CharacterAct(4+EnemyCounter);
        EnemyCounter += 1;
    }


    void EnemyMove(int EnemyIndex)
    {
        Debug.Log($"enemy Unit {EnemyIndex} is moving to spot {EnemyIndex + EnemyMovementDirection[EnemyIndex]}");
        EnemyMovementActions[EnemyIndex] = 0;
        SwappingCharacterElements(charactersEnemy, EnemyIndex-1, (EnemyIndex-1 + EnemyMovementDirection[EnemyIndex-1]));                  //Swap enemy character data
        SwappingArrayElements(EnemyMovementActions, EnemyIndex-1, (EnemyIndex-1 + EnemyMovementDirection[EnemyIndex-1]));                 //swap enemy movement choices
        SwappingTransformElements(characterPositions, (EnemyIndex + 4), ((EnemyIndex + 4) + (EnemyMovementDirection[EnemyIndex]))); //swap enemy sprites
        Debug.Log("enemy Unit " + EnemyIndex + " has moved");
        RecalculateCharacterPositions();

        curStage = BattleStage.playerAct;
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
