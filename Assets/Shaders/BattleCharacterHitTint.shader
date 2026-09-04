Shader "ProjectGuilt/Battle/Character Hit Tint"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1, 1, 1, 1)
        _HitTintColor ("Hit Tint Color", Color) = (1, 0.15, 0.15, 1)
        _HitTintStrength ("Hit Tint Strength", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }

        Pass
        {
            Name "BattleCharacterHitTint"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _HitTintColor;
                half _HitTintStrength;
            CBUFFER_END

            half4 _RendererColor;

            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                SetUpSpriteInstanceProperties();

                Varyings output;
                output.positionHCS = TransformObjectToHClip(
                    input.positionOS.xyz
                );
                output.uv = input.uv;
                output.color = input.color * _Color *
                    _RendererColor * unity_SpriteColor;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 baseColor = SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    input.uv
                ) * input.color;
                half luminance = dot(
                    baseColor.rgb,
                    half3(0.299h, 0.587h, 0.114h)
                );
                half3 redizedRgb = baseColor.rgb * 0.35h +
                    _HitTintColor.rgb * luminance * 0.65h;
                half3 finalRgb = lerp(
                    baseColor.rgb,
                    redizedRgb,
                    saturate(_HitTintStrength)
                );
                return half4(finalRgb, baseColor.a);
            }
            ENDHLSL
        }
    }
}
