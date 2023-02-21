using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FocusMenu : MonoBehaviour
{
    public Text focusText;

    public Transform buttonParent;
    public GameObject prefabButton;


    public void ShowFocusMenu()
    {
        Manager manager = Manager.instance;
        Pays country = manager.player;
        foreach (Transform child in buttonParent)
        {
            Destroy(child.gameObject);
        }

        if (country.currentFocus != "NONE")
        {
            focusText.text = "Current : " + manager.focus[country.currentFocus].focusName + " (" + (country.maxFocusTime - country.currentFocusTime) + "/" + country.maxFocusTime + ")";
        }
        else
        {
            focusText.text = "Current : None";
            List<Focus> available = country.GetAvailableFocus();
            foreach (Focus focus in available)
            {
                Instantiate(prefabButton, buttonParent).GetComponent<FocusButton>().Init(focus, this);
            }
        }
    }

    public void SelectFocus(string focus)
    {
        Manager manager = Manager.instance;
        Pays country = manager.player;
        country.ChangeFocus(focus);
        ShowFocusMenu();
    }
}
