/* Each character's data is stored on this object - be it friend or foe.
 * main consideration is avalible actions (actions to show in prep screen) and actionChosen (action to execute during the round)
 * For now, these are assigned in-editor, soon they will be selectable in-game
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Character
{
    public string name = "DefaultCharacter";
    public int position;                        //each character stores data of it's own position (counting from 1 from the left to right)
    public float hpCur=100;
    public float hpMax=100;
    public Action[] actionsAvalible= new Action[4];
    public Action actionChosen;

    public void TakeDamage(float dmg)   //to be extended later with damage types 
    {
        hpCur -= dmg;
        if (hpCur < 0) Die();
    }
    
    //Kill character when health reaches 0 and do some funky stuff and effects in the future
    private void Die() 
    {
        Debug.LogWarning($"The character {name} has just died! Someone get the cheap roses!");
        //Reshuffle positions, since the corpse is no longer a target
        BattleManager.instance.characterPositions[position - 1].gameObject.GetComponent<SpriteRenderer>().color = Color.black;
        BattleManager.instance.characterPositions.RemoveAt(position - 1);

        //Remove
        BattleManager.instance.charactersEnemy.Remove(this);
        BattleManager.instance.RecalculateCharacterPositions();


        //Check if all chars are dead for ending the fight with victory/defeat
        if (BattleManager.instance.charactersEnemy.Count < 1) 
        {
            Debug.Log("All enemies defeated! Victory!");
            BattleManager.instance.EndEncounter(true);
        }
        if (BattleManager.instance.charactersPlayer.Count < 1)
        {
            Debug.Log("All player champtions defeated! Victory!");
            BattleManager.instance.EndEncounter(false);
        }
    }
}
