Shader "UI/FrostedGlassBlur"
{
    Properties
    {
        _BlurSize("Blur Intensity", Range(0, 20)) = 5
        _Color("Tint Color", Color) = (0.1, 0.1, 0.1, 0.6)
        [HideInInspector] _MainTex ("Sprite Texture (Canvas Reserve)", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            sampler2D _CameraOpaqueTexture; 
            float4 _CameraOpaqueTexture_TexelSize;
            float _BlurSize;
            float4 _Color;

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float4 blurAccumulation = float4(0, 0, 0, 0);

                float offsetScale = _BlurSize * 0.0005;
                blurAccumulation += tex2D(_CameraOpaqueTexture, screenUV + float2(-offsetScale, -offsetScale));
                blurAccumulation += tex2D(_CameraOpaqueTexture, screenUV + float2(offsetScale, -offsetScale));
                blurAccumulation += tex2D(_CameraOpaqueTexture, screenUV + float2(-offsetScale, offsetScale));
                blurAccumulation += tex2D(_CameraOpaqueTexture, screenUV + float2(offsetScale, offsetScale));
                blurAccumulation += tex2D(_CameraOpaqueTexture, screenUV) * 2.0;
                
                float4 blurredWorld = blurAccumulation / 6.0;

                return lerp(blurredWorld, _Color, _Color.a);
            }
            ENDCG
        }
    }
}