using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : MonoBehaviour {
    [SerializeField]
    private MeshRenderer arrowMesh;

    [SerializeField]
    private TaskManager taskManager;

	void Update () {
        GameObject currentObjectToSelect = taskManager.GetCurrentObjectToSelect();
        if (currentObjectToSelect != null)
        {
            this.transform.LookAt(currentObjectToSelect.transform.position);
        }
	}
}
