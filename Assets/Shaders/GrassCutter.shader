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
            
            float2 _Center;
            float _Radius;

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

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

            float4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                
                float dist = distance(uv, _Center);
                float cutCircle = 1- smoothstep(_Radius *0.8,_Radius,dist);
                
                float current = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv).r;
                float newValue = min(current,1-cutCircle);
                
                return float4(newValue,0,0,1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}