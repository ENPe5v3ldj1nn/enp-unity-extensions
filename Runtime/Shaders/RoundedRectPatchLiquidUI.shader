Shader "UI/ENP/RoundedRectPatchLiquid"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BaseColorA ("Base Color A", Color) = (0.2235,0.6549,1,1)
        _BaseColorB ("Base Color B", Color) = (0.4118,0.3569,1,1)
        _AccentColor ("Accent Color", Color) = (0.8431,0.9333,1,1)
        _Intensity ("Global Intensity", Range(0,2)) = 1
        _CenterClear ("Center Clear", Range(0,1)) = 0.58
        _Roundness ("Roundness", Float) = 16
        _AmbientIntensity ("Ambient Base Layer Intensity", Range(0,1)) = 0.12
        _PatchShapeInfluence ("Patch Shape Influence", Range(0,2)) = 1
        _PatchColorInfluence ("Patch Color Influence", Range(0,2)) = 1
        _BottomDominance ("Bottom Dominance", Range(0,2)) = 1.35
        _SideSupport ("Side Support", Range(0,2)) = 0.42
        _TopSuppression ("Top Suppression", Range(0,1)) = 0.97
        _BottomToSideBleed ("Bottom To Side Bleed", Range(0,1)) = 0.14
        _CornerBleedAttenuation ("Corner Bleed Attenuation", Range(0,1)) = 0.78
        _BottomThicknessMultiplier ("Bottom Thickness Multiplier", Range(0,2)) = 0.84
        _SideThicknessMultiplier ("Side Thickness Multiplier", Range(0,2)) = 0.46
        _TimeSeed ("Time Seed", Float) = 0
        _PatchCount ("Patch Count", Float) = 0
        _RectSize ("Rect Size", Vector) = (100,100,0,0)
        _RectCenter ("Rect Center", Vector) = (0,0,0,0)

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
            Name "RoundedRectPatchLiquid"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #define MAX_PATCHES 9

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float2 localPos : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _BaseColorA;
            fixed4 _BaseColorB;
            fixed4 _AccentColor;
            float _Intensity;
            float _CenterClear;
            float _Roundness;
            float _AmbientIntensity;
            float _PatchShapeInfluence;
            float _PatchColorInfluence;
            float _BottomDominance;
            float _SideSupport;
            float _TopSuppression;
            float _BottomToSideBleed;
            float _CornerBleedAttenuation;
            float _BottomThicknessMultiplier;
            float _SideThicknessMultiplier;
            float _TimeSeed;
            float _PatchCount;
            float4 _RectSize;
            float4 _RectCenter;
            float4 _PatchA[MAX_PATCHES];
            float4 _PatchB[MAX_PATCHES];
            float4 _ClipRect;

            float SdRoundedBox(float2 p, float2 halfSize, float radius)
            {
                float2 q = abs(p) - halfSize + radius;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float PatchFalloff(float alongDistance, float inward, float width, float depth)
            {
                float x = alongDistance / max(width, 0.0001);
                float y = inward / max(depth, 0.0001);
                float field = exp(-(x * x * 2.15 + y * y * 1.35));
                float body = smoothstep(0.015, 0.92, field);
                return field * body;
            }

            float CircularDistance01(float a, float b)
            {
                float d = abs(a - b);
                return min(d, 1.0 - d);
            }

            float SideDistance01(float a, float b)
            {
                return abs(a - b);
            }

            float CornerQuiet(float2 uv)
            {
                float left = smoothstep(0.0, lerp(0.12, 0.24, _CornerBleedAttenuation), uv.x);
                float right = smoothstep(0.0, lerp(0.12, 0.24, _CornerBleedAttenuation), 1.0 - uv.x);
                float bottom = smoothstep(0.0, lerp(0.1, 0.22, _CornerBleedAttenuation), uv.y);
                float top = smoothstep(0.0, 0.1, 1.0 - uv.y);
                return max(min(left, right), min(bottom, top));
            }

            float LowerCornerAttenuation(float2 uv)
            {
                float sideProximity = 1.0 - smoothstep(0.02, 0.24, min(uv.x, 1.0 - uv.x));
                float lowerProximity = 1.0 - smoothstep(0.02, 0.32, uv.y);
                float cornerLoad = sideProximity * lowerProximity;
                return lerp(1.0, 1.0 - cornerLoad * 0.88, _CornerBleedAttenuation);
            }

            float LowerSideTaper(float2 uv)
            {
                float lower = smoothstep(0.04, 0.42, uv.y);
                float taper = lerp(0.34, 1.0, lower);
                return lerp(taper, 1.0, _BottomToSideBleed);
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.localPos = v.vertex.xy;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 rectSize = max(_RectSize.xy, float2(1.0, 1.0));
                float2 halfSize = rectSize * 0.5;
                float2 p = i.localPos - _RectCenter.xy;
                float2 uv = saturate((p + halfSize) / rectSize);

                float radius = min(max(_Roundness, 0.0), max(min(halfSize.x, halfSize.y) - 0.0001, 0.0));
                float sd = SdRoundedBox(p, halfSize, radius);
                float aa = max(fwidth(sd), 0.75);
                float insideMask = saturate((-sd + aa) / aa);

                float2 centered = abs(uv - 0.5) * 2.0;
                float centerMetric = max(centered.x, centered.y);
                float centerMask = smoothstep(_CenterClear, 1.0, centerMetric);
                float cornerQuiet = CornerQuiet(uv);

                float lowerCornerAttenuation = LowerCornerAttenuation(uv);
                float sideTaper = LowerSideTaper(uv);

                float ambientBottom = exp(-(uv.y * uv.y) / max(0.18 + _AmbientIntensity * 0.32, 0.001)) * _BottomThicknessMultiplier;
                float ambientSide = (exp(-(uv.x * uv.x) / 0.06) + exp(-((1.0 - uv.x) * (1.0 - uv.x)) / 0.06)) * 0.18 * _SideSupport * _SideThicknessMultiplier * sideTaper;
                float ambientTop = exp(-((1.0 - uv.y) * (1.0 - uv.y)) / 0.08) * (1.0 - _TopSuppression) * 0.08;
                float ambient = (ambientBottom * _BottomDominance + ambientSide + ambientTop) * _AmbientIntensity * lowerCornerAttenuation;

                float fieldSum = 0.0;
                float shapeSum = 0.0;
                float colorSum = 0.0;

                float patchCount = min(_PatchCount, MAX_PATCHES);

                [unroll]
                for (int idx = 0; idx < MAX_PATCHES; idx++)
                {
                    if (idx < patchCount)
                    {
                        float4 a = _PatchA[idx];
                        float4 b = _PatchB[idx];
                        float side = a.x;
                        float center = a.y;
                        float width = max(a.z, 0.001);
                        float depth = max(a.w, 0.001);
                        float intensity = b.x;
                        float colorMix = b.y;
                        float shape = b.z;

                        float alongDistance = 1.0;
                        float inward = 1.0;
                        float sideWeight = 1.0;

                        if (side < 0.5)
                        {
                            alongDistance = CircularDistance01(uv.x, center);
                            inward = uv.y;
                            sideWeight = _BottomDominance * _BottomThicknessMultiplier;
                        }
                        else if (side < 1.5)
                        {
                            alongDistance = SideDistance01(uv.y, center);
                            inward = uv.x;
                            sideWeight = _SideSupport * _SideThicknessMultiplier * sideTaper;
                        }
                        else if (side < 2.5)
                        {
                            alongDistance = SideDistance01(uv.y, center);
                            inward = 1.0 - uv.x;
                            sideWeight = _SideSupport * _SideThicknessMultiplier * sideTaper;
                        }
                        else
                        {
                            alongDistance = CircularDistance01(uv.x, center);
                            inward = 1.0 - uv.y;
                            sideWeight = (1.0 - _TopSuppression) * 0.2;
                        }

                        float patch = PatchFalloff(alongDistance, inward, width, depth);
                        float edgeProximity = smoothstep(depth * 1.6, 0.0, inward);
                        float cornerAttenuation = side < 2.5 ? lowerCornerAttenuation : 1.0;
                        float patchBody = patch * intensity * sideWeight * cornerAttenuation;
                        float patchShape = patch * edgeProximity * shape * sideWeight * cornerAttenuation;
                        fieldSum += patchBody;
                        shapeSum += patchShape;
                        colorSum += patchBody * colorMix;
                    }
                }

                float liquid = saturate((fieldSum + ambient) * centerMask * insideMask * cornerQuiet);
                float mass = saturate(shapeSum * _PatchShapeInfluence);
                float luminance = saturate(liquid + mass * 0.32);
                float accentT = saturate((colorSum / max(fieldSum, 0.0001)) * _PatchColorInfluence);

                float coolNoise = ValueNoise(float2(uv.x * 2.7 + _TimeSeed * 0.03, uv.y * 2.3 - _TimeSeed * 0.021));
                float4 baseColor = lerp(_BaseColorA, _BaseColorB, saturate(coolNoise * 0.42 + uv.y * 0.2 + fieldSum * 0.16));
                float4 patchColor = lerp(baseColor, _AccentColor, accentT);
                patchColor.rgb *= 1.0 + mass * 0.22;

                float alpha = saturate(_Intensity * luminance);
                fixed4 col = patchColor * i.color * _Color;
                col.rgb *= alpha;
                col.a *= alpha;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
