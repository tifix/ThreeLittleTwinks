/* This class handles UI, toggling various screens, screen transitions, button presses etc
 * 
 * current Debug Keymap 
 * SPACE - toggle Action selection menu
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static UnityEditor.PlayerSettings;
using static Unity.Burst.Intrinsics.X86;

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

    [Space(5), Header("Object references")]
    public LineRenderer targetLine; //the Line renderer rendering the targetting parabola from attacker to attackee
    public Animator transitions;    //Screen transitions!

    //Individual ability value displayers
    public TMP_Text captionAbility1, captionAbility2, captionAbility3, captionAbility4;
    public TMP_Text descriptionAbility1, descriptionAbility2, descriptionAbility3, descriptionAbility4;
    public TMP_Text dmgAbility1, dmgAbility2, dmgAbility3, dmgAbility4;
    public TMP_Text rangeAbility1, rangeAbility2, rangeAbility3, rangeAbility4;
    public TMP_Text costAbility1, costAbility2, costAbility3, costAbility4;
    //StatusPlayer and StatusEnemy    
    public TMP_Text statusCharPlayer1, statusCharPlayer2, statusCharPlayer3, statusCharPlayer4;
    public TMP_Text statusCharEnemy1, statusCharEnemy2, statusCharEnemy3, statusCharEnemy4;

    public List<GameObject> SelectedTokens = new List<GameObject>(4);       //The action selected tokens
    public GameObject SelectedArrow;                                        //Arrow showing which character's abilities are getting assigned
    private bool isAttackShowingNow = false;    //Checker for ensuring an attack plays out before a next one is started
    [SerializeField] Vector3[] pos; //Debug utility for the target line preview

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(this);
    }

    void Start()
    {
        HideTargetParabola();
        LoadDataForCharacter(BattleManager.instance.charactersPlayer[0]);
        //RefreshStatusCorners() IS CALLED ON START ON BATTLEMANAGER as it depends on character data loaded in start in BattleManager 
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) ToggleActionSelection(!screen_AbilitySelect.activeInHierarchy); //LoadDataForCharacter(BattleManager.instance.charactersPlayer[0]);
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
        ShowTargetParabola(init, fin, true);
        yield return new WaitForSeconds(0.5f);
        GameObject HitFX = Instantiate(BattleManager.instance.HitMarker, fin, Quaternion.identity);


        isAttackShowingNow = false;
        yield return new WaitForSeconds(2);
        Destroy(HitFX);
    }
    public void ShowTargetParabola(Vector3 init, Vector3 fin,bool hideAfterTime) 
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
        if (hideAfterTime) Invoke("HideTargetParabola", TargetParabolaLifetime);

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
        if (stats.Count > 0) statusCharPlayer1.text = $"{stats[0].name}: {stats[0].hpCur}/ {stats[0].hpMax}";
        if (stats.Count > 1) statusCharPlayer2.text = $"{stats[1].name}: {stats[1].hpCur}/ {stats[1].hpMax}";
        if (stats.Count > 2) statusCharPlayer3.text = $"{stats[2].name}: {stats[2].hpCur}/ {stats[2].hpMax}";
        if (stats.Count > 3) statusCharPlayer4.text = $"{stats[3].name}: {stats[3].hpCur}/ {stats[3].hpMax}";

        stats = BattleManager.instance.charactersEnemy;
        if (stats.Count > 0) statusCharEnemy1.text = $"{stats[0].name}: {stats[0].hpCur}/ {stats[0].hpMax}";
        if (stats.Count > 1) statusCharEnemy2.text = $"{stats[1].name}: {stats[1].hpCur}/ {stats[1].hpMax}";
        if (stats.Count > 2) statusCharEnemy3.text = $"{stats[2].name}: {stats[2].hpCur}/ {stats[2].hpMax}";
        if (stats.Count > 3) statusCharEnemy4.text = $"{stats[3].name}: {stats[3].hpCur}/ {stats[3].hpMax}";
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

    #endregion

    public void OnDrawGizmos()
    {
        if(pos.Length>0)
        for (int i = 0; i < pos.Length; i++)
        {
            Gizmos.DrawSphere(pos[i], .3f);
        }
    }
}
