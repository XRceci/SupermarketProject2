using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : MonoBehaviour {
    [SerializeField]
    private MeshRenderer arrowMesh;

    [SerializeField]
    private TaskManager taskManager;

	void Update () {
        SelectableObject currentSelectableObject = taskManager.getCurrentSelectableObject();
        if (currentSelectableObject != null)
        {
            this.transform.LookAt(currentSelectableObject.transform.position);
        }
	}
}
