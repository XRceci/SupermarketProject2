using UnityEngine;
using System.Collections.Generic;

/// A VR selection technique that allows users to select occluded objects by bringing them closer
/// in a circular arrangement, highlighting them when pointed at, and selecting with the A button.

public class MyTechnique : InteractionTechnique
{
    // Reference to VR camera rig for movement
    [SerializeField]
    private OVRCameraRig cameraRig;

    // right controller 
    [SerializeField]
    private GameObject rightController;
    
    // Maximum distance for ray detection
    [SerializeField]
    private float raycastDistance = 500f;
    
    // Speed at which objects move to/from their positions
    [SerializeField]
    private float attractionSpeed = 2f;
    
    // Radius of the circle in which objects are arranged
    [SerializeField]
    private float circleRadius = 2f;
    
    // Distance of the circle from the controller
    [SerializeField]
    private float circleDistance = 3f;
    
    // Player movement speed with joystick
    [SerializeField]
    private float moveSpeed = 4f;

    // Material used to highlight objects when pointing at them
    [SerializeField]
    private Material highlightMaterial;

    // Line renderer for visual ray feedback
    private LineRenderer lineRenderer;

    // Dictionary to store original positions of moved objects
    private Dictionary<GameObject, Vector3> originalPositions = new Dictionary<GameObject, Vector3>();

    // Dictionary to store original materials of objects
    private Dictionary<GameObject, Material> originalMaterials = new Dictionary<GameObject, Material>();

    // List of currently active (moved) objects
    private List<GameObject> activeObjects = new List<GameObject>();

    // State tracking variables
    private bool isAttracting = false;        // Whether objects are being attracted
    private bool isReturning = false;         // Whether objects are returning
    private bool wasTriggerPressed = false;   // For trigger press detection
    private bool wasAButtonPressed = false;   // For A button press detection
    private GameObject pointedObject = null;   // Currently pointed object
    private GameObject selectedObject = null;  // Currently selected object
    private float selectionTime = 0f;         // Time when last selection occurred

    private void Start()
    {   
        // Initialize line renderer for visual feedback
        lineRenderer = rightController.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = rightController.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.01f;
            lineRenderer.endWidth = 0.01f;
            lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
            lineRenderer.material.color = Color.red;
        }
    }

    private void Update()
    {
        HandleMovement();           // Process player movement
        HandleObjectAttraction();   // Handle bringing objects closer/returning
        HandleObjectSelection();    // Handle pointing and selection
        base.CheckForSelection();   // Parent class selection check
    }

    /// Handles player movement using the left joystick.
    private void HandleMovement()
    {
        Vector2 joystickInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        if (joystickInput.magnitude > 0.1f)
        {
            // Get forward and right directions from camera
            Vector3 forward = Camera.main.transform.forward;
            Vector3 right = Camera.main.transform.right;
            
            // Remove vertical component for horizontal-only movement
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            // Apply movement in the direction of joystick input
            Vector3 moveDirection = (forward * joystickInput.y + right * joystickInput.x);
            cameraRig.transform.position += moveDirection * moveSpeed * Time.deltaTime;
        }
    }

    /// Handles object highlighting and selection with A button.
    /// Objects can be highlighted by pointing at them and selected with the A button.
    /// Selected objects maintain their success material for 3 seconds.
    private void HandleObjectSelection()
    {
        if (!isAttracting || activeObjects.Count == 0) return;

        Transform controllerTransform = rightController.transform;
        RaycastHit hit;

        // Clear selection after 5 seconds
        if (selectedObject != null && Time.time - selectionTime > 3f)
        {
            selectedObject = null;
        }

        // Reset highlight on previously pointed object if it's not selected
        if (pointedObject != null && pointedObject != selectedObject)
        {
            pointedObject.GetComponent<MeshRenderer>().material = originalMaterials[pointedObject];
            pointedObject = null;
        }

        // Check if pointing at any object
        if (Physics.Raycast(controllerTransform.position, controllerTransform.forward, out hit))
        {
            GameObject hitObject = hit.collider.gameObject;
            
            // Only handle objects that are active and not currently selected
            if (activeObjects.Contains(hitObject) && hitObject != selectedObject)
            {
                // Apply highlight material
                pointedObject = hitObject;
                hitObject.GetComponent<MeshRenderer>().material = highlightMaterial;

                // Check for A button press to select
                bool isAButtonPressed = OVRInput.Get(OVRInput.Button.One);
                if (isAButtonPressed && !wasAButtonPressed)
                {
                    selectedObject = hitObject;
                    currentSelectedObject = hitObject;
                    selectionTime = Time.time;
                    pointedObject = null; // Prevent highlighting selected object

                    // Apply success material through SelectableObject component
                    SelectableObject selectableObject = hitObject.GetComponent<SelectableObject>();
                    if (selectableObject != null)
                    {
                        selectableObject.SetAsSuccess();
                    }
                }
            }
        }
        
        wasAButtonPressed = OVRInput.Get(OVRInput.Button.One);
    }

    /// Handles bringing objects closer and returning them based on trigger input.
    /// First trigger press brings objects forward, second trigger returns them.
    private void HandleObjectAttraction()
    {
        Transform controllerTransform = rightController.transform;
        
        // Update visual ray
        lineRenderer.SetPosition(0, controllerTransform.position);
        lineRenderer.SetPosition(1, controllerTransform.position + controllerTransform.forward * raycastDistance);

        float triggerValue = OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger);
        bool isTriggerPressed = triggerValue > 0.1f;

        // Handle trigger press (not continuous hold)
        if (isTriggerPressed && !wasTriggerPressed)
        {
            if (isAttracting)
            {
                // Start returning objects
                isAttracting = false;
                isReturning = true;
                RestoreOriginalMaterials();
                selectedObject = null;
            }
            else if (!isReturning)
            {
                // Detect all objects along the ray
                RaycastHit[] hits = Physics.RaycastAll(controllerTransform.position, 
                                                     controllerTransform.forward, 
                                                     raycastDistance);

                // Clear previous selection if pointing in new direction
                if (hits.Length > 0 && activeObjects.Count > 0)
                {
                    bool foundOverlap = false;
                    foreach (RaycastHit hit in hits)
                    {
                        if (activeObjects.Contains(hit.collider.gameObject))
                        {
                            foundOverlap = true;
                            break;
                        }
                    }
                    
                    if (!foundOverlap)
                    {
                        activeObjects.Clear();
                    }
                }

                // Add new objects to active list
                foreach (RaycastHit hit in hits)
                {
                    GameObject hitObject = hit.collider.gameObject;
                    if (!activeObjects.Contains(hitObject))
                    {
                        activeObjects.Add(hitObject);
                        if (!originalPositions.ContainsKey(hitObject))
                        {
                            // Store original position and material
                            originalPositions[hitObject] = hitObject.transform.position;
                            MeshRenderer renderer = hitObject.GetComponent<MeshRenderer>();
                            originalMaterials[hitObject] = renderer.material;
                        }
                    }
                }

                if (activeObjects.Count > 0)
                {
                    isAttracting = true;
                }
            }
        }

        // Update object positions based on current state
        if (isAttracting && activeObjects.Count > 0)
        {
            ArrangeObjectsInCircle();
        }
        else if (isReturning && activeObjects.Count > 0)
        {
            ReturnObjectsToOriginalPositions();
        }

        wasTriggerPressed = isTriggerPressed;
    }

    /// Restores original materials for objects
    private void RestoreOriginalMaterials()
    {
        if (pointedObject != null)
        {
            pointedObject.GetComponent<MeshRenderer>().material = originalMaterials[pointedObject];
            pointedObject = null;
        }
    }

    /// Arranges active objects in a circle in front of the controller
    private void ArrangeObjectsInCircle()
    {
        Transform controllerTransform = rightController.transform;
        Vector3 centerPoint = controllerTransform.position + controllerTransform.forward * circleDistance;
        
        // Calculate angle step for even distribution
        float angleStep = 360f / activeObjects.Count;
        
        // Position each object around the circle
        for (int i = 0; i < activeObjects.Count; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 targetPosition = centerPoint + new Vector3(
                Mathf.Sin(angle) * circleRadius,
                0,
                Mathf.Cos(angle) * circleRadius
            );

            // Smoothly move object to target position
            activeObjects[i].transform.position = Vector3.Lerp(
                activeObjects[i].transform.position,
                targetPosition,
                Time.deltaTime * attractionSpeed
            );
        }
    }

    /// Returns all active objects to their original positions
    private void ReturnObjectsToOriginalPositions()
    {
        bool allObjectsReturned = true;
        List<GameObject> objectsToRemove = new List<GameObject>();

        // Move objects back to original positions
        foreach (GameObject obj in activeObjects)
        {
            if (originalPositions.ContainsKey(obj))
            {
                Vector3 currentPos = obj.transform.position;
                Vector3 targetPos = originalPositions[obj];
                
                obj.transform.position = Vector3.Lerp(currentPos, targetPos, Time.deltaTime * attractionSpeed);
                
                // Check if object has reached its original position
                if (Vector3.Distance(currentPos, targetPos) < 0.01f)
                {
                    objectsToRemove.Add(obj);
                    obj.transform.position = targetPos;
                }
                else
                {
                    allObjectsReturned = false;
                }
            }
        }

        // Clean up objects that have returned
        foreach (GameObject obj in objectsToRemove)
        {
            activeObjects.Remove(obj);
            originalPositions.Remove(obj);
            originalMaterials.Remove(obj);
        }

        // Reset state when all objects have returned
        if (allObjectsReturned && activeObjects.Count == 0)
        {
            isReturning = false;
            currentSelectedObject = null;
        }
    }
}