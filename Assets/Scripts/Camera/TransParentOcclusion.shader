Shader "Custom/TransparentOcclusion"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        _BaseColor("Color", Color) = (1, 1, 1, 1)
        _Alpha("Alpha", Range(0, 1)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                #include "UnityCG.cginc"

                struct Attributes
                {
                    float4 positionOS : POSITION;
                    float2 uv : TEXCOORD0;
                    float3 normalOS : NORMAL;
                };

                struct Varyings
                {
                    float4 positionCS : SV_POSITION;
                    float2 uv : TEXCOORD0;
                };

                sampler2D _BaseMap;
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Alpha;

                Varyings vert(Attributes input)
                {
                    Varyings output;
                    output.positionCS = UnityObjectToClipPos(input.positionOS);
                    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                    return output;
                }

                float4 frag(Varyings input) : SV_Target
                {
                    float4 texColor = tex2D(_BaseMap, input.uv);
                    float4 finalColor = texColor * _BaseColor;
                    finalColor.a = finalColor.a * _Alpha;
                    return finalColor;
                }
            ENDHLSL
        }
    }

    Fallback "Transparent/VertexLit"
}
