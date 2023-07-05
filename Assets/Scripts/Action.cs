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

    public void Initialise() { updatedData = baseData.ActionValues; name = updatedData.ToString(); }    //While scriptable objects are very convenient for handling Actions, Values of a SO cannot be edited per instance
    
    
    

    public void Perform() 
    {
        Character owner = BattleManager.instance.GetCharacterByID(ownerID);

        Debug.Log($"{owner.name} at position {owner.position} is performing action");

        if (owner.position < 5) //If the player is performing the action
        {
            int hitPosition = owner.position + updatedData.range;
            if (hitPosition > 4) //Attack reaches the enemy
            {
                Debug.Log($"the attack is hitting the enemy at spot {hitPosition} ");
                BattleManager.instance.charactersEnemy[hitPosition - 5].TakeDamage(updatedData.damage);
                BattleManager.instance.HitMarker.transform.position = BattleManager.instance.characterPositions[hitPosition].position;
            }
            else 
            {
                Debug.Log($"the attack is hitting player's own troops! at position {hitPosition}");
                BattleManager.instance.charactersPlayer[hitPosition - 1].TakeDamage(updatedData.damage);
                BattleManager.instance.HitMarker.transform.position = BattleManager.instance.characterPositions[hitPosition].position;
            }
        }
        else            //if the enemy is performing the action
        {
        
        
        }/*
        */
    }
}
