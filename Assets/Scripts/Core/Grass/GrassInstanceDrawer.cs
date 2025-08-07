using System.Collections.Generic;
using UnityEngine;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class GrassInstanceDrawer : MonoBehaviour
{
    [Header("Grass Settings")] [SerializeField]
    private Mesh grassMesh;

    [SerializeField] private Material grassMaterial;
    [SerializeField] private Material cutterMaterial;
    [SerializeField] private int instanceCount = 1000;

    [Header("Surface Settings")] 
    [SerializeField] private MeshFilter surfaceMesh;

    [SerializeField] private float pixelPerUnit = 100f;

    private List<Matrix4x4> _matrices = new List<Matrix4x4>(1023);
    private const int BATCH_SIZE = 1023;

    private int _grassCountX;
    private int _grassCountZ;

    private float _grassSpacingX;
    private float _grassSpacingZ;

    private float _surfaceWidth;
    private float _surfaceLength;
    
    private Vector3 _surfaceOrigin;

    public Material _runtimeMaterial;
    private Material _cutterMaterial;
    public RenderTexture _cutMask;


    private void Start()
    {
        SurfaceCalculations();
        CreateMaterialAndMask();
        CreateCutterMaterial();
        GenerateInstances();
    }

    private void Update()
    {
        for (int i = 0; i < _matrices.Count; i += BATCH_SIZE)
        {
            int count = Mathf.Min(BATCH_SIZE, _matrices.Count - i);
            Graphics.DrawMeshInstanced(grassMesh, 0, _runtimeMaterial, _matrices.GetRange(i, count));
        }
    }

    private void SurfaceCalculations()
    {
        if (surfaceMesh == null)
        {
            Debug.LogError("Surface Mesh is null");
            return;
        }
        
        Vector3 size = surfaceMesh.sharedMesh.bounds.size;
        Vector3 surfaceScale = surfaceMesh.transform.lossyScale;
        
        _surfaceWidth = size.x * surfaceScale.x;
        _surfaceLength = size.z * surfaceScale.z;
        
        _grassCountX = Mathf.RoundToInt(Mathf.Sqrt(instanceCount * (_surfaceWidth / _surfaceLength)));
        _grassCountZ = Mathf.RoundToInt((float)instanceCount / _grassCountX);
        
        _grassSpacingX = _surfaceWidth / _grassCountX;
        _grassSpacingZ = _surfaceLength / _grassCountZ;
        

        _surfaceOrigin = surfaceMesh.transform.position - new Vector3(_surfaceWidth, 0, _surfaceLength) * .5f;

    }

    private void CreateMaterialAndMask()
    {
        _runtimeMaterial = new Material(grassMaterial);
        int textureWidth = Mathf.CeilToInt(_surfaceWidth * pixelPerUnit);
        int textureHeight = Mathf.CeilToInt(_surfaceLength * pixelPerUnit);
        
        _cutMask = new RenderTexture(textureWidth, textureHeight, 0, RenderTextureFormat.ARGB32);
        _cutMask.enableRandomWrite = true;
        _cutMask.Create();
        Graphics.Blit(Texture2D.whiteTexture,_cutMask);
        
        
        _runtimeMaterial.SetTexture("_CutMask", _cutMask);
        
        Vector2 tiling = new Vector2(1/_surfaceWidth, 1/_surfaceLength);
        _runtimeMaterial.SetVector("_CutMaskTiling",tiling);
        
        _runtimeMaterial.SetFloat("_SurfaceOriginX",_surfaceOrigin.x);
        _runtimeMaterial.SetFloat("_SurfaceOriginZ",_surfaceOrigin.z);
        _runtimeMaterial.SetFloat("_SurfaceWidth",_surfaceWidth);
        _runtimeMaterial.SetFloat("_SurfaceLength",_surfaceLength);
        
    }

    private void CreateCutterMaterial()
    {
        if (_cutterMaterial != null) return;
        _cutterMaterial = new Material(cutterMaterial);
        
        _cutterMaterial.SetTexture("_MainTex",_cutMask);

    }
    

    private void GenerateInstances()
    {
        _matrices.Clear();

        for (int x = 0; x < _grassCountX; x++)
        {
            for (int z = 0; z < _grassCountZ; z++)
            {
                if (_matrices.Count >= instanceCount) break;
                
                Vector3 localOffset = new Vector3(x * _grassSpacingX+_grassSpacingX*.5f, 0, z * _grassSpacingZ+_grassSpacingZ*.5f);
                Vector3 worldPos = _surfaceOrigin + localOffset;
                Vector3 scale = Vector3.one;
                Matrix4x4 matrix = Matrix4x4.TRS(worldPos, Quaternion.identity, scale);
                _matrices.Add(matrix);
            }
        }

        Debug.Log($"Generated {_matrices.Count} grass over {_surfaceWidth}x{_surfaceLength}");
    }

    public void CutAtWorld(Vector3 worldPos, float radius)
    {
        if (_cutterMaterial == null) return;
        
        float u = (worldPos.x -_surfaceOrigin.x) / _surfaceWidth;
        float v = (worldPos.z - _surfaceOrigin.z) / _surfaceLength;
        
        Vector2 uv = new Vector2(u, v);
        
        _cutterMaterial.SetVector("_Center",uv);
        _cutterMaterial.SetFloat("_Radius", radius/Mathf.Max(_surfaceWidth,_surfaceLength));
        
        RenderTexture temp = RenderTexture.GetTemporary(_cutMask.width, _cutMask.height, 0, _cutMask.format);
        Graphics.Blit(_cutMask,temp);
        _cutterMaterial.SetTexture("_MainTex",temp);
        Graphics.Blit(temp,_cutMask,_cutterMaterial);
        RenderTexture.ReleaseTemporary(temp);
    }
    
}