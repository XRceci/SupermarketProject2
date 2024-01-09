using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectableObject : MonoBehaviour
{

    public bool isSelected = false;
    public Material matSelection;
    public Material originalMaterial;

    TaskManager manager;
    
    // Start is called before the first frame update
    void Awake()
    {
        manager = GameObject.Find("Models").GetComponent<TaskManager>();
        try
        {
            originalMaterial = this.GetComponent<MeshRenderer>().material;
            matSelection = manager.materialSelection;
        }
        catch(Exception ex)
        {
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void HighlightObject()
    {
        if (matSelection)
        {
            this.GetComponent<MeshRenderer>().material = matSelection;
            isSelected = true;
        }
    }

    public void SelectObject()
    {
        if (matSelection)
        {
            if (manager.isCurrentSelectableObject(this.name))
            {
                this.GetComponent<MeshRenderer>().material = originalMaterial;
                isSelected = true;
                manager.notifySelection();
                print("right stuff");
            }
            else
            {
                manager.incrementErrors();
                print("wrong stuff");
                //play sound?
            }
            //manager.SendMessage("isSelectedObjectSelected", this.gameObject.name);
        }
    }

    public void deSelectObject()
    {
        this.GetComponent<MeshRenderer>().material = originalMaterial;
        isSelected = false;
    }
}
