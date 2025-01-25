# What is RingSelect ?
A VR selection technique that allows users to select multiple objects along a ray and bring them closer for inspection in a circular arrangement.

# Implementation

## 1 - First, Let's walk !

We should provide our user with the ability to explore the supermarket to check out what they like, we need to be able to walk!

for that, we can use the left joy stick by getting the input from `PrimaryThumbstick`, where "Primary" is the left hand controller.

the "forward" and "right" directions should depend on where the user is looking, so we get where the camera is pointing at

then we calculate how much the user should move and we apply this transformation to our `cameraRig`'s position depending on the `moveSpeed` that we want .

```C#
    private void HandleMovement() 
    {
        Vector2 joystickInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        if (joystickInput.magnitude > 0.1f) 
        {
            // Get forward and right directions from the camera
            Vector3 forward = Camera.main.transform.forward;
            Vector3 right = Camera.main.transform.right;
            // Remove vertical component for horizontal-only movement
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();
            // Calculate and apply movement
            Vector3 moveDirection = (forward * joystickInput.y + right * joystickInput.x);
            cameraRig.transform.position += moveDirection * moveSpeed * Time.deltaTime;
        }
    }
```


## 2 - Pre-Selection
### 2.1 - Ray casting

Our technique begins by allowing users to point at objects using ray casting from their right controller. First we create a visual representation of the ray using Unity's `LineRenderer`:

```C#
private LineRenderer lineRenderer;
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
```

In our `HandleObjectAttraction` method, we continuously update the ray's position and direction:
```C#
// Update visual ray
lineRenderer.SetPosition(0, controllerTransform.position);
lineRenderer.SetPosition(1, controllerTransform.position + controllerTransform.forward * raycastDistance);
```

Since we want to be able to detect even occluded objects, we use 3. we use `Physics.RaycastAll` to detect all objects along the ray:
```C#
RaycastHit[] hits = Physics.RaycastAll(controllerTransform.position, 
                                     controllerTransform.forward, 
                                     raycastDistance);
```

the array `RaycastHit[]` will contain all the objects that has been detected along the ray

### 2.2 - Bringing Objects Closer

Once we've detected all objects along the ray, we need to bring them closer to the user and arrange them in a circle for easier selection. This happens when the user pulls the trigger.

First, we store the original positions of all detected objects so we can return them later:
```C#
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
```

Then we arrange the objects in a circle, for that we have an `ArrangeObjectsInCircle` method:

```C#
private void ArrangeObjectsInCircle()
{
    Transform controllerTransform = rightController.transform;
    // Calculate the center point of the circle in front of the controller
    Vector3 centerPoint = controllerTransform.position + controllerTransform.forward * circleDistance;
    
    // Calculate angle step for even distribution
    float angleStep = 360f / activeObjects.Count;
    
    // Position each object around the circle
    for (int i = 0; i < activeObjects.Count; i++)
    {
        float angle = i * angleStep * Mathf.Deg2Rad;
        Vector3 targetPosition = centerPoint + new Vector3(
            Mathf.Sin(angle) * circleRadius,  // X position
            0,                                // Keep Y position constant
            Mathf.Cos(angle) * circleRadius   // Z position
        );

        // Smoothly move object to target position
        activeObjects[i].transform.position = Vector3.Lerp(
            activeObjects[i].transform.position,
            targetPosition,
            Time.deltaTime * attractionSpeed
        );
    }
}
```

- `circleDistance`: Controls how far in front of the user the circle appears
- `circleRadius`: Determines the size of the circle
- `attractionSpeed`: Controls how quickly objects move to their positions

Objects are evenly spaced around the circle by calculating `angleStep`, and we use `Vector3.Lerp` to get a smooth movement animation

## 3 - Selection Process

Once all objects are arranged in a circle in front of the user, now it's time to choose which object they want ! For that, we need to:
1. highlight the object that we're pointing at
2. be able to confirm our selection using the button "A"

###  3.1 - Object Highlighting

First, we need to detect which object the user is pointing at and highlight it:
```C#
// Check if pointing at any object
if (Physics.Raycast(controllerTransform.position, controllerTransform.forward, out hit)) {
	GameObject hitObject = hit.collider.gameObject;    
    // Only highlight active objects that aren't currently selected
    if (activeObjects.Contains(hitObject) && hitObject != selectedObject)
    {
	    pointedObject = hitObject;
	    hitObject.GetComponent<MeshRenderer>().material = highlightMaterial;
    }
}
```
This time, we use `Physics.Raycast` and not `Physics.RaycastAll` because there won't be any occluded objects.
When we point at an object, we load a "Highlight" material to get visual feedback that this object can now be selected, when we're not pointing at the object anymore, the original material is loaded back on our object

### 3.2 - Selecting

When the user decides which object they want, they can press the A button to select it:
```C# 
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
```



## 4 - Returning the objects back

When users are done examining the objects and have made their selection (or decided not to select anything), they can press the trigger again to return all objects to their original positions. 

1. **Tracking Return Progress**:
```C#
bool allObjectsReturned = true;
List<GameObject> objectsToRemove = new List<GameObject>();
```
We use `allObjectsReturned` to track if all objects have reached their destinations
and `objectsToRemove` to store objects that have completed their return journey


2. **Arrival Detection**:
Objects that have arrived are marked for removal from active tracking
```C#
if (Vector3.Distance(currentPos, targetPos) < 0.01f)
{
    objectsToRemove.Add(obj);
    obj.transform.position = targetPos; // Ensure exact position
}
else
{
    allObjectsReturned = false;
}
```

3. **Cleanup**:
Then, we remove returned objects from all tracking collections to prevent further updates to objects that have completed their return
```C#
foreach (GameObject obj in objectsToRemove)
{
    activeObjects.Remove(obj);
    originalPositions.Remove(obj);
    originalMaterials.Remove(obj);
}
```

