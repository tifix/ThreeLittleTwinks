/* This class handles the flow of the battle - initialising characters, turn order etc.
 * 
 * Current Debug mapping SPACE to advance to act stage.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class BattleManager : MonoBehaviour
{
    public bool CHEAT_setDefaults = false;
    [SerializeField] private int TargetChosenNow = 0;
    public bool isChoosingTarget = false; //if an attack allows selecting a target, this protects against constant and duplicate calls
    public enum BattleStage     {planning, playerAct,playerMove, enemyAct};
                                                                                                                        public static BattleManager instance;
    [Header("Game current state")]
    [Tooltip("the 'Level' the player is currently playing ")]                                                           public string               battleID = "1";
    [Tooltip("is the entire encounter finished? ")]                                                                     public bool                 isInBattle = true;
    [Tooltip("Advance from planning, player act, player move to enemy act and back to player act")]                     public BattleStage          curStage = BattleStage.planning;
    [Tooltip("Points spent to perform complex/powerful actions. No passive generation")]                                       int                  venganceCur;
    [Tooltip("which direction the unit is set to move: -1 = left, 1 = right, 0 = No Movement prepped.")]                public int[]                PlayerMovementDirection = { 0, 0, 0, 0 };
    [Tooltip("which direction the unit is set to move: -1 = left, 1 = right, 0 = No Movement prepped.")]                public int[]                EnemyMovementDirection = { 1, -1, -1, -1 };   
    [Header("Data of combatants")]
    [Tooltip("Player character data object container. Access values via Character")]                                    public List <Character>     charactersPlayer =  new List<Character>(4);
    [Tooltip("Enemy character data object container. Access values via Character")]                                     public List <Character>     charactersEnemy =   new List<Character>(4);
    [Tooltip("Sprite displayers and overall aesthetics relative to a given combatant- shared between friend and foe")]  public List<Transform>      characterPositions = new List<Transform>(8);
    [Header("Game Presets")]
    [Tooltip("Points spent to perform complex/powerful actions. This is the maximum amount player can hold")]           public int                  venganceMax=5;
    [Tooltip("Amount of actions each enemy character can make"), SerializeField]                                               int                  ActionsPerEnemy = 1;                
    [Tooltip("Whether a character can or cannot move anymore"), ]                                                       public int[]                PlayerMovementActions = { 1, 1, 1, 1 };     //each unit has a specific amonut of movement, which works off of charactersPlayer. when a movement is done, it cannot be done a gain for that unit
    [Tooltip("Whether a character can or cannot move anymore"), SerializeField]                                                int[]                EnemyMovementActions = { 1, 1, 1, 1 };      //each unit has a specific amonut of movement, which works off of charactersPlayer. when a movement is done, it cannot be done a gain for that unit
    [Tooltip("Delay between enemy attack and enemy movement - adjust for animations"), SerializeField]                         float                EnemyMoveDelay = 1f;
    [Tooltip("Delay between executing player act and previewing enemy action - adjust for animations"), SerializeField]        float                EnemyAttackDelay = 1f;
    [Tooltip("Which enemy is set to attack next"), SerializeField]                                                             int                  EnemyCounter = 1;
    [Header("Object references, prefabs")]
    [Tooltip("this is the VFX spawned when hit lands on player/enemy")] public GameObject HitMarker;
    [Tooltip("This is the default action to enact if none are assigned")] public Action DefaultAction;

    //Assign instance if none found, otherwise destroy
    //Initialise health to full and actions from base action values. Start main battle flow
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(this);


    }

    private void Start()
    {
        StartEncounter("1");

        if (CHEAT_setDefaults)
        {
            PlayerMovementDirection[0] = 1;
            for (int i = 1; i < PlayerMovementDirection.Length; i++)
                PlayerMovementDirection[i] = -1;

            for (int i = 0; i < charactersPlayer.Count; i++)
                SelectAction(0, i);

        }
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

            //Initialising action instances from base action Scriptable Objects and setting default if unassigned
            for (int a = 0; a < charactersPlayer[i].actionsAvalible.Length; a++)
            {
                try { int temp = charactersPlayer[i].actionsAvalible[a].baseBehaviours.ActionBehaviour[0].cost; }           //Action is a non-nullable type, check validity by checking first subBehaviour
                catch { Debug.Log($"unassigned player action at <{i}> Filling with default."); charactersPlayer[i].actionsAvalible[a] = DefaultAction; }
                  
                charactersPlayer[i].actionsAvalible[a].Initialise();
            }

            //initialising action owners
            charactersPlayer[i].actionChosen.ownerID = charactersPlayer[i].GetHashCode();
            foreach (Action a in charactersPlayer[i].actionsAvalible)
                a.ownerID = charactersPlayer[i].GetHashCode();

            if (charactersPlayer[i].actionChosen == null) SelectAction(0, i);
        }

        //if enemies have been reset or deleted, set default values, default attacks
        for (int i = 0; i < charactersEnemy.Count; i++)
        {
            if (charactersEnemy[i].hpMax == 0)
            {
                charactersEnemy[i].actionChosen = DefaultAction;
                charactersEnemy[i].name = "Zombie fairy";
            }
        }

        //Initialising Enemy Characters
        GenerateEnemyAttacksAndMoves();
        for (int i = 0; i < charactersEnemy.Count; i++)
        {
            //Refill health
            charactersEnemy[i].hpCur = charactersEnemy[i].hpMax;
            charactersEnemy[i].position = i + 1 + charactersPlayer.Count;

            //Initialising action instances from base action Scriptable Objects
            for (int a = 0; a < charactersEnemy[i].actionsAvalible.Length; a++)
            {
                try { int temp = charactersEnemy[i].actionsAvalible[a].baseBehaviours.ActionBehaviour[0].cost; }           //Action is a non-nullable type, check validity by checking first subBehaviour
                catch { Debug.Log($"unassigned action at enemy index <{i}> filling with default"); charactersEnemy[i].actionsAvalible[a] = DefaultAction; }
            }

            charactersEnemy[i].actionChosen.Initialise(); ;
            foreach (Action a in charactersEnemy[i].actionsAvalible) a.Initialise();

            //initialising action owners
            charactersEnemy[i].actionChosen.ownerID = charactersEnemy[i].GetHashCode();
            foreach (Action a in charactersEnemy[i].actionsAvalible)
                a.ownerID = charactersEnemy[i].GetHashCode();

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
        //Reset movement-ability of player
        for (int i = 0; i < PlayerMovementActions.Length; i++) PlayerMovementActions[i] = 1;
        

        //Wait until the player enters battle
        while (!isInBattle || curStage == BattleStage.planning) yield return new WaitForSeconds(0.5f);
        
        while (isInBattle && curStage != BattleStage.planning) 
        {
            //Show targetted position for actions chosen, enact one of the selected actions
            if (curStage == BattleStage.playerAct)
            {
                Debug.Log($" entered act");
                GenerageVengancePoints(2);

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
                DetermineNextEnemy();
                //Switch Animator to show the prepared moves panel
                UIManager.instance.AnimatorTrigger("SwitchToMove");
                Invoke("PreviewEnemyBehaviour", EnemyAttackDelay);                //Preview what the enemy is about to do, where to move

                //Wait for player to choose a move
                while (curStage == BattleStage.playerMove)
                {
                    yield return new WaitForEndOfFrame();
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
                yield return new WaitForSeconds(EnemyMoveDelay);
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

    public void GenerageVengancePoints(int amount) {venganceCur += amount; Mathf.Clamp(venganceCur, 0, venganceMax); }

    //trajectory parabola and shifting arrow methods are found here
    #region targetting and action previews
    public void PreviewActionOfCharacter(int position, int attackIndex) 
    { 
        if (CheckIfValidAction(position, attackIndex)) 
        { 
            if(GetCharacterByPosition(position).CheckIsThisPlayer() && curStage == BattleStage.playerAct && attackIndex == -1)  //show when previewing player attacks locked in during combat
                GetCharacterByPosition(position).actionChosen.Preview(true);

            if(GetCharacterByPosition(position).CheckIsThisPlayer() && curStage == BattleStage.planning && attackIndex!=-1)     //show when previewing attack options in planning phase
                GetCharacterByPosition(position).actionsAvalible[attackIndex].Preview(true);

        }  
    }
    public void PreviewActionSelected(int attackIndex) => PreviewActionOfCharacter(UIManager.instance.selectedCharacter + 1, attackIndex);

    public void PreviewCharacterAction(Transform position) => PreviewActionOfCharacter(GetPositionByCharacter(GetCharacterBySprite(position)),-1); //-1 previews chosen attack, 0-3 preview attack options
    public void PreviewEndCharacterAction(int position) 
    { 
        if (GetCharacterByPosition(position).actionChosen != null) 
        {
            //if (curStage == BattleStage.playerMove) return;     //Can't hide anything during player Move stage            position < 5 && 
            //if (curStage == BattleStage.playerAct) return;     //Can't hide anything during player Move stage            position < 5 && 

            if (curStage==BattleStage.playerAct && position < 5) GetCharacterByPosition(position).actionChosen.Preview(false);  //
        }
    } 
    public void PreviewEnemyBehaviour() 
    {
        if (CheckIfValidAction(GetCharacterByPosition(4+EnemyCounter).position) && curStage == BattleStage.playerMove)  //if the pleyer is moving, preview what the enemy is going to do
        { 
            GetCharacterByPosition(4 + EnemyCounter).actionChosen.Preview(true);
            UIManager.instance.ShowMoveArrow(GetSpriteByPosition(4 + EnemyCounter), EnemyCounter - 1);
        } 
    }

    #endregion



    public void SwitchBetweenPlanActPhases()
    {
        TrajectorySelect(); //Break target selection loop. Must be BEFORE changing selectedCharacter to work
        UIManager.instance.selectedCharacter = 0;
        if (curStage != BattleStage.planning) curStage = BattleStage.planning;
        else curStage = BattleStage.playerAct;
        

        if (curStage == BattleStage.planning)
        {
            UIManager.instance.SelectedArrow.SetActive(true);
            UIManager.instance.MovePreviewArrow.SetActive(false);
            UIManager.instance.selectedCharacter = 0;
            UIManager.instance.AnimatorTrigger("SwitchToPlan");
        }
        else
        {
            UIManager.instance.SelectedArrow.SetActive(false);

            UIManager.instance.AnimatorTrigger("SwitchToAct");

            //Force action [0] if none were selected
            for (int i = 0; i < 4; i++)
            {
                if (charactersPlayer[i].actionChosen == null) SelectAction(0, i);
            }
            UIManager.instance.LoadActionDescriptions();   //Load details of actions chosen in plan phase and displays them on execute actions panel
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

    public void SelectAction(int actionIndex, int characterIndex)
    {
        Character c;
        try { c = charactersPlayer[characterIndex]; } //if out of bounds, break
        catch { Debug.LogWarning("out of bounds call on ability setting"); return; }

        c.actionChosen = c.actionsAvalible[actionIndex];
        c.actionChosen.Initialise();

        //If selectable targets, invoke selection process. Terminate existing coroutine on double-call to avoid multiple running in parallel
        if(!isChoosingTarget) StartCoroutine(selectTarget());
        else { TrajectorySelect(); StartCoroutine(selectTarget()); }

        //Find apropriate action token and pulse it
        Transform tokenParent = null;
        try
        {
            tokenParent = characterPositions[characterIndex];
            for (int i = 0; i < tokenParent.childCount; i++)
            {
                if (tokenParent.GetChild(i).CompareTag("token")) { UIManager.instance.AnimatorTrigger("pulse" + tokenParent.GetChild(i).name); Debug.Log("pulse" + tokenParent.GetChild(i).name); return; }
            }
        }
        catch { Debug.LogWarning("Action Token missing, cannot destroy"); }
    }
    public IEnumerator selectTarget() 
    {
        isChoosingTarget = true;
        while (isChoosingTarget) 
        {
            yield return null;
            if (Input.GetKeyDown(KeyCode.O)) 
            {
                TrajectoryCycle();
                if(UIManager.instance.selectedCharacter > 0) UIManager.instance.ShowTargetParabola(GetSpriteByPosition(UIManager.instance.selectedCharacter + 1).position, GetSpriteByPosition(TargetChosenNow).position, -1);
                //Snazzy trajectory display here
            }
            else if (Input.GetKeyDown(KeyCode.P))
            {
                isChoosingTarget = false;
                TrajectorySelect();
                break;                      //loop break condition
            }
            else if(Input.GetKeyUp(KeyCode.O))
            {
                UIManager.instance.HideTargetParabola();
            }
        }
    }
    public void TrajectoryCycle() //cycle the POSITIONS avalible for a selectable multi-trajectory attack
    {
        Action a = GetCharacterByPosition(UIManager.instance.selectedCharacter+1).actionChosen; //Selected character is index-based, starting from 0, not 1

        for (int b = 0; b < a.updatedBehaviours.Count; b++)
        {
            ActionBehaviour behaviour = a.updatedBehaviours[b];
            if (behaviour.targets.multiTargetLogic == GameManager.Logic.SelectOr)
            {
                TargetChosenNow++; 
                Debug.Log($"targets possible: {a.GetTargetPositions(behaviour).Count} last target {a.GetTargetPositions(behaviour)[a.GetTargetPositions(behaviour).Count - 1]}");
                
                if (TargetChosenNow > a.GetTargetPositions(behaviour)[a.GetTargetPositions(behaviour).Count - 1]) 
                { TargetChosenNow = a.GetTargetPositions(behaviour)[0];}
                //a.GetTargetPositions()[a.GetTargetPositions().Count - 1]
                //a.GetTargetPositions(behaviour)[0]

                
                //cycle between the different targets possible
            }
        }
    }
    //gets player index 0, 


    public void TrajectorySelect() //For selectable targets, this confirms the option chosen 
    {
        //StopCoroutine(selectTarget());
        isChoosingTarget = false;

        Action a = GetCharacterByPosition(UIManager.instance.selectedCharacter+1).actionChosen; //Selected character is index-based, starting from 0, not 1
        if (CheckIfValidAction(UIManager.instance.selectedCharacter + 1)) { Debug.LogWarning("Cannot confirm action, aborting locking."); }


        for (int b = 0; b < a.updatedBehaviours.Count; b++)
        {
            ActionBehaviour behaviour = a.updatedBehaviours[b];
            //modify the behaviour from a select logic, to AND logic once selected
            if (behaviour.targets.multiTargetLogic == GameManager.Logic.SelectOr)
            {
                behaviour.targets.multiTargetLogic = GameManager.Logic.And;
                //TargetChosenNow is a position index, not distance. Distances hit operates on range, so it must be converted from targetHit to range.
                behaviour.targets.distancesHit = new int[] { TargetChosenNow - UIManager.instance.selectedCharacter-1 };
                a.updatedBehaviours[b] = behaviour;
            }
        }
        Debug.Log($"Target for {GetCharacterByPosition(UIManager.instance.selectedCharacter + 1).actionChosen.name} confirmed");
        GetCharacterByPosition(UIManager.instance.selectedCharacter + 1).actionChosen = a; //Selected character is index-based, starting from 0, not 1
    }

    #region ACT phase behaviours
    public void CharacterAct(int position) //Execute the character's action and trigger a corresponding enemy reaction (player 3, triggers enemy 3)
    {
        Character c = GetCharacterByPosition(position);
        if (CheckIfValidAttacker(c.position) == false) return;

        if (c.CheckIsThisPlayer())                  //if it's player character acting
        {
            if (venganceCur < c.actionChosen.updatedBehaviours[0].cost) { curStage = BattleStage.playerMove; return; }
            venganceCur -= c.actionChosen.updatedBehaviours[0].cost;
        }

        c.actionChosen.Perform();
        if (curStage == BattleStage.playerAct) curStage = BattleStage.playerMove;   //prgress to move stage if action points exhausted

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
        Debug.Log($"Player Unit {position} is moving");
        int i = position - 1;                       //Index starts at 0, positions start as 1; shifting by 1 for clarity
        if (PlayerMovementActions[i] < 1) return;   //breaking if this character has already moved
        PlayerMovementActions[i] = 0;
        PlayerMoveSimple(i, PlayerMovementDirection[i]);
        Debug.Log($"Player Unit {position} has moved to position : {i + PlayerMovementDirection[i]}");

        //after the player action is performed, proceed to the enemy action
        curStage = BattleStage.enemyAct;
        UIManager.instance.AnimatorTrigger("SwitchToAct");
    }                   //PlayerMove with turn logic and animations                             - use for actual moves
    public void PlayerMoveSimple(int index, int direction) //PlayerMove stripped of turn logic, animations and move token handling - use for special moving via actions, environment and such
    {
        SwappingCharacterElements(charactersPlayer,   index, index + direction);
        SwappingTransformElements(characterPositions, index, index + direction);
    }

    // this is set to have each character in a row attack, so each enemy hits once per turn. the max amount of actions for enemies for now is the same as the player
    void EnemyAct() => CharacterAct(4+EnemyCounter);
    void EnemyMove(int EnemyPosition)
    {
        EnemyMovementActions[EnemyPosition - 1] = 0;
        SwappingCharacterElements(charactersEnemy,      EnemyPosition - 1, EnemyPosition - 1 + EnemyMovementDirection[EnemyPosition-1]);      //Swap enemy character data
        SwappingTransformElements(characterPositions,   EnemyPosition + 3, EnemyPosition + 3 + EnemyMovementDirection[EnemyPosition-1]);      //swap enemy sprites (offset by player count-1)
        Debug.Log($"enemy Unit { EnemyPosition} has moved to spot {EnemyPosition + EnemyMovementDirection[EnemyPosition-1]}");
        RecalculateCharacterPositions();

        DetermineNextEnemy();
        if (EnemyCounter > -1) curStage = BattleStage.playerAct;
        else SwitchBetweenPlanActPhases();  //when the last enemy performs their attack, return to planning phase
    }   //perform the planned move and retrieve next enemy to act
    void GenerateEnemyAttacksAndMoves() //DOES NOT DO ANYTHING YET 
    {
        for (int i = 0; i < charactersEnemy.Count; i++)
        {
            //SelectAction(0, i);
            if (charactersEnemy[i].actionChosen.baseBehaviours == null || charactersEnemy[i].actionChosen==null)
                charactersEnemy[i].actionChosen = charactersEnemy[i].actionsAvalible[0];    //Just choose the first one avalible
        }
    }
    int DetermineNextEnemy() //Determine which enemy should act next(tries to attack from right to left) 
    {
        int rightmost = -1;

        for (int i = 4; i >0; i--)
        {
            if (CheckIfValidAction(charactersEnemy[i - 1].position) && charactersEnemy[i-1].position > rightmost) //if has a valid attack and is the rightmost avalible
            { rightmost = charactersEnemy[i-1].position; } 
        }

        EnemyCounter = rightmost;
        if (rightmost < 0)  { EnemyCounter = -1;            return -1; }  //if no enemies have valid attacks, end the whole turn
        else                { EnemyCounter = rightmost - 4; return rightmost - 4; }
        
    }

    public void ApplyDebuff(string _name, int position) 
    {
        //might want to do some tokens representing the debuff

        switch(_name) 
        {
            case ("poisoned"): { break; }   //take damage continously.
            case ("poisoned2"): { break; }   //take damage continously.
            case ("poisoned3"): { break; }   //take damage continously.
            case ("braced"): { break; }     //take 50% damage

            case ("huffed up"): { break; }  //empower blow if active
            case ("puffed up"): { break; }  //take -2 incoming damage, empower blow if active
            case ("charmed"): { break; }    //if hits caster, take damage themself
            case ("vulnerable"): { break; } //takes 50% more damage
            case ("sleeping"): { break; }   //cannot act
        }

        Debug.LogWarning($"Applying a snazzy debuff <{_name}>");
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
        if (position < 0) return null;
        if (position - 1 < charactersPlayer.Count) { return charactersPlayer[position - 1]; }
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
    public string    GetNodeByPosition(int position) 
    {
        Transform NodeParent= GetSpriteByPosition(position);
        Transform temp =null;
        for (int i = 0; i < NodeParent.childCount; i++)
        {
            if (NodeParent.GetChild(i).CompareTag("token"))
                {
                if (NodeParent.GetChild(i).name == "ActionToken1" ||
                    NodeParent.GetChild(i).name == "ActionToken2" ||
                    NodeParent.GetChild(i).name == "ActionToken3" ||
                    NodeParent.GetChild(i).name == "ActionToken3")
                    {
                    temp = NodeParent.GetChild(i);
                    Debug.DrawRay(temp.transform.position, Vector3.up, Color.red, 1f);
                    return temp.name;
                    } 
                }
                    
        }
        Debug.LogWarning("Could not retrieve");
        return null;
    }

    public int GetDistanceBetweenActors(int posAttacker, int posHit)
    {
        int distance = 0;
        //if player damaging enemy - simple math.
        if (posAttacker > 4) distance = posHit - posAttacker;
        //if enemy damaging player - distance must be negative to show leftward direction
        else distance= -(posHit - posAttacker);

        return distance;
    }

    public bool         CheckIfValidAttacker(int position)
    {
        if (CheckIfValidAction(position) && !GetCharacterByPosition(position).isDead) { return true; }  //Debug.Log(GetCharacterByPosition(position).name + " is a valid attacker"); 
        else return false;
    }      //If the player exists, has an attack equipped and is alive - can attack

    public bool CheckIfValidAction(int position) => CheckIfValidAction(position, -1);   //If action unspecified, check CHOSEN action
    public bool         CheckIfValidAction(int position, int attackIndex)//Use index -1 for action CHOSEN, 0-3 for avalible actions
    {
        

        //check if downright null
        if (GetCharacterByPosition(position) == null)                                { Debug.LogWarning($"Character at position {position} is invalid");                    return false; }
        if (attackIndex<0 && GetCharacterByPosition(position).actionChosen == null)  { Debug.LogWarning($"Character at position {position} action is null!");               return false; }
        if (attackIndex>0 && GetCharacterByPosition(position).actionsAvalible[attackIndex]==null) { Debug.LogWarning($"Character at position {position} action is null!");  return false; }

        //check if reset to 0 properties but interpreted as non-null
        string s;
        try
        {
            if (attackIndex < 1) s = GetCharacterByPosition(position).actionChosen.name;
            else s = GetCharacterByPosition(position).actionsAvalible[attackIndex].name;
            if (s.Length < 1)                                       { Debug.LogWarning($"Character at position {position} has empty action!"); return false; }
        }
        catch                                                       { Debug.LogWarning($"Character at position {position} has suspicious action!");return false; }

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
