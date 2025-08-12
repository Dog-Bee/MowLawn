using System;
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
    [Range(0,1)][SerializeField] private float defaultStrength = 1.0f;
    

    private List<Matrix4x4> _matrices = new List<Matrix4x4>(1023);
    private const int BATCH_SIZE = 1023;

    private int _grassCountX;
    private int _grassCountZ;

    private float _grassSpacingX;
    private float _grassSpacingZ;

    private float _surfaceWidth;
    private float _surfaceLength;
    
    private Vector3 _surfaceOrigin;

    private Material _runtimeMaterial;
    private Material _cutterMaterial;

    private RenderTexture _cutMask;
    
    public RenderTexture CutMask=>_cutMask;

    private void Awake()
    {
        Application.targetFrameRate = 30;
    }

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
        _cutterMaterial.SetFloat("_Strength",defaultStrength);

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

    private Vector2 WorldToUV(Vector3 worldPos)
    {
        float u = (worldPos.x - _surfaceOrigin.x)/_surfaceWidth;
        float v = (worldPos.z - _surfaceOrigin.z)/_surfaceLength;
        
        return new Vector2(u, v);
    }

    public void CutCircleAtWorld(Vector3 worldPos, float radius)
    {
        if (_cutterMaterial == null) return;
        
        float u = (worldPos.x -_surfaceOrigin.x) / _surfaceWidth;
        float v = (worldPos.z - _surfaceOrigin.z) / _surfaceLength;
        
        Vector2 uv = new Vector2(u, v);
        
        _cutterMaterial.SetVector("_Center",uv);
        _cutterMaterial.SetFloat("_Radius", radius/Mathf.Max(_surfaceWidth,_surfaceLength));
        
        BlitToMask();
    }

    public void CutAllGrass()
    {
        if (_cutMask == null) return;
        Graphics.Blit(Texture2D.blackTexture, _cutMask);
    }

    public void RestoreAllGrass()
    {
        if (_cutMask == null) return;
        Graphics.Blit(Texture2D.whiteTexture, _cutMask);
    }

    public void PushGrassFromCenter(Vector3 centerWorld, float radiusWorld)
    {
        Vector2 cUV = WorldToUV(centerWorld);
        
        float rUV = radiusWorld/Mathf.Max(_surfaceWidth,_surfaceLength);

        _runtimeMaterial.SetVector("_PushCenterUV", cUV);
        _runtimeMaterial.SetFloat("_PushRadiusUV", rUV);

    }

    public void CutSweepStampWorld(Texture stampTex, Vector3 prevWorld, Vector3 currentWorld, Vector3 worldSize,
        float strength = 0)
    {
        if (_cutterMaterial == null || stampTex == null) return;
        
        strength = strength is <= 0 or > 1? defaultStrength : strength;
        Vector2 a = WorldToUV(prevWorld);
        Vector2 b = WorldToUV(currentWorld);

        Vector2 ab = b - a;
        float EPS = 1e-6f;
        float lenUV = Math.Max(EPS, ab.magnitude);
        
        Vector2 axisU = ab / lenUV;
        Vector2 axisV = new Vector2(-axisU.y, axisU.x);
        
        Vector2 sizeUV = new Vector2(worldSize.x / _surfaceWidth, worldSize.y / _surfaceLength);
        float invV = 1/MathF.Max(EPS, sizeUV.y);
        
        float texelUV = Mathf.Max(1/_cutMask.width, 1/_cutMask.height);
        float pad = texelUV * 2f;
        Vector2 aPad = a - axisU * pad;
        Vector2 bPad = b + axisU * pad;
        float lenPad = lenUV + 2f * pad;
        Vector2 invSizeUV = new Vector2(1/Mathf.Max(EPS,lenPad),invV);
        
        _cutterMaterial.SetFloat("_UseSweep", 1);
        _cutterMaterial.SetTexture("_StampTex",stampTex);
        _cutterMaterial.SetVector("_PrevStampCenterUV",aPad);
        _cutterMaterial.SetVector("_StampCenterUV",bPad);
        _cutterMaterial.SetVector("_StampAxisU", axisU);
        _cutterMaterial.SetVector("_StampAxisV", axisV);
        _cutterMaterial.SetVector("_StampInvSizeUV", invSizeUV);
        _cutterMaterial.SetFloat("_Strength", Mathf.Clamp01(strength));

        BlitToMask();

    }

    public void CutStampWorld(Texture stampTex, Vector3 centerWorld, Vector3 worldSize, float angle, float strength =0)
    {
        if(_cutterMaterial == null || stampTex==null) return; 
        
        strength = strength is <= 0 or > 1? defaultStrength : strength;

        Vector3 centerUV = WorldToUV(centerWorld);
        Vector2 sizeUV = new Vector2(worldSize.x / _surfaceWidth, worldSize.y / _surfaceLength);
        Vector2 invSizeUV = new Vector2(1f/Mathf.Max(sizeUV.x,0),1f/Mathf.Max(sizeUV.y,0));
        
        float rad = angle*Mathf.Deg2Rad;
        Vector2 axisU = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        Vector2 axisV =  new Vector2(-axisU.y, axisU.x);
        
        _cutterMaterial.SetFloat("_UseSweep", 0);
        _cutterMaterial.SetTexture("_StampTex",stampTex);
        _cutterMaterial.SetVector("_StampCenterUV",centerUV);
        _cutterMaterial.SetVector("_StampAxisU", axisU);
        _cutterMaterial.SetVector("_StampAxisV", axisV);
        _cutterMaterial.SetVector("_StampInvSizeUV", invSizeUV);
        _cutterMaterial.SetFloat("_Strength", Mathf.Clamp01(strength));
        
        BlitToMask();
    }

    private void BlitToMask()
    {
        RenderTexture temp = RenderTexture.GetTemporary(_cutMask.width, _cutMask.height, 0, _cutMask.format);
        Graphics.Blit(_cutMask,temp);
        _cutterMaterial.SetTexture("_MainTex",temp);
        Graphics.Blit(temp,_cutMask,_cutterMaterial);
        RenderTexture.ReleaseTemporary(temp);

    }
    
}