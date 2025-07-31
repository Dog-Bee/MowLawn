using UnityEditor;
using UnityEngine;

public class GrassMeshGeneratorEditor : EditorWindow
{
    private int _gridSize = 10;
    private int _densityPerCell = 10;
    private float _minGrassWidth = 0.1f;
    private float _maxGrassWidth = 0.5f;
    private float _minGrassHeight = 0.1f;
    private float _maxGrassHeight = 0.5f;
    private int _bladesPerPoint = 2;
    private float _angleSpread = 20f;
    private Material _grassMaterial;

    private Mesh _currentMesh;


    [MenuItem("Tools/Generators/Grass Mesh Generator")]
    private static void ShowWindow()
    {
        GetWindow<GrassMeshGeneratorEditor>("Grass Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Grass Generator", EditorStyles.boldLabel);
        
        _gridSize = EditorGUILayout.IntField("Grid Size", _gridSize);
        _densityPerCell = EditorGUILayout.IntSlider("Density Per Cell", _densityPerCell, 1, 1000);
        _bladesPerPoint = EditorGUILayout.IntSlider("Grass Count Per Point", _bladesPerPoint, 1, 5);
        _angleSpread = EditorGUILayout.Slider("Angle Spread (Degrees)",_angleSpread,0,90);
        
        GUILayout.Label("Grass Height Range");
        EditorGUILayout.LabelField($"'MinHeight: {_minGrassHeight:F2}, MaxHeight: {_maxGrassHeight:F2}");
        EditorGUILayout.MinMaxSlider(ref _minGrassHeight, ref _maxGrassHeight, 0.01f, 1f);
        
        GUILayout.Label("Grass Width Range");
        EditorGUILayout.LabelField($"'MinWidth: {_minGrassWidth:F2}, MaxWidth: {_maxGrassWidth:F2}");
        EditorGUILayout.MinMaxSlider(ref _minGrassWidth, ref _maxGrassWidth, 0.01f, 1f);
        
        
        
        _grassMaterial = EditorGUILayout.ObjectField("Grass Material", _grassMaterial, typeof(Material), false) as Material;

        if (GUILayout.Button("Generate Grass Mesh"))
        {
            GenerateMesh();
        }

        if (GUILayout.Button("Save Mesh"))
        {
            SaveMesh();
        }
    }

    private void SaveMesh()
    {
        string path = EditorUtility.SaveFilePanelInProject("Save generated mesh", "GrassMesh", "Asset",
            "Specify where to save generated mesh");
        
        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(_currentMesh, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"Mesh saved to {path}");
        } 
    }


    private void GenerateMesh()
    {
        GameObject grassGO = new GameObject("GrassMesh");
        var filter = grassGO.AddComponent<MeshFilter>();
        var renderer = grassGO.AddComponent<MeshRenderer>();
        
        Vector3 clusterCenter = new Vector3(_gridSize*.5f, 0, _gridSize*.5f);
        
        if(_grassMaterial != null)
            renderer.sharedMaterial = _grassMaterial;
        
        _currentMesh = new Mesh();
        _currentMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        
        int totalGrass = _densityPerCell * _gridSize * _gridSize*_bladesPerPoint;
        Vector3[] vertices = new Vector3[totalGrass * 3];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[totalGrass*3];

        int v = 0;
        int t = 0;

        for (int gx = 0; gx < _gridSize; gx++)
        {
            for (int gz = 0; gz < _gridSize; gz++)
            {
                for (int i = 0; i < _densityPerCell; i++)
                {
                    Vector3 pos = new Vector3(gx+Random.value,0,gz+Random.value)-clusterCenter;

                    for (int j = 0; j < _bladesPerPoint; j++)
                    {
                        float angle = Random.Range(0,Mathf.PI*2);
                        float spreadOffset = Random.Range(-_angleSpread, _angleSpread)*Mathf.Deg2Rad;
                        float tilt = Random.Range(0, Mathf.Deg2Rad * 15f);
                    
                        float grassWidth = Random.Range(_minGrassWidth,_maxGrassWidth);
                        float grassHeight = Random.Range(_minGrassHeight,_maxGrassHeight);

                        float finalAngle = angle + spreadOffset;
                        Vector3 right = new Vector3(Mathf.Cos(finalAngle), 0, Mathf.Sin(finalAngle))* grassWidth * .5f;
                        Vector3 tiltOffset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * Mathf.Tan(tilt) *
                                             grassHeight;

                        vertices[v] = pos - right;
                        vertices[v + 1] = pos + right;
                        vertices[v + 2] = pos + Vector3.up*grassHeight+tiltOffset;
                    
                        uvs[v] = new Vector2(0,0);
                        uvs[v + 1] = new Vector2(1,0);
                        uvs[v + 2] = new Vector2(0.5f,1);
                    
                        triangles[t++] = v;
                        triangles[t++] = v + 1;
                        triangles[t++] = v + 2;

                        v += 3;
                    }
                }
            }
        }
        
        _currentMesh.vertices = vertices;
        _currentMesh.uv = uvs;
        _currentMesh.triangles = triangles;
        _currentMesh.RecalculateNormals();
        _currentMesh.name = "GeneratedGrassMesh";
        
        filter.sharedMesh = _currentMesh;
        
        Undo.RegisterCreatedObjectUndo(grassGO, "Create Grass Mesh");
        Selection.activeGameObject = grassGO;

       

    }
}
