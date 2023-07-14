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
    public string ElementType;

    public void TakeDamage(float dmg)   //to be extended later with damage types 
    {
        //if (ElementType == "Water" && dmg.El
        hpCur -= dmg;
        if (hpCur < 0) Die();
        Debug.Log("Damage has been taken");
    }
    
    //Kill character when health reaches 0 and do some funky stuff and effects in the future
    private void Die() 
    {
        Debug.LogWarning($"The character {name} has just died! Someone get the cheap roses!");
    }
}
