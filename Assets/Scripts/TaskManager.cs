using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum INTERACTION_TYPE
{
    RAYCASTING,
    DIRECT,
    MY_TECHNIQUE
}

public class TaskManager : MonoBehaviour
{
    public int userId = 0;
    public INTERACTION_TYPE interactionType = INTERACTION_TYPE.DIRECT;

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
        if (objectsToBeSelected != null)
        {
            if(objectsToBeSelected.Count > 0)
            {
                //objectsToBeSelected[0].GetComponent<SelectableObject>().HighlightObject();
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
            //select.deSelectObject();
            SelectableObject nextObject = objectsToBeSelected[currentObject+1].GetComponent<SelectableObject>();
            //nextObject.HighlightObject();
            logTasks[currentObject].Endtimestamp = Time.realtimeSinceStartup;
            
            currentObject++;
            logTasks.Add(new Tasklog(Time.realtimeSinceStartup));
            //
        }
        else
        {
            SelectableObject select = objectsToBeSelected[currentObject].GetComponent<SelectableObject>();
            //select.deSelectObject();
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
                //objectsToBeSelected[currentObject].GetComponent<SelectableObject>().SelectObject();
            }
        }
    }

    void selectWrongObject()
    {
        if(objectsToBeSelected != null)
        {
            if(objectsToBeSelected.Count > (currentObject +1))
            {
                //objectsToBeSelected[currentObject].GetComponent<SelectableObject>().SelectObject();
            }
        }
    }

    public void generateReport()
    {
        string fileName = userId+ ","+interactionType.ToString() + ".csv";
        string str = "timestamp,userId,interactionType,task,time,precisionX,precisionY,precisionZ\n";
        foreach(Tasklog tLog in logTasks)
        {
            str +=  Time.time + ","+ userId + ","+ interactionType + "," + (tLog.Endtimestamp - tLog.Inittimestamp) + "," + tLog.numberErrors + "," + tLog.Precision.x + "," + 
                    tLog.Precision.y + "," + tLog.Precision.z + "\n";
        }

        str += "Total," + userId + "," + interactionType + "," + (endTimestamp - startTimestamp) + "," + countErrors + "\n";

        CreateNewDataFile(fileName, str);

    }

    private void CreateNewDataFile(string filename,string xontent)
    {
        //** N.B. use .persistentDataPath if running on Quest/device unlinked,
        //**      else use .dataPath if Quest/device is linked/connected to machine via USB.
        string path = Application.dataPath + "/" + filename;
        //string path = Application.persistentDataPath + "/" + filename;

        //** Create new file if it doesn't exist, if append to file to avoid overwritting data due to selection error from previous scene.
        if (!File.Exists(path))
        {
            //var text = System.DateTime.Now + ",START\n";
            File.WriteAllText(path, xontent);
        }
        else
        {
            //** Add data to file
            //string content = System.DateTime.Now + ",File Already Exists\n";
            File.AppendAllText(path, xontent);
        }
    }

    void startTask()
    {
    }
}
