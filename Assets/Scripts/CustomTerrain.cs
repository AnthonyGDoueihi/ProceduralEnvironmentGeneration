using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

[ExecuteInEditMode]

public class CustomTerrain : MonoBehaviour {

    public Vector2 randomHeightRange = new Vector2(0, 0.1f);
    public Texture2D heightMapImage = null;
    public Vector3 heightMapScale = new Vector3(1, 1, 1);

    public bool resetTerrain = true;

    public float perlinXScale = 0.01f;
    public float perlinYScale = 0.01f;
    public int perlinXOffset = 0;
    public int perlinYOffset = 0;
    public int perlinOctaves = 3;
    public float perlinPersistance = 8;
    public float perlinHeightScale = 0.09f;
       
    [System.Serializable]
    public class PerlinParameters
    {
        public float mPerlinXScale = 0.01f;
        public float mPerlinYScale = 0.01f;
        public int mPerlinOctaves = 3;
        public float mPerlinPersistance = 8;
        public float mPerlinHeightScale = 0.09f;
        public int mPerlinXOffset = 0;
        public int mPerlinYOffset = 0;
        public bool remove = false;

    }

    public List<PerlinParameters> perlinParameters = new List<PerlinParameters>()
    {
        new PerlinParameters()
    };

    [System.Serializable]
    public class SplatHeights
    {
        public Texture2D texture= null;
        public float minHeight = 0.1f;
        public float maxHeight = 0.2f;
        public float minSlope = 0f;
        public float maxSlope = 90f;
        public Vector2 tileOffset = new Vector2(0, 0);
        public Vector2 tileSize = new Vector2(50, 50);
        public float splatNoiseX = 0.01f;
        public float splatNoiseY = 0.01f;
        public float splatNoiseScale = 0.2f;
        public float splatOffet = 0.01f;
        public bool remove = false;
    }

    public List<SplatHeights> splatHeights = new List<SplatHeights>()
    {
        new SplatHeights()
    };



    public int voronoiPeakCount = 1;
    public float voronoiFallOff = 0.2f;
    public float voronoiDropOff = 0.8f;
    public float voronoiMinHeight = 0.01f;
    public float voronoiMaxHeight = 0.2f;
    public enum VoronoiType {  Linear = 0, Power = 1, Combined = 2, SinPow = 3 }
    public VoronoiType voronoiType = VoronoiType.Linear;

    public float mpMinHeight = -2.0f;
    public float mpMaxHeight = 2.0f;
    public float mpDampPower = 2.0f;
    public float mpRoughness = 2.0f;      

    public int smoothTimes = 1;

    public int vegTreeMax = 5000;
    public int vegTreeSpacing = 5;
    public float vegRandom = 5;

    [System.Serializable]
    public class Vegetation
    {
        public GameObject treeMesh = null;
        public float minHeight = 0.1f;
        public float maxHeight = 0.2f;
        public float minSlope = 0;
        public float maxSlope = 90;
        public float minScale = 0.8f;
        public float maxScale = 2.0f;
        public Color colour1 = Color.white;
        public Color colour2 = Color.white;
        public Color lightColour = Color.white;
        public float minRotation = 0f;
        public float maxRotation = 360f;
        public float density = 0.5f;
        public bool remove = false;
    }

    public List<Vegetation> vegetation = new List<Vegetation>()
    {
        new Vegetation()
    };

    public int detailMax = 5000;
    public int detailSpacing = 5;

    [System.Serializable]
    public class Detail
    {
        public GameObject prototype = null;
        public Texture2D prototpyeTexture = null;
        public float minHeight = 0.1f;
        public float maxHeight = 0.2f;
        public float minSlope = 0;
        public float maxSlope = 90;
        public Color dryColour = Color.white;
        public Color healthyColour = Color.white;
        public Vector2 heightRange = new Vector2(0.8f, 1);
        public Vector2 widthRange = new Vector2(0.8f, 1);
        public float noiseSpread = 0.5f;
        public float overlap = 0.1f;
        public float feather = 0.05f;
        public float density = 0.5f;
        public bool remove = false;
    }

    public List<Detail> details = new List<Detail>()
    {
        new Detail()
    };

    public float waterHeight = 0.1f;
    public GameObject waterGO = null;
    public Material shoreLineMat = null;

    public enum ErosionType { Rain, Thermal, Tidal, River, Wind }
    public ErosionType erosionType = ErosionType.Rain;
    public float erosionStrength = 0.1f;
    public float erosionAmount = 0.1f;
    public int erosionDroplets = 10;
    public float erosionSolubility = 0.01f;
    public int erosionSpringsPerRiver = 5;
    public int erosionSmoothAmount = 5;
    public float erosionWindDir = 0;

    public Terrain terrain;
    public TerrainData terrainData;

    float[,] undoHeightStore;

    public enum TagType { Tag = 0, Layer = 1 }
    [SerializeField]
    int terrainLayer = -1;
    private void Awake()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        AddTag(tagsProp, "Terrain", TagType.Tag);
        AddTag(tagsProp, "Shore", TagType.Tag);
        tagManager.ApplyModifiedProperties();

        SerializedProperty layerProp = tagManager.FindProperty("layers");
        terrainLayer = AddTag(layerProp, "Terrain", TagType.Layer);
        tagManager.ApplyModifiedProperties();

        this.gameObject.tag = "Terrain";
        this.gameObject.layer = terrainLayer;

        undoHeightStore = new float[terrainData.heightmapWidth, terrainData.heightmapHeight];
    }

    int AddTag(SerializedProperty tagsProp, string newTag, TagType tType)
    {
        bool found = false;

        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
            if (t.stringValue.Equals(newTag)) { found = true; return i; }
        }

        if (!found && tType == TagType.Tag)
        {
            tagsProp.InsertArrayElementAtIndex(0);
            SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(0);
            newTagProp.stringValue = newTag;
        }
        else if (!found && tType == TagType.Layer)
        {
            for(int j = 8; j < tagsProp.arraySize; j++)
            {
                SerializedProperty newLayer = tagsProp.GetArrayElementAtIndex(j);

                if(newLayer.stringValue == "")
                {
                    Debug.Log("Adding New Layer: " + newTag);
                    newLayer.stringValue = newTag;
                    return j;
                }
            }
        }
        return -1;
    }

    float[,] GetHeightMap()
    {
        if (!resetTerrain)
        {
            return terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
        }
        else
        {
            return new float[terrainData.heightmapWidth, terrainData.heightmapHeight];
        }
    }

    List<Vector2> GenerateNeighbours(Vector2 pos, int width, int height)
    {
        List<Vector2> neighbours = new List<Vector2>();
        for (int y = -1; y < 2; y++)
        {
            for (int x = -1; x < 2; x++)
            {
                if (!(x == 0 && y == 0))
                {
                    Vector2 nPos = new Vector2(Mathf.Clamp(pos.x + x, 0, width - 1),
                                                Mathf.Clamp(pos.y + y, 0, height - 1));
                    if (!neighbours.Contains(nPos))
                        neighbours.Add(nPos);
                }
            }
        }
        return neighbours;
    }     

    public void Erosion()
    {
        StoreUndo();

        if (erosionType == ErosionType.Rain)
        {
            Rain();
        }
        else if (erosionType == ErosionType.Tidal)
        {
            Tidal();
        }
        else if (erosionType == ErosionType.Thermal)
        {
            Thermal();
        }
        else if (erosionType == ErosionType.River)
        {
            River();
        }
        else if (erosionType == ErosionType.Wind)
        {
            Wind();
        }

        smoothTimes = erosionSmoothAmount;
        Smooth();
        
    }

    public void Rain()
    {
        float[,] heightMap =  terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
        
        for (int d = 0; d < erosionDroplets; d++)
        {
            heightMap[Random.Range(0, terrainData.heightmapWidth), Random.Range(0, terrainData.heightmapHeight)] -= erosionStrength;        

        }


        terrainData.SetHeights(0, 0, heightMap);
    }

    public void Tidal()
    {
        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);

        for (int x = 1; x < terrainData.heightmapWidth -1; x++)
        {
            for (int y = 1; y < terrainData.heightmapHeight -1; y++)
            {
                Vector2 thisLocation = new Vector2(x, y);
                List<Vector2> neighbours = GenerateNeighbours(thisLocation, terrainData.heightmapWidth, terrainData.heightmapHeight);

                foreach (Vector2 n in neighbours)
                {
                    if (heightMap[x, y] < waterHeight && heightMap[(int)n.x, (int)n.y] > waterHeight)
                    {
                        heightMap[x, y] = waterHeight;
                        heightMap[(int)n.x, (int)n.y] = waterHeight;
                    }
                }
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }

    public void Thermal()
    {
        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);

        for(int x=0; x <terrainData.heightmapWidth; x++)
        {
            for(int y=0; y < terrainData.heightmapHeight; y++)
            {
                Vector2 thisLocation = new Vector2(x, y);
                List<Vector2> neighbours = GenerateNeighbours(thisLocation, terrainData.heightmapWidth, terrainData.heightmapHeight);

                foreach (Vector2 n in neighbours)
                {
                    if (heightMap[x,y] > heightMap[(int)n.x, (int) n.y] + erosionStrength)
                    {
                        float currentHeight = heightMap[x, y];
                        heightMap[x, y] -= currentHeight * erosionAmount;
                        heightMap[(int)n.x, (int)n.y] += currentHeight * erosionAmount;
                    }
                }
            }
        }

        terrainData.SetHeights(0, 0, heightMap);
    }

    public void River()
    {
        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);

        float[,] erosionMap = new float[terrainData.heightmapWidth, terrainData.heightmapHeight];

        for(int d = 0; d < erosionDroplets; d++)
        {
            Vector2 dropletPosition = new Vector2 (Random.Range(0, terrainData.heightmapWidth), Random.Range(0, terrainData.heightmapHeight));
            erosionMap[(int)dropletPosition.x, (int)dropletPosition.y] = erosionStrength;
            for (int j = 0; j < erosionSpringsPerRiver; j++)
            {
                erosionMap = RunRiver(dropletPosition, heightMap, erosionMap,
                                      terrainData.heightmapWidth, terrainData.heightmapHeight);
            }

        }

        for(int x = 0; x < terrainData.heightmapWidth; x++)
        {
            for (int y = 0; y < terrainData.heightmapHeight; y++)
            {
                if (erosionMap[x,y] > 0)
                {
                    heightMap[x, y] -= erosionMap[x, y];
                }
            }
        }

        terrainData.SetHeights(0, 0, heightMap);
    }

    float[,] RunRiver(Vector2 dropletPosition, float[,] heightMap, float[,] erosionMap, int width, int height)
    {
        while(erosionMap[(int)dropletPosition.x, (int)dropletPosition.y] > 0)
        {
            List<Vector2> neighbours = GenerateNeighbours(dropletPosition, width, height);
            neighbours.Shuffle();

            bool foundLower = false;
            foreach (Vector2 n in neighbours)
            {
                if(heightMap[(int)n.x,(int)n.y] < heightMap[(int)dropletPosition.x, (int)dropletPosition.y])
                {
                    erosionMap[(int)n.x, (int)n.y] = erosionMap[(int)dropletPosition.x, (int)dropletPosition.y] - erosionSolubility ;
                    dropletPosition = n;
                    foundLower = true;
                    break;
                }
            }
            if (!foundLower)
            {
                erosionMap[(int)dropletPosition.x, (int)dropletPosition.y] -= erosionSolubility;
            }
        }

        return erosionMap;
    }    

    public void Wind()
    {
        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);

        int width = terrainData.heightmapWidth;
        int height = terrainData.heightmapHeight;

        float sinAngle = -Mathf.Sin(Mathf.Deg2Rad * erosionWindDir);
        float cosAngle = Mathf.Cos(Mathf.Deg2Rad * erosionWindDir);

        for (int y = -(height -1)*2; y <= height*2; y += 10)
        {
            for (int x = -(width-1)*2; x <= width*2; x += 1)
            {
                float thisNoise = (float)Mathf.PerlinNoise(x * 0.06f, y * 0.06f) * 20 * erosionStrength;
                int nx = (int)x;
                int digy = (int)y + (int)thisNoise;
                int ny = (int)y + 5 + (int)thisNoise;

                Vector2 digCoords = new Vector2(x * cosAngle - digy * sinAngle, digy * cosAngle + x * sinAngle);
                Vector2 pileCoords = new Vector2(nx * cosAngle - ny * sinAngle, ny * cosAngle + nx * sinAngle);
                
                if (!(pileCoords.x < 0 || pileCoords.x > (width - 1) || pileCoords.y < 0 || pileCoords.y > (height - 1)||
                    (int)digCoords.x < 0 || (int)digCoords.x > (width - 1) || (int)digCoords.y < 0 || (int)digCoords.y > (height - 1)))
                {
                    heightMap[(int)digCoords.x, (int)digCoords.y] -= 0.001f;
                    heightMap[(int)pileCoords.x, (int)pileCoords.y] += 0.001f;
                }
            }
        }

        terrainData.SetHeights(0, 0, heightMap);
    }       

    public void Water()
    {
        GameObject water = GameObject.Find("water");
        if (!water)
        {
            water = Instantiate(waterGO);
            water.name = "water";
        }

        water.transform.position = this.transform.position + new Vector3(terrainData.size.x / 2, waterHeight * terrainData.size.y, terrainData.size.z / 2);
        water.transform.localScale = new Vector3(terrainData.size.x, 1, terrainData.size.z);

    }

    public void Shoreline()
    {
        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);

        for (int x = 0; x < terrainData.heightmapWidth; x++)
        {
            for (int y = 0; y < terrainData.heightmapHeight; y++)
            {
                Vector2 thisLocation = new Vector2(x, y);
                List<Vector2> neighbours = GenerateNeighbours(thisLocation, terrainData.heightmapWidth, terrainData.heightmapHeight);

                foreach (Vector2 n in neighbours)
                {
                    if (heightMap[x,y] < waterHeight && heightMap[(int)n.x, (int)n.y] > waterHeight)
                    {

                        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        go.transform.localScale *= 10;
                        go.transform.position = this.transform.position + new Vector3(y / (float)terrainData.heightmapHeight * terrainData.size.z,
                                                                                      waterHeight * terrainData.size.y,
                                                                                      x / (float)terrainData.heightmapWidth * terrainData.size.x);
                        go.transform.LookAt(new Vector3(n.y / (float)terrainData.heightmapHeight * terrainData.size.z,
                                                         waterHeight * terrainData.size.y,
                                                         n.x / (float)terrainData.heightmapWidth * terrainData.size.x));


                        go.transform.Rotate(90, 0, 0);
                        go.tag = "Shore";
                        
                    }
                }
            }
        }

        GameObject[] shoreQuads = GameObject.FindGameObjectsWithTag("Shore");
        MeshFilter[] meshFilters = new MeshFilter[shoreQuads.Length];
        for (int m = 0; m < shoreQuads.Length; m++)
        {
            meshFilters[m] = shoreQuads[m].GetComponent<MeshFilter>();
        }
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        for (int i = 0; i < meshFilters.Length; i++)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            meshFilters[i].gameObject.SetActive(false);
        }

        GameObject currentShoreLine = GameObject.Find("ShoreLine");
        if (currentShoreLine)
        {
            DestroyImmediate(currentShoreLine);
        }
        GameObject shoreLine = new GameObject();
        shoreLine.name = "ShoreLine";
        shoreLine.AddComponent<WaveAnimation>();
        shoreLine.transform.position = this.transform.position;
        shoreLine.transform.rotation = this.transform.rotation;

        MeshFilter thisMF = shoreLine.AddComponent<MeshFilter>();
        thisMF.mesh = new Mesh();
        shoreLine.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(combine);

        MeshRenderer r = shoreLine.AddComponent<MeshRenderer>();
        r.sharedMaterial = shoreLineMat;

        for (int sQ = 0; sQ <shoreQuads.Length; sQ++)
        {
            DestroyImmediate(shoreQuads[sQ]);
        }
    }

    public void AddNewDetail()
    {
        details.Add(new Detail());

    }

    public void RemoveDetail()
    {
        List<Detail> keptDetails = new List<Detail>();
        for (int i = 0; i < details.Count; i++)
        {
            if (!details[i].remove)
            {
                keptDetails.Add(details[i]);
            }
        }
        if (keptDetails.Count == 0)
        {
            keptDetails.Add(details[0]);
        }
        details = keptDetails;
    }

    public void PlantDetails()
    {
        DetailPrototype[] newDetailPrototypes;
        newDetailPrototypes = new DetailPrototype[details.Count];
        int dindex = 0;
        foreach (Detail d in details)
        {
            newDetailPrototypes[dindex] = new DetailPrototype();
            newDetailPrototypes[dindex].prototype = d.prototype;
            newDetailPrototypes[dindex].prototypeTexture = d.prototpyeTexture;
            newDetailPrototypes[dindex].healthyColor = d.healthyColour;
            newDetailPrototypes[dindex].dryColor = d.dryColour;
            newDetailPrototypes[dindex].noiseSpread = d.noiseSpread;

            newDetailPrototypes[dindex].minHeight = d.heightRange.x;
            newDetailPrototypes[dindex].maxHeight = d.heightRange.y;
            newDetailPrototypes[dindex].minWidth = d.heightRange.x;
            newDetailPrototypes[dindex].maxWidth = d.heightRange.y;

            if (newDetailPrototypes[dindex].prototype)
            {
                newDetailPrototypes[dindex].usePrototypeMesh = true;
                newDetailPrototypes[dindex].renderMode = DetailRenderMode.VertexLit;
            }
            else
            {


                newDetailPrototypes[dindex].usePrototypeMesh = false;
                newDetailPrototypes[dindex].renderMode = DetailRenderMode.GrassBillboard;
            }
            dindex++;
        }
        terrainData.detailPrototypes = newDetailPrototypes;

        float[,] heightMap;
        heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);

        for (int i = 0; i < terrainData.detailPrototypes.Length; i++)
        {
            int[,] detailMap = new int[terrainData.detailWidth, terrainData.detailHeight];

            for (int y = 0; y < terrainData.detailHeight; y += detailSpacing)
            {
                for (int x = 0; x < terrainData.detailWidth; x += detailSpacing) 
                {
                    if (Random.Range(0f, 1f) > details[i].density) continue;

                    int xHM = (int)(x / (float)terrainData.detailWidth * terrainData.heightmapWidth);
                    int yHM = (int)(y / (float)terrainData.detailHeight * terrainData.heightmapHeight);

                    
                    float noise = Utils.Map(Mathf.PerlinNoise(x * details[i].feather, y*details[i].feather),0,1,0.5f,1);

                    float thisHeightStart = details[i].minHeight * noise - details[i].overlap * noise;
                    float thisHeightEnd = details[i].maxHeight * noise + details[i].overlap * noise;
                    float thisHeight = heightMap[yHM, xHM];

                    float steepness = terrainData.GetSteepness(xHM / (float)terrainData.size.x, yHM / (float)terrainData.size.z);


                    if ((thisHeight >= thisHeightStart && thisHeight <= thisHeightEnd) && (steepness >= details[i].minSlope && steepness<= details[i].maxSlope))
                    {
                        detailMap[y, x] = 1;
                    }
                }
            }
            terrainData.SetDetailLayer(0, 0, i, detailMap);
        }

    }

    public void AddNewTree()
    {
        vegetation.Add(new Vegetation());
    }

    public void RemoveTree()
    {
        List<Vegetation> keptTrees = new List<Vegetation>();
        for (int i = 0; i < vegetation.Count; i++)
        {
            if (!vegetation[i].remove)
            {
                keptTrees.Add(vegetation[i]);
            }
        }
        if (keptTrees.Count == 0)
        {
            keptTrees.Add(vegetation[0]);
        }
        vegetation = keptTrees;
    }

    public void PlantVegetation()
    {
        TreePrototype[] newTreePrototypes;
        newTreePrototypes = new TreePrototype[vegetation.Count];
        int tindex = 0;
        foreach (Vegetation t in vegetation)
        {
            newTreePrototypes[tindex] = new TreePrototype();
            newTreePrototypes[tindex].prefab = t.treeMesh;
            tindex++;
        }
        terrainData.treePrototypes = newTreePrototypes;
        List<TreeInstance> allVegetation = new List<TreeInstance>();
        for (int z = 0; z < terrainData.size.z; z += (vegTreeSpacing))
        {
            for (int x = 0; x < terrainData.size.x; x += vegTreeSpacing)
            {
                for (int tp = 0; tp < terrainData.treePrototypes.Length; tp++)
                {
                    if (Random.Range(0.0f, 1.0f) > vegetation[tp].density) break;

                    float thisHeight = terrainData.GetHeight(x, z) / terrainData.size.y;
                    float thisHeightStart = vegetation[tp].minHeight;
                    float thisHeightEnd = vegetation[tp].maxHeight;
                    float steepness = terrainData.GetSteepness(x / (float)terrainData.size.x, z / (float)terrainData.size.z);

                    if ((thisHeightEnd >= thisHeight && thisHeightStart <= thisHeight) && (steepness >= vegetation[tp].minSlope && steepness <= vegetation[tp].maxSlope))
                    {
                        TreeInstance instance = new TreeInstance();
                        instance.position = new Vector3((x + Random.Range(-vegRandom, vegRandom)) / terrainData.size.x,
                                                        thisHeight,
                                                        (z + Random.Range(-vegRandom, vegRandom)) / terrainData.size.z);
                        Vector3 treeWorldPos = new Vector3(instance.position.x * terrainData.size.x,
                                                            instance.position.y * terrainData.size.y,
                                                            instance.position.z * terrainData.size.z) 
                                                            + transform.position;
                        RaycastHit hit;
                        int layerMask = 1 << terrainLayer;
                        if (Physics.Raycast(treeWorldPos + new Vector3(0, 10, 0), -Vector3.up, out hit, 100, layerMask) || Physics.Raycast(treeWorldPos - new Vector3(0, 10, 0), Vector3.up, out hit, 100, layerMask))
                        {
                            float treeHeight = (hit.point.y - this.transform.position.y) / terrainData.size.y;
                            instance.position = new Vector3(instance.position.x,
                                                            treeHeight,
                                                            instance.position.z);

                            instance.rotation = Random.Range(vegetation[tp].minRotation, vegetation[tp].maxRotation);
                            instance.prototypeIndex = tp;
                            instance.color = Color.Lerp(vegetation[tp].colour1,
                                                        vegetation[tp].colour2, 
                                                        Random.Range(0.0f, 1.0f));
                            instance.lightmapColor = vegetation[tp].lightColour;
                            float scale = Random.Range(vegetation[tp].minScale, vegetation[tp].maxScale);
                            instance.heightScale = scale + Random.Range(-0.6f, 0.6f);
                            instance.widthScale = scale + Random.Range(-0.6f, 0.6f);
                            allVegetation.Add(instance);
                            if (allVegetation.Count >= vegTreeMax) goto TREESDONE;
                        }


                    }
                }
            }
        }

        TREESDONE:
        terrainData.treeInstances = allVegetation.ToArray();
    }

    public void AddNewSplatHeight()
    {
        splatHeights.Add(new SplatHeights());
    }

    public void RemoveSplatHeight()
    {
        List<SplatHeights> keptSplatHeights = new List<SplatHeights>();
        for (int i = 0; i < splatHeights.Count; i++)
        {
            if (!splatHeights[i].remove)
            {
                keptSplatHeights.Add(splatHeights[i]);
            }
        }
        if(keptSplatHeights.Count == 0)
        {
            keptSplatHeights.Add(splatHeights[0]);
        }
        splatHeights = keptSplatHeights;
    }

    public void SplatMaps()
    {
        SplatPrototype[] newSplatPrototypes;
        newSplatPrototypes = new SplatPrototype[splatHeights.Count];
        int spindex = 0;
        foreach (SplatHeights sh in splatHeights)
        {
            newSplatPrototypes[spindex] = new SplatPrototype();
            newSplatPrototypes[spindex].texture = sh.texture;
            newSplatPrototypes[spindex].tileOffset = sh.tileOffset;
            newSplatPrototypes[spindex].tileSize = sh.tileSize;   
            newSplatPrototypes[spindex].texture.Apply(true);
            spindex++;
        }
        terrainData.splatPrototypes = newSplatPrototypes;

        float[,] heightMap = terrainData.GetHeights(0, 0, terrain.terrainData.heightmapWidth, terrain.terrainData.heightmapHeight);

        float[,,] splatMapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];

        for(int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for(int x=0; x < terrainData.alphamapWidth; x++)
            {
                float[] splat = new float[terrainData.alphamapLayers];
                for(int i=0; i < splatHeights.Count; i++)
                {
                    float noise = Mathf.PerlinNoise(x * splatHeights[i].splatNoiseX, y* splatHeights[i].splatNoiseY) * splatHeights[i].splatNoiseScale;
                    float offset = splatHeights[i].splatOffet + noise;
                    float thisHeightStart = splatHeights[i].minHeight - offset;
                    float thisHeightStop = splatHeights[i].maxHeight + offset;
                    float steepness = terrainData.GetSteepness(y / (float)terrainData.alphamapHeight, x / (float)terrainData.alphamapWidth);

                    if ((heightMap[x,y] >= thisHeightStart && heightMap[x,y] <= thisHeightStop) && (steepness >= splatHeights[i].minSlope && steepness <= splatHeights[i].maxSlope))
                    {
                        splat[i] = 1;
                    }
                }

                NormaliseVector(splat);
                for(int j =0; j < splatHeights.Count; j++)
                {
                    splatMapData[x, y, j] = splat[j];
                }
            }
        }
        terrainData.SetAlphamaps(0, 0, splatMapData);
    }

    void NormaliseVector(float[] v)
    {
        float total = 0;
        for (int i = 0; i < v.Length; i++)
        {
            total += v[i];
        }

        for (int i = 0; i < v.Length; i++)
        {
            v[i] /= total;
        }
    }
    
    public void Smooth()
    {
        StoreUndo();

        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
        float smoothProgress = 0;
        EditorUtility.DisplayProgressBar("Smoothing Terrain", "Progress", smoothProgress);
        
        for (int s = 0; s < smoothTimes; s++)
        {
            for (int y = 0; y < terrainData.heightmapHeight; y++)
            {
                for (int x = 0; x < terrainData.heightmapWidth; x++)
                {
                    float avgHeight = heightMap[x, y];
                    List<Vector2> neighbours = GenerateNeighbours(new Vector2(x, y),
                                                                  terrainData.heightmapWidth,
                                                                  terrainData.heightmapHeight);
                    foreach (Vector2 n in neighbours)
                    {
                        avgHeight += heightMap[(int)n.x, (int)n.y];
                    }

                    heightMap[x, y] = avgHeight / ((float)neighbours.Count + 1);
                }
            }

            smoothProgress++;
            EditorUtility.DisplayProgressBar("Smoothing Terrain", "Progress", smoothProgress/smoothTimes);
        }
        terrainData.SetHeights(0, 0, heightMap);
        EditorUtility.ClearProgressBar();

    }

    public void MPD()
    {
        StoreUndo();

        float[,] heightMap = GetHeightMap();
        int width = terrainData.heightmapWidth - 1;
        int squareSize = width;
        float minHeight = mpMinHeight;
        float maxHeight = mpMaxHeight;
        float heightDampener = (float)Mathf.Pow(mpDampPower, -1 * mpRoughness);

        int cornerX, cornerY;
        int midX, midY;
        int pmidXL, pmidXR, pmidYU, pmidYD;

        while (squareSize > 0)
        {
            for (int x = 0; x < width; x += squareSize)
            {
                for (int y = 0; y < width; y += squareSize)
                {
                    cornerX = (x + squareSize);
                    cornerY = (y + squareSize);

                    midX = (int)(x + squareSize / 2.0f);
                    midY = (int)(y + squareSize / 2.0f);

                    heightMap[midX, midY] = (float)((heightMap[x, y] + heightMap[cornerX, y] + heightMap[x, cornerY] + heightMap[cornerX, cornerY]) 
                                                    / 4.0f + Random.Range(minHeight, maxHeight));
                }
            }

            for (int x = 0; x < width; x += squareSize)
            {
                for (int y = 0; y < width; y += squareSize)
                {

                    cornerX = (x + squareSize);
                    cornerY = (y + squareSize);

                    midX = (int)(x + squareSize / 2.0f);
                    midY = (int)(y + squareSize / 2.0f);

                    pmidXR = (int)(midX + squareSize);
                    pmidXL = (int)(midX - squareSize);
                    pmidYU = (int)(midY + squareSize);
                    pmidYD = (int)(midY - squareSize);

                    if (pmidXR >= width - 1 || pmidXL <= 0 || pmidYU >= width - 1 || pmidYD <= 0) continue;


                    heightMap[midX, y] = (float)((heightMap[x, y] + heightMap[cornerX, y] + heightMap[midX, midY] + heightMap[midX, pmidYD])
                                                    / 4.0f + Random.Range(minHeight, maxHeight));
                    heightMap[midX, cornerY] = (float)((heightMap[cornerX, cornerY] + heightMap[x, cornerY] + heightMap[midX, midY] + heightMap[midX, pmidYU])
                                                     / 4.0f + Random.Range(minHeight, maxHeight));
                    heightMap[x, midY] = (float)((heightMap[x, y] + heightMap[x, cornerY] + heightMap[midX, midY] + heightMap[pmidXL, midY])
                                                    / 4.0f + Random.Range(minHeight, maxHeight));
                    heightMap[cornerX, midY] = (float)((heightMap[cornerX, cornerY] + heightMap[cornerX, y] + heightMap[midX, midY] + heightMap[pmidXR, midY])
                                                     / 4.0f + Random.Range(minHeight, maxHeight));
                }
            }

            squareSize = (int)(squareSize / 2.0f);
            minHeight *= heightDampener;
            maxHeight *= heightDampener;
        }
        terrainData.SetHeights(0, 0, heightMap);
    }

    public void Voronoi()
    {
        StoreUndo();

        float[,] heightMap;
        heightMap = GetHeightMap();

        for (int p = 0; p < voronoiPeakCount; p++)
        {
            int peakX = Random.Range(0, terrainData.heightmapWidth);
            int peakZ = Random.Range(0, terrainData.heightmapHeight);
            float peakHeight = Random.Range(voronoiMinHeight, voronoiMaxHeight);

            if (heightMap[peakX, peakZ] < peakHeight)
            {
                heightMap[peakX, peakZ] = peakHeight;
            }
            else { continue; }
            Vector2 peakLocation = new Vector2(peakX, peakZ);
            float maxDistance = Vector2.Distance(new Vector2(0, 0), new Vector2(terrainData.heightmapWidth, terrainData.heightmapHeight));

            for (int x = 0; x < terrainData.heightmapWidth; x++)
            {
                for (int y = 0; y < terrainData.heightmapHeight; y++)
                {
                    if (!(x == peakX && y == peakZ))
                    {
                        float distanceToPeak = Vector2.Distance(peakLocation, new Vector2(x, y)) / maxDistance;
                        float h;

                        if (voronoiType == VoronoiType.SinPow)
                        {
                            h = peakHeight - Mathf.Pow(distanceToPeak * 3, voronoiFallOff) - Mathf.Sin(distanceToPeak * 2 * Mathf.PI) / voronoiDropOff;
                        }
                        else if (voronoiType == VoronoiType.Combined)
                        {
                            h = peakHeight - distanceToPeak * voronoiFallOff - Mathf.Pow(distanceToPeak, voronoiDropOff);
                        }
                        else if (voronoiType == VoronoiType.Power)
                        { 
                            h = peakHeight - distanceToPeak - Mathf.Pow(distanceToPeak, voronoiDropOff) * voronoiFallOff;
                        }
                        else 
                        {
                            h = peakHeight - distanceToPeak * voronoiFallOff;
                        }


                        if (heightMap[x, y] < h)
                        {
                            heightMap[x, y] = h;
                        }
                    }

                }
            }
        }

        terrainData.SetHeights(0, 0, heightMap);

    }

    public void Perlin()
    {
        StoreUndo();

        float[,] heightMap;
        heightMap = GetHeightMap();

        for (int x = 0; x < terrainData.heightmapWidth; x++)
        {
            for (int y = 0; y < terrainData.heightmapHeight; y++)
            {
                heightMap[x, y] += Utils.fBM((x + perlinXOffset) * perlinXScale, (y + perlinYOffset) * perlinYScale, perlinOctaves,
                                            perlinPersistance) * perlinHeightScale;
            }
        }

        terrainData.SetHeights(0, 0, heightMap);
    }

    public void MultiplePerlin()
    {
        StoreUndo();

        float[,] heightMap;
        heightMap = GetHeightMap();

        for (int x = 0; x < terrainData.heightmapWidth; x++)
        {
            for (int y = 0; y < terrainData.heightmapHeight; y++)
            {
                foreach (PerlinParameters p in perlinParameters)
                {
                    heightMap[x, y] += Utils.fBM((x + p.mPerlinXOffset) * p.mPerlinXScale, (y + p.mPerlinYOffset) * p.mPerlinYScale, p.mPerlinOctaves,
                                                p.mPerlinPersistance) * p.mPerlinHeightScale;
                }
            }
        }

        terrainData.SetHeights(0, 0, heightMap);
    }

    public void AddNewPerlin()
    {
        perlinParameters.Add(new PerlinParameters());
    }

    public void RemovePerlin()
    {
        List<PerlinParameters> keptPerlinParameters = new List<PerlinParameters>();
        for (int i = 0; i < perlinParameters.Count; i++)
        {
            if (!perlinParameters[i].remove)
            {
                keptPerlinParameters.Add(perlinParameters[i]);
            }
        }

        if(keptPerlinParameters.Count == 0)
        {
            keptPerlinParameters.Add(perlinParameters[0]);
        }
        perlinParameters = keptPerlinParameters;
    }

    public void RandomTerrain()
    {
        StoreUndo();

        float[,] heightMap;
        heightMap = GetHeightMap();

        for (int x = 0; x < terrainData.heightmapWidth; x++)
        {
            for (int y = 0; y < terrainData.heightmapHeight; y++)
            {
                heightMap[x, y] += Random.Range(randomHeightRange.x, randomHeightRange.y);
            }
        }

        terrainData.SetHeights(0, 0, heightMap);
    }

    public void LoadTexture()
    {
        StoreUndo();

        float[,] heightMap;
        heightMap = GetHeightMap();

        for (int x = 0; x < terrainData.heightmapWidth; x++)
        {
            for (int y = 0; y < terrainData.heightmapHeight; y++)
            {
                heightMap[x, y] += heightMapImage.GetPixel((int)(x * heightMapScale.x), (int)(y * heightMapScale.z)).grayscale * heightMapScale.y;
            }
        }

        terrainData.SetHeights(0, 0, heightMap);
    }

    void StoreUndo()
    {
        undoHeightStore = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
    }

    public void ResetTerrain()
    {
        float[,] heightMap;

        heightMap = new float[terrainData.heightmapWidth, terrainData.heightmapHeight];

        for (int x = 0; x < terrainData.heightmapWidth; x++)
        {
            for (int y = 0; y < terrainData.heightmapHeight; y++)
            {
                heightMap[x, y] = 0;
            }
        }

        terrainData.SetHeights(0, 0, heightMap);
    }

    public void UndoLastTerrain()
    {
        terrainData.SetHeights(0, 0, undoHeightStore);
    }

    private void OnEnable()
    {
        Debug.Log("Ininitalising Terrain Data");
        terrain = this.GetComponent<Terrain>();
        terrainData = Terrain.activeTerrain.terrainData;
    }


    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
