
using System;
using ColourCompatibility;
using GenerationRules;
using UnityEditor;
using UnityEngine;

/*The Editor window for generating levels.
 
 The user can control the entire generation process from here by determining the level building rules*/
public class GenerationEditorWindow : EditorWindow, IGenerationComponent
{
    public static GenerationEditorWindow Instance { get; private set; }
    private Vector2 scrollPos = Vector2.zero;
    
    //Variables
    [SerializeField] private LevelRegeneration levelRegeneration;
    [SerializeField] private string levelName = "LevelContainer";
    [SerializeField] private int connectionFailures = 200000;
    [SerializeField] private float pieceDistance = 0f; //Determines how far apart each mesh is when instanced
    [SerializeField] private Database prefabDatabase;
    [SerializeField] private int numOfBranches = 3; //User defined for branch model
    
    [SerializeField] private int gridSize = 500;    //Changes how strict overlaps are
    [SerializeField] private float voxelSize = 1f;

    [SerializeField] private AlgorithmType chosenAlgorithm; //Model that the generation process should follow
    [SerializeField] private AlgorithmParameters algorithmParameters;

    [SerializeField] private int seedValue; //A way to replicate the same levels without saving JSON
    [SerializeField] private bool useSeedValue;
    [SerializeField] private PrefabData starterPiece; //Assign if you want to use this piece first
    [SerializeField] private ColourArray colourArray;   //Change this based on the database being used. Determines which colours connect

    [SerializeField] private TextAsset levelJson;   //For regenerating a level using a JSON file
    [SerializeField] private GameObject connectorPrefab;    //For painting connectors
    [SerializeField] private float brushSize;
    [SerializeField] private int density;

    private float spaceInPixels = 15f;
    
    //Algorithm types
    private Arena arena;
    private Branch branch;
    private Corridor corridor;
    private Star star;
    private DefaultAlgorithm defaultAlgorithm;

    private bool doOnce = true;
    
    [MenuItem("Window/Generation Editor")]
    public static void ShowWindow()
    {
        GenerationEditorWindow window = GetWindow<GenerationEditorWindow>("Generation Editor");
        window.minSize = new Vector2(400, 200);
    }
    
    private void OnEnable()
    {
        Instance = this;
        
        EditorJsonUtility.FromJsonOverwrite(EditorPrefs.GetString(typeof(GenerationEditorWindow).ToString()), this);
        
        
        levelRegeneration = new LevelRegeneration();
        arena = new Arena();
        branch = new Branch();
        corridor = new Corridor();
        star = new Star();
        defaultAlgorithm = new DefaultAlgorithm();
    }

    private void OnDisable()
    {
        /*Save the changes to the editor window*/
        EditorPrefs.SetString(typeof(GenerationEditorWindow).ToString(), EditorJsonUtility.ToJson(this));
        Instance = null;
    }

    private void DrawGeneralProperties()
    {
        //GENERAL PROPERTIES
        GUILayout.Space(spaceInPixels);
        GUILayout.Label("General Properties", EditorStyles.boldLabel);
        levelName = EditorGUILayout.TextField("Level Name", levelName);
        connectionFailures = EditorGUILayout.IntField("Connection Failures", connectionFailures);
        
        Rect pieceDistanceRect = EditorGUILayout.GetControlRect();
        EditorGUI.LabelField(new Rect(pieceDistanceRect.x, pieceDistanceRect.y, 100, EditorGUIUtility.singleLineHeight), "Piece Distance");
        pieceDistance = EditorGUI.FloatField(new Rect(pieceDistanceRect.x + 150, pieceDistanceRect.y, pieceDistanceRect.width - 150, EditorGUIUtility.singleLineHeight), pieceDistance);

        numOfBranches = EditorGUILayout.IntField("Num of Branches", numOfBranches);
    }

    private void DrawVoxelProperties()
    {
        //VOXEL PROPERTIES
        GUILayout.Space(spaceInPixels);
        GUILayout.Label("Voxel Grid Properties", EditorStyles.boldLabel);
        gridSize = EditorGUILayout.IntField("Grid Size", gridSize);
        
        Rect voxelSizeRect = EditorGUILayout.GetControlRect();
        EditorGUI.LabelField(new Rect(voxelSizeRect.x, voxelSizeRect.y, 100, EditorGUIUtility.singleLineHeight), "Voxel Size");
        voxelSize = EditorGUI.FloatField(new Rect(voxelSizeRect.x + 150, voxelSizeRect.y, voxelSizeRect.width - 150, EditorGUIUtility.singleLineHeight), voxelSize);
    }

    private void DrawAlgorithmProperties()
    {
        //ALGORITHM PROPERTIES
        GUILayout.Space(spaceInPixels);
        GUILayout.Label("Algorithm Properties", EditorStyles.boldLabel);
        chosenAlgorithm = (AlgorithmType)EditorGUILayout.EnumPopup("Algorithm Type", chosenAlgorithm);
        algorithmParameters = (AlgorithmParameters)EditorGUILayout.EnumFlagsField("Algorithm Parameters", algorithmParameters);

        EditorGUILayout.BeginHorizontal();
        seedValue = EditorGUILayout.IntField("Seed Value", seedValue);
        useSeedValue = EditorGUILayout.Toggle("Use Seed Value", useSeedValue);
        EditorGUILayout.EndHorizontal();
        
        starterPiece = (PrefabData)EditorGUILayout.ObjectField("Starter Piece (Optional)", starterPiece, typeof(PrefabData), true);
        colourArray =
            EditorGUILayout.ObjectField("Colour Array", colourArray, typeof(ColourArray), false) as ColourArray;
        
        if (colourArray != null)
        {
            if (doOnce)
            {
                colourArray.GetCells();
                doOnce = false;
            }
            EditorGUI.BeginChangeCheck();
            DrawColourArrayEditor(colourArray);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(colourArray);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Please assign a ColourArray asset.", MessageType.Info);
        }
    }

    private void DrawDatabaseProperties()
    {
        //DATABASE PROPERTIES
        GUILayout.Space(spaceInPixels);
        GUILayout.Label("Database Properties", EditorStyles.boldLabel);
        prefabDatabase = EditorGUILayout.ObjectField("Level Database", prefabDatabase, typeof(Database), false) as Database;
    }

    private void DrawRegenerateLevelProperties()
    {
        //REGENERATE LEVEL PROPERTIES
        GUILayout.Space(spaceInPixels);
        GUILayout.Label("Regenerate Level Properties", EditorStyles.boldLabel);
        levelJson = EditorGUILayout.ObjectField("Level File", levelJson, typeof(TextAsset), false) as TextAsset;
    }

    private void DrawPaintingProperties()
    {
        //PAINTING PROPERTIES
        GUILayout.Space(spaceInPixels);
        GUILayout.Label("Painting Properties", EditorStyles.boldLabel);
        connectorPrefab = EditorGUILayout.ObjectField("Connector Prefab", connectorPrefab, typeof(GameObject), false) as GameObject;
        
        Rect brushSizeRect = EditorGUILayout.GetControlRect();
        EditorGUI.LabelField(new Rect(brushSizeRect.x, brushSizeRect.y, 100, EditorGUIUtility.singleLineHeight), "Brush Size");
        brushSize = EditorGUI.FloatField(new Rect(brushSizeRect.x + 150, brushSizeRect.y, brushSizeRect.width - 150, EditorGUIUtility.singleLineHeight), brushSize);
        
        density = EditorGUILayout.IntField("Density Value", density);
    }
    
    private void OnGUI()
    {
        /*Start scroll view*/
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        /*Add other fields if you wish.*/
        DrawGeneralProperties();
        DrawVoxelProperties();
        DrawAlgorithmProperties();
        DrawDatabaseProperties();
        DrawRegenerateLevelProperties();        
        DrawPaintingProperties();
        
        EditorGUILayout.EndScrollView();
        
        // Button functionality
        GUILayout.Space(20);
        if (GUILayout.Button("Generate Level"))
        {
            GenerateLevel();
        }

        if (GUILayout.Button("Save Level"))
        {
            SaveLevel();
        }
        
        if (GUILayout.Button("Regenerate Level"))
        {
            RegenerateLevel();
        }
        
        // Button to toggle the connector painter tool
        GUILayout.Space(spaceInPixels);
        if (GUILayout.Button("Toggle Connector Painter Tool"))
        {
            ConnectorPainterTool.SetDensity(density);
            ConnectorPainterTool.SetBrushSize(brushSize);
            ConnectorPainterTool.SetConnectorPrefab(connectorPrefab);
            ConnectorPainterTool.ToggleConnectorPainterTool();
        }
    }
    
    private void DrawColourArrayEditor(ColourArray array)
    {
        ColourArrayEditor editor = Editor.CreateEditor(array) as ColourArrayEditor;
        if (editor != null)
        {
            editor.OnInspectorGUI();
        }
    }
    
    //Initiate the chosen algorithm
    private void InitialiseChosenAlgorithm(AlgorithmType chosenAlgorithm)
    {
        switch (chosenAlgorithm)
        {
            case AlgorithmType.Arena:
                arena.SetGenerationManagerRef(this);
                arena.Initialise();
                break;
            case AlgorithmType.Branch:
                branch.SetGenerationManagerRef(this);
                branch.Initialise();
                break;
            case AlgorithmType.Corridor:
                corridor.SetGenerationManagerRef(this);
                corridor.Initialise();
                break;
            case AlgorithmType.Star:
                star.SetGenerationManagerRef(this);
                star.Initialise();
                break;
            case AlgorithmType.Default:
                defaultAlgorithm.SetGenerationManagerRef(this);
                defaultAlgorithm.Initialise();
                break;
            default:
                break;
        }
    }
    
    private void GenerateLevel()
    {
        /*Here is the beef of the proc gen :P */
        
        //Error handling function
        bool validated = ValidateParameters();

        if (!validated) return;
        AlgorithmType chosenAlgorithm = GetChosenAlgorithm();
        InitialiseChosenAlgorithm(chosenAlgorithm);
    }
    
    private void SaveLevel()
    {
        /*Save the level layout to a json file*/
        switch (chosenAlgorithm)
        {
            case AlgorithmType.Arena:
                arena.SaveLevel();
                break;
            case AlgorithmType.Corridor:
                //Function
                corridor.SaveLevel();
                break;
            case AlgorithmType.Branch:
                //Function
                branch.SaveLevel();
                break;
            case AlgorithmType.Star:
                //Function
                star.SaveLevel();
                break;
            case AlgorithmType.Default:
                //Function
                defaultAlgorithm.SaveLevel();
                break;
        }
    }
    
    private void RegenerateLevel()
    {
        /*Pass the json file to the level regeneration class if there is a json file*/
        if (levelJson == null)
        {
            Debug.LogError("The Level File may not have been assigned in the Generation Editor.");
            return;
        }

        levelRegeneration.RegenerateLevel(levelJson);
    }

    private bool ValidateParameters()
    {
        bool validated = true;
        /*GENERAL PROPERTIES*/
        if (levelName == "")
        {
            Debug.LogError("Level Name is empty. Please fill the field with an appropriate name.");
            validated = false;
        }
        if (connectionFailures <= 0)
        {
            Debug.LogError("Connection Failures cannot be 0 or less than 0.");
            validated = false;
        }
        if (pieceDistance < 0)
        {
            Debug.LogError("Piece Distance cannot be below 0.");
            validated = false;
        }
        if (numOfBranches < 0)
        {
            Debug.LogError("Number of Branches cannot be below 0.");
            validated = false;
        }
        
        /*VOXEL GRID PROPERTIES*/
        if (gridSize > 1500f || gridSize <= 100)
        {
            Debug.LogError("Voxel Grid size must be between the values of 100 and 1500.");
            validated = false;
        }
        if (voxelSize <= 0)
        {
            Debug.LogError("Voxel Size must be a positive value.");
            validated = false;
        }
        
        /*ALGORITHM PROPERTIES*/
        if ((algorithmParameters & AlgorithmParameters.Colour) == 0 && (algorithmParameters & AlgorithmParameters.NumPins) == 0 && (algorithmParameters & AlgorithmParameters.PinShape) == 0)
        {
            Debug.LogError("The Algorithm Parameter must be of selection: 'Colour,' 'NumPins,' or 'PinShape'.");
            validated = false;
        }
        if (colourArray == null)
        {
            Debug.LogError("Colour Array may not be assigned in Generation Editor.");
            validated = false;
        }
        
        
        /*DATABASE PROPERTIES*/
        if (prefabDatabase == null)
        {
            Debug.LogError("The Level Database may not have been assigned in the Generation Editor.");
            validated = false;
        }
        return validated;
    }
}
