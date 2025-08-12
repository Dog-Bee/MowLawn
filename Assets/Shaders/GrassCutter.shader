Shader "Custom/GrassCutter"
{
    Properties {}
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque" "Queue"="Overlay"
        }
        Pass
        {
            Name "GrassCutter"



            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_StampTex);
            SAMPLER(sampler_StampTex);

            float4 _MainTex_TexelSize;

            float2 _StampCenterUV;
            float2 _PrevStampCenterUV;

            float2 _StampAxisU;
            float2 _StampAxisV;

            float2 _StampSize;
            float2 _StampInvSizeUV;

            float _Strength;
            float _UseSweep;
            
            struct Attributes
            {
                float4 positionOS :POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS :SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);;
                OUT.uv = IN.uv;
                return OUT;
            }
            float4 defaultStamp(float2 uv, float current)
            {
                float2 rel = uv - _StampCenterUV;

                float projU = dot(rel,_StampAxisU) * _StampInvSizeUV.x+0.5;
                float projV = dot(rel,_StampAxisV) * _StampInvSizeUV.y+0.5;
                float2 suv = float2(projU,projV);

                float2 inside  = step(0.0,suv) * step(suv,1.0);
                float insideMask = inside.x * inside.y;

                float4 s = SAMPLE_TEXTURE2D(_StampTex,sampler_StampTex,saturate(suv));
                float stamp = s.a*insideMask;

                float cut = saturate(stamp*_Strength);
                float newValue = min(current,1.0-cut);
                return float4(newValue,0,0,1);
            }

            float4 sweepStamp(float2 uv,float current)
            {
                float2 a = _PrevStampCenterUV;
                float2 b = _StampCenterUV;
                float2 rel = uv-a;

                float projU = dot(rel,_StampAxisU) * _StampInvSizeUV.x;
                float projV = dot(rel,_StampAxisV) * _StampInvSizeUV.y+0.5;
                float2 suv = float2(projU,projV);

                float eps = max(_MainTex_TexelSize.x,_MainTex_TexelSize.y) * 1.5;
                float2 inside  = step(-eps,suv) * step(suv,1.0+eps);
                float insideMask = inside.x * inside.y;

                float4 s = SAMPLE_TEXTURE2D(_StampTex,sampler_StampTex,saturate(suv));
                float stamp = s.a*insideMask;

                float cut = saturate(stamp*_Strength);
                float newValue = min(current,1.0-cut);
                return float4(newValue,0,0,1);
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                float current = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).r;
                
                if (_UseSweep>0.5)
                {
                    return sweepStamp(uv,current);
                }
                
                return defaultStamp(uv,current);

            }
            ENDHLSL
        }
    }
    Fallback Off
}