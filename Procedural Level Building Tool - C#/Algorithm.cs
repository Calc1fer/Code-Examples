
using System.Collections.Generic;
using System.IO;
using ColourCompatibility;
using ConnectorInfo;
using GenerationRules;
using UnityEditor;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

/*Base class for each level generation model. This is the base functionality that the derived scripts can extend*/

#if UNITY_EDITOR
[ExecuteInEditMode]
public class Algorithm : MonoBehaviour
{
    protected IGenerationComponent generationManager;
    protected List<PrefabData> generationList;    //temporary list taken from the database used for generating the level
    protected List<PrefabData> prefabData;    //This is for the save system where we can retrieve the prefab data for when we want to regenerate the level
    protected Database prefabDatabase;        //Reference from manager to fill generation list
    protected GameObject starterPiece;        //Random choice from the list
    protected GameObject tentativePiece;      //Potential connection piece from the list
    protected int totalFailedConns;           //Total before the loop exits
    protected List<GameObject> pieceList;   //Successfully placed pieces
    protected AlgorithmParameters algorithmParameters; //For determining how to connect meshes
    protected Transform tempTrans; //Make temp trans for parenting tentativeconn
    protected int tempIdx = 0; 
    private GameObject levelContainer; // This will contain the entire level
    
    //Voxel Grid parameters
    protected VoxelGrid voxelGrid;
    
    //Reference to the colour array from the generation manager
    private ColourArray colourArray;
    
    public virtual void Initialise()
    {
        //Setup necessary parameters before generation begins
        generationList = new List<PrefabData>();
        pieceList = new List<GameObject>();
        prefabData = new List<PrefabData>();
        tempTrans = new GameObject("Temptrans").transform;
        
        totalFailedConns = generationManager.GetConnectionFailures();
        prefabDatabase = generationManager.GetDatabaseRef();
        
        voxelGrid = new VoxelGrid();
        voxelGrid.InitialiseVoxelGrid(generationManager.GetGridSize(), generationManager.GetVoxelSize());

        //Register for the undo operations
        levelContainer = new GameObject();
        Undo.RegisterCreatedObjectUndo(levelContainer, "Create LevelContainer");
        levelContainer.name = generationManager.GetLevelName();
        Undo.RegisterFullObjectHierarchyUndo(levelContainer, "Level Generation");

        //Get the connection parameters chosen by the user
        algorithmParameters = generationManager.GetAlgorithmParameters();
        colourArray = generationManager.GetColourArray();
        
        PopulateGenerationList();
    }

    //Function that will access the database and fill the generation list
    protected void PopulateGenerationList()
    {
        //Use a random seed value if selected by user
        if(generationManager.GetUseSeedValue()) Random.InitState(generationManager.GetSeedValue());
        
        /*Populate the generation list using the frequency of each prefab in the database*/
        if (prefabDatabase != null)
        {
            int totalObjectsToGenerate = prefabDatabase.databaseSize;
            //Until we have the total in list desired...
            while(generationList.Count < totalObjectsToGenerate)
            {
                 int randIdx = Random.Range(0, prefabDatabase.Prefabs.Length);
                 float rand = Random.value;
                 
                 PrefabData data = prefabDatabase.Prefabs[randIdx];
                 if (rand < data.frequency / 100)
                 {
                     generationList.Add(data);
                 }
            }
            
            /*Shuffle the entries in the generationList*/
            ShuffleList(generationList);
        }
        else
        {
            Debug.Log("Prefab Database is null");
        }
    }
    
    //Function for shuffling a list
    private List<T> ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }

        return list;
    }

    //Function to contain all level pieces in one parent object
    //For neatness sake
    protected void ContainLevelObjects()
    {
        foreach (GameObject obj in pieceList)
        {
            obj.transform.parent = levelContainer.transform;
        }
        
        tempTrans.SetParent(levelContainer.transform);
    }
    
    protected void PlaceStarter()
    {
        bool canStart = false;
        int totalTries = 0; 
        
        //Set the starter piece from list if no specified starter object by the user
        while (!canStart && generationManager.GetStarterPiece() == null)
        {
            tempIdx = GetRandomIdx();
            if (generationList[tempIdx].canStart)
            {
                starterPiece = generationList[tempIdx].prefab;
                canStart = true;
            }

            totalTries++;
            if (totalTries >= generationList.Count) return;
        }
        
        //Set the starter piece here if specified by the user
        if (generationManager.GetStarterPiece() != null)
        {
            starterPiece = generationManager.GetStarterPiece().prefab;
            prefabData.Add(generationManager.GetStarterPiece());
        }
        
        /*Place the starter piece*/
        starterPiece.transform.position = new Vector3(0f, 0f, 0f);
        starterPiece.transform.rotation = Quaternion.identity;
        starterPiece = Instantiate(starterPiece, starterPiece.transform.position, starterPiece.transform.rotation);
        starterPiece.GetComponentInChildren<Collider>().enabled = true;
        starterPiece.name = "Starter";
        
        //Add the starter to the pieceList
        pieceList.Add(starterPiece);

        if (generationManager.GetStarterPiece() == null)
        {
            prefabData.Add(generationList[tempIdx]);
            generationList.RemoveAt(tempIdx); //Remove only after adding to pieceList to avoid null refs
        }
        
        //Mark the space that the object is occupying in the voxel grid
        voxelGrid.MarkOccupied(starterPiece.transform.position, starterPiece.GetComponentInChildren<Collider>().bounds.size);
    }
    
    protected virtual void GenerateLevel()
    {
        if (starterPiece == null) return;
       // While the generation list is not empty
        while (generationList.Count != 0)
        {
            bool validConnection = false;
            
            // Choose the piece for a potential connection to starterPiece
            tempIdx = GetRandomIdx();

            // Check for valid pairings and return for each connector on the starter piece
            for (int i = 0; i < starterPiece.GetComponentsInChildren<Connector>().Length; i++)
            {
                // Break if total failures have been met
                if (totalFailedConns <= 0) break;

                // Inner loop to iterate through the generationList
                for (int j = 0; j < generationList.Count; j++)
                {
                    if (totalFailedConns <= 0) break;
                    
                    // Instantiate the tentative piece
                    tempIdx = GetRandomIdx();
                    tentativePiece = generationList[tempIdx].prefab;
                    tentativePiece = Instantiate(tentativePiece, Vector3.zero, Quaternion.identity);
                    tentativePiece.GetComponentInChildren<Collider>().enabled = true;
                    
                    // Check for valid connection based on selected parameters
                    validConnection = ConnectMeshes();

                    // If the connection is valid for this tentative, break out of the inner loop
                    if (validConnection)
                    {
                        // Add the new object to the list
                        pieceList.Add(tentativePiece);
                        prefabData.Add(generationList[tempIdx]);
                        break;
                    }
                    else
                    {
                        // If no connection was made, decrement the total failed connections (pieces total) and destroy the object
                        totalFailedConns--;
                        DestroyImmediate(tentativePiece);
                        generationList.RemoveAt(j);
                    }
                }

                // Break out of the outer loop if a valid connection is found
                if (validConnection) break;
            }

            // If the total fails have reached zero, exit the loop (reached maximum failed conns)
            if (totalFailedConns <= 0) break;

            // If a connection is made, update the starter piece and remove the previous one
            if (validConnection)
            {
                starterPiece = tentativePiece;

                // Set the new tentative piece
                tempIdx = GetRandomIdx();
                tentativePiece = generationList[tempIdx].prefab;
                generationList.RemoveAt(tempIdx);
            }

            // If no valid connection was made, decrement the total fails
            if (!validConnection)
            {
                if (tentativePiece == null)
                {
                    starterPiece = pieceList[^1];
                }
                totalFailedConns--;
            }
        }
    }

    //Clean up and release any memory not needed
    protected void Cleanup()
    {
        starterPiece = null;
        tentativePiece = null;
        voxelGrid = null;

        Resources.UnloadUnusedAssets();
    }

    /*Function returns whether the meshes have been met with a valid connection*/
    protected virtual bool ConnectMeshes()
    {
        bool isConnected = false;
        bool colourParam = false;
        bool pinShapeParam = false;
        bool numPinParam = false;
        
        //Get the connectors which will be compared to see if they overlap
        Connector[] starterConns = starterPiece.GetComponentsInChildren<Connector>();
        Connector[] tentativeConns = tentativePiece.GetComponentsInChildren<Connector>();
        
        //Check if the any of the connectors in tentative match the starter
        foreach (Connector conn in starterConns)
        {
            if (isConnected) break;
            for (int i = 0; i < tentativeConns.Length; i++)
            {
                /*Compare each of the connectors in the tentative conn list to see if there is a match*/
                if ((algorithmParameters & AlgorithmParameters.Colour) != 0) colourParam = CompareConnectorColours(conn, tentativeConns[i]);
                if ((algorithmParameters & AlgorithmParameters.PinShape) != 0) pinShapeParam = CompareConnectorPinShape(conn, tentativeConns[i]);
                if ((algorithmParameters & AlgorithmParameters.NumPins) != 0) numPinParam = CompareConnectorNumPins(conn, tentativeConns[i]);

                //Only proceed if a valid parameter combination has been selected by the user
                switch (algorithmParameters)
                {
                    case AlgorithmParameters.Colour | AlgorithmParameters.NumPins | AlgorithmParameters.PinShape:
                        if (colourParam && numPinParam && pinShapeParam)
                            isConnected = ConnectMeshesHelper(conn, tentativeConns[i]);
                        break;
                    case AlgorithmParameters.Colour | AlgorithmParameters.NumPins:
                        if (colourParam && numPinParam) isConnected = ConnectMeshesHelper(conn, tentativeConns[i]);
                        break;
                    case AlgorithmParameters.PinShape | AlgorithmParameters.NumPins:
                        if (pinShapeParam && numPinParam) isConnected = ConnectMeshesHelper(conn, tentativeConns[i]);
                        break;
                    case AlgorithmParameters.PinShape | AlgorithmParameters.Colour:
                        if (pinShapeParam && colourParam) isConnected = ConnectMeshesHelper(conn, tentativeConns[i]);
                        break;                          
                    case AlgorithmParameters.Colour:
                        if (colourParam) isConnected = ConnectMeshesHelper(conn, tentativeConns[i]);
                        break;                    
                    case AlgorithmParameters.PinShape:
                        if (pinShapeParam) isConnected = ConnectMeshesHelper(conn, tentativeConns[i]);
                        break;                    
                    case AlgorithmParameters.NumPins:
                        if (numPinParam) isConnected = ConnectMeshesHelper(conn, tentativeConns[i]);
                        break;
                    default:
                        break;
                }
                
                if (isConnected) break;
            }
        }
        
        return isConnected;
    }
    
    //Comparison functions for checking validity of connectors
    protected bool CompareConnectorColours(Connector starterConn, Connector tentativeConn)
    {
        bool valid = (IsValidColourRelationship(GetColourIndex(starterConn.GetConnectorColour()), GetColourIndex(tentativeConn.GetConnectorColour())));

        return valid;
    }    
    
    protected bool CompareConnectorPinShape(Connector starterConn, Connector tentativeConn)
    {
        bool valid = starterConn.GetPinShape() == tentativeConn.GetPinShape();

        return valid;
    }    
    
    protected bool CompareConnectorNumPins(Connector starterConn, Connector tentativeConn)
    {
        bool valid = starterConn.GetNumPins() == tentativeConn.GetNumPins();

        return valid;
    }
    
    /*Parameters functions where we connect based on the algorithm parameters*/
    protected bool ConnectMeshesHelper(Connector starterConn, Connector tentativeConn)
    {
        // Snap the meshes together
        bool overlap = SnapObjects(starterPiece, tentativePiece, starterConn, tentativeConn);
                    
        if (!overlap)
        {
            voxelGrid.MarkOccupied(tentativePiece.transform.position, tentativePiece.GetComponentInChildren<Collider>().bounds.size);
        }
                    
        if (overlap)
        {
            return false;
        }
        return true;
    }
    
    /*This function will connect the objects together if the connectors are not already connected to a previous tentativeConn*/
    protected bool SnapObjects(GameObject starter, GameObject tentative, Connector starterConn, Connector tentativeConn)
    {
        /*Removed some code here*/
        
        //Check for the overlaps
        overlap = voxelGrid.CheckOverlap(tentativePiece.transform.position);
        return overlap;
    }
    
    /*Normalise helper function*/
    private float NormaliseAngle(float val)
    {
        val %= 360f;
        if (val < 0f)
        {
            val += 360f;
        }

        return val;
    }

    protected bool IsValidColourRelationship(int colour1, int colour2)
    {
        // Access the ColourArray and check the boolean matrix
        if (colourArray != null && colourArray.GetCells() != null &&
            colour1 >= 0 && colour1 < colourArray.GetCells().GetLength(0) &&
            colour2 >= 0 && colour2 < colourArray.GetCells().GetLength(1))
        {
            return colourArray.GetCells()[colour1, colour2];
        }
        
        //Invalid so return false
        return false;
    }
    
    //Function for saving the generated level
    public void SaveLevel()
    {
        LevelData levelData = new LevelData();

        if (pieceList == null)
        {
            Debug.LogError("There is no data to save. Try generating a new level.");
            return;
        }
        //Add each object currently generated in the scene to the levelData
        for(int i = 0; i < pieceList.Count; i++)
        {
            GameObject piece = pieceList[i];
            ObjectData obj = new ObjectData();
            
            obj.prefab = prefabData[i];
            obj.rotation = piece.transform.rotation;
            obj.position = piece.transform.position;
            
            levelData.objects.Add(obj);
        }
        
        // Generate a unique filename based on the current timestamp
        string fileName = $"{generationManager.GetLevelName()}.json";

        // Combine the path to the Assets folder with the desired subfolder and generated file name
        string filePath = Path.Combine(Application.dataPath, "SavedLevels_JSON", fileName);

        string json = JsonUtility.ToJson(levelData);
        // Write the JSON content to the file
        File.WriteAllText(filePath, json);
        
        AssetDatabase.Refresh();

        Debug.Log($"JSON saved to: {filePath}");
    }
    
    /*Getters*/
    //Function returns a random index for choosing the new starter/guide piece
    public int GetRandomIdx()
    {
        if (generationList.Count == 0)
        {
            return 0;
        }
        
        int randomIdx = Random.Range(0, generationList.Count);
        
        return randomIdx;
    }

    /*Function to convert the connector colour enums to integer values for relationship comparisons
     in the colour array*/
    protected int GetColourIndex(ConnectorColour colour)
    {
        switch (colour)
        {
            case ConnectorColour.White:
                return 0;
            case ConnectorColour.Red:
                return 1;
            case ConnectorColour.Green:
                return 2;
            case ConnectorColour.Blue:
                return 3;
            case ConnectorColour.Cyan:
                return 4;
            case ConnectorColour.Orange:
                return 5;
            case ConnectorColour.Yellow:
                return 6;
            case ConnectorColour.Pink:
                return 7;
            case ConnectorColour.Purple:
                return 8;
            case ConnectorColour.Brown:
                return 9;
            case ConnectorColour.Black:
                return 10;
            case ConnectorColour.Gray:
                return 11;
            default:
                return -1; //handle default case (Just say naaaaaaah)
        }
    }

    protected IGenerationComponent GetGenerationManager()
    {
        return generationManager;
    }
    
    /*Setters*/
    public void SetGenerationManagerRef(IGenerationComponent val)
    {
        generationManager = val;
    }
}
#endif