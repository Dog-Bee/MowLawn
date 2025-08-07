Shader "Unlit/GrassShader"
{
    Properties
    {
        [Header(Colors)]
        _TopColor("Top Color", Color) = (0.2,1,0.2,1)
        _MidColor("Mid Color", Color) = (0.2,1,0.2,1)
        _BotColor("Bot Color", Color) = (0.2,1,0.2,1)

        [Space(10)]
        [Header(Color Gradient Range)]
        [Space(10)]
        _MidColorBorder ("Middle Color Broder", Range(0,1)) = 0
        _TopColorBorder ("Top Color Border", Range(0,1)) = 1
        _ColorVariation ("Color Height Variation", Range(0,0.5)) = 0.1

        [Space(10)]
        [Header(Wind Settins)]
        [Space(10)]
        _WindSpeed("Wind Speed", Range(0,1)) = 0.1
        _WindFrequency("Wind Frequency", Float) = 2

        [Space(10)]
        [Header(Noise Settings)]
        [Space(10)]
        _NoiseAmount("Noise Amount", Range(0,1)) = 0.05
        _NoiseFrequency("NoiseFrequency",Float) = 2

        [Space(10)]
        [Header(Cut Mask Settings)]
        [Space(10)]
        _CutMask("Cut Mask",2D) = "black"{}
        _CutThreshold("Cut Threshold", Range(0,1)) = 0.5
        

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
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float height : TEXCOORD0;
                float randomTop : TEXCOORD1;
                float2 worldUV : TEXCOORD2;
                float3 worldPos : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 _TopColor;
            float4 _MidColor;
            float4 _BotColor;
            float _MidColorBorder;
            float _TopColorBorder;
            float _ColorVariation;
            
            float _WindSpeed;
            float _WindFrequency;

            float _NoiseAmount;
            float _NoiseFrequency;
            
            float _WindMinY;
            float _WindMaxY;

            float _SurfaceOriginX;
            float _SurfaceOriginZ;
            float _SurfaceWidth;
            float _SurfaceLength;

            TEXTURE2D(_CutMask);
            SAMPLER(sampler_CutMask);
            
            float _CutThreshold;
            float2 _CutMaskTiling;
            
            

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float y = IN.positionOS.y;
                float colorNormY = saturate((y - _MidColorBorder) / (_TopColorBorder - _MidColorBorder));
                float windNormY = saturate(y);

                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                
                OUT.worldPos = worldPos;
                OUT.worldUV = worldPos.xz;

                float2 uv = worldPos.xz * 0.5;
                

                //---GLOBAL WIND---
                float wave = sin(_Time.y * _WindFrequency + worldPos.x * 0.5f + worldPos.z * 0.5);
                float globalWindOffset = wave * _WindSpeed * windNormY;

                //---NOISE---
                float phase = hash21(uv);
                float amp = hash21(uv + 10);
                float windNoise = sin(_Time.y * _NoiseFrequency + phase * 6.2831);
                float noiseOffset = windNoise * (_NoiseAmount + amp * 0.05) * windNormY;

                float angle = hash21(uv + 23.17) * 6.2831;
                float2 dir = float2(cos(angle), sin(angle));

                //---RANDOM---
                float topRand = (hash21(uv+87.42)*2.0-1.0)*_ColorVariation;
                OUT.randomTop = saturate(_TopColorBorder+topRand);

                float2 totalOffset = dir * (globalWindOffset + noiseOffset);

                

                IN.positionOS.xz += totalOffset;

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.height = colorNormY;
                return OUT;
            }

            float4 frag(Varyings IN) :SV_Target
            {
                float2 uv;
                uv.x = (IN.worldUV.x-_SurfaceOriginX)/_SurfaceWidth;
                uv.y = (IN.worldUV.y-_SurfaceOriginZ)/_SurfaceLength;
                uv = clamp(uv,0,1);
                
                
                float2 cutUV = uv;
                float mask = SAMPLE_TEXTURE2D(_CutMask,sampler_CutMask,cutUV).r;
                clip(mask-_CutThreshold);

                
                float h = IN.height;
                float mid = _MidColorBorder;
                float top = IN.randomTop;
                                
                float4 col;

                if(h<mid)
                {
                    col = lerp(_BotColor,_MidColor,h/max(mid,0.001));
                }
                else
                {
                    col = lerp(_MidColor,_TopColor,(h-mid)/max(top-mid,0.001));
                }

                return col;
            }
            ENDHLSL
        }
    }

    Fallback "Hidden/InternalErrorShader"
}