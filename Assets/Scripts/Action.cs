/* This class is the base for player and enemy actions. 
 * While ActionBase is a shorthand for creating and assigning attacks quickly, this class actually handles PERFORMING the attack
 * 
 * Action is initialised; cloned from non-editable BaseData into an editable instance 
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using static UnityEngine.GraphicsBuffer;
using Unity.Mathematics;
using System.Linq;

[System.Serializable]
public struct ActionBehaviour
{
    [Tooltip("in-game description for action from [0] member, use other fields to clear up sub-behaviours")]    public string   description;
    [Tooltip("Characters this many slots away will be affected")]                                               public Targets  targets;
    [Tooltip("behaviour driven on [0] member, others redundant")]                                               public int      cost;
    [Tooltip("damage dealt. Negative values HEAL instead")]                                                     public int      damage;
                                                                                                                public int      movement;
    [Tooltip("leave blank if none, full list in BattleManager ApplyDebuff()")]                                  public string   appliesDebuff;

    public ActionBehaviour(string _description, int _damage, int _cost, Targets _targets, int _movement, string _debuff)   //Full constructor
    { description = _description; damage = _damage;cost = _cost; targets = _targets; movement = _movement; appliesDebuff = _debuff; }
    public ActionBehaviour(string _description, int _damage, int _cost, int _targetsSimple, int _movement) //Simplest constructor
    { description = _description; damage = _damage;cost = _cost; targets = new Targets(_targetsSimple);movement = 0; appliesDebuff = ""; } 
}

[System.Serializable]
public struct Targets
{
    public GameManager.Logic multiTargetLogic;
    public int[] distancesHit;
    public Targets(int simpleRange) { distancesHit = new int[]{ simpleRange}; multiTargetLogic = GameManager.Logic.And; }
    public Targets(int[] _hits, GameManager.Logic _logic) { distancesHit = _hits; multiTargetLogic = _logic; }
}

[System.Serializable]
public class Action {

    public string name;
    public ActionBase baseBehaviours;
    public List<ActionBehaviour> updatedBehaviours;
    public int ownerID;

    public void Initialise() 
    {
        updatedBehaviours = new List<ActionBehaviour>(baseBehaviours.ActionBehaviour);
        name = baseBehaviours.name.ToString(); 
    }    //While scriptable objects are very convenient for handling Actions, Values of a SO cannot be edited per instance

    public void Perform()                   //Perform the action - damaging the target and previewing trajectory
    {
        Debug.LogWarning($"{GetOwnerCharacter().name} is Performing attack {name}");

        //As each action can do different things to different targets, tackle them one at a time
        for (int b = 0; b < updatedBehaviours.Count; b++) 
        {
            ActionBehaviour behaviour = updatedBehaviours[b];   //Shortcut for readability's sake

            //If a random of multiple target is used - remove all but one targets
            if (behaviour.targets.multiTargetLogic == GameManager.Logic.RandomOr) 
            {
            int index = UnityEngine.Random.Range(0, behaviour.targets.distancesHit.Length);
                behaviour.targets.distancesHit=new int[] { behaviour.targets.distancesHit[index] };
            }

            //Can target multiple characters, apply effects
            foreach (Character Target in GetTargetCharacter(behaviour))  //get the targets for this individual behaviour
            {
                Target.TakeDamage(behaviour.damage);
                UIManager.instance.ShowAttackEffects(
                                BattleManager.instance.characterPositions[GetOwnerCharacter().position - 1].position,
                                BattleManager.instance.characterPositions[Target.position - 1].position);

                UIManager.instance.SetDamageTakenCaptions(GetOwnerCharacter(), Target);
                if (behaviour.appliesDebuff != "") BattleManager.instance.ApplyDebuff(behaviour.appliesDebuff, Target.position);    //apply debuff after damage
            }

            //If the attack comes with an extra movement of some sort - execute it here
            if (Mathf.Abs(behaviour.movement) > 0)
                BattleManager.instance.PlayerMoveSimple(GetOwnerCharacter().position - 1, behaviour.movement);
        }

        

        //hide the selection token once used
        if (GetOwnerCharacter().position < BattleManager.instance.charactersPlayer.Count + 1) 
        {
            Debug.LogWarning($"removing token from position {GetOwnerCharacter().position} ({GetOwnerCharacter().name})");
            UIManager.instance.AnimatorTrigger("hide" + BattleManager.instance.GetNodeByPosition(GetOwnerCharacter().position));
        } 
    }
    public void Preview(bool targetState)   //Called when hovered over the Enemy/Player attack token - previews where an attack is aimed
    {
        if (targetState)
        {
            foreach (Character Target in GetTargetCharacter())  //Showing targetting [for first subAction only]
            {
                //Display target parabola
                UIManager.instance.ShowTargetParabola(
                                    BattleManager.instance.characterPositions[GetOwnerCharacter().position - 1].position,
                                    BattleManager.instance.characterPositions[Target.position - 1].position, -1);
            }

        }
        else { UIManager.instance.HideTargetParabola(); UIManager.instance.MovePreviewArrow.SetActive(false); }
    }



    public List<int> GetTargetPositions() => GetTargetPositions(updatedBehaviours[0]); //Default shorthand; while complex attacks have subBehaviours, simple ones will just have one
    public List<int> GetTargetPositions(ActionBehaviour subBehaviour)   //Currently handles AND and XOR logic
    { //subBehaviour
        List<int> hitPositions=new List<int>();
        Character owner = BattleManager.instance.GetCharacterByID(ownerID);

        if (   subBehaviour.targets.multiTargetLogic == GameManager.Logic.And 
            || subBehaviour.targets.multiTargetLogic == GameManager.Logic.RandomOr 
            || subBehaviour.targets.multiTargetLogic == GameManager.Logic.SelectOr)  //Random hit previews all, target selection happens during Perform()
        {
            foreach (int hit in subBehaviour.targets.distancesHit)
            {
                if (owner.CheckIsThisPlayer() && CheckIsHitValid(owner.position + hit)) hitPositions.Add(owner.position + hit);  //If the player is attacking
                else if (CheckIsHitValid(owner.position - hit)) hitPositions.Add(owner.position - hit);  //If the enemy  is attacking
            }
        }/*
        else if (subBehaviour.targets.multiTargetLogic == GameManager.Logic.SelectOr)
        {
            //hitPositions.Add(UnityEngine.Random.Range(0, subBehaviour.targets.distancesHit.Length));

        }*/
        else if (subBehaviour.targets.multiTargetLogic == GameManager.Logic.Allies)  //If damaging just one target - generate random index, hit that one
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

    public List<Character> GetTargetCharacter() => GetTargetCharacter(updatedBehaviours[0]);
    public List<Character> GetTargetCharacter(ActionBehaviour subBehaviour)
    {
        List<Character> pl = BattleManager.instance.charactersPlayer;   //shorthand for reading clarity
        List<Character> hitChars = new List<Character>();

        foreach (int hitPosition in GetTargetPositions(subBehaviour))
        {
            //Debug.Log($"Hitting position {hitPosition}");
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
