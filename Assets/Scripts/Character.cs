/* Each character's data is stored on this object - be it friend or foe.
 * main consideration is avalible actions (actions to show in prep screen) and actionChosen (action to execute during the round)
 * For now, these are assigned in-editor, soon they will be selectable in-game
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class Character
{
    public string name = "DefaultCharacter";
    public int position;
    public bool isDead = false;//each character stores data of it's own position (counting from 1 from the left to right)
    public float hpCur = 100, hpMax = 100;
    public Action[] actionsAvalible = new Action[4];
    public Action actionChosen;
    public List<Debuff> curDebuffs = new List<Debuff>();
    public GameManager.Element vulnerableTo = GameManager.Element.Curse;
    public GameManager.Element ResistantTo = GameManager.Element.Physical;

    public void TakeDamage(float dmg, Character attacker)   //to be extended later with damage types 
    {
        if (CheckCurrentDebuff("braced")) { dmg = Mathf.RoundToInt(dmg / 2); Debug.LogWarning($"BRACED! Dmg:{dmg}! {attacker.name} takes dmg too!"); attacker.TakeDamage(2,this); } 
        if (CheckCurrentDebuff("huffed up")) { if (dmg > 2) dmg -= 2; else if(dmg>-1){ dmg = 0; } Debug.LogWarning($"HUFFED UP! Dmg:{dmg}!"); } 

        if (dmg < 0)                    { hpCur -= dmg; hpCur=Mathf.Clamp(hpCur, 0, hpMax); Debug.Log($"{name} healed {-dmg}"); }     //When healing, clamp at maximum health
        else if (dmg > 0 && hpCur > 0)  { hpCur -= dmg;                                     Debug.Log($"{name} took {dmg} dmg");  } //Take damage only if alive to avoid repeat Death calls
        if (hpCur < 1) Die();       //if pushed to death with this attack - die
        
    }

    //Compares player characters based on their relative value
    public Vector2 GetMostValuableTarget()
    {
        List<Character> possibleTargets = new ();    
        foreach (Action action in actionsAvalible) 
        {
            //
            //int hitPosition = position - action.updatedBehaviours.range;
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

    public void RefreshDebuffEffects() { }



    public void ApplyDebuff(string _name,int duration)
    {
        //might want to do some tokens representing the debuff
        
        //If debuff applied is already active, do not add again. Extend duration instead;
        foreach (var debuff in curDebuffs)
        {
            if (debuff.name == _name) 
            {
                if (debuff.duration < duration)
                    debuff.duration = duration;
                else { }
                RefreshDebuffEffects();
                return;
            }
        }

        //Applying new effects
        curDebuffs.Add(new Debuff(duration,_name, GetHashCode()));
        Debuff deb = GetDebuffByName(_name);

        switch (_name)
        {
            case ("poisoned"): { break; }   //take damage continously.
            case ("poisoned2"): { break; }   //take damage continously.
            case ("poisoned3"): { break; }   //take damage continously.
            case ("braced"): //take 50% damage
                {
                    //DamageTaken.AddListener(Braced);

                    //time-down triggers added
                    BattleManager.instance.roundEnd.AddListener(deb.ReduceDebuffDuration);
                    BattleManager.instance.Test.AddListener(deb.ReduceDebuffDuration);
                    break; 
                }     
                
            case ("huffed up"): 
                {
                    //empower blow if active

                    //time-down triggers added
                    BattleManager.instance.roundEnd.AddListener(deb.ReduceDebuffDuration);
                    break; 
                }  
            case ("puffed up"): { break; }  //take -2 incoming damage, empower blow if active
            case ("charmed"): 
                {
                    //DamageTaken.AddListener(Charmed);
                    break; 
                }    //if hits caster, take damage themself
            case ("vulnerable"): { break; } //takes 50% more damage
            case ("sleeping"): { break; }   //cannot act
                default : { Debug.LogWarning($"{_name} is not a valid Debuff!"); break; }
        }

        Debug.LogWarning($"Applying a snazzy debuff <{_name}>");
    }
    public bool CheckCurrentDebuff(string _name) 
    {
        foreach (var debuff in curDebuffs)
            if (debuff.name == _name) return true;
        
        return false;
    }
    public Debuff GetDebuffByName(string _name) 
    {
        foreach (var debuff in curDebuffs)
            if (debuff.name == _name) return debuff;

        return null;
    }

    #region debuffs
    public void Charmed() 
    {
    }
    public void Braced() 
    { 
    
    }
    #endregion
}
