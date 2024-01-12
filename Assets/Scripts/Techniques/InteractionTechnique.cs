using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InteractionTechnique : MonoBehaviour
{
    public UnityEvent<GameObject> objectSelectedEvent;

    private void SendObjectSelectedEvent(GameObject selectedObject)
    {
        objectSelectedEvent.Invoke(selectedObject);
    }
}
