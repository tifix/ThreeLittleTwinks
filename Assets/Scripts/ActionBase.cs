/* This ScriptableObject class is a shortand for creating and assigning actions to characters
 * Logic is handled on Action class.
 * 
 * ScriptableObjects are not mutable (values changed per instance) so they're used for defining an attack like "what is a punch?" "what is a slash?", 
 * The actual values of the attack (buffs, passives and whatnot) are applied on the Action class.
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "new action")]
public class ActionBase : ScriptableObject
{
    [Tooltip("never retrieved in code, used for matching up data")] public new string name;
    public List<ActionBehaviour> ActionBehaviour = new List<ActionBehaviour>();
    //public ActionBehaviour ActionBehaviour = new ActionBehaviour();

}