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
    private void Die() 
    {
        Debug.LogWarning($"The character {name} has just died! Someone get the cheap roses!");
    }
}
