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

public class BattleManager : MonoBehaviour
{
    public bool isInBattle = true;
    //Data of player characters and enemy characters
    public List <Character> charactersPlayer =  new List<Character>(4);
    public List <Character> charactersEnemy =   new List<Character>(4);
    public List<Transform> characterPositions = new List<Transform>(8);
    public string battleID="1";

    public static BattleManager instance;
    public GameObject HitMarker;
    public Action DefaultAction;

    //Assign instance if none found, otherwise destroy
    private void Awake()
    {
        if(instance == null) instance = this;
        else Destroy(this);
    }

    void Start()
    {
        StartEncounter("1"); //Initialise health to full and actions from base action values.

    }   

    //Execute character actions when the player presses corresponding keys.  Needs constrainment to 1 per turn.
    void Update()
    {
        if (!isInBattle) return;
        if (Input.GetKeyDown(KeyCode.Q) && charactersPlayer.Count > 0)  ExecuteCharacterAction(charactersPlayer[0]); 
        if (Input.GetKeyDown(KeyCode.W) && charactersPlayer.Count > 1)  ExecuteCharacterAction(charactersPlayer[1]); 
        if (Input.GetKeyDown(KeyCode.E) && charactersPlayer.Count > 2)  ExecuteCharacterAction(charactersPlayer[2]); 
        if (Input.GetKeyDown(KeyCode.R) && charactersPlayer.Count > 3)  ExecuteCharacterAction(charactersPlayer[3]);
        if (Input.GetKeyDown(KeyCode.A) && charactersEnemy.Count > 0) ExecuteCharacterAction(charactersEnemy[0]);
        if (Input.GetKeyDown(KeyCode.S) && charactersEnemy.Count > 1) ExecuteCharacterAction(charactersEnemy[1]);
        if (Input.GetKeyDown(KeyCode.D) && charactersEnemy.Count > 2) ExecuteCharacterAction(charactersEnemy[2]);
        if (Input.GetKeyDown(KeyCode.F) && charactersEnemy.Count > 3) ExecuteCharacterAction(charactersEnemy[3]);
        if (Input.GetKeyDown(KeyCode.Alpha0))   EndEncounter(true);
        if (Input.GetKeyDown(KeyCode.Alpha9))   EndEncounter(false); 
    }

    void ExecuteCharacterAction(Character c)    //Execute the selected action of a given Character.
    {
        c.actionChosen.Perform();
    }

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
    public ActionBase GetActionBaseByName(string name) 
    {
        ScriptableObject temp = (ScriptableObject)Resources.Load("Actions/" + name);
        Debug.LogWarning(temp.name);
        return temp as ActionBase;
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
            charactersPlayer[i].actionChosen.Initialise(); ;
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
    }   //Initialise health to full and actions from base action values.

    public void RecalculateCharacterPositions() //Rethink where the various characters are after one's been removed
    {

        for (int i = 0; i < charactersPlayer.Count; i++)
            charactersPlayer[i].position = i + 1;

        for (int i = 0; i < charactersEnemy.Count; i++)
            charactersEnemy[i].position = i + charactersPlayer.Count+1;
    }

    public void EndEncounter(bool isWon)
    {
        isInBattle=false;
        Debug.Log($"Just finished encounter: {battleID}");
        if (isWon) 
        {
            UIManager.instance.ToggleEncounterSelection(true); //show the map screen
            mapManager.instance.NodeUnlockInitialise(battleID);
        }

        else 
        {
            Debug.LogWarning("Game Over! Starting over from level 0...");
        }

    }

}
