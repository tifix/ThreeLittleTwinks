/* This class is the base for player and enemy actions. 
 * While ActionBase is a shorthand for creating and assigning attacks quickly, this class actually handles PERFORMING the attack
 * 
 * Action is initialised; cloned from non-editable BaseData into an editable instance 
 */

using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ActionValues
{
    public string description;
    public int damage;
    public int cost;
    public Targets targets;
    public int movement;
    
    public ActionValues(string _description, int _damage, int _cost, Targets _targets, int _movement)   //Full constructor
    { description = _description; damage = _damage;cost = _cost; targets = _targets; movement = _movement; }
    public ActionValues(string _description, int _damage, int _cost, int _targetsSimple, int _movement) //Simplest constructor
    { description = _description; damage = _damage;cost = _cost; targets = new Targets(_targetsSimple);movement = 0; } 
}

[System.Serializable]
public struct Targets
{
    public int[] positionsHit;
    public GameManager.Logic multiTargetLogic;
    public Targets(int simpleRange) { positionsHit = new int[]{ simpleRange}; multiTargetLogic = GameManager.Logic.And; }
    public Targets(int[] _hits, GameManager.Logic _logic) { positionsHit = _hits; multiTargetLogic = _logic; }
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
        Debug.LogWarning($"Performing attack {name}");

        foreach (Character Target in GetTargetCharacter())  //Showing targetting 
        {
            Debug.Log($"Hitting {Target.name} now!");
            Target.TakeDamage(updatedData.damage);
            UIManager.instance.ShowAttackEffects(
                            BattleManager.instance.characterPositions[GetOwnerCharacter().position - 1].position,
                            BattleManager.instance.characterPositions[Target.position - 1].position);

            UIManager.instance.SetDamageTakenCaptions(GetOwnerCharacter(), Target);
        }

        //If the attack comes with an extra movement of some sort - execute it here
        if(Mathf.Abs(updatedData.movement)>1)
        BattleManager.instance.PlayerMoveSimple(GetOwnerCharacter().position,updatedData.movement);

        //hide the selection token once used
        if (GetOwnerCharacter().position< BattleManager.instance.charactersPlayer.Count + 1) UIManager.instance.AnimatorTrigger("hideActionToken" + (GetOwnerCharacter().position).ToString()); //offset by -1?
    }
    public void Preview(bool targetState)   //Called when hovered over the Enemy/Player attack token - previews where an attack is aimed
    {
        if (targetState)
        {
            foreach (Character Target in GetTargetCharacter())  //Showing targetting 
            {
                //Display target parabola
                UIManager.instance.ShowTargetParabola(
                                    BattleManager.instance.characterPositions[GetOwnerCharacter().position - 1].position,
                                    BattleManager.instance.characterPositions[Target.position - 1].position, -1);
            }

        }
        else { UIManager.instance.HideTargetParabola(); UIManager.instance.MovePreviewArrow.SetActive(false); }
    }


    public List<int> GetTargetPositions()   //Currently handles AND and XOR logic
    {
        List<int> hitPositions=new List<int>();
        Character owner = BattleManager.instance.GetCharacterByID(ownerID);

        if (updatedData.targets.multiTargetLogic == GameManager.Logic.And)  //If damaging all hit targets
        {
            foreach (int hit in updatedData.targets.positionsHit)
            {
                if (owner.CheckIsThisPlayer() && CheckIsHitValid(owner.position + hit)) hitPositions.Add(owner.position + hit);  //If the player is attacking
                else if (CheckIsHitValid(owner.position - hit)) hitPositions.Add(owner.position - hit);  //If the enemy  is attacking

            }
        }
        else if (updatedData.targets.multiTargetLogic == GameManager.Logic.Xor)  //If damaging just one target - generate random index, hit that one
        {
            hitPositions.Add(UnityEngine.Random.Range(0, updatedData.targets.positionsHit.Length));
        }
        else if (updatedData.targets.multiTargetLogic == GameManager.Logic.Allies)  //If damaging just one target - generate random index, hit that one
        {
            if (owner.CheckIsThisPlayer()) 
                for (int i = 1; i < 5; i++)
                {
                    if(!BattleManager.instance.GetCharacterByPosition(i).isDead) hitPositions.Add(i);
                }
            else 
                for (int i = 5; i < 9; i++)
                {
                    if (!BattleManager.instance.GetCharacterByPosition(i).isDead) hitPositions.Add(i);
                }
        }
        else Debug.LogWarning("This logic system is not yet implemented");

        return hitPositions;
    }       //Shorthand for getting the position targetted by this action based on the caster position
    public List<Character> GetTargetCharacter()
    {
        List<Character> pl = BattleManager.instance.charactersPlayer;   //shorthand for reading clarity
        List<Character> hitChars = new List<Character>();

        foreach (int hitPosition in GetTargetPositions())
        {
            //If the player is attacking
            if (BattleManager.instance.GetCharacterByID(ownerID).position - 1 < pl.Count)      
            {
                if (hitPosition > pl.Count)     hitChars.Add(BattleManager.instance.charactersEnemy[hitPosition - pl.Count - 1]);  //factor in number of players to get accurate list position
                else                            hitChars.Add(pl[hitPosition - 1]);
            }
            //if the enemy is attacking
            else
            {
                if (hitPosition < pl.Count + 1) hitChars.Add(pl[hitPosition - 1]);
                else                            hitChars.Add(BattleManager.instance.charactersEnemy[hitPosition - pl.Count - 1]);  //factor in number of players to get accurate list position
            }
        }
        if (hitChars.Count < 1) Debug.LogWarning("Hit positions returns empty! Retrieval likely failed");

        return hitChars;
    } //Get the character hit by this current action

    public Character GetOwnerCharacter()  { return BattleManager.instance.GetCharacterByID(ownerID); }

    public bool CheckIsHitValid(int hit) 
    {
        if (hit < 1 || BattleManager.instance.charactersEnemy.Count < 1 ||
        hit > BattleManager.instance.charactersEnemy[BattleManager.instance.charactersEnemy.Count - 1].position)
        {
            Debug.LogWarning("Hitting beyond the enemies!");
            return false;
        }
        else return true;
    }
}
