
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

/*An editor tool script that will allow the user to toggle a brush to paint connectors on
 surfaces that contain a collider component. This is currently an experimental addition
 for increasing the speed and efficiency of implementing connectors on objects.*/

public static class ConnectorPainterTool
{
    private static bool isPainting = false;
    private static GameObject connector = null;
    private static float brushSize;
    private static float density;
    private static float maxBrushSize = 10f;

    [MenuItem("Tools/Connector Painter Tool")]
    public static void ToggleConnectorPainterTool()
    {
        isPainting = !isPainting;

        if (isPainting)
        {
            Debug.Log("Active");

            //Register the connector painter tool
            SceneView.duringSceneGui += OnSceneGUI;
        }
        else
        {
            Debug.Log("Inactive");

            // Unregister the connector painter tool
            SceneView.duringSceneGui -= OnSceneGUI;
        }
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        /*Implement painting logic here
         Get the current mouse event*/ 
        Event currentEvent = Event.current;
        int controlId = GUIUtility.GetControlID(FocusType.Passive);
        EventType eventType = currentEvent.GetTypeForControl(controlId);

        // Get the mouse position
        Vector3 mousePosition = Event.current.mousePosition;
        mousePosition.y = sceneView.camera.pixelHeight - mousePosition.y; // Invert Y coordinate
        mousePosition.z = 10f; // Set Z coordinate to a reasonable distance from the camera

        // Convert the mouse position to a world space point
        Vector3 start = SceneView.currentDrawingSceneView.camera.ScreenToWorldPoint(mousePosition);

        //Get the camera position
        Vector3 camPosition = SceneView.currentDrawingSceneView.camera.transform.position;
        
        //Calculate disc radius and draw it for reference
        Ray ray = sceneView.camera.ScreenPointToRay(mousePosition);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            float distance = Vector3.Distance(hit.point, camPosition) / maxBrushSize;
            float radius = brushSize / distance;
            Handles.DrawWireDisc(start, Vector3.up, radius);
        }
        
        /*Handle the mouse event*/
        switch (eventType)
        {
            case EventType.MouseDown:
                // Handle mouse down event
                if (currentEvent.button == 0) // Left mouse button
                {
                    // Perform connector painting logic
                    PaintConnector(currentEvent.mousePosition, sceneView.camera);
                    currentEvent.Use(); // Mark the event as used
                }
                break;
            case EventType.MouseDrag:
                // Handle mouse drag event
                if (currentEvent.button == 0) // Left mouse button
                {
                    // Perform connector painting logic while dragging
                    PaintConnector(currentEvent.mousePosition, sceneView.camera);
                    currentEvent.Use(); // Mark the event as used
                }
                break;
        }
        
        /*Make sure to update the scene view so that the wireframe disc updates correctly*/
        SceneView.RepaintAll();
    }

    private static void PaintConnector(Vector2 mousePosition, Camera sceneCamera)
    {
        // Get the mouse position in screen coordinates
        Vector3 screenMousePosition = new Vector3(mousePosition.x, sceneCamera.pixelHeight - mousePosition.y, 0f);

        // Calculate the ray origin and direction
        Ray ray = sceneCamera.ScreenPointToRay(screenMousePosition);

        // Perform a raycast to detect objects in the scene
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            // Check if there is a container for the connectors, if not, create one
            Transform containerTransform = hit.transform.Find("Connectors");
            GameObject container = containerTransform ? containerTransform.gameObject : new GameObject("Connectors");

            // Set the container as a child of the hit object
            container.transform.SetParent(hit.transform);

            // Calculate the number of connectors to spawn based on density
            int numConnectors = Mathf.RoundToInt(Mathf.PI * brushSize * density);

            // Spawn connectors
            for (int i = 0; i < density; i++)
            {
                // Generate random position inside the disc space
                Vector3 randomPosition = Vector3.zero;
                if (density > 1)
                {
                    randomPosition = RandomPointInCircle(hit.point, brushSize);
                }
                else
                {
                    randomPosition = hit.point;
                }
                
                /*Calculate the direction from the camera to the random position so we can
                 paint connectors on any surface*/
                Vector3 dir = (sceneCamera.transform.position - randomPosition).normalized;
                
                RaycastHit objHit;
                if (Physics.Raycast(randomPosition + dir * 1000f, -dir, out objHit))
                {
                    // Instantiate a new connector at the calculated position
                    GameObject newConnector = PrefabUtility.InstantiatePrefab(connector) as GameObject;
                    newConnector.transform.position = objHit.point;
                    newConnector.transform.rotation = connector.transform.rotation;
                    newConnector.transform.SetParent(container.transform);
                    Undo.RegisterCreatedObjectUndo(newConnector, "Paint Connector");
                }
            }
        }
    }
    
    /*Ideally there will be brush preset shapes to choose from but this function will do for showcase purposes*/
    private static Vector3 RandomPointInCircle(Vector3 center, float radius)
    {
        //Generate random points in the brush space
        float angle = Random.Range(0f, 360f);
        float height = Random.Range(-radius, radius); //Provides a value so connectors will be given a point inside the brush space when converting to cartesian (radius)

        // Convert to Cartesian coordinates
        float x = Mathf.Cos(angle) * Mathf.Sqrt(radius * radius - height * height);
        float z = Mathf.Sin(angle) * Mathf.Sqrt(radius * radius - height * height);        

        // Offset by the center and return random position
        return center + new Vector3(x, 0, z);
    }
}