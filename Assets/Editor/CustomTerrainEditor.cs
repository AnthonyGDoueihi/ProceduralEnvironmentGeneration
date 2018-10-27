using UnityEngine;
using UnityEditor;
using EditorGUITable;

[CustomEditor(typeof(CustomTerrain))]
[CanEditMultipleObjects]

public class CustomTerrainEditor : Editor {

    SerializedProperty randomHeightRange;
    SerializedProperty heightMapScale;
    SerializedProperty heightMapImage;

    SerializedProperty perlinXScale;
    SerializedProperty perlinYScale;
    SerializedProperty perlinXOffset;
    SerializedProperty perlinYOffset;
    SerializedProperty perlinOctaves;
    SerializedProperty perlinPersistance;
    SerializedProperty perlinHeightScale;

    SerializedProperty resetTerrain;

    SerializedProperty peakCount;
    SerializedProperty fallOff;
    SerializedProperty dropOff;
    SerializedProperty vMinHeight;
    SerializedProperty vMaxHeight;
    SerializedProperty voroniType;

    SerializedProperty mpMinHeight;
    SerializedProperty mpMaxHeight;
    SerializedProperty mpDampPower;
    SerializedProperty mpRoughness;

    SerializedProperty smoothTimes;

    GUITableState perlinParameterTable;
    SerializedProperty perlinParameters;

    GUITableState splatHeightsTable;
    SerializedProperty splatHeights;
    SerializedProperty splatNoiseX;
    SerializedProperty splatNoiseY;
    SerializedProperty splatNoiseScale;
    SerializedProperty splatOffet;

    SerializedProperty vegTreeMax;
    SerializedProperty vegTreeSpacing;
    SerializedProperty vegRandom;
    GUITableState vegetation;

    SerializedProperty detailMax;
    SerializedProperty detailSpacing;
    GUITableState details;

    SerializedProperty waterHeight;
    SerializedProperty waterGO;
    SerializedProperty shoreLineMat;

    SerializedProperty erosionType;
    SerializedProperty erosionStrength;
    SerializedProperty erosionAmount;
    SerializedProperty erosionDroplets;
    SerializedProperty erosionSolubility;
    SerializedProperty erosionSpringsPerRiver;
    SerializedProperty erosionSmoothAmount;
    SerializedProperty erosionWindDir;


    bool showRandom = false;
    bool showLoadHeights = false;
    bool showPerlin = false;
    bool showMultiplePerlin = false;
    bool showVoronoi = false;
    bool showMPD = false;
    bool showSmooth = false;
    bool showSplatMaps = false;
    bool showHeightMap = false;
    bool showVegetation = false;
    bool showDetail = false;
    bool showWater = false;
    bool showErosion = false;


    Texture2D hmTexture;

    private void OnEnable()
    {
        randomHeightRange = serializedObject.FindProperty("randomHeightRange");
        heightMapScale = serializedObject.FindProperty("heightMapScale");
        heightMapImage = serializedObject.FindProperty("heightMapImage");
        perlinXScale = serializedObject.FindProperty("perlinXScale");
        perlinYScale = serializedObject.FindProperty("perlinYScale");
        perlinXOffset = serializedObject.FindProperty("perlinXOffset");
        perlinYOffset = serializedObject.FindProperty("perlinYOffset");
        perlinOctaves = serializedObject.FindProperty("perlinOctaves");
        perlinPersistance = serializedObject.FindProperty("perlinPersistance");
        perlinHeightScale = serializedObject.FindProperty("perlinHeightScale");
        resetTerrain = serializedObject.FindProperty("resetTerrain");

        perlinParameterTable = new GUITableState("perlinParameterTable");
        perlinParameters = serializedObject.FindProperty("perlinParameters");

        peakCount = serializedObject.FindProperty("voronoiPeakCount");
        fallOff = serializedObject.FindProperty("voronoiFallOff");
        dropOff = serializedObject.FindProperty("voronoiDropOff");
        vMinHeight = serializedObject.FindProperty("voronoiMinHeight");
        vMaxHeight = serializedObject.FindProperty("voronoiMaxHeight");
        voroniType = serializedObject.FindProperty("voronoiType");

        mpMinHeight = serializedObject.FindProperty("mpMinHeight");
        mpMaxHeight = serializedObject.FindProperty("mpMaxHeight");
        mpDampPower = serializedObject.FindProperty("mpDampPower");
        mpRoughness = serializedObject.FindProperty("mpRoughness");

        smoothTimes = serializedObject.FindProperty("smoothTimes");

        splatHeightsTable = new GUITableState("splatHeightsTable");
        splatHeights = serializedObject.FindProperty("splatHeights");
        splatNoiseX = serializedObject.FindProperty("splatNoiseX");
        splatNoiseY = serializedObject.FindProperty("splatNoiseY");
        splatNoiseScale = serializedObject.FindProperty("splatNoiseScale");
        splatOffet = serializedObject.FindProperty("splatOffet");

        vegTreeMax = serializedObject.FindProperty("vegTreeMax");
        vegTreeSpacing = serializedObject.FindProperty("vegTreeSpacing");
        vegetation = new GUITableState("vegetation");
        vegRandom = serializedObject.FindProperty("vegRandom");

        detailMax = serializedObject.FindProperty("detailMax");
        detailSpacing = serializedObject.FindProperty("detailSpacing");
        details = new GUITableState("details");

        waterHeight = serializedObject.FindProperty("waterHeight");
        waterGO = serializedObject.FindProperty("waterGO");
        shoreLineMat = serializedObject.FindProperty("shoreLineMat");

        erosionType = serializedObject.FindProperty("erosionType");
        erosionStrength = serializedObject.FindProperty("erosionStrength");
        erosionAmount = serializedObject.FindProperty("erosionAmount");
        erosionDroplets = serializedObject.FindProperty ("erosionDroplets");
        erosionSolubility = serializedObject.FindProperty ("erosionSolubility");
        erosionSpringsPerRiver = serializedObject.FindProperty ("erosionSpringsPerRiver");
        erosionSmoothAmount = serializedObject.FindProperty ("erosionSmoothAmount");
        erosionWindDir = serializedObject.FindProperty("erosionWindDir");

        hmTexture = new Texture2D(513, 513, TextureFormat.ARGB32, false);
    }

    Vector2 scrollPos;
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        CustomTerrain terrain = (CustomTerrain)target;

        Rect r = EditorGUILayout.BeginVertical();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(r.width), GUILayout.Height(r.height));
        EditorGUI.indentLevel++;

        EditorGUILayout.PropertyField(resetTerrain);

        showRandom = EditorGUILayout.Foldout(showRandom, "Random");
        if (showRandom)
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Set Heights Between Random Values", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(randomHeightRange);
            if(GUILayout.Button("Random Height"))
            {
                terrain.RandomTerrain();
            }

        }

        showLoadHeights = EditorGUILayout.Foldout(showLoadHeights, "Load Heights");
        if (showLoadHeights)
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Load Heights From Texture", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(heightMapImage);
            EditorGUILayout.PropertyField(heightMapScale);
            if (GUILayout.Button("Load Texture"))
            {
                terrain.LoadTexture();
            }

        }

        showPerlin = EditorGUILayout.Foldout(showPerlin, "Single Perlin Noise");
        if (showPerlin)
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Perlin Noise", EditorStyles.boldLabel);
            EditorGUILayout.Slider(perlinXScale, 0, 1, new GUIContent("X Scale"));
            EditorGUILayout.Slider(perlinYScale, 0, 1, new GUIContent("Y Scale"));
            EditorGUILayout.IntSlider(perlinXOffset, 0, 10000, new GUIContent("X Offset"));
            EditorGUILayout.IntSlider(perlinYOffset, 0, 10000, new GUIContent("Y Offset"));
            EditorGUILayout.IntSlider(perlinOctaves, 1, 10, new GUIContent("Octaves"));
            EditorGUILayout.Slider(perlinPersistance, 0.1f, 10, new GUIContent("Persistance"));
            EditorGUILayout.Slider(perlinHeightScale, 0, 1, new GUIContent("Height Scale"));

            if (GUILayout.Button("Random Perlin"))
            {
                terrain.Perlin();
            }

        }

        showMultiplePerlin = EditorGUILayout.Foldout(showMultiplePerlin, "Multiple Perlin Noise");
        if (showMultiplePerlin)
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Multiple Perlin Noise", EditorStyles.boldLabel);
            perlinParameterTable = GUITableLayout.DrawTable(perlinParameterTable, serializedObject.FindProperty("perlinParameters"));

            GUILayout.Space(20);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+"))
            {
                terrain.AddNewPerlin();
            }
            if (GUILayout.Button("-"))
            {
                terrain.RemovePerlin();
            }
            EditorGUILayout.EndHorizontal();
            if(GUILayout.Button("Apply Multiple Perlin"))
            {
                terrain.MultiplePerlin();
            }
        }

        showVoronoi = EditorGUILayout.Foldout(showVoronoi, "Voronoi");
        if (showVoronoi)
        {
            EditorGUILayout.IntSlider(peakCount, 1, 10, new GUIContent("Peak Count"));
            EditorGUILayout.Slider(fallOff, 0, 10, new GUIContent("Fall Off"));
            EditorGUILayout.Slider(dropOff, 0, 10, new GUIContent("Drop Off"));
            EditorGUILayout.Slider(vMinHeight, 0, 1, new GUIContent("Min Height"));
            EditorGUILayout.Slider(vMaxHeight, 0, 1, new GUIContent("Max Height"));
            EditorGUILayout.PropertyField(voroniType);
            if (GUILayout.Button("Voronoi"))
            {
                terrain.Voronoi();
            }
        }

        showMPD = EditorGUILayout.Foldout(showMPD, "Midpoint Displacement");
        if (showMPD)
        {
            EditorGUILayout.PropertyField(mpMinHeight);
            EditorGUILayout.PropertyField(mpMaxHeight);
            EditorGUILayout.PropertyField(mpDampPower);
            EditorGUILayout.PropertyField(mpRoughness);

            if (GUILayout.Button("MPD"))
            {
                terrain.MPD();
            }
        }
               
        showSplatMaps = EditorGUILayout.Foldout(showSplatMaps, "Splat Maps");
        if (showSplatMaps)
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Splat Maps", EditorStyles.boldLabel);
            splatHeightsTable = GUITableLayout.DrawTable(splatHeightsTable, serializedObject.FindProperty("splatHeights"));

            GUILayout.Space(20);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+"))
            {
                terrain.AddNewSplatHeight();
            }
            if (GUILayout.Button("-"))
            {
                terrain.RemoveSplatHeight();
            }
            EditorGUILayout.EndHorizontal();


            if (GUILayout.Button("Apply Splatmaps"))
            {
                terrain.SplatMaps();
            }
        }

        showVegetation = EditorGUILayout.Foldout(showVegetation, "Vegetation");
        if (showVegetation)
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Vegetation", EditorStyles.boldLabel);
            EditorGUILayout.IntSlider(vegTreeMax, 0, 10000, new GUIContent("Max Trees"));
            EditorGUILayout.IntSlider(vegTreeSpacing, 1, 50, new GUIContent("Spacing"));
            EditorGUILayout.Slider(vegRandom, 0, 10, new GUIContent("Random"));

            vegetation = GUITableLayout.DrawTable(vegetation, serializedObject.FindProperty("vegetation"));
            GUILayout.Space(20);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+"))
            {
                terrain.AddNewTree();
            }
            if (GUILayout.Button("-"))
            {
                terrain.RemoveTree();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Apply Vegetation"))
            {
                terrain.PlantVegetation();
            }
        }

        showDetail = EditorGUILayout.Foldout(showDetail, "Details");
        if (showDetail)
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Details", EditorStyles.boldLabel);
            EditorGUILayout.IntSlider(detailMax, 0, 1000, new GUIContent("Details Render Distance"));
            EditorGUILayout.IntSlider(detailSpacing, 1, 50, new GUIContent("Detail Spacing"));
            details = GUITableLayout.DrawTable(details, serializedObject.FindProperty("details"));
            GUILayout.Space(20);

            terrain.GetComponent<Terrain>().detailObjectDistance = detailMax.intValue;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+"))
            {
                terrain.AddNewDetail();
            }
            if (GUILayout.Button("-"))
            {
                terrain.RemoveDetail();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Apply Details"))
            {
                terrain.PlantDetails();
            }
        }

        showWater = EditorGUILayout.Foldout(showWater, "Water");
        if (showWater)
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Label("Water", EditorStyles.boldLabel);
            EditorGUILayout.Slider(waterHeight, 0, 1, new GUIContent("Water Height"));
            EditorGUILayout.PropertyField(waterGO);

            if (GUILayout.Button("Add Water"))
            {
                terrain.Water();
            }
            
            EditorGUILayout.PropertyField(shoreLineMat);
            if (GUILayout.Button("Add Shoreline"))
            {
                terrain.Shoreline();
            }

        }

        showErosion = EditorGUILayout.Foldout(showErosion, "Erosion");
        if (showErosion)
        {
            EditorGUILayout.PropertyField(erosionType);
            EditorGUILayout.Slider(erosionStrength, 0, 1, new GUIContent("Erosion Strength"));
            EditorGUILayout.Slider(erosionAmount, 0, 1, new GUIContent("Erosion Amout"));            
            EditorGUILayout.IntSlider(erosionDroplets, 0, 500, new GUIContent("Droplets"));
            EditorGUILayout.Slider(erosionSolubility, 0.001f, 1, new GUIContent("Solubility"));
            EditorGUILayout.IntSlider(erosionSpringsPerRiver, 0, 20, new GUIContent("Springs Per River"));
            EditorGUILayout.IntSlider(erosionSmoothAmount, 0, 10, new GUIContent("Smooth Amount"));
            EditorGUILayout.Slider(erosionWindDir, 0, 360, new GUIContent("Wind Direction"));
            

            if (GUILayout.Button("Erode"))
            {
                terrain.Erosion();
            }
        }

        showSmooth = EditorGUILayout.Foldout(showSmooth, "Smooth");
        if (showSmooth)

        {
            EditorGUILayout.PropertyField(smoothTimes);

            if (GUILayout.Button("Smooth"))
            {
                terrain.Smooth();
            }
        }

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        if (GUILayout.Button("Undo Last"))
        {
            terrain.UndoLastTerrain();
        }

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        if (GUILayout.Button("Reset Height"))
        {
            terrain.ResetTerrain();
        }


        showHeightMap = EditorGUILayout.Foldout(showHeightMap, "Height Map");
        if (showHeightMap)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            int wSize = (int)(EditorGUIUtility.currentViewWidth - 100);
            GUILayout.Label(hmTexture, GUILayout.Width(wSize), GUILayout.Height(wSize));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", GUILayout.Width(wSize)))
            {
                float[,] heightMap = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapWidth, terrain.terrainData.heightmapHeight);

                for (int x = 0; x < terrain.terrainData.heightmapWidth; x++)
                {
                    for (int y = 0; y < terrain.terrainData.heightmapHeight; y++)
                    {
                        Color colour = new Color(heightMap[x, y], heightMap[x, y], heightMap[x, y], 1);
                        hmTexture.SetPixel(x, y, colour);
                    }
                }
                hmTexture.Apply();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
