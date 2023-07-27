/* This class handles the flow of the battle - initialising characters, turn order etc.
 * 
 * Current Debug mapping SPACE to advance to act stage.
 */
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class BattleManager : MonoBehaviour
{
                                                                                                                        public enum BattleStage     {planning, playerAct,playerMove, enemyAct};
                                                                                                                        public static BattleManager instance;
    [Header("Game current state")]
    [Tooltip("the 'Level' the player is currently playing ")]                                                           public string               battleID = "1";
    [Tooltip("is the entire encounter finished? ")]                                                                     public bool                 isInBattle = true;
    [Tooltip("Advance from planning, player act, player move to enemy act and back to player act")]                     public BattleStage          curStage = BattleStage.planning;
                                                                                                                               int                  CurrentActionPoints;
    [Tooltip("which direction the unit is set to move: -1 = left, 1 = right, 0 = No Movement prepped.")]                public int[]                PlayerMovementDirection = { 0, 0, 0, 0 };
    [Tooltip("which direction the unit is set to move: -1 = left, 1 = right, 0 = No Movement prepped.")]                public int[]                EnemyMovementDirection = { 1, -1, -1, -1 };   
    [Header("Data of combatants")]
    [Tooltip("Player character data object container. Access values via Character")]                                    public List <Character>     charactersPlayer =  new List<Character>(4);
    [Tooltip("Enemy character data object container. Access values via Character")]                                     public List <Character>     charactersEnemy =   new List<Character>(4);
    [Tooltip("Sprite displayers and overall aesthetics relative to a given combatant- shared between friend and foe")]  public List<Transform>      characterPositions = new List<Transform>(8);
    [Header("Game Presets")]
    [Tooltip("Amount of actions each player character can make"), SerializeField]                                              int                  ActionsPerCharacter = 1;
    [Tooltip("Amount of actions each enemy character can make"), SerializeField]                                               int                  ActionsPerEnemy = 1;                
    [Tooltip("Whether a character can or cannot move anymore"), SerializeField]                                                int[]                PlayerMovementActions = { 1, 1, 1, 1 };     //each unit has a specific amonut of movement, which works off of charactersPlayer. when a movement is done, it cannot be done a gain for that unit
    [Tooltip("Whether a character can or cannot move anymore"), SerializeField]                                                int[]                EnemyMovementActions = { 1, 1, 1, 1 };      //each unit has a specific amonut of movement, which works off of charactersPlayer. when a movement is done, it cannot be done a gain for that unit
    [Tooltip("Delay between enemy attack and enemy movement - adjust for animations"), SerializeField]                         float                EnemyAttackDelay = 1f;
    [Tooltip("Which enemy is set to attack next")]                                                                             int                  EnemyCounter = 1;
    [Header("Object references, prefabs")]
    [Tooltip("this is the VFX spawned when hit lands on player/enemy")] public GameObject HitMarker;
    [Tooltip("This is the default action to enact if none are assigned")] public Action DefaultAction;

    //Assign instance if none found, otherwise destroy
    private void Awake()
    {
        if(instance == null) instance = this;
        else Destroy(this);
    }

    void Start() => StartEncounter("1");     //Initialise health to full and actions from base action values. Start main battle flow
    
    void Update()   //handle the switching to act/plan phases 
    {
        if (Input.GetKeyDown(KeyCode.Space)) SwitchBetweenPlanActPhases();         
    }



    //Call when the encounter begins and ends - wrap the structure for ease of understandability
    public void StartEncounter(string ID)   //Initialise health to full and actions from base action values.   
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
            if (charactersEnemy[i].hpMax == 0)
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
            charactersEnemy[i].position = i + 1 + charactersPlayer.Count;

            //Initialising action instances from base action Scriptable Objects
            charactersEnemy[i].actionChosen.Initialise(); ;
            foreach (Action a in charactersEnemy[i].actionsAvalible) a.Initialise();

            //initialising action owners
            charactersEnemy[i].actionChosen.ownerID = charactersEnemy[i].GetHashCode();
            foreach (Action a in charactersEnemy[i].actionsAvalible)
                a.ownerID = charactersEnemy[i].GetHashCode();

            if (charactersEnemy[i].actionChosen == null) charactersEnemy[i].actionChosen = charactersEnemy[i].actionsAvalible[0];
        }
        EnemyCounter = 4;

        StartCoroutine(BattleFlow());
        UIManager.instance.RefreshStatusCorners();  //Once character data is fully retrieved, update the current stats to match.
    }    

    //////////////////    MAIN Turn Flow   ////////////////////////////////
    IEnumerator BattleFlow() 
    {
        //Send back to this when the round ends to maintain looping
        startOfRound:
        GenerateEnemyAttacksAndMoves();

        //Wait until the player enters battle
        while (!isInBattle || curStage == BattleStage.planning) yield return new WaitForSeconds(0.5f);
        
        while (isInBattle && curStage != BattleStage.planning) 
        {
            //Show targetted position for actions chosen, enact one of the selected actions
            if (curStage == BattleStage.playerAct)
            {
                Debug.Log($" entered act");
                CurrentActionPoints = ActionsPerCharacter;

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
                    //Debug.Log($" standing by in Move");;
                }
            }
            
            //choosing a dodge changes the state to enemy act - upon entering, perform attack, wait a bit, then dodge
            else if(curStage == BattleStage.enemyAct) 
            {
                Debug.Log($" entered enemy actions");
                UIManager.instance.HideMoveArrow();  

                EnemyAct();
                yield return new WaitForSeconds(EnemyAttackDelay);
                EnemyMove(EnemyCounter);

                //perform the enemy attack and enemy move
                while (curStage == BattleStage.enemyAct)
                {
                    yield return new WaitForSeconds(0.5f);
                    //Debug.Log($" tick within EnemyAct state");
                }
            }

            Debug.Log("Cycling batlte logic back to start of round");
            if (curStage == BattleStage.planning) goto startOfRound;
        }

        //Battle would naturally end here
    }

    public void EndEncounter(bool isWon)//Disable fight behaviours, attacks and turn flow, then toggle map view
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
    }     



    //trajectory parabola and shifting arrow methods are found here
    #region targetting and action previews
    public void PreviewCharacterAction(int position) 
    { 
        if (CheckIfValidAction(position)) 
        {
            if(!GetCharacterByPosition(position).CheckIsThisPlayer() && curStage == BattleStage.playerMove)   //if not player than preview only if on move stage
                GetCharacterByPosition(position).actionChosen.Preview(true);     
            
            else if(curStage == BattleStage.playerAct || curStage == BattleStage.playerMove) 
                GetCharacterByPosition(position).actionChosen.Preview(true);                                  //if player then preview in act and move stages
        }  
    }        
    public void PreviewCharacterAction(Transform position) 
    {
        if (CheckIfValidAction(GetCharacterBySprite(position).position))
        {
            PreviewCharacterAction(GetPositionByCharacter(GetCharacterBySprite(position)));
        }
    }   //redirects to PreviewCharacterAction(int)
    public void EndPreviewCharacterAction(int position) {if(GetCharacterByPosition(position).actionChosen!=null) GetCharacterByPosition(position).actionChosen.Preview(false); } 
    public void ToggleActionPreview(Character c, bool state) => c.actionChosen.Preview(state);
    public void PreviewEnemyBehaviour() 
    {
        if (CheckIfValidAction(GetCharacterByPosition(4+EnemyCounter).position) && curStage == BattleStage.playerMove)  //if the pleyer is moving, preview what the enemy is going to do
        { 
            GetCharacterByPosition(4 + EnemyCounter).actionChosen.Preview(true);
            UIManager.instance.ShowMoveArrow(GetSpriteByPosition(4 + EnemyCounter), EnemyCounter - 1);
        } 
    }
    #endregion



    void SwitchBetweenPlanActPhases()
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
    }   //Switch between planning and act phases using snazzy animations
    public void ChangePlayerMovements(int index, bool isNowMovingLeft) //Left/Right to decide whether moving left/right by ONE tile. Or three if last/first member
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

    #region ACT phase behaviours
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

        c.actionChosen = null;    //set action to null once used to prevent re-use
        UIManager.instance.LoadActionDescriptions();
        UIManager.instance.RefreshStatusCorners();
    }

    void PlayerAttacking() //Keybind attacks, redirects to appropriate CharacterAct() 
    {
            if (Input.GetKeyDown(KeyCode.Alpha1) && CheckIfValidAttacker(1) == true)    CharacterAct(1);
            if (Input.GetKeyDown(KeyCode.Alpha2) && CheckIfValidAttacker(2) == true)    CharacterAct(2);
            if (Input.GetKeyDown(KeyCode.Alpha3) && CheckIfValidAttacker(3) == true)    CharacterAct(3);
            if (Input.GetKeyDown(KeyCode.Alpha4) && CheckIfValidAttacker(4) == true)    CharacterAct(4);     
    }
    void PlayerMoves()
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
    void EnemyAct() => CharacterAct(4+EnemyCounter);
    void EnemyMove(int EnemyPosition)
    {
        EnemyMovementActions[EnemyPosition - 1] = 0;
        SwappingCharacterElements(charactersEnemy, EnemyPosition-1, (EnemyPosition-1 + EnemyMovementDirection[EnemyPosition-1]));                  //Swap enemy character data
        SwappingArrayElements(EnemyMovementActions, EnemyPosition-1, (EnemyPosition-1 + EnemyMovementDirection[EnemyPosition-1]));                 //swap enemy movement choices
        SwappingTransformElements(characterPositions, (EnemyPosition + 3), ((EnemyPosition + 3) + (EnemyMovementDirection[EnemyPosition-1]))); //swap enemy sprites
        Debug.Log($"enemy Unit { EnemyPosition} has moved to spot {EnemyPosition + EnemyMovementDirection[EnemyPosition-1]}");
        RecalculateCharacterPositions();

        EnemyCounter = DetermineNextEnemy();
        if (EnemyCounter > -1) curStage = BattleStage.playerAct;
        else SwitchBetweenPlanActPhases();  //when the last enemy performs their attack, return to planning phase
    }   //perform the planned move and retrieve next enemy to act
    void GenerateEnemyAttacksAndMoves() //DOES NOT DO ANYTHING YET 
    {
        for (int i = 0; i < charactersEnemy.Count; i++)
        {

            charactersEnemy[i].actionChosen = charactersEnemy[i].actionsAvalible[0];    //Just choose the first one avalible
        }
    }
    int DetermineNextEnemy() //Determine which enemy should act next(tries to attack from right to left) 
    {
        int rightmost = -1;

        for (int i = 4; i >0; i--)
        {
            Debug.Log($"X: {i}");
            if (CheckIfValidAction(charactersEnemy[i - 1].position) && charactersEnemy[i-1].position > rightmost) //if has a valid attack and is the rightmost avalible
            { rightmost = charactersEnemy[i-1].position; } 
            Debug.Log($"new rightmost obtained {rightmost}");
        }

        if (rightmost < 0) return -1;  //if no enemies have valid attacks, end the whole turn
        else return rightmost - 4;
    }


    #endregion

    //Convenient shorthands for retrieving one kind of data from another - character data from position, sprite by position ETC
    #region utilities
    //Gets character by their ID values
    public Character    GetCharacterByID(int ID)
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
    public Character    GetCharacterByPosition(int position)
    {
        if (position - 1 < charactersPlayer.Count) return charactersPlayer[position - 1];
        else return charactersEnemy[position - charactersPlayer.Count - 1];
    }    //returns character class given its position
    public Character    GetCharacterBySprite(Transform t)
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
    public int          GetPositionBySprite(Transform t)
    {
        for (int i = 0; i < characterPositions.Count; i++)
        {
            if (characterPositions[i].position.Equals(t.position))
            {
                i++;
                Debug.Log($"this be position: {i}");
                return i + 1;
            }
        }
        Debug.LogWarning("INVALID POSITION REEEE");
        return 1;

    }
    public int          GetPositionByCharacter(Character c)
    {
        for (int i = 0; i < charactersPlayer.Count; i++)
            if (charactersPlayer[i].GetHashCode() == c.GetHashCode()) return charactersPlayer[i].position;

        for (int i = 0; i < charactersEnemy.Count; i++)
            if (charactersEnemy[i].GetHashCode() == c.GetHashCode()) return charactersEnemy[i].position;

        Debug.LogWarning("Character requested is not among players or enemies - returning -1");
        return -1;
    }     //returns the character's position given a character.
    public Transform    GetSpriteByPosition(int position)
    {
        return characterPositions[position - 1];
    }       //returns the character displaing object given its position
    public ActionBase   GetActionBaseByName(string name)
    {
        ScriptableObject temp = (ScriptableObject)Resources.Load("Actions/" + name);
        Debug.LogWarning(temp.name);
        return temp as ActionBase;
    }        //Returns default action given it's name
    public bool         CheckIfValidAttacker(int position)
    {
        if (CheckIfValidAction(position) && !GetCharacterByPosition(position).isDead) { Debug.Log(GetCharacterByPosition(position).name + " is a valid attacker"); return true; }
        else return false;
    }      //If the player exists, has an attack equipped and is alive - can attack
    public bool         CheckIfValidAction(int position)
    {
        //check if downright null
        if (GetCharacterByPosition(position).actionChosen == null) return false;

        //check if reset to 0 properties but interpreted as non-null
        string s;
        try
        {
            s = GetCharacterByPosition(position).actionChosen.name;
            if (s.Length < 1) return false;
        }
        catch { return false; }

        //Otherwise, action is valid
        return true;
    }        //if the character exists, has an action equipped, action is valid and can be enacted
    public void         RecalculateCharacterPositions() //Rethink where the various characters are after someone's been killed
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
        UIManager.instance.RefreshStatusCorners();
    }
    #endregion

    //used for moving character to different positions - datawise and sprite-wise
    #region array handlers
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
    #endregion

}
