using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Debuff
{
    public string name = "Debuff";
    public int duration = 3;
    private int ownerID = 1;

    public Debuff(int _dur, string _name,int ID)
    {
        duration = _dur;
        name = _name;
        ownerID = ID;
    }

    public void ReduceDebuffDuration()
    {
        duration -= 1;
        if(duration<1) BattleManager.instance.GetCharacterByID(ownerID).curDebuffs.Remove(this);    //remove upon expiry
    }
}
