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
        [Space(6)]
        _MidColorBorder ("Middle Color Broder", Range(0,1)) = 0
        _TopColorBorder ("Top Color Border", Range(0,1)) = 1
        _ColorVariation ("Color Height Variation", Range(0,0.5)) = 0.1

        [Space(6)]
        [Header(Field Texture)]
        [Space(6)]
        _FieldTex("Field Texture", 2D) = "white"{}
        _FieldAlphaCutoff("Field Alpha Cutoff",Range(0,1)) = 0.1

        [Space(10)]
        [Header(Wind Settins)]
        [Space(6)]
        _WindSpeed("Wind Speed", Range(0,1)) = 0.1
        _WindFrequency("Wind Frequency", Float) = 2

        [Space(10)]
        [Header(Noise Settings)]
        [Space(6)]
        _NoiseAmount("Noise Amount", Range(0,1)) = 0.05
        _NoiseFrequency("NoiseFrequency",Float) = 2

        [Space(10)]
        [Header(Push Wind Settings)]
        [Space(6)]
        _PushWidthUV("Push Width",Range(0,1)) = 0.1
        _PushAmountWorld("Push Strength", Range(0,1)) = 0.05

        [Space(10)]
        [Header(Cut Mask Settings)]
        [Space(6)]
        _CutThreshold("Cut Threshold", Range(0,1)) = 0.5
        _CutMinHeight("Cut Min Height", Range(0,1)) = 0.3
        _GrassHeight("Grass Height Y",Float) = 0.33


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
                float baseWorldY : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 _TopColor;
            float4 _MidColor;
            float4 _BotColor;
            float _MidColorBorder;
            float _TopColorBorder;
            float _ColorVariation;

            TEXTURE2D(_FieldTex);
            SAMPLER(sampler_FieldTex);
            float4 _FieldTex_TexelSize;
            float _FieldAlphaCutoff;

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
            float _CutMinHeight;
            float _GrassHeight;

            float2 _PushCenterUV;
            float _PushRadiusUV;
            float _PushWidthUV;
            float _PushAmountWorld;


            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }


            float2 windOutline(float3 worldPos, float wave, float windNormy, float2 totalOffset)
            {
                float2 fieldUV;
                fieldUV.x = (worldPos.x - _SurfaceOriginX) / _SurfaceWidth;
                fieldUV.y = (worldPos.z - _SurfaceOriginZ) / _SurfaceLength;

                float2 toC = fieldUV - _PushCenterUV;
                float d = length(toC);

                float outside = step(_PushRadiusUV, d);
                float t = saturate((d - _PushRadiusUV) / max(_PushWidthUV, 1e-6));
                float band = 1.0 - t;
                band = smoothstep(0.0, 1.0, band) * outside;

                float2 dirUV = (d > 1e-6) ? toC / d : float2(0, 0);
                float2 radialObj = normalize(float2(dirUV.x * _SurfaceWidth, dirUV.y * _SurfaceLength));
                float2 tangObj = float2(-radialObj.y, radialObj.x);

                float pushBiasAmt = _PushAmountWorld * band * windNormy;
                float2 pushBias = radialObj * pushBiasAmt;

                float radialWind = dot(totalOffset, radialObj);
                float tangWind = dot(totalOffset, tangObj);

                radialWind = max(0.0, radialWind) * band + radialWind * (1.0 - band);

                float2 windRebased = radialObj*radialWind+tangObj*tangWind;

                return pushBias + windRebased;
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
                OUT.baseWorldY = y;

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
                float topRand = (hash21(uv + 87.42) * 2.0 - 1.0) * _ColorVariation;
                OUT.randomTop = saturate(_TopColorBorder + topRand);

                float2 totalOffset = dir * (globalWindOffset + noiseOffset);

                float2 offsetWorld = windOutline(worldPos, wave, windNormY,totalOffset);


                IN.positionOS.xz += offsetWorld;

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.height = colorNormY;
                return OUT;
            }


            float4 HeightGradientColor(float h, float mid, float topRand)
            {
                if (h < mid)
                    return lerp(_BotColor, _MidColor, h / max(mid, 0.001));

                return lerp(_MidColor, _TopColor, (h - mid) / max(topRand - mid, 0.001));
            }

            float4 FieldTexColor(float2 uv)
            {
                uv = saturate(uv);

                return SAMPLE_TEXTURE2D(_FieldTex, sampler_FieldTex, uv);
            }

            float4 frag(Varyings IN) :SV_Target
            {
                //---UV MASK WORLD SPACE---
                float2 uv;
                uv.x = (IN.worldUV.x - _SurfaceOriginX) / _SurfaceWidth;
                uv.y = (IN.worldUV.y - _SurfaceOriginZ) / _SurfaceLength;
                uv = clamp(uv, 0, 1);

                float mask = SAMPLE_TEXTURE2D(_CutMask, sampler_CutMask, uv).r;

                float normY = IN.baseWorldY / _GrassHeight;

                if (mask < _CutThreshold)
                {
                    clip(_CutMinHeight - normY);
                }

                //---COLOR INTERPOLATION---
                float4 heightCol = HeightGradientColor(IN.height, _MidColorBorder, IN.randomTop);

                float hasFieldTex = (_FieldTex_TexelSize.z > 1.0) && (_FieldTex_TexelSize.w > 1.0) ? 1.0 : 0.0;

                if (hasFieldTex > 0.5)
                {
                    float4 texCol = FieldTexColor(uv);
                    float alpha = texCol.a;

                    return lerp(heightCol, texCol, alpha);
                }
                return heightCol;
            }
            ENDHLSL
        }
    }

    Fallback "Hidden/InternalErrorShader"
}