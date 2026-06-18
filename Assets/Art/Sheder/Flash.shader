Shader "Custom/FlashEffect"
{
    Properties
    {
        [HDR] _FlashColor   ("Flash Color",      Color)       = (1,1,1,1)
        _FlashStrength      ("Flash Strength",   Float)       = 5
        _BandCount          ("Band Count",       Float)       = 8
        _BandWidth          ("Band Width",       Range(0,1))  = 0.15
        _ScrollSpeed        ("Scroll Speed",     Float)       = 2
        _NoiseScale         ("Noise Scale",      Float)       = 8
        _NoiseStrength      ("Noise Strength",   Float)       = 0.15
        _FlashStartTime     ("Flash Start Time", Float)       = -999
        _FlashDuration      ("Flash Duration",   Float)       = 0.2
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Transparent"
        }

        Blend One One
        ZWrite Off
        ZTest LEqual

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _FlashColor;
                float  _FlashStrength;
                float  _BandCount;
                float  _BandWidth;
                float  _ScrollSpeed;
                float  _NoiseScale;
                float  _NoiseStrength;
                float  _FlashStartTime;
                float  _FlashDuration;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            float hash21(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float distortY(float3 posOS, float noiseScale, float noiseStrength, float time)
            {
                // 20fps でバチバチ切り替え
                float t = floor(time * 20.0);

                // X座標を2つの整数セルで挟む
                float xScaled = posOS.x * noiseScale;
                float x0 = floor(xScaled);
                float x1 = x0 + 1.0;

                // 両端のランダム値
                float r0 = hash21(float2(x0, t));
                float r1 = hash21(float2(x1, t));

                // smoothstepで滑らかに補間
                float blend = smoothstep(0.0, 1.0, frac(xScaled));
                float offsetY = lerp(r0, r1, blend);

                // -0.5～0.5 に正規化してnoiseStrength倍
                offsetY = (offsetY - 0.5) * 2.0 * noiseStrength;

                return posOS.y + offsetY;
            }

            float randomBandMask(float y, float bandCount, float baseWidth, float time)
            {
                float t = floor(time * 8.0);

                float scaled = y * bandCount;
                float cellID = floor(scaled);

                float accumOffset = 0.0;

                [unroll]
                for (int i = 0; i < 32; i++)
                {
                    accumOffset += hash11(float(i) * 7.13 + t) * 0.8;

                    if (float(i) >= cellID)
                        break;
                }

                float cellFrac =
                    frac(scaled + accumOffset);

                float randWidth =
                    baseWidth *
                    (0.3 + hash11(cellID + t) * 1.4);

                randWidth =
                    clamp(randWidth, 0.01, 0.49);

                float center =
                    abs(cellFrac - 0.5);

                return smoothstep(
                    randWidth,
                    0.0,
                    center);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionCS =
                    TransformObjectToHClip(IN.positionOS.xyz);

                OUT.positionOS =
                    IN.positionOS.xyz;

                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float elapsed =
                    _Time.y - _FlashStartTime;

                float active =
                    step(0.0, elapsed) *
                    step(elapsed, _FlashDuration);

                float life =
                    saturate(elapsed / _FlashDuration);

                float fade =
                    (1.0 - life) *
                    (1.0 - life);

                float distortedY =
                    distortY(
                        IN.positionOS,
                        _NoiseScale,
                        _NoiseStrength,
                        _Time.y);

                float scrollY =
                    distortedY +
                    elapsed * _ScrollSpeed;

                float bandMask =
                    randomBandMask(
                        scrollY,
                        _BandCount,
                        _BandWidth,
                        _Time.y);

                float flash =
                    bandMask *
                    fade *
                    _FlashStrength *
                    active;

                float3 emission =
                    _FlashColor.rgb * flash;

                return float4(emission, 1.0);
            }

            ENDHLSL
        }
    }
}
