using UnityEngine;
using UnityEditor;

public class GrassPlaneGeneratorEditor : EditorWindow
{
    private int _resolution = 128;
    private float _size = 10f;
    private Material _material;


    [MenuItem("Tools/Generators/Grass Plane Generator")]
    private static void ShowWindow()
    {
        GetWindow<GrassPlaneGeneratorEditor>("GrassGenerator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Grass Plane Generator", EditorStyles.boldLabel);
        _resolution = EditorGUILayout.IntSlider("Resolution", _resolution, 2, 512);
        _size = EditorGUILayout.FloatField("Size", _size);
        _material = (Material)EditorGUILayout.ObjectField("Material", _material, typeof(Material), false);

        if (GUILayout.Button("Generate Plane"))
        {
            GeneratePlane();
        }
    }

    private void GeneratePlane()
    {
        GameObject plane = new GameObject("GrassPlane");
        
        var filter = plane.AddComponent<MeshFilter>();
        var renderer = plane.AddComponent<MeshRenderer>();
        
        if (_material != null)
        {
            renderer.sharedMaterial = _material;
        }

        Mesh mesh = new Mesh();
        Vector3[] verts = new Vector3[(_resolution + 1) * (_resolution + 1)];
        Vector2[] uv = new Vector2[verts.Length];
        int[] tris = new int[_resolution * _resolution * 6];

        float step = _size / _resolution;

        int vi = 0;
        int ti = 0;

        for (int y = 0; y <= _resolution; y++)
        {
            for (int x = 0; x <= _resolution; x++)
            {
                float px = x * step - _size / 2;
                float py = y * step - _size / 2;

                verts[vi] = new Vector3(px, 0, py);
                uv[vi] = new Vector2((float)x / _resolution, (float)y / _resolution);

                vi++;
            }
        }


        for (int y = 0; y < _resolution; y++)
        {
            for (int x = 0; x < _resolution; x++)
            {
                int i = y * (_resolution + 1) + x;
                
                tris[ti++] = i;
                tris[ti++] = i + _resolution + 1;
                tris[ti++] = i + 1;
                
                tris[ti++] = i + 1;
                tris[ti++] = i + _resolution + 1;
                tris[ti++] = i + _resolution + 2;
            }
        }

        mesh.name = $"GrassMesh_{_resolution}x{_resolution}";
        mesh.vertices = verts;
        mesh.uv = uv;
        mesh.triangles = tris;
        
        mesh.RecalculateNormals();
        
        filter.sharedMesh = mesh;
        
        Undo.RegisterCreatedObjectUndo(plane, "Create Grass Plane");
        Selection.activeGameObject = plane;
    }
}