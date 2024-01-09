using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaskManager : MonoBehaviour
{
    // Start is called before the first frame update
    public enum InteractionTYpe { Raycasting, Direct, MyTechnique};

    public InteractionTYpe type = InteractionTYpe.Direct;
    public List<GameObject> objectsToBeSelected;

    List<Tasklog> logTasks = new List<Tasklog>();
    List<SelectableObject> _selectableObjects;

    int currentObject = 0;
    List<SelectableObject> taskObjects;

    public Material materialSelection;


    SelectableObject currentSelectableObject;

    float startTimestamp = 0;
    float endTimestamp = 0;
    int numberOfObjects = 10;

    int countErrors = 0;
    float totalTime = 0;

    void Start()
    {
        _selectableObjects = new List<SelectableObject>();
        for(int i = 0; i < this.transform.childCount; i++)
        {
            if(transform.GetChild(i).GetComponent<MeshRenderer>() == null)
            {
                for(int j = 0; j < transform.GetChild(i).childCount; j++)
                {

                    SelectableObject tmp = transform.GetChild(i).GetChild(j).gameObject.AddComponent<SelectableObject>();
                    BoxCollider boxC = tmp.gameObject.AddComponent<BoxCollider>();
                    Rigidbody rigid = tmp.gameObject.AddComponent<Rigidbody>();
                    rigid.isKinematic = true;
                    rigid.useGravity = false;
                    boxC.isTrigger = true;
                    _selectableObjects.Add(tmp);
                    tmp.name = "objSelected_" + generateID("objSelected");
                    
                    //generate Unique name to gameObject
                }
            }
        }

        if (objectsToBeSelected != null)
        {
            if(objectsToBeSelected.Count > 0)
            {
                objectsToBeSelected[0].GetComponent<SelectableObject>().HighlightObject();
                startTimestamp = Time.realtimeSinceStartup;
                logTasks.Add(new Tasklog(startTimestamp));
            }
        }
    }

    public bool isCurrentSelectableObject(string objName)
    {
        return true;   
    }

    public void incrementErrors()
    {
        countErrors++;
        logTasks[currentObject].numberErrors++;
    }
    public SelectableObject getCurrentSelectableObject()
    {
        if(objectsToBeSelected != null)
        {
            if(objectsToBeSelected.Count < currentObject)
            {
                return objectsToBeSelected[currentObject].GetComponent<SelectableObject>();
            }
        }
        return null;
    }


    public string generateID(string url_add)
    {
        long i = 1;

        foreach (byte b in Guid.NewGuid().ToByteArray())
        {
            i *= ((int)b + 1);
        }

        string number = String.Format("{0:d9}", (DateTime.Now.Ticks / 10) % 1000000000);

        return number;
    }

    public void notifySelection(bool rightObject)
    {
        if (rightObject)
        {
            nextObject();
        }
        else
        {
            incrementErrors();
        }
            
    }

    void nextObject()
    {
        if (currentObject + 1 < objectsToBeSelected.Count)
        {
            SelectableObject select = objectsToBeSelected[currentObject].GetComponent<SelectableObject>();
            select.deSelectObject();
            SelectableObject nextObject = objectsToBeSelected[currentObject+1].GetComponent<SelectableObject>();
            nextObject.HighlightObject();
            logTasks[currentObject].Endtimestamp = Time.realtimeSinceStartup;
            
            currentObject++;
            logTasks.Add(new Tasklog(Time.realtimeSinceStartup));
            //
        }
        else
        {
            SelectableObject select = objectsToBeSelected[currentObject].GetComponent<SelectableObject>();
            select.deSelectObject();
            endTimestamp = Time.realtimeSinceStartup;
            logTasks[currentObject].Endtimestamp = Time.realtimeSinceStartup;
            //c'est fini
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            nextObject();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            selectRightObject();
        }
    }

    void selectRightObject()
    {
        if(objectsToBeSelected != null)
        {
            if(objectsToBeSelected.Count > currentObject)
            {
                objectsToBeSelected[currentObject].GetComponent<SelectableObject>().SelectObject();
            }
        }
    }

    void selectWrongObject()
    {
        if(objectsToBeSelected != null)
        {
            if(objectsToBeSelected.Count > (currentObject +1))
            {
                objectsToBeSelected[currentObject].GetComponent<SelectableObject>().SelectObject();
            }
        }
    }

    public void generateReport()
    {
        string fileName = type.ToString() + ".csv";
        string str = "task,time,precisionX,precisionY,precisionZ\n";
        foreach(Tasklog tLog in logTasks)
        {
            str += (tLog.Endtimestamp - tLog.Inittimestamp) + "," + tLog.numberErrors + "," + tLog.Precision.x + "," + 
                    tLog.Precision.y + "," + tLog.Precision.z + "\n";
        }
    }

    void startTask()
    {
    }
}
