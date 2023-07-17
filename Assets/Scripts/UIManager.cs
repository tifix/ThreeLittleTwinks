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

[ExecuteInEditMode]
public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [SerializeField] int selectedCharacter = 0;
    public GameObject screen_AbilitySelect;
    public GameObject screen_EncounterSelect;
    public TMP_Text CaptionCharacterName;

    [Header("Targetting line parameters")]
    [Tooltip("how many points is the curve made up of?"), SerializeField] int   targetCurvatureResolution;
    [Tooltip("how tall should the curve be"), SerializeField]           float   targetBaseHeight, targetMaxHeight;
                                             [SerializeField]           float   TargetParabolaLifetime = 0.5f;    //Checker for ensuring an attack plays out before a next one is started
                                                                public  float   PreviewParabolaLifetime = 0.5f;    //Checker for ensuring an attack plays out before a next one is started

    [Space(5), Header("Object references")]
    public LineRenderer targetLine; //the Line renderer rendering the targetting parabola from attacker to attackee
    public Animator transitions;    //Screen transitions!

    //Individual ability value displayers - PLAN
    public TMP_Text captionAbility1, captionAbility2, captionAbility3, captionAbility4;
    public TMP_Text descriptionAbility1, descriptionAbility2, descriptionAbility3, descriptionAbility4;
    public TMP_Text dmgAbility1, dmgAbility2, dmgAbility3, dmgAbility4;
    public TMP_Text rangeAbility1, rangeAbility2, rangeAbility3, rangeAbility4;
    public TMP_Text costAbility1, costAbility2, costAbility3, costAbility4;

    //Individual ability value displayers - ACT
    public TMP_Text captionAbility1_act, captionAbility2_act, captionAbility3_act, captionAbility4_act;
    public TMP_Text casterAbility1_act, casterAbility2_act, casterAbility3_act, casterAbility4_act;
    public TMP_Text dmgAbility1_act, dmgAbility2_act, dmgAbility3_act, dmgAbility4_act;
    public TMP_Text rangeAbility1_act, rangeAbility2_act, rangeAbility3_act, rangeAbility4_act;
    public TMP_Text costAbility1_act, costAbility2_act, costAbility3_act, costAbility4_act;

    //StatusPlayer and StatusEnemy    
    public TMP_Text statusCharPlayer1, statusCharPlayer2, statusCharPlayer3, statusCharPlayer4;
    public TMP_Text statusCharEnemy1, statusCharEnemy2, statusCharEnemy3, statusCharEnemy4;

    public List<GameObject> SelectedTokens = new List<GameObject>(4);       //The action selected tokens
    public GameObject SelectedArrow;                                        //Arrow showing which character's abilities are getting assigned
    private bool isAttackShowingNow = false;    //Checker for ensuring an attack plays out before a next one is started
    public TMP_Text popupDamage_left, popupDamage_right;
    private List<Character> initialCharacterData =new();
    [SerializeField] Vector3[] pos; //Debug utility for the target line preview

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

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) SwitchBetweenPlanActPhases();  //ToggleActionSelection(!screen_AbilitySelect.activeInHierarchy); //LoadDataForCharacter(BattleManager.instance.charactersPlayer[0]);
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
    public void SwitchBetweenPlanActPhases() 
    {
        bool isPlanning = !BattleManager.instance.isPlanningStage;
        BattleManager.instance.isPlanningStage = isPlanning;

        if (isPlanning)
        {
            SelectedArrow.SetActive(true);
            AnimatorTrigger("SwitchToPlan");
        }
        else
        {
            LoadActionDescriptions();   //Load details of actions chosen in plan phase and displays them on execute actions panel
            SelectedArrow.SetActive(false);
            AnimatorTrigger("SwitchToAct");
        }    
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
    public void ShowTargetParabola(Vector3 init, Vector3 fin,float hideAfterTime) //Call with negative float to NOT hide
    {
        //Drawing initial and final positions
        targetLine.positionCount = targetCurvatureResolution;
        Vector3[] trajectory = new Vector3[targetCurvatureResolution];
        pos = new Vector3[targetCurvatureResolution];
        trajectory[0] = new Vector3(init.x,init.y+ targetBaseHeight,init.z); 
        trajectory[targetCurvatureResolution-1] = new Vector3(fin.x, fin.y + targetBaseHeight, fin.z);

        //Determining natural peak height by solving for midpoint coordinate
        float peakY = -1 * ((init.x+fin.x)/2-init.x) * ((init.x + fin.x) / 2 - fin.x);

        Vector3 midPosition;
        for (int i = 1; i < targetCurvatureResolution-1; i++)
        {
            midPosition = (targetCurvatureResolution - (float)i) / targetCurvatureResolution * init + ((float)i / targetCurvatureResolution * fin); //getting X from curve resolution
            float y = -1 * (midPosition.x - init.x)*(midPosition.x - fin.x);                                                          //The equation of the curve

            //Lerp the curve so that the height of both end-points and peak is consistent and adjustable
            float relativeY = Mathf.InverseLerp(0, peakY, y); //this represents how far up th
            y = Mathf.Lerp(targetBaseHeight,targetMaxHeight, relativeY);

            //Exporting values to Gizmos and LineRenderer
            midPosition +=  y * Vector3.up;
            trajectory[i] = midPosition;
            pos[i] = midPosition;
        }

        targetLine.enabled = true;
        targetLine.SetPositions(trajectory);
        if (hideAfterTime>0) Invoke("HideTargetParabola", hideAfterTime);

    }
    public void HideTargetParabola() { targetLine.positionCount = 0; }
    #endregion

    // Functionality of Ability selection menu - choosing which abilities which characters will cast in combat this turn
    #region PLAN UI

    //Load text-box data
    void LoadDataForCharacter(Character c) 
    {
        CaptionCharacterName.text = c.name;
        SelectedArrow.transform.SetParent(BattleManager.instance.characterPositions[selectedCharacter]);
        SelectedArrow.transform.localPosition = Vector3.zero;
    }
    public void ToggleActionSelection(bool state) 
    { 
        selectedCharacter=0;
        if (state) LoadDataForCharacter(BattleManager.instance.charactersPlayer[selectedCharacter]);
        screen_AbilitySelect.SetActive(state); 
    }

    void LoadDataForCharacter(Character c) 
    {
        CaptionCharacterName.text = c.name;

        captionAbility1.text = c.actionsAvalible[0].name;
        captionAbility2.text = c.actionsAvalible[1].name;
        captionAbility3.text = c.actionsAvalible[2].name;
        captionAbility4.text = c.actionsAvalible[3].name;

        descriptionAbility1.text = c.actionsAvalible[0].updatedData.description;
        descriptionAbility2.text = c.actionsAvalible[1].updatedData.description;
        descriptionAbility3.text = c.actionsAvalible[2].updatedData.description;
        descriptionAbility4.text = c.actionsAvalible[3].updatedData.description;

        dmgAbility1.text = c.actionsAvalible[0].updatedData.damage.ToString();
        dmgAbility2.text = c.actionsAvalible[1].updatedData.damage.ToString();
        dmgAbility3.text = c.actionsAvalible[2].updatedData.damage.ToString();
        dmgAbility4.text = c.actionsAvalible[3].updatedData.damage.ToString();

        rangeAbility1.text = c.actionsAvalible[0].updatedData.range.ToString();
        rangeAbility2.text = c.actionsAvalible[1].updatedData.range.ToString();
        rangeAbility3.text = c.actionsAvalible[2].updatedData.range.ToString();
        rangeAbility4.text = c.actionsAvalible[3].updatedData.range.ToString();

        costAbility1.text = c.actionsAvalible[0].updatedData.cost.ToString();
        costAbility2.text = c.actionsAvalible[1].updatedData.cost.ToString();
        costAbility3.text = c.actionsAvalible[2].updatedData.cost.ToString();
        costAbility4.text = c.actionsAvalible[3].updatedData.cost.ToString();
    }

    void LoadDataDefault() => LoadDataForCharacter(BattleManager.instance.charactersPlayer[selectedCharacter]); //Load data for currently selected character, [0] by default
    
    public void RefreshStatusCorners() 
    {
        List<Character> stats = BattleManager.instance.charactersPlayer;
        if (stats.Count > 0) statusCharPlayer1.text = $"{stats[0].name}: {stats[0].hpCur}/ {stats[0].hpMax}"; else statusCharPlayer1.text = $"{initialCharacterData[0].name} DEAD";
        if (stats.Count > 1) statusCharPlayer2.text = $"{stats[1].name}: {stats[1].hpCur}/ {stats[1].hpMax}"; else statusCharPlayer2.text = $"{initialCharacterData[1].name} DEAD";
        if (stats.Count > 2) statusCharPlayer3.text = $"{stats[2].name}: {stats[2].hpCur}/ {stats[2].hpMax}"; else statusCharPlayer3.text = $"{initialCharacterData[2].name} DEAD";
        if (stats.Count > 3) statusCharPlayer4.text = $"{stats[3].name}: {stats[3].hpCur}/ {stats[3].hpMax}"; else statusCharPlayer4.text = $"{initialCharacterData[3].name} DEAD"; 

        stats = BattleManager.instance.charactersEnemy;
        if (stats.Count > 0) statusCharEnemy1.text = $"{stats[0].name}: {stats[0].hpCur}/ {stats[0].hpMax}"; else statusCharEnemy1.text = $"{initialCharacterData[4].name} DEAD";
        if (stats.Count > 1) statusCharEnemy2.text = $"{stats[1].name}: {stats[1].hpCur}/ {stats[1].hpMax}"; else statusCharEnemy2.text = $"{initialCharacterData[5].name} DEAD";
        if (stats.Count > 2) statusCharEnemy3.text = $"{stats[2].name}: {stats[2].hpCur}/ {stats[2].hpMax}"; else statusCharEnemy3.text = $"{initialCharacterData[6].name} DEAD";
        if (stats.Count > 3) statusCharEnemy4.text = $"{stats[3].name}: {stats[3].hpCur}/ {stats[3].hpMax}"; else statusCharEnemy4.text = $"{initialCharacterData[7].name} DEAD";
    }   //Refresh the textboxes displaying various character's health


    //Buttons for Ability selection menu - SetAbility sets the character's chosen ability to one of the 4 abilities, SelectNext/Previous cycles between player characters.
    public void SetAbility(int index) 
    {
        Character c = BattleManager.instance.charactersPlayer[selectedCharacter];

        AnimatorTrigger("pulseActionToken"+ (selectedCharacter+1).ToString());
        Debug.Log("pulseActionToken" + (selectedCharacter + 1).ToString());
        c.actionChosen = c.actionsAvalible[index];
        c.actionChosen.Initialise();
    }
    public void SelectNextCharacter() 
    { 
        selectedCharacter++; 
        if (selectedCharacter > BattleManager.instance.charactersPlayer.Count-1) selectedCharacter = 0;

        LoadDataForCharacter(BattleManager.instance.charactersPlayer[selectedCharacter]);
    }       //cycle character + fetch new char's ability data
    public void SelectPreviousCharacter() 
    { 
        selectedCharacter--; 
        if (selectedCharacter < 0) selectedCharacter = BattleManager.instance.charactersPlayer.Count - 1;

        LoadDataForCharacter(BattleManager.instance.charactersPlayer[selectedCharacter]);
    }   //cycle character + fetch new char's ability data

    public void ToggleSelectedToken(bool state) => SelectedTokens[selectedCharacter].SetActive(state);  //Toggle ability selected token On/off for the character currently selected
    public void EnableSelectedForCharacter(int index) { SelectedTokens[index].SetActive(true); Debug.Log("Now activating"+index); }
    #endregion

    #region ACT UI
    public void LoadActionDescriptions()
    {
        if (BattleManager.instance.CheckIfActionValid(1))
        {
            captionAbility1_act.text = BattleManager.instance.charactersPlayer[0].actionChosen.name;
            casterAbility1_act.text = BattleManager.instance.charactersPlayer[0].name;
            dmgAbility1_act.text = BattleManager.instance.charactersPlayer[0].actionChosen.updatedData.damage.ToString();
            rangeAbility1_act.text = BattleManager.instance.charactersPlayer[0].actionChosen.updatedData.range.ToString();
            costAbility1_act.text = BattleManager.instance.charactersPlayer[0].actionChosen.updatedData.cost.ToString();
        }
        else { captionAbility1_act.text = "DEAD"; dmgAbility1_act.text = ""; rangeAbility1_act.text = ""; costAbility1_act.text = ""; }
        if (BattleManager.instance.CheckIfActionValid(2))
        {
            captionAbility2_act.text = BattleManager.instance.charactersPlayer[1].actionChosen.name;
            casterAbility2_act.text = BattleManager.instance.charactersPlayer[1].name;
            dmgAbility2_act.text = BattleManager.instance.charactersPlayer[1].actionChosen.updatedData.damage.ToString();
            rangeAbility2_act.text = BattleManager.instance.charactersPlayer[1].actionChosen.updatedData.range.ToString();
            costAbility2_act.text = BattleManager.instance.charactersPlayer[1].actionChosen.updatedData.cost.ToString();
        }
        else { captionAbility2_act.text = "DEAD"; dmgAbility2_act.text = ""; rangeAbility2_act.text = ""; costAbility2_act.text = ""; }

        if (BattleManager.instance.CheckIfActionValid(3))
        {

            captionAbility3_act.text = BattleManager.instance.charactersPlayer[2].actionChosen.name;
            casterAbility3_act.text = BattleManager.instance.charactersPlayer[2].name;
            dmgAbility3_act.text = BattleManager.instance.charactersPlayer[2].actionChosen.updatedData.damage.ToString();
            rangeAbility3_act.text = BattleManager.instance.charactersPlayer[2].actionChosen.updatedData.range.ToString();
            costAbility3_act.text = BattleManager.instance.charactersPlayer[2].actionChosen.updatedData.cost.ToString();
        }
        else { captionAbility3_act.text = "DEAD"; dmgAbility3_act.text = ""; rangeAbility3_act.text = ""; costAbility3_act.text = ""; }
        if (BattleManager.instance.CheckIfActionValid(4)) 
        {
            captionAbility4_act.text = BattleManager.instance.charactersPlayer[3].actionChosen.name;
            casterAbility4_act.text = BattleManager.instance.charactersPlayer[3].name;
            dmgAbility4_act.text = BattleManager.instance.charactersPlayer[3].actionChosen.updatedData.damage.ToString();
            rangeAbility4_act.text = BattleManager.instance.charactersPlayer[3].actionChosen.updatedData.range.ToString();
            costAbility4_act.text = BattleManager.instance.charactersPlayer[3].actionChosen.updatedData.cost.ToString();
        }
        else { captionAbility4_act.text = "DEAD"; dmgAbility4_act.text = ""; rangeAbility4_act.text = ""; costAbility4_act.text = ""; }

    }   //Load details of actions chosen in plan phase and displays them on execute actions panel
    public void SetDamageTakenCaptions(Character attacker) 
    {
        
        string damageDealerCaption =    $"{attacker.name} used {attacker.actionChosen.name}!";                  
        string damageTakerCaption =     $"{attacker.actionChosen.GetTargetCharacter().name} has taken {attacker.actionChosen.updatedData.damage}dmg";    

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
        if(pos.Length>0)
        for (int i = 0; i < pos.Length; i++)
        {
            Gizmos.DrawSphere(pos[i], .3f);
        }

    public void SetAbility(int index) 
    { 
        BattleManager.instance.charactersPlayer[selectedCharacter].actionChosen = BattleManager.instance.charactersPlayer[selectedCharacter].actionsAvalible[index];
        BattleManager.instance.charactersPlayer[selectedCharacter].actionChosen.Initialise();
    }

    public void SelectNextCharacter() 
    { 
        selectedCharacter++; 
        if (selectedCharacter > BattleManager.instance.charactersPlayer.Length-1) selectedCharacter = 0;

        LoadDataForCharacter(BattleManager.instance.charactersPlayer[selectedCharacter]);
    }
    public void SelectPreviousCharacter() 
    { 
        selectedCharacter--; 
        if (selectedCharacter < 0) selectedCharacter = BattleManager.instance.charactersPlayer.Length-1;

        LoadDataForCharacter(BattleManager.instance.charactersPlayer[selectedCharacter]);
    }
}
