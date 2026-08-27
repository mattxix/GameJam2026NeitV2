Shader "Custom/DrinkLiquid"
{
    Properties
    {
        [HDR]_Tint      ("Liquid Color", Color) = (0.85, 0.45, 0.05, 0.9)
        [HDR]_TopColor  ("Surface Color", Color) = (1.0, 0.6, 0.15, 1)
        [HDR]_FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
        _FoamWidth   ("Foam Width (m)", Range(0, 0.05)) = 0.008
        _FillAmount  ("Fill Height above pivot (m)", Float) = 0
        _WobbleX     ("Wobble X", Range(-1, 1)) = 0
        _WobbleZ     ("Wobble Z", Range(-1, 1)) = 0
        _Fresnel     ("Edge Brightness", Range(0, 2)) = 0.4
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            // Draw before the glass shell so the glass blends on top of the liquid.
            "Queue"           = "Transparent-100"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Liquid"
            Tags { "LightMode" = "UniversalForward" }

            // Cull Off is REQUIRED: the back faces are what fake the flat liquid surface.
            Cull Off
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _Tint;
                half4  _TopColor;
                half4  _FoamColor;
                float  _FoamWidth;
                float  _FillAmount;
                float  _WobbleX;
                float  _WobbleZ;
                half   _Fresnel;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 viewDirWS  : TEXCOORD1;
                // Signed distance from this fragment to the liquid surface plane.
                // Positive = above the surface (gets clipped away).
                float  fillEdge   : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(positionWS);
                o.normalWS   = TransformObjectToWorldNormal(v.normalOS);
                o.viewDirWS  = GetWorldSpaceViewDir(positionWS);

                // Position relative to the object's pivot, but still in WORLD axes.
                // This is the whole trick: the offset ignores the glass's rotation,
                // so the cut plane stays horizontal no matter how the glass is tilted.
                float3 pivotWS = TransformObjectToWorld(float3(0, 0, 0));
                float3 offset  = positionWS - pivotWS;

                // Tilting the plane slightly by the wobble values gives the slosh.
                o.fillEdge = offset.y
                           + offset.x * _WobbleX
                           + offset.z * _WobbleZ
                           - _FillAmount;

                return o;
            }

            half4 frag(Varyings i, FRONT_FACE_TYPE facing : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // Everything above the liquid line disappears.
                clip(-i.fillEdge);

                half isFront = IS_FRONT_VFACE(facing, 1.0h, 0.0h);

                // Back faces are the inside of the far wall / bottom. Because they're
                // clipped at the same plane, painting them flat reads as a liquid surface.
                if (isFront < 0.5h)
                {
                    return half4(_TopColor.rgb, 1.0h);
                }

                // Bright band right at the waterline.
                half depth = -i.fillEdge;
                half foam  = 1.0h - saturate(depth / max(_FoamWidth, 1e-4h));

                half3 normalWS  = normalize(i.normalWS);
                half3 viewDirWS = normalize(i.viewDirWS);
                half  fres      = pow(1.0h - saturate(dot(normalWS, viewDirWS)), 3.0h);

                half3 col = lerp(_Tint.rgb, _FoamColor.rgb, foam);
                col += fres * _Fresnel * _Tint.rgb;

                half alpha = saturate(lerp(_Tint.a, 1.0h, max(foam, fres * _Fresnel)));
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
