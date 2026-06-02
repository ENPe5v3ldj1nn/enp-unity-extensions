Shader "UI/RoundedShapeSDF"
{
    Properties
    {
        [PerRendererData] _MainTex ("Ramp (256x2)", 2D) = "white" {}
        _EdgeSoftness ("Edge Softness", Range(0.5, 3.0)) = 1.0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            sampler2D _MainTex;
            float4 _ClipRect;
            float _EdgeSoftness;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float4 texcoord : TEXCOORD0;
                float4 texcoord1 : TEXCOORD1;
                float4 texcoord2 : TEXCOORD2;
                float4 texcoord3 : TEXCOORD3;
                float4 tangent : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float3 local : TEXCOORD0;
                float4 params0 : TEXCOORD1;
                float4 gradDirs : TEXCOORD2;
                float4 shadowParams : TEXCOORD3;
                float4 shadowColor : TEXCOORD4;
                float4 worldPosition : TEXCOORD5;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float sdRoundRect(float2 p, float2 halfSize, float radius)
            {
                float2 q = abs(p) - (halfSize - radius);
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            float sdEllipseApprox(float2 p, float2 halfSize)
            {
                float2 q = p / max(halfSize, float2(1e-4, 1e-4));
                return (length(q) - 1.0) * min(halfSize.x, halfSize.y);
            }

            float sdShape(float2 p, float2 halfSize, float radius, float shapeType)
            {
                if (shapeType < 0.5) return sdRoundRect(p, halfSize, radius);
                return sdEllipseApprox(p, halfSize);
            }

            fixed4 SampleRamp(float t, float rowV)
            {
                return tex2D(_MainTex, float2(t, rowV));
            }

            float GradientT(float2 p, float2 halfSize, float2 dir)
            {
                float2 d = normalize(dir);
                float extent = abs(d.x) * halfSize.x + abs(d.y) * halfSize.y;
                if (extent < 1e-5) return 0.5;
                return saturate((dot(p, d) + extent) / (2.0 * extent));
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.shadowParams = v.texcoord;
                o.local = float3(v.texcoord1.xy, v.texcoord1.z);
                o.params0 = v.texcoord2;
                o.gradDirs = v.texcoord3;
                o.shadowColor = v.tangent;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 halfSize = i.params0.xy;
                float radius = min(i.params0.z, min(halfSize.x, halfSize.y));
                float border = max(0.0, i.params0.w);
                float shapeType = i.local.z;

                float2 p = i.local.xy;

                float fillAngleDeg = i.gradDirs.x + i.gradDirs.y * _Time.y;
                float borderAngleDeg = i.gradDirs.z + i.gradDirs.w * _Time.y;
                float fillRad = radians(fillAngleDeg);
                float borderRad = radians(borderAngleDeg);
                float2 fillDir = float2(cos(fillRad), sin(fillRad));
                float2 borderDir = float2(cos(borderRad), sin(borderRad));
                float tFill = GradientT(p, halfSize, fillDir);
                float tBorder = GradientT(p, halfSize, borderDir);

                fixed4 fillS = SampleRamp(tFill, 0.25);
                fixed4 borderS = SampleRamp(tBorder, 0.75);

                fixed3 fillRGB = fillS.rgb * i.color.rgb;
                fixed3 borderRGB = borderS.rgb * i.color.rgb;

                float d = sdShape(p, halfSize, radius, shapeType);
                float aa = max(fwidth(d) * _EdgeSoftness, 1e-4);
                float inside = saturate(0.5 - d / aa);

                float borderMask = 0.0;
                if (border > 0.0001)
                {
                    float insideDist = -d;
                    borderMask = saturate(0.5 - (insideDist - border) / aa);
                    borderMask *= inside;
                }

                float fillMask = inside - borderMask;

                float Afill = fillMask * fillS.a * i.color.a;
                float Aborder = borderMask * borderS.a * i.color.a;
                float Ashape = Afill + Aborder;

                fixed3 PMshape = fillRGB * Afill + borderRGB * Aborder;

                float2 shOffset = i.shadowParams.xy;
                float shBlur = max(0.0, i.shadowParams.z);
                float shSpread = i.shadowParams.w;

                float Sa = i.shadowColor.a;
                fixed3 Cs = i.shadowColor.rgb;

                float Ashadow = 0.0;

                if (Sa > 0.0001 && (shBlur > 0.0001 || shSpread > 0.0001 || abs(shOffset.x) > 0.0001 || abs(shOffset.y) > 0.0001))
                {
                    float ds = sdShape(p - shOffset, halfSize, radius, shapeType) - shSpread;
                    float aaS = max(fwidth(ds) * _EdgeSoftness, 1e-4);
                    aaS = max(aaS, shBlur);
                    float sInside = saturate(0.5 - ds / aaS);
                    Ashadow = sInside * Sa * i.color.a;
                }

                fixed3 PMshadow = Cs * Ashadow;

                float outA = Ashape + Ashadow * (1.0 - Ashape);
                fixed3 outPM = PMshape + PMshadow * (1.0 - Ashape);

                fixed3 outRGB = (outA > 1e-5) ? (outPM / outA) : 0;
                fixed4 res = fixed4(outRGB, outA);

                #ifdef UNITY_UI_CLIP_RECT
                res.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(res.a - 0.001);
                #endif

                return res;
            }
            ENDCG
        }
    }
}
