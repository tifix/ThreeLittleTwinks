using UnityEngine;

[CreateAssetMenu(fileName = "new action")]
public class ActionBase : ScriptableObject
{
    public new string name;
    public ActionValues ActionValues = new ActionValues();

}