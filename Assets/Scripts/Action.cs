/* This class is the base for player and enemy actions. 
 * While ActionBase is a shorthand for creating and assigning attacks quickly, this class actually handles PERFORMING the attack
 * 
 * Action is initialised; cloned from non-editable BaseData into an editable instance 
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public struct ActionValues
{
    public string description;
    public int damage;
    public int cost;
    public int range;

    
    public ActionValues(string _description, int _damage, int _cost, int _range) { description = _description; damage = _damage;cost = _cost; range = _range; }
}

[System.Serializable]
public class Action {

    public string name;
    public ActionBase baseData;
    public ActionValues updatedData;
    public int ownerID;

    public void Initialise() { updatedData = baseData.ActionValues; name = baseData.name.ToString(); }    //While scriptable objects are very convenient for handling Actions, Values of a SO cannot be edited per instance
    
 
    public void Perform() 
    {
        Character owner = BattleManager.instance.GetCharacterByID(ownerID);
        Debug.Log($"{owner.name} at position {owner.position} is performing action {name} (range{updatedData.range})");
        List<Character> en = BattleManager.instance.charactersEnemy;
        List<Character> pl = BattleManager.instance.charactersPlayer;

        if (owner.position < pl.Count+1) //If the player is attacking
        {
            int hitPosition = owner.position + updatedData.range;

            //Breaking if hitting out of range
            if (hitPosition<0 || en.Count < 1 || hitPosition > en[en.Count-1].position) 
            {Debug.LogWarning("Hitting beyond the enemies!"); return; } 
                     
            if (hitPosition > pl.Count) //Attack reaches the enemy
            {
                Debug.Log($"the attack is hitting the enemy at spot {hitPosition} ");

                //Apply Damage
                BattleManager.instance.charactersEnemy[hitPosition - pl.Count-1].TakeDamage(updatedData.damage); 

                //Show Snazzy effects
                UIManager.instance.ShowAttackEffects(
                                                    BattleManager.instance.characterPositions[owner.position - 1].position, 
                                                    BattleManager.instance.characterPositions[hitPosition - 1].position);   
            }
            else            //Attack hits player's characters
            {
                Debug.Log($"the attack is hitting player's own troops! at position {hitPosition}");

                //Apply Damage
                BattleManager.instance.charactersPlayer[hitPosition - 1].TakeDamage(updatedData.damage);

                //Show Snazzy effects
                UIManager.instance.ShowAttackEffects(
                                                    BattleManager.instance.characterPositions[owner.position - 1].position, 
                                                    BattleManager.instance.characterPositions[hitPosition - 1].position);
            }
        }
        else            //if the enemy is attacking
        {
            int hitPosition = owner.position + updatedData.range;
            Debug.LogWarning($"Enemy attacking from position{hitPosition}");
            //ENEMY ATTACK LOGIC HERE!
        
        }/*
        */
    }
}
