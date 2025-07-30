Shader "Unlit/GrassShader"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.2,1,0.2,1)
        _ColorMap("ColorMap",2D) = "white"{}
        _CutMask("Cut Mask",2D) = "black"{}
        _GrassHeight("Grass Height", Float) = 0.3
        _WindSpeed("Wind Speed", Float) = 1.0
        _WindStrength("Wind Strength", Float) = 0.1
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque" "Queue"="Geometry"
        }
        LOD 200
        Pass
        {
            Name "GrassPass"
            Tags
            {
                "LightMode"="UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS :POSITION;
                float2 uv :TEXCOORD0;
                float3 normalOS :NORMAL;
            };

            struct Varyings
            {
                float4 positionOS :SV_POSITION;
                float2 uv :TEXCOORD0;
                float3 worldNormal :TEXCOORD1;
                float3 worldPos :TEXCOORD2;
            };

            
            sampler2D _ColorMap;
            sampler2D _CutMask;

            float _GrassHeight;
            float _WindSpeed;
            float _WindStrength;
            float4 _BaseColor;

            float4x4 _ObjectToWorld;
            float4x4 _WorldToObject;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float2 uv = IN.uv;

                float cutValue = tex2Dlod(_CutMask, float4(uv, 0, 0)).r;
                float heightFactor = saturate(1.0 - cutValue);

                float wave = sin(uv.x * 4.0 + _Time.y * _WindSpeed) + cos(uv.y * 4.0 + _Time.y * _WindSpeed);

                wave *= 0.5 * _WindStrength;


                float3 displacedPos = IN.positionOS.xyz;
                displacedPos.y = heightFactor*_GrassHeight+wave;
                
                float3 worldPos = mul(_ObjectToWorld, float4(displacedPos,1)).xyz;

                OUT.positionOS = TransformWorldToHClip(worldPos);
                OUT.uv = uv;
                OUT.worldNormal = normalize(mul((float3x3)_ObjectToWorld, IN.normalOS));
                OUT.worldPos = worldPos;

                return OUT;
            }

            float4 frag(Varyings IN):SV_Target
            {
                float4 texColor = tex2D(_ColorMap, IN.uv);
                float4 color = texColor * _BaseColor;
                return color;
            }
            ENDHLSL
        }
    }

    Fallback "Hidden/InternalErrorShader"
}