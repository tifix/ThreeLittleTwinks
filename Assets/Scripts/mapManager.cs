/* This class handles the map progression screen behaviour
 * 
 * GetNodeByID retrieves a node by a unique ID
 * NodeUnlock handles unlocking the node (call through)
 * UpdateNodeDisplay refreshes the node GameObject based on the states of the data nodes
 * 
 * 
 * The mapNode data class contains data for each encounter - whether it's completed, whether it's avalible to be played next, 
 * what encounter it connects to and which encounters are unlocked upon completion
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class mapNode
{
    [Tooltip("this object represents the encounter visually (button on the map)")]  public GameObject   nodeObject;
    [Tooltip("UNIQUE name of the encounter")]                                       public string       ID;
                                                                                    public bool         isComplete, isNext;
    [Tooltip("which encounters are unlocked upon completion")]                      public List<string> nextID;

    public void Unlock()
    {
        isComplete = true;
        isNext =    false;
        foreach (string next in nextID) 
            mapManager.instance.GetNodeByID(next).SetNext();
        nodeObject.GetComponent<Button>().enabled = false;//interactable = false;
    }
    public void SetNext() 
    { 
        isNext = true;
        nodeObject.GetComponent<Button>().enabled = true;//interactable = true;
    }
}

[ExecuteInEditMode]
public class mapManager : MonoBehaviour
{
    public static mapManager instance;
    public List<mapNode> nodes;
    public Color colorComplete;
    public Color colorNext;
    public bool t1=false;
    public string t2="alpha";
    public string currentID="";
    public GameObject VFX_Highlight;


    //Assign instance if none found, otherwise destroy
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(this);
    }
    private void Start()
    {
        UpdateNodeDisplay();
    }

    //Debug for test-unlocking specific node
    void Update()
    {
        if (t1 == true) NodeUnlock(t2);

    }



    #region utilities
    public mapNode GetNodeByID(string ID)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].ID == ID)
            {
                return nodes[i];
            }
        }
        Debug.LogWarning($"Could not find requested node: {ID}!");
        return new mapNode();
    }                                           //Retrieve a node from mapNode list by it's ID. If IDs repeat (THEY SHOULD NOT) retrieves first from the list
    #endregion

    public void NodeUnlock(string id) 
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].ID == id)
                nodes[i].Unlock();
        }
    }                                                   //Actually triggers the node unlocking
    public void NodeUnlockInitialise(string ID) => StartCoroutine(NodeUnlockEffects(ID));   //Initiates snazzy Unlocking wrapper
    IEnumerator NodeUnlockEffects(string ID)
    {
        //Destroy the outdated Highlight VFX immediatelly
        try { GameObject.Destroy(GetNodeByID(ID).nodeObject.transform.Find("VFX_highlight(Clone)").gameObject); } catch { Debug.Log("no highlights left"); }    
        
        yield return new WaitForSeconds(1);
        //Some nifty progression effect here

        //Unlock the next nodes
        NodeUnlock(ID);
        UpdateNodeDisplay();
        yield return new WaitForSeconds(1);
    }                                             //Wrapper for node unlocking with snazzy VFX and what have you


    public void UpdateNodeDisplay()
    {
        GameObject VFX_highlight;
        foreach (var node in nodes)
        {
            if (node.isComplete) node.nodeObject.GetComponent<Image>().color = colorComplete;
            else if (node.isNext) node.nodeObject.GetComponent<Image>().color = colorNext;
            else { node.nodeObject.GetComponent<Image>().color = Color.gray; node.nodeObject.GetComponent<Button>().enabled = false; }

            if (node.isNext) VFX_highlight = GameObject.Instantiate(VFX_Highlight, node.nodeObject.transform);
            /*else 
            {
                try { Destroy(node.nodeObject.transform.Find("VFX_highlight(Clone)").gameObject); } catch { Debug.Log("no highlights left"); }
            }*/
        }
    }                       //Refresh how nodes are displayed based on their state - grey if locked, golden if unlocked, highlighted if up next

    public void StartBattle(string ID) => BattleManager.instance.StartEncounter(ID);    //Exits the battle screen onto combat. Called by map buttons.
                                                          

}
