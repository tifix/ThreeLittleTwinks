/* This class handles the flow of the battle - initialising characters, turn order etc.
 * 
 * 
 */

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;


public class BattleManager : MonoBehaviour
{
    //Data of player characters and enemy characters
    public Character[] charactersPlayer = new Character[4];
    public Character[] charactersEnemy = new Character[4];
    public GameObject[] PlayerSprites = new GameObject[4];
    public GameObject[] EnemySprites = new GameObject[4];
    public Transform[] characterPositions = new Transform[8];

    public Vector3 PlayerSwapPosition;
    public string SceneTranisiton;

    public static BattleManager instance;
    public GameObject HitMarker;

    //Amount of actions and movements the player can make
    public int MaxActionPoints = 4;
    private int CurrentActionPoints;
    public int[] PlayerMovementActions = { 1, 1, 1, 1 };//each unit has a specific amonut of movement, which works off of charactersPlayer. when a movement is done, it cannot be done a gain for that unit
    public int[] PlayerMovementDirection = { 0, 0, 0, 0 };// keeps track of what position units have been prepped to move too, -1 = left, 1 = right, 0 = No Movement prepped.
    public bool[] PlayersDead = { false, false, false, false };
    public bool[] EnemiesDead = { false, false, false, false };
    public int PlayerDeathCount = 0;


    //Amount of Actions enemy can make
    public int MaxEnemyActions = 4;
    private int EnemyActions = 4;
    public float EnemyAttackDelay = 2f;
    public int EnemyCounter = 0;
    public int EnemyDeathCount = 0;
    public int[] EnemyMovementActions = { 1, 1, 1, 1 };//each unit has a specific amonut of movement, which works off of charactersPlayer. when a movement is done, it cannot be done a gain for that unit
    public int[] EnemyMovementDirection = { 1, -1, -1, -1 };// keeps track of what position units have been prepped to move too, -1 = left, 1 = right, 0 = No Movement prepped.

    public int PlayerCharacterSelection = 3;

    public int TurnCounter = 0;
    public bool OutOfTokens = true;//For refilling player and enemy tokens, if this is true during player turn, player actions are refilled and This variable becomes false.

    //Assign instance if none found, otherwise destroy
    private void Awake()
    {
        if(instance == null) instance = this;
        else Destroy(this);
    }

    void Start()
    {
        //Initialising Player Characters
        for (int i = 0; i < charactersPlayer.Length; i++)
        {
            //Refill health
            charactersPlayer[i].hpCur = charactersPlayer[i].hpMax;
            charactersPlayer[i].position = i+1;

            //Initialising action instances from base action Scriptable Objects
            charactersPlayer[i].actionChosen.Initialise(); ;
            foreach (Action a in charactersPlayer[i].actionsAvalible) a.Initialise();

            //initialising action owners
            charactersPlayer[i].actionChosen.ownerID = charactersPlayer[i].GetHashCode();
            foreach (Action a in charactersPlayer[i].actionsAvalible) 
                a.ownerID = charactersPlayer[i].GetHashCode();

            if (charactersPlayer[i].actionChosen == null) charactersPlayer[i].actionChosen = charactersPlayer[i].actionsAvalible[0];
        }

        //Initialising Enemy Characters
        for (int i = 0; i < charactersEnemy.Length; i++)
        {
            //Refill health
            charactersEnemy[i].hpCur = charactersEnemy[i].hpMax;
            charactersEnemy[i].position = i + 1;

            //Initialising action instances from base action Scriptable Objects
            charactersEnemy[i].actionChosen.Initialise(); ;
            foreach (Action a in charactersEnemy[i].actionsAvalible) a.Initialise();

            //initialising action owners
            charactersEnemy[i].actionChosen.ownerID = charactersEnemy[i].GetHashCode();
            foreach (Action a in charactersEnemy[i].actionsAvalible)
                a.ownerID = charactersEnemy[i].GetHashCode();

            if (charactersEnemy[i].actionChosen == null) charactersEnemy[i].actionChosen = charactersEnemy[i].actionsAvalible[0];
        }
        TurnCounter = 0;
    }   //Initialise health to full and actions from base action values.

    //Execute character actions when the player presses corresponding keys.  Needs constrainment to 1 per turn.
    void Update()
    {
        IsBattleOver();
        if (TurnCounter == 0) { PlayerTurn(); PreparingPlayerMovements(); PlayerUnitAttacking(); SelectingPlayerUnit(); }
        if (TurnCounter == 1) { EnemyTurn(); EnemyAttacks(); PlayerReactions(); SelectingPlayerUnit(); }
    }

    void ExecuteCharacterAction(Character c)  //Execute the selected action of a given Character.
    {
        c.actionChosen.Perform();
    }

    void IsBattleOver()
    {

        //checks for if all players are dead
        for (int i = 0; i < 4; i++)
        {
            if (charactersPlayer[i].hpCur <= 0 && PlayersDead[i] == false) { PlayersDead[i] = true; PlayerDeathCount += 1; }
            if (PlayerDeathCount == 4)
            {
                if (Input.GetKeyDown(KeyCode.Return)) { SceneManager.LoadScene(SceneTranisiton); }
            }
            else if (EnemyDeathCount == 4)
            {
                if (Input.GetKeyDown(KeyCode.Return)) { SceneManager.LoadScene(SceneTranisiton); }
            }
        }

        //checks for if all enemies are dead
        for (int i = 0; i < 4; i++)
        {
            if (charactersEnemy[i].hpCur <= 0 && EnemiesDead[i] == false) { EnemiesDead[i] = true; EnemyDeathCount += 1; }
        }
    }

    void PlayerTurn()
    {
        // when player turn starts, restore action points to maximum, the value inside MaxActionPoints Variable.
        if (OutOfTokens && TurnCounter == 0) { CurrentActionPoints = MaxActionPoints; OutOfTokens = false; }

        if (CurrentActionPoints == 0)
        {
            TurnCounter = 1;
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
        if (Input.GetKeyDown(KeyCode.Q) && PlayersDead[0] != true)
        {
            PlayerCharacterSelection = 0;
        }
        else if (Input.GetKeyDown(KeyCode.W) && PlayersDead[1] != true)
        {
            PlayerCharacterSelection = 1;
        }
        else if (Input.GetKeyDown(KeyCode.E) && PlayersDead[2] != true)
        {
            PlayerCharacterSelection = 2;
        }
        else if (Input.GetKeyDown(KeyCode.R) && PlayersDead[3] != true)
        {
            PlayerCharacterSelection = 3;
        }

    }

    void PreparingPlayerMovements()
    {
        //This actually deciding movements for the player unit selected,
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (PlayerCharacterSelection == 0) { PlayerMovementDirection[PlayerCharacterSelection] = 3; Debug.Log("Player Unit " + PlayerCharacterSelection + " will move backwards"); }
            else { PlayerMovementDirection[PlayerCharacterSelection] = -1; Debug.Log("Player Unit " + PlayerCharacterSelection + " move backwards"); }
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (PlayerCharacterSelection == 3) { PlayerMovementDirection[PlayerCharacterSelection] = -3; Debug.Log("Player Unit " + PlayerCharacterSelection + " will move backwards"); }
            else { PlayerMovementDirection[PlayerCharacterSelection] = 1; Debug.Log("Player Unit " + PlayerCharacterSelection + " will move forwards"); }
        }
    }

    void PlayerUnitAttacking()
    {
        if (TurnCounter == 0)
        {

            if (Input.GetKeyDown(KeyCode.Alpha1) && PlayersDead[0] != true)
            {
                ExecuteCharacterAction(charactersPlayer[0]);
                CurrentActionPoints -= 1;
                if(EnemiesDead[0] != true)
                {
                    EnemyReactions(0);
                }
            }
            if (Input.GetKeyDown(KeyCode.Alpha2) && PlayersDead[1] != true)
            {
                ExecuteCharacterAction(charactersPlayer[1]);
                CurrentActionPoints -= 1;
                if (EnemiesDead[1] != true)
                {
                    EnemyReactions(1);
                }
            }
            if (Input.GetKeyDown(KeyCode.Alpha3) && PlayersDead[2] != true)
            {
                ExecuteCharacterAction(charactersPlayer[2]);
                CurrentActionPoints -= 1;
                if (EnemiesDead[2] != true)
                {
                    EnemyReactions(2);
                }
            }
            if (Input.GetKeyDown(KeyCode.Alpha4) && PlayersDead[3] != true)
            {
                ExecuteCharacterAction(charactersPlayer[3]);
                CurrentActionPoints -= 1;
                if (EnemiesDead[3] != true)
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
        SwappingCharacterElements(charactersEnemy, EnemyIndex, (EnemyIndex + EnemyMovementDirection[EnemyIndex]));
        SwappingArrayElements(EnemyMovementActions, EnemyIndex, (EnemyIndex + EnemyMovementDirection[EnemyIndex]));
        SwappingTransformElements(characterPositions, (EnemyIndex), ((EnemyIndex) + (EnemyMovementDirection[EnemyIndex])));
        SwappingGameObjectElements(EnemySprites, EnemyIndex, (EnemyIndex + EnemyMovementDirection[EnemyIndex]));
        Debug.Log("enemy Unit " + EnemyIndex + " has moved");
    }

    void EnemyTurn()
    {
        if (!OutOfTokens && TurnCounter == 1) { EnemyActions = MaxEnemyActions; OutOfTokens = true; }

        if (EnemyActions == 0)
        {
            TurnCounter = 0;
            EnemyCounter = 0;
        }
    }

    // this is set to have each character in a row attack, so each enemy hits once per turn. the max amount of actions for enemies for now is the same as the player
    void EnemyAttacks()
    {
        if (TurnCounter == 1)
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
        if (TurnCounter == 1)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) && PlayerMovementActions[0] == 1)
            {
                PlayerMovementActions[0] = 0;
                SwappingCharacterElements(charactersPlayer, 0, (0 + PlayerMovementDirection[0]));
                SwappingArrayElements(PlayerMovementActions, 0, (0 + PlayerMovementDirection[0]));
                SwappingTransformElements(characterPositions, 0, (0 + PlayerMovementDirection[0]));
                SwappingGameObjectElements(PlayerSprites , 0, (0 + PlayerMovementDirection[0]));
                SwappingArrayElements(PlayerMovementDirection, 0, (0 + PlayerMovementDirection[0]));
                Debug.Log("Player Unit 1 has moved");
            }
            if (Input.GetKeyDown(KeyCode.Alpha2) && PlayerMovementActions[1] == 1)
            {
                PlayerMovementActions[1] = 0;
                SwappingCharacterElements(charactersPlayer, 1, (1 + PlayerMovementDirection[1]));
                SwappingArrayElements(PlayerMovementActions, 1, (1 + PlayerMovementDirection[1]));
                SwappingTransformElements(characterPositions, 1, (1 + PlayerMovementDirection[1]));
                SwappingGameObjectElements(PlayerSprites, 1, (1 + PlayerMovementDirection[1]));
                SwappingArrayElements(PlayerMovementDirection, 1, (1 + PlayerMovementDirection[1]));
                Debug.Log("Player Unit 2 has moved");
            }
            if (Input.GetKeyDown(KeyCode.Alpha3) && PlayerMovementActions[2] == 1)
            {
                PlayerMovementActions[2] = 0;
                SwappingCharacterElements(charactersPlayer, 2, (2 + PlayerMovementDirection[2]));
                SwappingArrayElements(PlayerMovementActions, 2, (2 + PlayerMovementDirection[2]));
                SwappingTransformElements(characterPositions, 2, (2 + PlayerMovementDirection[2]));
                SwappingGameObjectElements(PlayerSprites, 2, (2 + PlayerMovementDirection[2]));
                SwappingArrayElements(PlayerMovementDirection, 2, (2 + PlayerMovementDirection[2]));
                Debug.Log("Player Unit 3 has moved");
            }
            if (Input.GetKeyDown(KeyCode.Alpha4) && PlayerMovementActions[3] == 1)
            {
                PlayerMovementActions[3] = 0;
                SwappingCharacterElements(charactersPlayer, 3, (3 + PlayerMovementDirection[3]));
                SwappingArrayElements(PlayerMovementActions, 3, (3 + PlayerMovementDirection[3]));
                SwappingTransformElements(characterPositions, 3, (3 + PlayerMovementDirection[3]));
                SwappingGameObjectElements(PlayerSprites, 3, (3 + PlayerMovementDirection[3]));
                SwappingArrayElements(PlayerMovementDirection, 3, (3 + PlayerMovementDirection[3]));
                Debug.Log("Player Unit 4 has moved");
            }
        }
    }

    //used for moving character to different positions on the board
    public void SwappingArrayElements(int[] Array, int Index1, int Index2)
    {
        (Array[Index1], Array[Index2]) = (Array[Index1], Array[Index2]);
    }

    public void SwappingCharacterElements(Character[] Array, int Index1, int Index2)
    {

        (Array[Index1], Array[Index2]) = (Array[Index1], Array[Index2]);
    }

    public void SwappingTransformElements(Transform[] Array, int Index1, int Index2)
    {
        (Array[Index1], Array[Index2]) = (Array[Index1], Array[Index2]);
    }

    public void SwappingGameObjectElements(GameObject[] Array, int Index1, int Index2)
    {
        PlayerSwapPosition = Array[Index1].transform.position;

        Array[Index1].transform.position = Array[Index2].transform.position;

        Array[Index2].transform.position = PlayerSwapPosition;

        (Array[Index1], Array[Index2]) = (Array[Index1], Array[Index2]);
    }

    #region utilities
    //Gets character by their ID values
    public Character GetCharacterByID(int ID) 
    {
        for (int i = 0; i < charactersPlayer.Length; i++)
        {
            if (charactersPlayer[i].GetHashCode() == ID) return charactersPlayer[i];
        }
        for (int i = 0; i < charactersEnemy.Length; i++)
        {
            if (charactersEnemy[i].GetHashCode() == ID) return charactersEnemy[i];
        }

        Debug.LogWarning("Failed to retrive character by ID");
        return null;
    }
    public ActionBase GetActionBaseByName(string name) 
    {
        ScriptableObject temp = (ScriptableObject)Resources.Load("Actions/" + name);
        Debug.LogWarning(temp.name);
        return temp as ActionBase;
    }
    #endregion


}
