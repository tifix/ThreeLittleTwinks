/* This class handles UI, toggling various screens, screen transitions, button presses etc
 * 
 * current Debug Keymap 
 * SPACE - switch between PLAN and ACT phases
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static UnityEditor.PlayerSettings;
using static Unity.Burst.Intrinsics.X86;
using Unity.VisualScripting;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;
using static UnityEngine.GraphicsBuffer;

[ExecuteInEditMode]
public class UIManager : MonoBehaviour
{
    public static       UIManager instance;
    public              int          selectedCharacter = 0;
    [SerializeField]    GameObject   screen_AbilitySelect;
    [SerializeField]    GameObject   screen_EncounterSelect;
    [SerializeField]    TMP_Text     CaptionCharacterName;

    [Header("Targetting line parameters")]
    [Tooltip("how many points is the curve made up of?"),   SerializeField]         int     targetCurvatureResolution;
    [Tooltip("how tall should the curve be"),               SerializeField]         float   targetBaseHeight, targetMaxHeight;
                                                           [SerializeField]         float   TargetParabolaLifetime = 0.5f;      //Checker for ensuring an attack plays out before a next one is started
                                                                             public float   PreviewParabolaLifetime = 0.5f;    //Checker for ensuring an attack plays out before a next one is started
    [Space(5), Header("Object references")]
    public Animator transitions;    //Screen transitions!

    //Individual ability value displayers - PLAN
    [Space(2), Header("PLAN phase")]
    public GameObject SelectedArrow;                                        //Arrow showing which character's abilities are getting assigned
    [SerializeField] TMP_Text captionAbility1,      captionAbility2,        captionAbility3,        captionAbility4;
    [SerializeField] TMP_Text descriptionAbility1,  descriptionAbility2,    descriptionAbility3,    descriptionAbility4;
    [SerializeField] TMP_Text dmgAbility1,          dmgAbility2,            dmgAbility3,            dmgAbility4;
    [SerializeField] TMP_Text rangeAbility1,        rangeAbility2,          rangeAbility3,          rangeAbility4;
    [SerializeField] TMP_Text costAbility1,         costAbility2,           costAbility3,           costAbility4;
    //[SerializeField] TMP_Text buttonCaptionAbility1,buttonCaptionAbility2,  buttonCaptionAbility3,  buttonCaptionAbility4;

    //Individual ability value displayers - ACT
    [Space(2), Header("ACT phase")]
    public  GameObject      MovePreviewArrow;                                   //Arrow showing where a character's going to move
    public  GameObject      targetLine;                                         //the Line renderer rendering the targetting parabola from attacker to attackee PREFAB. Spawned when called, killed when disabled
            Vector3[]       pos = new Vector3[0];                               //target line preview positions for GIZMOS
            bool            isAttackShowingNow = false;                         //Checker for ensuring an attack plays out before a next one is started
    [Tooltip("top-of screen who hit who"), SerializeField] TMP_Text popupDamage_left, popupDamage_right;      
    [SerializeField] TMP_Text captionAbility1_act,  captionAbility2_act,    captionAbility3_act,    captionAbility4_act;
    [SerializeField] TMP_Text casterAbility1_act,   casterAbility2_act,     casterAbility3_act,     casterAbility4_act;
    [SerializeField] TMP_Text dmgAbility1_act,      dmgAbility2_act,        dmgAbility3_act,        dmgAbility4_act;
    [SerializeField] TMP_Text rangeAbility1_act,    rangeAbility2_act,      rangeAbility3_act,      rangeAbility4_act;
    [SerializeField] TMP_Text costAbility1_act,     costAbility2_act,       costAbility3_act,       costAbility4_act;

    //StatusPlayer and StatusEnemy    
    [SerializeField] TMP_Text statusCharPlayer1, statusCharPlayer2, statusCharPlayer3, statusCharPlayer4;
    [SerializeField] TMP_Text statusCharEnemy1, statusCharEnemy2, statusCharEnemy3, statusCharEnemy4;
    
    //names for characters when dodging
    [SerializeField] TMP_Text moveName1, moveName2, moveName3, moveName4;
    [SerializeField] TMP_Text moveDir1, moveDir2, moveDir3, moveDir4;

    public  List<GameObject> SelectedTokens = new List<GameObject>(4);       //The action selected tokens
            List<Character>  initialCharacterData = new();

    //public List<int> DebugList;


    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(this);
    }

    void Start()
    {
        HideTargetParabola();
        initialCharacterData.AddRange(BattleManager.instance.charactersPlayer);
        initialCharacterData.AddRange(BattleManager.instance.charactersEnemy);
        LoadDataForCharacter(BattleManager.instance.charactersPlayer[0]);
        //RefreshStatusCorners() IS CALLED ON START ON BATTLEMANAGER as it depends on character data loaded in start in BattleManager 
    }
    private void Update()
    {
        if (Input.GetKey(KeyCode.Minus)) Time.timeScale = 0; //ShowTargetParabola()
        if (Input.GetKey(KeyCode.Plus)) Time.timeScale = 1; //ShowTargetParabola()
    }

    //Switching between major game screens - from Map to encounter, from combat to Action selection
    #region Screen transitions
    public void AnimatorTrigger(string s) { transitions.SetTrigger(s); }
    public void ToggleActionSelection(bool state)
    {
        selectedCharacter = 0;
        if (state) LoadDataForCharacter(BattleManager.instance.charactersPlayer[selectedCharacter]);
        screen_AbilitySelect.SetActive(state);
    }

    public void ToggleEncounterSelection(bool state)
    {
        screen_EncounterSelect.SetActive(state);
    }
    #endregion

    //UI behaviours during combat
    #region Combat Effects
    public void ShowAttackEffects(Vector3 init, Vector3 fin) => StartCoroutine(AttackEffectsProcess(init, fin));
    public IEnumerator AttackEffectsProcess(Vector3 init, Vector3 fin)
    {
        if (isAttackShowingNow) yield break;
        isAttackShowingNow = true;
        ShowTargetParabola(init, fin, TargetParabolaLifetime);
        yield return new WaitForSeconds(0.5f);
        GameObject HitFX = Instantiate(BattleManager.instance.HitMarker, fin, Quaternion.identity);


        isAttackShowingNow = false;
        yield return new WaitForSeconds(2);
        Destroy(HitFX);
    }

    public void ShowTargetParabola(Vector3 init, Vector3 fin, float hideAfterTime) //Call with negative float to NOT hide
    {
        GameObject GO = GameObject.Instantiate(targetLine, transform,true);
        //Drawing initial and final positions
        GO.GetComponent<LineRenderer>().positionCount = targetCurvatureResolution;
        Vector3[] trajectory = new Vector3[targetCurvatureResolution];
        pos = new Vector3[targetCurvatureResolution];
        if (Vector3.Distance(init, fin) < Mathf.Epsilon) {Debug.Log("Self target!"); init += Vector3.up * 2; }  //if self-targetting, enforce a fixed height

        trajectory[0] = new Vector3(init.x, init.y + targetBaseHeight, init.z);
        trajectory[targetCurvatureResolution - 1] = new Vector3(fin.x, fin.y + targetBaseHeight, fin.z);

        //Determining natural peak height by solving for midpoint coordinate
        
        float peakY = -1 * ((init.x + fin.x) / 2 - init.x) * ((init.x + fin.x) / 2 - fin.x);
                      
        Vector3 midPosition;
        for (int i = 1; i < targetCurvatureResolution - 1; i++)
        {
            midPosition = (targetCurvatureResolution - (float)i) / targetCurvatureResolution * init + ((float)i / targetCurvatureResolution * fin); //getting X from curve resolution
            float y = -1 * (midPosition.x - init.x) * (midPosition.x - fin.x);                                                          //The equation of the curve

            //Lerp the curve so that the height of both end-points and peak is consistent and adjustable
            float relativeY = Mathf.InverseLerp(0, peakY, y); //this represents how far up th
            y = Mathf.Lerp(targetBaseHeight, targetMaxHeight, relativeY);
            if (Vector3.Distance(init, fin) < Mathf.Epsilon) {Debug.Log("Force offset here to avoid dot parabola");}


                //Exporting values to Gizmos and LineRenderer
                midPosition += y * Vector3.up;
            trajectory[i] = midPosition;
            pos[i] = midPosition;
        }

        GO.GetComponent<LineRenderer>().enabled = true;
        GO.GetComponent<LineRenderer>().SetPositions(trajectory);
        if (hideAfterTime > 0) Invoke("HideTargetParabola", hideAfterTime);

    }
    public void HideTargetParabola() 
    {
        foreach (var LineGizmo in GameObject.FindGameObjectsWithTag("parabola"))
        {
            Destroy(LineGizmo);
        }
    }
    public void ShowMoveArrow(Transform from, int whichEnemy)
    {
        GameObject arrow = MovePreviewArrow;
        arrow.SetActive(true);
        arrow.transform.SetParent(from); //sets parent to the moving character

        float distance_x = 0.01f;
        distance_x *= BattleManager.instance.EnemyMovementDirection[whichEnemy];
        arrow.transform.localScale = new Vector3(distance_x, 0.01f, 0.01f);                                             //scales depending on move distance
        arrow.transform.localPosition = Vector3.zero;
    }
    public void HideMoveArrow() => MovePreviewArrow.SetActive(false);
    #endregion

    // Functionality of Ability selection menu - choosing which abilities which characters will cast in combat this turn
    #region PLAN UI

    //Load text-box data
    void LoadDataForCharacter(Character c)
    {
        CaptionCharacterName.text = c.name;
        SelectedArrow.transform.SetParent(BattleManager.instance.characterPositions[selectedCharacter]);
        SelectedArrow.transform.localPosition = Vector3.zero;

        captionAbility1.text = c.actionsAvalible[0].name;
        captionAbility2.text = c.actionsAvalible[1].name;
        captionAbility3.text = c.actionsAvalible[2].name;
        captionAbility4.text = c.actionsAvalible[3].name;

        descriptionAbility1.text = c.actionsAvalible[0].updatedBehaviours[0].description;
        descriptionAbility2.text = c.actionsAvalible[1].updatedBehaviours[0].description;
        descriptionAbility3.text = c.actionsAvalible[2].updatedBehaviours[0].description;
        descriptionAbility4.text = c.actionsAvalible[3].updatedBehaviours[0].description;

        dmgAbility1.text = GetTotalActionValue(c.actionsAvalible[0], "damage").ToString();
        dmgAbility2.text = GetTotalActionValue(c.actionsAvalible[1], "damage").ToString();
        dmgAbility3.text = GetTotalActionValue(c.actionsAvalible[2], "damage").ToString();
        dmgAbility4.text = GetTotalActionValue(c.actionsAvalible[3], "damage").ToString();

        rangeAbility1.text = TargetsToString(c.actionsAvalible[0]);
        rangeAbility2.text = TargetsToString(c.actionsAvalible[1]);
        rangeAbility3.text = TargetsToString(c.actionsAvalible[2]);
        rangeAbility4.text = TargetsToString(c.actionsAvalible[3]);

        costAbility1.text = GetTotalActionValue(c.actionsAvalible[0], "cost").ToString();
        costAbility2.text = GetTotalActionValue(c.actionsAvalible[1], "cost").ToString();
        costAbility3.text = GetTotalActionValue(c.actionsAvalible[2], "cost").ToString();
        costAbility4.text = GetTotalActionValue(c.actionsAvalible[3], "cost").ToString();

        //buttonCaptionAbility1.text = "Select"; buttonCaptionAbility2.text = "Select"; buttonCaptionAbility3.text = "Select"; buttonCaptionAbility4.text = "Select";
    }

    public string TargetsToString(Action a) //Converts the multi-target struct of an action to a readable string
    {
        Targets t;      //clarity shorthand
        string desc;    //return value

        //for simple behaviours use the only targets, for complex behaviours try to find a multi-target, non-self-targetting if possible
        if (a.updatedBehaviours.Count == 1) t = a.updatedBehaviours[0].targets;    
        else                                    
        {
            t = a.updatedBehaviours[0].targets;                                                 
            foreach (var subBehaviour in a.updatedBehaviours)
            {
                if ((subBehaviour.targets.distancesHit.Length == 1 && subBehaviour.targets.distancesHit[0] == 0)) { t = subBehaviour.targets;  }                 
                else { t = subBehaviour.targets; break; }
            }
        }
        if (t.distancesHit.Length < 1) return "No targets!";    //return break if Action constructed wrong

        //Convert the Target struct into a displayable string
        desc = t.distancesHit[0].ToString();
        for (int i = 1; i < t.distancesHit.Length; i++)
        {
            if (t.multiTargetLogic == GameManager.Logic.And) desc+= "+";
            else if (t.multiTargetLogic == GameManager.Logic.SelectOr) desc+= "/";
            else desc+= "?";

            desc+= t.distancesHit[i];
        }     
        return desc;
    }
    public int GetTotalActionValue(Action A, string parameter) 
    {
        int temp=0;
        switch (parameter)
        {
            case ("damage"):
                {
                    foreach (var subBehaviour in A.updatedBehaviours) 
                    {
                        temp += subBehaviour.damage;
                        //if (subBehaviour.targets.multiTargetLogic == GameManager.Logic.And)
                    }
                        
                      
                    break; 
                }
            case ("movement"):
                {
                    foreach (var subBehaviour in A.updatedBehaviours)
                        temp += subBehaviour.movement;

                    break;
                }
            case ("cost"):
                {
                    foreach (var subBehaviour in A.updatedBehaviours)
                        temp += subBehaviour.cost;

                    break;
                }
        }
        return temp;
    }

    //Load data for the dodge window
    public void LoadDataForMoves() 
    {
        //If moved already, state so, otherwise show how the character can move, otherwise they are dead

        List<Character> stats = BattleManager.instance.charactersPlayer;
        if (BattleManager.instance.PlayerMovementActions[0] < 1) { moveDir1.text = "already moved"; }
        else if (!stats[0].isDead) { moveName1.text = $"position 1 "; moveDir1.text = MoveDirectionToText(0); } 
        else moveName1.text = $"{initialCharacterData[0].name} is DEAD";

        if (BattleManager.instance.PlayerMovementActions[1] < 1) { moveDir2.text = "already moved"; }
        else if (!stats[1].isDead) { moveName2.text = $"position 2"; moveDir2.text = MoveDirectionToText(1); }
        else moveName2.text = $"{initialCharacterData[1].name} is DEAD";

        if (BattleManager.instance.PlayerMovementActions[2] < 1) { moveDir3.text = "already moved"; }
        else if (!stats[2].isDead) { moveName3.text = $"position 3"; moveDir3.text = MoveDirectionToText(2); }
        else moveName3.text = $"{initialCharacterData[2].name} is DEAD";

        if (BattleManager.instance.PlayerMovementActions[3] < 1) { moveDir4.text = "already moved"; }
        else if (!stats[3].isDead) { moveName4.text = $"position 4"; moveDir4.text = MoveDirectionToText(3); }
        else moveName4.text = $"{initialCharacterData[3].name} is DEAD";
    }

    void LoadDataDefault() => LoadDataForCharacter(BattleManager.instance.charactersPlayer[selectedCharacter]); //Load data for currently selected character, [0] by default

    string MoveDirectionToText(int index) 
    {
        string s = "";
        if (BattleManager.instance.PlayerMovementDirection[index] > 0) s = "will dodge right";
        else s = "will dodge left";

        if (index == 0 && BattleManager.instance.PlayerMovementDirection[index]   > 2) s = "will dodge left";   //dodging from leftmost will swap to furthest right
        else if(index==3 && BattleManager.instance.PlayerMovementDirection[index] <-1) s = "will dodge right"; //dodging from rightmost will swap to furthest left
        return s;
    } 

    public void RefreshStatusCorners()
    {
        List<Character> stats = BattleManager.instance.charactersPlayer;
        if (!stats[0].isDead) statusCharPlayer1.text = $"{stats[0].name}: {stats[0].hpCur}/ {stats[0].hpMax}"; else statusCharPlayer1.text = $"{initialCharacterData[0].name} DEAD";
        if (!stats[1].isDead) statusCharPlayer2.text = $"{stats[1].name}: {stats[1].hpCur}/ {stats[1].hpMax}"; else statusCharPlayer2.text = $"{initialCharacterData[1].name} DEAD";
        if (!stats[2].isDead) statusCharPlayer3.text = $"{stats[2].name}: {stats[2].hpCur}/ {stats[2].hpMax}"; else statusCharPlayer3.text = $"{initialCharacterData[2].name} DEAD";
        if (!stats[3].isDead) statusCharPlayer4.text = $"{stats[3].name}: {stats[3].hpCur}/ {stats[3].hpMax}"; else statusCharPlayer4.text = $"{initialCharacterData[3].name} DEAD";

        stats = BattleManager.instance.charactersEnemy;
        if (!stats[0].isDead) statusCharEnemy1.text = $"{stats[0].name}: {stats[0].hpCur}/ {stats[0].hpMax}"; else statusCharEnemy1.text = $"{initialCharacterData[4].name} DEAD";
        if (!stats[1].isDead) statusCharEnemy2.text = $"{stats[1].name}: {stats[1].hpCur}/ {stats[1].hpMax}"; else statusCharEnemy2.text = $"{initialCharacterData[5].name} DEAD";
        if (!stats[2].isDead) statusCharEnemy3.text = $"{stats[2].name}: {stats[2].hpCur}/ {stats[2].hpMax}"; else statusCharEnemy3.text = $"{initialCharacterData[6].name} DEAD";
        if (!stats[3].isDead) statusCharEnemy4.text = $"{stats[3].name}: {stats[3].hpCur}/ {stats[3].hpMax}"; else statusCharEnemy4.text = $"{initialCharacterData[7].name} DEAD";
    }   //Refresh the textboxes displaying various character's health


    //Buttons for Ability selection menu - SetAbility sets the character's chosen ability to one of the 4 abilities, SelectNext/Previous cycles between player characters.
    public void SetAbility(int index) => BattleManager.instance.SelectAction(index, selectedCharacter);

    public void SelectNextCharacter()
    {
        if (selectedCharacter == -999) selectedCharacter = 0;   //Loading OUT of movement selection if that was the last one chosen
        else 
        {
            BattleManager.instance.TrajectorySelect(); //if switching characters, lock on the chosen target to avoid carry-overs. Must be BEFORE changing SelectedCharacter
            selectedCharacter++;
        }
        if (selectedCharacter > BattleManager.instance.charactersPlayer.Count - 1 && selectedCharacter != -999) { ShowMovementInsteadOfActions(); selectedCharacter = -999; return; }//     //0
 
        LoadDataForCharacter(BattleManager.instance.charactersPlayer[selectedCharacter]);
    }       //cycle character + fetch new char's ability data
    public void SelectPreviousCharacter()
    {
        if (selectedCharacter == -999) selectedCharacter = BattleManager.instance.charactersPlayer.Count - 1; //Loading OUT of movement selection if that was the last one chosen
        else
        {
            BattleManager.instance.TrajectorySelect(); //if switching characters, lock on the chosen target to avoid carry-overs. Must be BEFORE changing SelectedCharacter
            selectedCharacter--; 
        }
        if (selectedCharacter < 0 && selectedCharacter != -999) { ShowMovementInsteadOfActions(); selectedCharacter = -999; return; }//selectedCharacter = BattleManager.instance.charactersPlayer.Count - 1;
        
        LoadDataForCharacter(BattleManager.instance.charactersPlayer[selectedCharacter]);
    }   //cycle character + fetch new char's ability data
    public void SwapMovementOnMovementScreen(int index) 
    {
        if (selectedCharacter == -999) 
        {
            BattleManager.instance.ChangePlayerMovements(index, (BattleManager.instance.PlayerMovementDirection[index] > 0 && BattleManager.instance.PlayerMovementDirection[index] <3) || BattleManager.instance.PlayerMovementDirection[index]<-2);
            ShowMovementInsteadOfActions(); 
        }
    }

    public void ShowMovementInsteadOfActions()
    {
        CaptionCharacterName.text = "MOVEMENT";

        captionAbility1.text = "Position 1";
        captionAbility2.text = "Position 2";
        captionAbility3.text = "Position 3";
        captionAbility4.text = "Position 4";

        //buttonCaptionAbility1.text = "Switch"; buttonCaptionAbility2.text = "Switch"; buttonCaptionAbility3.text = "Switch"; buttonCaptionAbility4.text = "Switch";

        if (BattleManager.instance.PlayerMovementDirection[0] == 0) descriptionAbility1.text = "swap not set";
        else if (BattleManager.instance.PlayerMovementDirection[0]>2) descriptionAbility1.text = "Will swap left";
        else descriptionAbility1.text = "Will swap right";

        if (BattleManager.instance.PlayerMovementDirection[1] == 0) descriptionAbility2.text = "swap not set";
        else if(BattleManager.instance.PlayerMovementDirection[1] < 0) descriptionAbility2.text = "Will swap left";
        else descriptionAbility2.text = "Will swap right";

        if (BattleManager.instance.PlayerMovementDirection[2] == 0) descriptionAbility3.text = "swap not set";
        else if (BattleManager.instance.PlayerMovementDirection[2] < 0) descriptionAbility3.text = "Will swap left";
        else descriptionAbility3.text = "Will swap right";

        if (BattleManager.instance.PlayerMovementDirection[3] == 0) descriptionAbility4.text = "swap not set";
        else if (BattleManager.instance.PlayerMovementDirection[3] < -2) descriptionAbility4.text = "Will swap right";
        else descriptionAbility4.text = "Will swap left";

        dmgAbility1.text = "";
        dmgAbility2.text = "";
        dmgAbility3.text = "";
        dmgAbility4.text = "";

        rangeAbility1.text = "";
        rangeAbility2.text = "";
        rangeAbility3.text = "";
        rangeAbility4.text = "";

        costAbility1.text = "";
        costAbility2.text = "";
        costAbility3.text = "";
        costAbility4.text = "";
    }


    public void ToggleSelectedToken(bool state) => SelectedTokens[selectedCharacter].SetActive(state);  //Toggle ability selected token On/off for the character currently selected
    public void EnableSelectedForCharacter(int index) { SelectedTokens[index].SetActive(true); }//Debug.Log("Now activating" + index); 
    public void DisableSelectedForCharacter(int index) { SelectedTokens[index].SetActive(false); } //Debug.Log("Now activating" + index);
    #endregion

    #region ACT UI
    public void LoadActionDescriptions()    //Loads the descriptions of chosen actions within the act GUI; full descriptions for actions, empty if action's been used, DEAD if the character is dead
    {
        //Loading the action chosen for the character in the first slot
        if (BattleManager.instance.CheckIfValidAction(1))
        {
            captionAbility1_act.text = BattleManager.instance.charactersPlayer[0].actionChosen.name;
            casterAbility1_act.text  = BattleManager.instance.charactersPlayer[0].name;
            dmgAbility1_act.text = GetTotalActionValue(BattleManager.instance.charactersPlayer[0].actionChosen, "damage").ToString();
            rangeAbility1_act.text   = TargetsToString(BattleManager.instance.charactersPlayer[0].actionChosen);
            costAbility1_act.text    = GetTotalActionValue(BattleManager.instance.charactersPlayer[0].actionChosen, "cost").ToString();
        }
        else if (!BattleManager.instance.charactersPlayer[0].isDead) 
        {
            casterAbility1_act.text  = BattleManager.instance.charactersPlayer[0].name;
            captionAbility1_act.text = "";  dmgAbility1_act.text = ""; rangeAbility1_act.text = ""; costAbility1_act.text = "";
        }
        else { captionAbility1_act.text = "DEAD"; dmgAbility1_act.text = ""; rangeAbility1_act.text = ""; costAbility1_act.text = ""; }

        //Loading the action chosen for the character in the second slot
        if (BattleManager.instance.CheckIfValidAction(2))
        {
            captionAbility2_act.text = BattleManager.instance.charactersPlayer[1].actionChosen.name;
            casterAbility2_act.text  = BattleManager.instance.charactersPlayer[1].name;
            dmgAbility2_act.text     = GetTotalActionValue(BattleManager.instance.charactersPlayer[1].actionChosen, "damage").ToString();
            rangeAbility2_act.text   = TargetsToString(BattleManager.instance.charactersPlayer[1].actionChosen);
            costAbility2_act.text    = GetTotalActionValue(BattleManager.instance.charactersPlayer[1].actionChosen, "cost").ToString();
        }
        else if (!BattleManager.instance.charactersPlayer[1].isDead)
        {
            casterAbility2_act.text  = BattleManager.instance.charactersPlayer[1].name;
            captionAbility2_act.text = ""; dmgAbility2_act.text = ""; rangeAbility2_act.text = ""; costAbility2_act.text = "";
        }
        else { captionAbility2_act.text = "DEAD"; dmgAbility2_act.text = ""; rangeAbility2_act.text = ""; costAbility2_act.text = ""; }

        //Loading the action chosen for the character in the third slot
        if (BattleManager.instance.CheckIfValidAction(3))
        {
            captionAbility3_act.text = BattleManager.instance.charactersPlayer[2].actionChosen.name;
            casterAbility3_act.text  = BattleManager.instance.charactersPlayer[2].name;
            dmgAbility3_act.text     = GetTotalActionValue(BattleManager.instance.charactersPlayer[2].actionChosen, "damage").ToString();
            rangeAbility3_act.text   = TargetsToString(BattleManager.instance.charactersPlayer[2].actionChosen);
            costAbility3_act.text    = GetTotalActionValue(BattleManager.instance.charactersPlayer[2].actionChosen, "cost").ToString();
        }
        else if (!BattleManager.instance.charactersPlayer[2].isDead)
        {
            casterAbility3_act.text  = BattleManager.instance.charactersPlayer[2].name;
            captionAbility3_act.text = ""; dmgAbility3_act.text = ""; rangeAbility3_act.text = ""; costAbility3_act.text = "";
        }
        else { captionAbility3_act.text = "DEAD"; dmgAbility3_act.text = ""; rangeAbility3_act.text = ""; costAbility3_act.text = ""; }

        //Loading the action chosen for the character in the fourth slot
        if (BattleManager.instance.CheckIfValidAction(4))
        {
            captionAbility4_act.text = BattleManager.instance.charactersPlayer[3].actionChosen.name;
            casterAbility4_act.text  = BattleManager.instance.charactersPlayer[3].name;
            dmgAbility4_act.text     = GetTotalActionValue(BattleManager.instance.charactersPlayer[3].actionChosen, "damage").ToString();
            rangeAbility4_act.text   = TargetsToString(BattleManager.instance.charactersPlayer[3].actionChosen);
            costAbility4_act.text    = GetTotalActionValue(BattleManager.instance.charactersPlayer[3].actionChosen, "cost").ToString();
        }
        else if (!BattleManager.instance.charactersPlayer[3].isDead)
        {
            casterAbility4_act.text  = BattleManager.instance.charactersPlayer[3].name;
            captionAbility4_act.text = ""; dmgAbility4_act.text = ""; rangeAbility4_act.text = ""; costAbility4_act.text = "";
        }
        else { captionAbility4_act.text = "DEAD"; dmgAbility4_act.text = ""; rangeAbility4_act.text = ""; costAbility4_act.text = ""; }

    }   //Load details of actions chosen in plan phase and displays them on execute actions panel
    public void SetDamageTakenCaptions(Character attacker, Character reciever)
    {

        string damageDealerCaption = $"{attacker.name} used {attacker.actionChosen.name}!";
        string damageTakerCaption = $"{reciever.name} has taken {GetTotalActionValue(attacker.actionChosen, "damage")}dmg";

        if (attacker.position < BattleManager.instance.charactersPlayer.Count)  //if the position is within bounds of player characters, the player is attacking, ergo RHS takes damage
        {
            AnimatorTrigger("takeDamageRight");
            popupDamage_left.text = damageDealerCaption;
            popupDamage_right.text = damageTakerCaption;
        }
        else
        {
            AnimatorTrigger("takeDamageLeft");
            popupDamage_left.text = damageTakerCaption;
            popupDamage_right.text = damageDealerCaption;
        }

    }
    #endregion

    public void OnDrawGizmos()
    {
        if (pos.Length > 0)
            for (int i = 0; i < pos.Length; i++)
            {
                Gizmos.DrawSphere(pos[i], .3f);
            }
    }
}
