using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using JetBrains.Annotations;

public class UI_SaveLoadSystem : MonoBehaviour
{
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] Button saveButton;
    [SerializeField] Button loadButton;
    [SerializeField] Button deleteButton;
    [SerializeField] Button exitButton;
    [SerializeField] TMP_InputField MachineNameInput;
    [SerializeField] Image MachinePreview;
    [SerializeField] TextMeshProUGUI[] MachineDetailsTexts;
    [SerializeField] ScrollRect ScrollView;
    [SerializeField] List<TMP_InputField> MachinesLoadedInputFields;
    

    void Start()
    {
        Play.OnPlayStateChange += OnPlayStateChange;
    }
    void OnDestroy()
    {
        Play.OnPlayStateChange -= OnPlayStateChange;
    }
    void OnPlayStateChange(Play.State state)
    {
        if (state == Play.State.SaveLoad)
        {
            LoadMachines();
            ShowItemsInScrollView();
        }
    }
    private void LoadCurrentMachine(){
          
    }
    public void LoadMachines()
    {
        for (int i = 1; i < MachinesLoadedInputFields.Count; i++)
        {
         Destroy(MachinesLoadedInputFields[i].gameObject);
        }
        Creator.MachineData machineData = Creator.instance.CreateMachineData("AutoSave");
        MachinesLoadedInputFields[0].text = machineData.machine_name;
        for (int i = 1; i < 10; i++)
        {
            if (PlayerPrefs.HasKey("machine_" + i))
            {
                TMP_InputField t = Instantiate(MachinesLoadedInputFields[0], ScrollView.content);
                t.gameObject.SetActive(true);
                
                MachinesLoadedInputFields[i].text = PlayerPrefs.GetString("machine_" + i);
            }
            else
            {
                MachinesLoadedInputFields[i].text = "Empty";
            }
        }
        // Load machines from the save file
        // Update the UI with the loaded machines
    }
    public void ShowItemsInScrollView()
    {
        
    }
}
