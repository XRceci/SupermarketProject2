using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum INTERACTION_TYPE
{
    RAYCASTING,
    MY_TECHNIQUE
}

public class TaskManager : MonoBehaviour
{
    public int userId = 0;
    public INTERACTION_TYPE interactionType = INTERACTION_TYPE.RAYCASTING;
    public List<GameObject> objectsToSelect;

    private int currentObjectIndex = 0;

    // REVIEW ME
    List<Tasklog> logTasks = new List<Tasklog>();
    float startTimestamp = 0;
    float endTimestamp = 0;
    int numberOfObjects = 10;
    int errorCount = 0;
    float totalTime = 0;

    private void Start()
    {
        objectsToSelect[currentObjectIndex].GetComponent<SelectableObject>().SetAsTarget();
    }

    public GameObject GetCurrentObjectToSelect()
    {
        return objectsToSelect[currentObjectIndex];
    }

    public void OnSelectionEvent(SelectableObject selectedObject)
    {
        SelectableObject selectedObjectScript = selectedObject.GetComponent<SelectableObject>();
        SelectableObject objectToSelectScript = objectsToSelect[currentObjectIndex].GetComponent<SelectableObject>();
        if (selectedObjectScript == null || selectedObjectScript.GetObjectName() != objectToSelectScript.GetObjectName())
        {
            HandleSelectionError();
        }
        else
        {
            objectToSelectScript.SetAsSuccess();
            // TODO add log
            BeginNextSelectionTask();
        }
    }

    private void HandleSelectionError()
    {
        errorCount++;
        // TODO add log
    }

    private void BeginNextSelectionTask()
    {
        if (currentObjectIndex + 1 < objectsToSelect.Count)
        {
            currentObjectIndex++;

            SelectableObject objectToSelectScript = objectsToSelect[currentObjectIndex].GetComponent<SelectableObject>();
            objectToSelectScript.SetAsTarget();

            // TODO add log
        }
        else
        {
            // TODO add log and handle end of study
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

        str += "Total," + userId + "," + interactionType + "," + (endTimestamp - startTimestamp) + "," + errorCount + "\n";

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
}
