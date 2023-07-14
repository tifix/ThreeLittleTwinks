/* This class handles UI, toggling various screens, screen transitions, button presses etc
 * 
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] int selectedCharacter = 0;
    public GameObject screen_AbilitySelect;
    public TMP_Text CaptionCharacterName;

    //Individual ability value displayers
    public TMP_Text captionAbility1, captionAbility2, captionAbility3, captionAbility4;
    public TMP_Text descriptionAbility1, descriptionAbility2, descriptionAbility3, descriptionAbility4;
    public TMP_Text dmgAbility1, dmgAbility2, dmgAbility3, dmgAbility4;
    public TMP_Text rangeAbility1, rangeAbility2, rangeAbility3, rangeAbility4;
    public TMP_Text costAbility1, costAbility2, costAbility3, costAbility4;


    void Start()
    {
        LoadDataForCharacter(BattleManager.instance.charactersPlayer[0]);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) ToggleActionSelection(!screen_AbilitySelect.activeInHierarchy); //LoadDataForCharacter(BattleManager.instance.charactersPlayer[0]);
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
