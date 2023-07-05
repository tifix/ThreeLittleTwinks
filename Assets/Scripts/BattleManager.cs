using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    //Data of player characters and enemy characters
    public Character[] charactersPlayer = new Character[4];
    public Character[] charactersEnemy = new Character[4];
    public Transform[] characterPositions = new Transform[8];


    public static BattleManager instance;
    public GameObject HitMarker;

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
    }

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

    //Execute character actions when the player presses corresponding keys
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Q)) { ExecuteCharacterAction(charactersPlayer[0]); }
        if(Input.GetKeyDown(KeyCode.W)) { ExecuteCharacterAction(charactersPlayer[1]); }
        if(Input.GetKeyDown(KeyCode.E)) { ExecuteCharacterAction(charactersPlayer[2]); }
        if(Input.GetKeyDown(KeyCode.R)) { ExecuteCharacterAction(charactersPlayer[3]); }
    }

    void ExecuteCharacterAction(Character c)
    {
        c.actionChosen.Perform();
    }
}
