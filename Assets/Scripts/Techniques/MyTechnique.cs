using UnityEngine;
using System.Collections.Generic;

public class MyTechnique : InteractionTechnique
{
    [SerializeField]
    public GameObject Quad1; // step1 movement 
    public GameObject Quad2; // step2 trigger open
    public GameObject Quad3; // step3 trigger close
    

    [SerializeField]
    private OVRCameraRig cameraRig;  // Reference to VR camera rig for movement

    [SerializeField]
    private GameObject rightController;  // Right controller 

    [SerializeField]
    private float raycastDistance = 500f;  // Maximum distance for ray detection

    [SerializeField]
    private float attractionSpeed = 2f;  // Speed at which objects move to/from their positions

    [SerializeField]
    private float circleRadius = 2f;  // Radius of the circle in which objects are arranged

    [SerializeField]
    private float circleDistance = 3f;  // Distance of the circle from the controller

    [SerializeField]
    private float moveSpeed = 4f;  // Player movement speed with joystick

    [SerializeField]
    private Material highlightMaterial;  // Material used to highlight objects when pointing at them

    private LineRenderer lineRenderer;  // Line renderer for visual ray feedback

    private Dictionary<GameObject, Vector3> originalPositions = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, Material> originalMaterials = new Dictionary<GameObject, Material>();

    [SerializeField]
    private List<GameObject> activeObjects = new List<GameObject>();

    private bool isAttracting = false;
    private bool isReturning = false;
    private bool wasTriggerPressed = false;
    private bool wasAButtonPressed = false;
    private GameObject pointedObject = null;
    private GameObject selectedObject = null;
    private float selectionTime = 0f;
    private bool isDropping = false;
    private float returnStartTime = 0f;  // Tracks when objects start returning
    private Vector3 circlePosition;  // Stores the fixed position of the circle
    private float rotationAngle = 0f;  // Tracks the rotation of the arranged objects
    private Vector3 selectedObjectPosition;  // Position where object was selected

    private void Start()
    {
        lineRenderer = rightController.GetComponent<LineRenderer>();
        // turn on quad 1 turn off 2 3 4 5 
        Quad1.SetActive(true);
        Quad2.SetActive(false);
        Quad3.SetActive(false);
     


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
        HandleMovement();
        HandleObjectAttraction();
        HandleObjectSelection();
        HandleObjectRotation();  // Handle rotation using the right joystick
        base.CheckForSelection();
    }

    private void HandleMovement()
    {
        Vector2 joystickInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        if (joystickInput.magnitude > 0.1f)
        {
            Vector3 forward = Camera.main.transform.forward;
            Vector3 right = Camera.main.transform.right;

            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDirection = (forward * joystickInput.y + right * joystickInput.x);
            cameraRig.transform.position += moveDirection * moveSpeed * Time.deltaTime;

            // turn on quad 2 turn off 1 3 4 
            Quad1.SetActive(false);
            Quad2.SetActive(true);
            Quad3.SetActive(false);
            
        }
    }

    private void HandleObjectSelection()
    {
        // Only check isAttracting - we should still be able to select even if activeObjects is empty
        if (!isAttracting) return;

        Transform controllerTransform = rightController.transform;
        RaycastHit hit;

        if (selectedObject != null && Time.time - selectionTime > 3f)
        {
            selectedObject = null;
        }

        if (pointedObject != null && pointedObject != selectedObject)
        {
            pointedObject.GetComponent<MeshRenderer>().material = originalMaterials[pointedObject];
            pointedObject = null;
        }

        // Only try to highlight/select if we have active objects
        if (activeObjects.Count > 0)
        {
            if (Physics.Raycast(controllerTransform.position, controllerTransform.forward, out hit))
            {
                GameObject hitObject = hit.collider.gameObject;

                if (activeObjects.Contains(hitObject) && hitObject != selectedObject)
                {
                    pointedObject = hitObject;
                    hitObject.GetComponent<MeshRenderer>().material = highlightMaterial;

                    bool isAButtonPressed = OVRInput.Get(OVRInput.Button.One);
                    if (isAButtonPressed && !wasAButtonPressed)
                    {
                        selectedObject = hitObject;
                        currentSelectedObject = hitObject;
                        selectionTime = Time.time;
                        pointedObject = null;

                        selectedObjectPosition = hitObject.transform.position;
                        activeObjects.Remove(hitObject);

                        SelectableObject selectableObject = hitObject.GetComponent<SelectableObject>();
                        if (selectableObject != null)
                        {
                            selectableObject.SetAsSuccess();
                            isDropping = true;
                        }
                    }
                }
            }
        }

        wasAButtonPressed = OVRInput.Get(OVRInput.Button.One);

        if (isDropping && selectedObject != null)
        {
            DropObject(selectedObject);
        }
    }

    private void HandleObjectAttraction()
    {
        Transform controllerTransform = rightController.transform;
        lineRenderer.SetPosition(0, controllerTransform.position);
        lineRenderer.SetPosition(1, controllerTransform.position + controllerTransform.forward * raycastDistance);

        float triggerValue = OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger);
        bool isTriggerPressed = triggerValue > 0.1f;

        

        if (isTriggerPressed && !wasTriggerPressed)
        {
             

            if (isAttracting && activeObjects.Count > 0)  // Only try to return if we have objects
            {
                isAttracting = false;
                isReturning = true;
                returnStartTime = Time.time;
                RestoreOriginalMaterials();
            }
            else if (!isReturning)  // This will now trigger when we have no objects
            {
                RaycastHit[] hits = Physics.RaycastAll(controllerTransform.position,
                                                    controllerTransform.forward,
                                                    raycastDistance);

                activeObjects.Clear();
                foreach (RaycastHit hit in hits)
                {
                    GameObject hitObject = hit.collider.gameObject;
                    if (!activeObjects.Contains(hitObject) && hitObject != selectedObject)
                    {
                        activeObjects.Add(hitObject);
                        if (!originalPositions.ContainsKey(hitObject))
                        {
                            originalPositions[hitObject] = hitObject.transform.position;
                            originalMaterials[hitObject] = hitObject.GetComponent<MeshRenderer>().material;
                        }
                    }
                }

                if (activeObjects.Count > 0)
                {
                    isAttracting = true;
                    circlePosition = controllerTransform.position + controllerTransform.forward * circleDistance;
                }
            }
        }

        if (isAttracting && activeObjects.Count > 0)
        {
            Quad1.SetActive(false);// turn on quad 3   turn off 1 2 4
            Quad2.SetActive(false);
            Quad3.SetActive(true);
            
            ArrangeObjectsInCircle();
        }
        else if (isReturning && activeObjects.Count > 0)
        {
            ReturnObjectsToOriginalPositions();
             
        }

        wasTriggerPressed = isTriggerPressed;
    }

    private void HandleObjectRotation()
    {
        // Don't allow rotation if objects are returning or there are no active objects
        if (isReturning || activeObjects.Count == 0) return;

        Vector2 joystickInput = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        if (Mathf.Abs(joystickInput.x) > 0.1f)
        {
            rotationAngle += joystickInput.x * moveSpeed * Time.deltaTime;
            ArrangeObjectsInCircle();

            Quad1.SetActive(false);
            Quad2.SetActive(false);
            Quad3.SetActive(true);
            
        }
    }

    private void ArrangeObjectsInCircle()
    {
        // Use stored position instead of controller position
        float angleStep = 360f / activeObjects.Count;

        for (int i = 0; i < activeObjects.Count; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad + rotationAngle;
            Vector3 targetPosition = circlePosition + new Vector3(
                Mathf.Sin(angle) * circleRadius,
                0,
                Mathf.Cos(angle) * circleRadius
            );

            activeObjects[i].transform.position = Vector3.Lerp(
                activeObjects[i].transform.position,
                targetPosition,
                Time.deltaTime * attractionSpeed
            );
            Quad1.SetActive(false);
            Quad2.SetActive(false);
            Quad3.SetActive(true);
            
        }
    }

    private void ReturnObjectsToOriginalPositions()
    {
        List<GameObject> objectsToRemove = new List<GameObject>();

        foreach (GameObject obj in activeObjects)
        {
            Vector3 currentPos = obj.transform.position;
            Vector3 targetPos = originalPositions[obj];

            obj.transform.position = Vector3.Lerp(currentPos, targetPos, Time.deltaTime * attractionSpeed);

            if (Vector3.Distance(currentPos, targetPos) < 0.1f)
            {
                objectsToRemove.Add(obj);
                obj.transform.position = targetPos;
            }
        }

        foreach (GameObject obj in objectsToRemove)
        {
            activeObjects.Remove(obj);
            originalPositions.Remove(obj);
            originalMaterials.Remove(obj);
        }

        if (activeObjects.Count == 0 || Time.time - returnStartTime > 2.0f)
        {
            isReturning = false;
            activeObjects.Clear();
            //originalPositions.Clear();
            //originalMaterials.Clear();

            Quad1.SetActive(false);
            Quad2.SetActive(true);
            Quad3.SetActive(false);
            
        }
    }

    private void RestoreOriginalMaterials()
    {
        if (pointedObject != null)
        {
            pointedObject.GetComponent<MeshRenderer>().material = originalMaterials[pointedObject];
            pointedObject = null;
        }
    }

    private void DropObject(GameObject obj)
    {
        // Set object position to where it was selected before adding physics
        obj.transform.position = selectedObjectPosition;

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = obj.AddComponent<Rigidbody>();
        }
        rb.isKinematic = false;
        isDropping = false;

        Quad1.SetActive(true);
        Quad2.SetActive(false);
        Quad3.SetActive(false);
        
    }
}