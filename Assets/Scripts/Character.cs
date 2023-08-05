/* Each character's data is stored on this object - be it friend or foe.
 * main consideration is avalible actions (actions to show in prep screen) and actionChosen (action to execute during the round)
 * For now, these are assigned in-editor, soon they will be selectable in-game
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Character
{
    public string name = "DefaultCharacter";
    public int position;
    public bool isDead = false;//each character stores data of it's own position (counting from 1 from the left to right)
    public float hpCur=100;
    public float hpMax=100;
    public Action[] actionsAvalible= new Action[4];
    public Action actionChosen;
    public string ElementType;

    public void TakeDamage(float dmg)   //to be extended later with damage types 
    {
        //if (ElementType == "Water" && dmg.El
        if(hpCur>0) hpCur -= dmg;               //Take damage only if alive to avoid repeat Death calls
        if (hpCur < 1) Die();                   //if pushed to death with this attack - die
        Debug.Log("Damage has been taken");
    }

    //Compares player characters based on their relative value
    public Vector2 GetMostValuableTarget()
    {
        List<Character> possibleTargets = new ();    
        foreach (Action action in actionsAvalible) 
        {
            //
            //int hitPosition = position - action.updatedData.range;
        }

        //List<int> targets = new List<int>();
        //three archetypes of heroes - Nemesis, Righteous, Just.
        //Nemesis chooses one character and tries to damage them whenever possible, Righteous tries to maximise damage, Just tries to maximise own team health and get enemy health evenly low

        //target player with lowest health by default




        //if have attack which character is vulnerable to, randomise using it instead

        //if player character is on low health - prioritise finishing off


        return new Vector2(1, 1);    //X-which attack to use, Y-whom to target 1,1 > 4,4
    }

    //Kill character when health reaches 0 and do some funky stuff and effects in the future
    private void Die()
    {
        Debug.LogWarning($"The character {name} has just died! Someone get the cheap roses!");
        isDead = true;

        BattleManager.instance.characterPositions[position - 1].gameObject.GetComponent<SpriteRenderer>().color = Color.black;

        //remove any tokens - buffs, debuffs, actions avalible
        try
        {
            GameObject GO = BattleManager.instance.characterPositions[position - 1].gameObject;
            for (int i = 0; i < GO.transform.childCount; i++)
            {
                if (GO.transform.GetChild(i).CompareTag("token")) GameObject.Destroy(GO.transform.GetChild(i).gameObject);
            }
        }
        catch { Debug.LogWarning("Action Token missing, cannot destroy"); }

        //remove from character positions
        //BattleManager.instance.characterPositions.RemoveAt(position - 1);

        //Remove from either enemies or players according to faction 
        //if (BattleManager.instance.charactersEnemy.Contains(this)) BattleManager.instance.charactersEnemy.Remove(this);
        //else BattleManager.instance.charactersPlayer.Remove(this);

        //recalculate once removed
        BattleManager.instance.RecalculateCharacterPositions();

        UIManager.instance.RefreshStatusCorners();

        //Check if all chars are dead for ending the fight with victory/defeat
        if (BattleManager.instance.charactersEnemy.Count < 1)
        {
            Debug.Log("All enemies defeated! Victory!");
            BattleManager.instance.EndEncounter(true);
        }
        if (BattleManager.instance.charactersPlayer.Count < 1)
        {
            Debug.LogWarning("All player champtions defeated! GAME OVER!");
            BattleManager.instance.EndEncounter(false);
        }
        
    }

    public bool CheckIsThisPlayer() 
    {
        if (position < BattleManager.instance.charactersPlayer.Count + 1) return true;
        else return false;
    }
}
