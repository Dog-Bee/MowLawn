Shader "Unlit/GrassShader"
{
    Properties
    {
        _TopColor("Top Color", Color) = (0.2,1,0.2,1)
        _MidColor("Mid Color", Color) = (0.2,1,0.2,1)
        _BotColor("Bot Color", Color) = (0.2,1,0.2,1)
        
        _MinY ("Min Y (gradient)", Range(0,1)) = 0
        _MaxY ("Max Y (gradient)", Range(0,1)) = 1
        
        
        _WindSpeed("Wind Speed", Float) = 0.1
        _WindFrequency("Wind Frequency", Float) = 2
        
        _ColorMap("ColorMap",2D) = "white"{}
        _CutMask("Cut Mask",2D) = "black"{}
        _GrassHeight("Grass Height", Float) = 0.3
        
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
                UNITY_VERTEX_INPUT_INSTANCE_ID

            };

            float4 _TopColor;
            float4 _MidColor;
            float4 _BotColor;

            float _WindSpeed;
            float _WindFrequency;
            float _MinY;
            float _MaxY;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN,OUT);

                float y = IN.positionOS.y;
                float normY = saturate((y-_MinY)/(_MaxY-_MinY));
                float3 worldPos = TransformObjectToWorld(IN.positionOS).xyz;

                float windOffset = sin(_Time.y * _WindFrequency+worldPos.x*1.5+worldPos.z*1.5)+cos(worldPos.x*0.7+_Time.y*0.5)*0.5;
                
                IN.positionOS.x += windOffset*_WindSpeed*normY;
                
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.height = normY;
                return OUT;
            }

            float4 frag(Varyings IN) :SV_Target
            {
                float h = IN.height;
                float4 col = h<0.5? lerp(_BotColor,_MidColor,h*2):lerp(_MidColor,_TopColor,(h-0.5)*2);
                return col;
                
            }            
           
            ENDHLSL
        }
    }

    Fallback "Hidden/InternalErrorShader"
}