/* Control the flow of the metagame through this class
 * 
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum Logic {And, SelectOr, RandomOr, Even, Allies};
    public enum Element { Fire, Water, Physical, Curse };

    [SerializeField] private float CHEAT_timescale = 1;
    public void DebugString(string text)=> Debug.Log(text);
    

    void Update()
    {
        if (CHEAT_timescale!=1)Time.timeScale= CHEAT_timescale;

    }

}
