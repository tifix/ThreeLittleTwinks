/* This class handles the flow of the battle - initialising characters, turn order etc.
 * 
 * Current Debug mapping QWER character actions
 * Alpha1 - WIN encounter
 * Alpha9 - LOOSE encounter
 */

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;


public class BattleManager : MonoBehaviour
{
    public static BattleManager instance;
    [Header("Game state")]
    public bool isInBattle = true;
    public bool isPlanningStage = true;

    //Data of player characters and enemy characters
    public List <Character> charactersPlayer =  new List<Character>(4);
    public List <Character> charactersEnemy =   new List<Character>(4);
    public List<Transform> characterPositions = new List<Transform>(8);
    public string battleID="1";

    //Data of player characters and enemy characters
    //public GameObject[] PlayerSprites = new GameObject[4];
    //public GameObject[] EnemySprites = new GameObject[4];

    public Vector3 PlayerSwapPosition;
    public string SceneTranisiton;

    public GameObject HitMarker;
    public Action DefaultAction;

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
        StartEncounter("1");                        //Initialise health to full and actions from base action values.
        UIManager.instance.RefreshStatusCorners();  //Once character data is fully retrieved, update the current stats to match.
    }   

    //Execute character actions when the player presses corresponding keys.  Needs constrainment to 1 per turn.
    void Update()
    {
        if (!isInBattle) return;
        if (!isPlanningStage) 
        {
            if (Input.GetKeyDown(KeyCode.Q) && charactersPlayer.Count > 0) ExecuteCharacterAction(charactersPlayer[0]);
            if (Input.GetKeyDown(KeyCode.W) && charactersPlayer.Count > 1) ExecuteCharacterAction(charactersPlayer[1]);
            if (Input.GetKeyDown(KeyCode.E) && charactersPlayer.Count > 2) ExecuteCharacterAction(charactersPlayer[2]);
            if (Input.GetKeyDown(KeyCode.R) && charactersPlayer.Count > 3) ExecuteCharacterAction(charactersPlayer[3]);
        }
        if (Input.GetKeyDown(KeyCode.A) && charactersEnemy.Count > 0) ExecuteCharacterAction(charactersEnemy[0]);
        if (Input.GetKeyDown(KeyCode.S) && charactersEnemy.Count > 1) ExecuteCharacterAction(charactersEnemy[1]);
        if (Input.GetKeyDown(KeyCode.D) && charactersEnemy.Count > 2) ExecuteCharacterAction(charactersEnemy[2]);
        if (Input.GetKeyDown(KeyCode.F) && charactersEnemy.Count > 3) ExecuteCharacterAction(charactersEnemy[3]);
        if (Input.GetKeyDown(KeyCode.Alpha0))   EndEncounter(true);
        if (Input.GetKeyDown(KeyCode.Alpha9))   EndEncounter(false); 
    }
    public void ExecuteCharacterAction(int index)=>ExecuteCharacterAction(charactersPlayer[index]);
    public void ExecuteCharacterAction(Character c)    //Execute the selected action of a given Character.
    {
        if(CheckIfActionValid(c.position)) c.actionChosen.Perform();
        UIManager.instance.RefreshStatusCorners();
    }
    public void PreviewCharacterAction(Transform position) { if (CheckIfActionValid(GetCharacterFromPosition(position).position)) GetCharacterFromPosition(position).actionChosen.Preview(true); }
    public void EndPreviewCharacterAction(int position) => GetCharacterByPosition(position).actionChosen.Preview(false);
    public void ToggleActionPreview(Character c, bool state) => c.actionChosen.Preview(state);

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
    public Character GetCharacterByPosition(int position) //To be tested
    {
        if(position-1 < charactersPlayer.Count)return charactersPlayer[position-1];
        else return charactersEnemy[position-charactersPlayer.Count-1];
    }
    public Character GetCharacterFromPosition(Transform t) 
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

    }
    public ActionBase GetActionBaseByName(string name) 
    {
        ScriptableObject temp = (ScriptableObject)Resources.Load("Actions/" + name);
        Debug.LogWarning(temp.name);
        return temp as ActionBase;
    }

    public bool CheckIfActionValid(int position) 
    {
        string s;
        try { s= GetCharacterByPosition(position).actionChosen.name; }
        catch { return false; }

        if(s.Length<1) return false;
        Debug.Log(s + " is valid");
        return true;
    }


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
        TurnCounter = 0;
    }   //Initialise health to full and actions from base action values.

    public void RecalculateCharacterPositions() //Rethink where the various characters are after one's been removed
    {

        for (int i = 0; i < charactersPlayer.Count; i++)
            charactersPlayer[i].position = i + 1;

        for (int i = 0; i < charactersEnemy.Count; i++)
            charactersEnemy[i].position = i + charactersPlayer.Count+1;   //count increases by 1 natively
    }

    /*
    public void EndEncounter(bool isWon)
    {
        isInBattle=false;
        Debug.Log($"Just finished encounter: {battleID}");
        if (isWon) 



        IsBattleOver();
        if (TurnCounter == 0) { PlayerTurn(); PreparingPlayerMovements(); PlayerUnitAttacking(); SelectingPlayerUnit(); }
        if (TurnCounter == 1) { EnemyTurn(); EnemyAttacks(); PlayerReactions(); SelectingPlayerUnit(); }
    }
    */
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



}
