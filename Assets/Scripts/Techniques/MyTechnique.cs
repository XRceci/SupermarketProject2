using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Your implemented technique inherits the InteractionTechnique class
public class MyTechnique : InteractionTechnique
{
    // You must implement your technique in this file
    // You need to assign the selected Object to the currentSelectedObject variable
    // Then it will be sent through a UnityEvent to another class for handling
    private void Start()
    {
        // TODO
    }

    private void Update()
    {
        //TODO : Select a GameObject and assign it to the currentSelectedObject variable


        // DO NOT REMOVE
        // If currentSelectedObject is not null, this will send it to the TaskManager for handling
        // Then it will set currentSelectedObject back to null
        base.CheckForSelection();
    }
}
