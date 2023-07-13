/* Control the flow of the metagame through this class
 * 
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private float CHEAT_timescale = 1;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (CHEAT_timescale!=1)Time.timeScale= CHEAT_timescale;
    }
}
