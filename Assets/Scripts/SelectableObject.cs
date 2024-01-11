using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectableObject : MonoBehaviour
{
    private string objectName = "undefined";

    public bool isSelected = false;
    public Material matSelection;
    public Material originalMaterial;

    TaskManager manager;
    
    // Start is called before the first frame update
    void Awake()
    {
        manager = GameObject.Find("SelectableObjects").GetComponent<TaskManager>();
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

    public string GetObjectName()
    {
        return objectName;
    }

    public void SetObjectName(string objectName)
    {
        this.objectName = objectName;
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
                manager.notifySelection(true);
                print("right stuff");
            }
            else
            {
                manager.notifySelection(false);
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
