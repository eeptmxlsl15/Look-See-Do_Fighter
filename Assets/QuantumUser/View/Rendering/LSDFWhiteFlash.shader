Shader "LSDF/Sprite White Flash" {
  Properties {
    [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
    _FlashOpacity ("Flash Opacity", Range(0, 1)) = 0
  }
  SubShader {
    Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True" }
    Cull Off
    ZWrite Off
    Blend SrcAlpha OneMinusSrcAlpha
    Pass {
      Tags { "LightMode"="SRPDefaultUnlit" }
      HLSLPROGRAM
      #pragma vertex Vert
      #pragma fragment Frag
      #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
      struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; };
      struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
      TEXTURE2D(_MainTex);
      SAMPLER(sampler_MainTex);
      CBUFFER_START(UnityPerMaterial)
        half _FlashOpacity;
      CBUFFER_END
      Varyings Vert(Attributes input) {
        Varyings output;
        output.positionCS = TransformObjectToHClip(input.positionOS);
        output.uv = input.uv;
        return output;
      }
      half4 Frag(Varyings input) : SV_Target {
        half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
        return half4(1, 1, 1, alpha * _FlashOpacity);
      }
      ENDHLSL
    }
  }
}
