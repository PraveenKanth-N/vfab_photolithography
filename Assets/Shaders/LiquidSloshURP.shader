Shader "Custom/LiquidSloshURP"
{
    Properties
    {
        _BaseColor ("Liquid Color", Color) = (0.0, 0.6, 0.8, 0.6)
        _WobbleX ("Wobble Delta X", Float) = 0.0
        _WobbleZ ("Wobble Delta Z", Float) = 0.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ForwardLit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 normalWS     : TEXCOORD0;
            };

            float4 _BaseColor;
            float _WobbleX;
            float _WobbleZ;

            Varyings vert(Attributes input)
            {
                Varyings output;
                float4 vPos = input.positionOS;

                // ABSOLUTE TILTING LINE: No conditions, no cutoff checks.
                // It tilts the vertex height directly based on its distance from the local X/Z center.
                vPos.y += (vPos.x * _WobbleX) + (vPos.z * _WobbleZ);

                output.positionCS = TransformObjectToHClip(vPos.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float ndotl = saturate(dot(normal, _MainLightPosition.xyz));
                float4 finalColor = _BaseColor;
                finalColor.rgb *= (ndotl * 0.3 + 0.7);
                return finalColor;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}