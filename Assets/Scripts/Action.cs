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
   
    public void Perform()                   //Perform the action - damaging the target and previewing trajectory
    {
        GetTargetCharacter().TakeDamage(updatedData.damage);
        UIManager.instance.ShowAttackEffects(
                                    BattleManager.instance.characterPositions[BattleManager.instance.GetCharacterByID(ownerID).position - 1].position,
                                    BattleManager.instance.characterPositions[GetTargetPosition()-1].position);
        UIManager.instance.SetDamageTakenCaptions(BattleManager.instance.GetCharacterByID(ownerID));
    }
    public void Preview(bool targetState)   //Called when hovered over the Enemy/Player attack token - previews where an attack is aimed
    {
        if (targetState) 
        {
            //Display target
            UIManager.instance.ShowTargetParabola(
                                                BattleManager.instance.characterPositions[BattleManager.instance.GetCharacterByID(ownerID).position - 1].position,
                                                BattleManager.instance.characterPositions[GetTargetPosition() - 1].position, -1
                );
            //TODO Display movement
        }
        else { UIManager.instance.HideTargetParabola(); }

    }

    public int GetTargetPosition() 
    {
        int hitPosition;
        Character owner = BattleManager.instance.GetCharacterByID(ownerID);
        if (owner.position < BattleManager.instance.charactersPlayer.Count + 1) //If the player is attacking
             hitPosition = owner.position + updatedData.range;
        else hitPosition = owner.position - updatedData.range;

        if (hitPosition < 1 || 
            BattleManager.instance.charactersEnemy.Count < 1 || 
            hitPosition > BattleManager.instance.charactersEnemy[BattleManager.instance.charactersEnemy.Count - 1].position)
        {
            Debug.LogWarning("Hitting beyond the enemies!");
            return -999;
        }
        return hitPosition;
    }       //Shorthand for getting the position targetted by this action based on the caster position
    public Character GetTargetCharacter()
    {
        List<Character> pl = BattleManager.instance.charactersPlayer;                       //shorthand for reaing clarity
        int hitPosition = GetTargetPosition();
        if (hitPosition == -999) return null;   //Breaking if position invalid

        if (BattleManager.instance.GetCharacterByID(ownerID).position - 1 < pl.Count )      //If the player is attacking
        {
            if (hitPosition > pl.Count)
                return BattleManager.instance.charactersEnemy[hitPosition - pl.Count - 1];  //factor in number of players to get accurate list position
            else
                return BattleManager.instance.charactersPlayer[hitPosition - 1];
        }
        else                                                                                //if the enemy is attacking
        {
            if (hitPosition < pl.Count + 1)
                return BattleManager.instance.charactersPlayer[hitPosition - 1];
            else
                return BattleManager.instance.charactersEnemy[hitPosition - pl.Count - 1];  //factor in number of players to get accurate list position
        }

    } //Get the character hit by this current action


}
