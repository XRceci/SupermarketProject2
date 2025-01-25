using UnityEngine;
using System.Collections.Generic;

public class MyTechnique : InteractionTechnique
{
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

    private float rotationAngle = 0f;  // Tracks the rotation of the arranged objects

    private void Start()
    {
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
        }
    }

    private void HandleObjectSelection()
    {
        if (!isAttracting || activeObjects.Count == 0) return;

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

                    SelectableObject selectableObject = hitObject.GetComponent<SelectableObject>();
                    if (selectableObject != null)
                    {
                        selectableObject.SetAsSuccess();
                        isDropping = true;
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
            if (isAttracting)
            {
                isAttracting = false;
                isReturning = true;
                RestoreOriginalMaterials();
                ReturnObjectsToOriginalPositions();
            }
            else if (!isReturning)
            {
                RaycastHit[] hits = Physics.RaycastAll(controllerTransform.position,
                                                     controllerTransform.forward,
                                                     raycastDistance);

                activeObjects.Clear();

                foreach (RaycastHit hit in hits)
                {
                    GameObject hitObject = hit.collider.gameObject;
                    if (!activeObjects.Contains(hitObject))
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
                }
            }
        }

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

    private void HandleObjectRotation()
    {
        Vector2 joystickInput = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        if (Mathf.Abs(joystickInput.x) > 0.1f)
        {
            rotationAngle += joystickInput.x * moveSpeed * Time.deltaTime;
            ArrangeObjectsInCircle();
        }
    }

    private void ArrangeObjectsInCircle()
    {
        Transform controllerTransform = rightController.transform;
        Vector3 centerPoint = controllerTransform.position + controllerTransform.forward * circleDistance;
        float angleStep = 360f / activeObjects.Count;

        for (int i = 0; i < activeObjects.Count; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad + rotationAngle;
            Vector3 targetPosition = centerPoint + new Vector3(
                Mathf.Sin(angle) * circleRadius,
                0,
                Mathf.Cos(angle) * circleRadius
            );

            activeObjects[i].transform.position = Vector3.Lerp(
                activeObjects[i].transform.position,
                targetPosition,
                Time.deltaTime * attractionSpeed
            );
        }
    }

    private void ReturnObjectsToOriginalPositions()
    {
        foreach (var obj in activeObjects)
        {
            obj.transform.position = originalPositions[obj];
        }
        activeObjects.Clear();
        isReturning = false;
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
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = obj.AddComponent<Rigidbody>();
        }
        rb.isKinematic = false;
        isDropping = false;
    }
}
