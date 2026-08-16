// Unlit particle shader for the Phoenix Flame layers. Output is premultiplied by alpha, so the
// same fragment maths serves both blend modes: One/One adds the flame, One/OneMinusSrcAlpha lays
// the smoke over it. The pass carries no LightMode tag, which is what puts it in the 2D renderer's
// unlit queue — the same place URP's own Sprite-Unlit shader lands.
Shader "SoftGames/Flame Particle"
{
    Properties
    {
        [MainTexture] _MainTex ("Texture", 2D) = "white" {}
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination blend", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Blend [_SrcBlend] [_DstBlend]
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // Every material property belongs in here, blend modes included, or the shader drops
            // out of SRP batching.
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _SrcBlend;
                float _DstBlend;
            CBUFFER_END

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                half4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half alpha = texel.a * input.color.a;
                return half4(texel.rgb * input.color.rgb * alpha, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
