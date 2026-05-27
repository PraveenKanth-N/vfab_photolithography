Shader "Custom/LiquidSloshURP"
{
    Properties
    {
        _BaseColor ("Liquid Tint Color", Color) = (0.0, 0.5, 0.8, 0.3)
        _WobbleX ("Wobble Delta X", Float) = 0.0
        _WobbleZ ("Wobble Delta Z", Float) = 0.0
        
        [Header(Surface Optics)]
        _Glossiness ("Glossiness / Smoothness", Range(0.0, 1.0)) = 0.92
        _SpecularIntensity ("Specular Intensity", Range(0.0, 5.0)) = 2.5
        
        [Header(Advanced Realism)]
        _RefractionIndex ("Refraction Distortion", Range(0.0, 0.2)) = 0.04
        _FresnelPower ("Fresnel Rim Power", Range(1.0, 5.0)) = 3.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent+1" // Render right after opaque objects are drawn
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 normalWS     : TEXCOORD0;
                float3 viewDirWS    : TEXCOORD1;
                float4 screenPos    : TEXCOORD2; // Captures screen projection coordinates
            };

            float4 _BaseColor;
            float _WobbleX;
            float _WobbleZ;
            float _Glossiness;
            float _SpecularIntensity;
            float _RefractionIndex;
            float _FresnelPower;

            // This texture holds the pre-rendered pixels of everything behind the fluid
            TEXTURE2D(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            Varyings vert(Attributes input)
            {
                Varyings output;
                float4 vPos = input.positionOS;

                // Calibrated wobble math
                vPos.y += (vPos.x * _WobbleX) + (vPos.z * _WobbleZ);

                output.positionCS = TransformObjectToHClip(vPos.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                float3 worldPos = TransformObjectToWorld(vPos.xyz);
                output.viewDirWS = GetWorldSpaceViewDir(worldPos);
                
                // Compute the exact viewport coordinates for background sampling
                output.screenPos = ComputeScreenPos(output.positionCS);
                
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);
                
                Light mainLight = GetMainLight();
                float3 lightDir = mainLight.direction;
                
                // --- 1. REAL-TIME PSEUDO REFRACTION ---
                // Distort screen space UV coordinates using the mesh's normal vectors
                float2 screenUV = input.screenPos.xy / max(input.screenPos.w, 0.001f);
                float2 distortedUV = screenUV + (normal.xz * _RefractionIndex);
                
                // Grab the background pixel color using the distorted UV coordinates
                float3 backgroundScene = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, distortedUV).rgb;

                // --- 2. ADVANCED SURFACE LIGHTING (Specular) ---
                float ndotl = saturate(dot(normal, lightDir));
                float3 diffuseTint = _BaseColor.rgb * (ndotl * 0.4 + 0.6);

                float3 halfDir = normalize(lightDir + viewDir);
                float ndoth = saturate(dot(normal, halfDir));
                float specPower = exp2(_Glossiness * 10.0 + 1.0);
                float specularFactor = pow(ndoth, specPower) * _SpecularIntensity;
                float3 specularComponent = mainLight.color * specularFactor;

                // --- 3. THE FRESNEL EFFECT ---
                // Real liquid reflects more light at shallow viewing angles (edges)
                float fresnel = pow(1.0 - saturate(dot(normal, viewDir)), _FresnelPower);

                // --- 4. FINAL COMPOSITING ---
                float4 finalColor;
                // Blend the warped background with our chemical dye color
                float3 waterBase = lerp(backgroundScene, diffuseTint, _BaseColor.a);
                
                // Add the specular reflections and edge fresnel highlights on top
                finalColor.rgb = waterBase + specularComponent + (fresnel * 0.4 * mainLight.color);
                finalColor.a = 1.0; // The pixel itself is solid because it contains the background!
                
                return finalColor;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}