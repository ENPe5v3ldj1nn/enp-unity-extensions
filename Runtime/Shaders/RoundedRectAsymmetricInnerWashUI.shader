Shader "UI/ENP/RoundedRectAsymmetricInnerWash"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _TintColor ("Base Tint", Color) = (1,1,1,1)
        _TopColor ("Top Color", Color) = (1,1,1,1)
        _BottomColor ("Bottom Color", Color) = (1,1,1,1)
        _BottomAccentColor ("Bottom Accent Color", Color) = (1,1,1,1)
        _Intensity ("Intensity", Range(0,1)) = 0.15
        _Thickness ("Thickness", Range(0,1)) = 0.2
        _Softness ("Softness", Range(0,1)) = 0.7
        _CenterClear ("Center Clear", Range(0,1)) = 0.65
        _Roundness ("Roundness", Float) = 24
        _TopStrength ("Top Strength", Range(0,2)) = 0.1
        _BottomStrength ("Bottom Strength", Range(0,2)) = 0.5
        _LeftStrength ("Left Strength", Range(0,2)) = 0.35
        _RightStrength ("Right Strength", Range(0,2)) = 0.35
        _RectSize ("Rect Size", Vector) = (100,100,0,0)
        _RectCenter ("Rect Center", Vector) = (0,0,0,0)

        _BottomSegmentsA ("Bottom Segments A", Vector) = (0,0,0,0)
        _BottomSegmentsB ("Bottom Segments B", Vector) = (0,0,0,0)
        _LeftSegments ("Left Segments", Vector) = (0,0,0,0)
        _RightSegments ("Right Segments", Vector) = (0,0,0,0)
        _BottomAccentA ("Bottom Accent A", Vector) = (0,0,0,0)
        _BottomAccentB ("Bottom Accent B", Vector) = (0,0,0,0)
        _LeftAccent ("Left Accent", Vector) = (0,0,0,0)
        _RightAccent ("Right Accent", Vector) = (0,0,0,0)

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
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
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
            Name "RoundedRectAsymmetricInnerWash"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            sampler2D _MainTex;

            float4 _Color;
            float4 _TintColor;
            float4 _TopColor;
            float4 _BottomColor;
            float4 _BottomAccentColor;
            float _Intensity;
            float _Thickness;
            float _Softness;
            float _CenterClear;
            float _Roundness;
            float _TopStrength;
            float _BottomStrength;
            float _LeftStrength;
            float _RightStrength;
            float4 _RectSize;
            float4 _RectCenter;
            float4 _BottomSegmentsA;
            float4 _BottomSegmentsB;
            float4 _LeftSegments;
            float4 _RightSegments;
            float4 _BottomAccentA;
            float4 _BottomAccentB;
            float4 _LeftAccent;
            float4 _RightAccent;
            float4 _ClipRect;

            float4x4 unity_ObjectToWorld;
            float4x4 unity_MatrixVP;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float2 localPos : TEXCOORD2;
            };

            float SdRoundedBox(float2 p, float2 halfSize, float radius)
            {
                float2 q = abs(p) - halfSize + radius;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            float GetClipRectMask(float2 position, float4 clipRect)
            {
                float2 insideMin = step(clipRect.xy, position);
                float2 insideMax = step(position, clipRect.zw);
                return insideMin.x * insideMin.y * insideMax.x * insideMax.y;
            }

            float EvaluateSide(float distanceToSide, float thicknessPx, float softness)
            {
                float feather = max(thicknessPx * lerp(0.08, 1.0, softness), 0.0001);
                return 1.0 - smoothstep(0.0, thicknessPx + feather, distanceToSide);
            }

            float Select3(float4 values, int index)
            {
                if (index <= 0)
                    return values.x;

                if (index == 1)
                    return values.y;

                return values.z;
            }

            float Select5(float4 valuesA, float4 valuesB, int index)
            {
                if (index <= 0)
                    return valuesA.x;

                if (index == 1)
                    return valuesA.y;

                if (index == 2)
                    return valuesA.z;

                if (index == 3)
                    return valuesA.w;

                return valuesB.x;
            }

            float SampleSegment3(float coord, float4 values)
            {
                float scaled = saturate(coord) * 2.0;
                int index = (int)floor(scaled);
                float blend = scaled - index;
                blend = blend * blend * (3.0 - 2.0 * blend);

                if (index >= 2)
                    return values.z;

                float a = Select3(values, index);
                float b = Select3(values, index + 1);
                return lerp(a, b, blend);
            }

            float SampleSegment5(float coord, float4 valuesA, float4 valuesB)
            {
                float scaled = saturate(coord) * 4.0;
                int index = (int)floor(scaled);
                float blend = scaled - index;
                blend = blend * blend * (3.0 - 2.0 * blend);

                if (index >= 4)
                    return valuesB.x;

                float a = Select5(valuesA, valuesB, index);
                float b = Select5(valuesA, valuesB, index + 1);
                return lerp(a, b, blend);
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                float4 worldVertex = mul(unity_ObjectToWorld, v.vertex);
                o.vertex = mul(unity_MatrixVP, worldVertex);
                o.worldPosition = v.vertex;
                o.localPos = v.vertex.xy;
                o.uv = v.texcoord;
                o.color = v.color;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 rectSize = max(_RectSize.xy, float2(1.0, 1.0));
                float2 halfSize = rectSize * 0.5;
                float2 p = i.localPos - _RectCenter.xy;
                float2 uv = saturate((p + halfSize) / rectSize);

                float halfMin = min(halfSize.x, halfSize.y);
                float radius = min(max(_Roundness, 0.0), max(halfMin - 0.0001, 0.0));
                float sd = SdRoundedBox(p, halfSize, radius);
                float aa = max(fwidth(sd), 0.75);
                float insideMask = saturate((-sd + aa) / aa);

                float thicknessPx = max(halfMin * _Thickness, 0.0001);

                float distLeft = p.x + halfSize.x;
                float distRight = halfSize.x - p.x;
                float distBottom = p.y + halfSize.y;
                float distTop = halfSize.y - p.y;

                float leftEval = EvaluateSide(distLeft, thicknessPx, _Softness);
                float rightEval = EvaluateSide(distRight, thicknessPx, _Softness);
                float bottomEval = EvaluateSide(distBottom, thicknessPx, _Softness);
                float topEval = EvaluateSide(distTop, thicknessPx, _Softness);

                float segmentedBottomStrength = SampleSegment5(uv.x, _BottomSegmentsA, _BottomSegmentsB);
                float segmentedLeftStrength = SampleSegment3(uv.y, _LeftSegments);
                float segmentedRightStrength = SampleSegment3(uv.y, _RightSegments);

                float segmentedBottomAccent = SampleSegment5(uv.x, _BottomAccentA, _BottomAccentB);
                float segmentedLeftAccent = SampleSegment3(uv.y, _LeftAccent);
                float segmentedRightAccent = SampleSegment3(uv.y, _RightAccent);

                float bottomStrength = max(_BottomStrength, segmentedBottomStrength);
                float leftStrength = max(_LeftStrength, segmentedLeftStrength);
                float rightStrength = max(_RightStrength, segmentedRightStrength);
                float topStrength = _TopStrength;

                float bottomAccent = saturate(segmentedBottomAccent);
                float leftAccent = saturate(segmentedLeftAccent);
                float rightAccent = saturate(segmentedRightAccent);

                float leftBand = leftEval * leftStrength * (1.0 + leftAccent * 0.16);
                float rightBand = rightEval * rightStrength * (1.0 + rightAccent * 0.16);
                float bottomBand = bottomEval * bottomStrength * (1.0 + bottomAccent * 0.22);
                float topBand = topEval * topStrength;

                float sideMask = saturate(leftBand + rightBand + bottomBand + topBand);
                sideMask = pow(sideMask, lerp(2.2, 0.85, _Softness));

                float2 normalizedPos = abs(p) / max(halfSize, float2(1.0, 1.0));
                float centerMetric = saturate(max(normalizedPos.x, normalizedPos.y));
                float centerStart = lerp(0.0, 0.86, _CenterClear);
                float centerMask = saturate((centerMetric - centerStart) / max(1.0 - centerStart, 0.0001));

                float wash = saturate(_Intensity) * sideMask * centerMask * insideMask;

                float4 baseGradient = lerp(_BottomColor, _TopColor, uv.y) * _TintColor;
                float4 accentGradient = lerp(_BottomAccentColor, _TopColor, saturate(uv.y * 1.15)) * _TintColor;

                float sideAccentMask = max(leftEval * leftAccent, rightEval * rightAccent);
                float accentMask = saturate(bottomEval * bottomAccent + sideAccentMask * 0.85);
                accentMask *= saturate(1.0 - uv.y * 0.55);
                accentMask *= insideMask;

                float4 gradient = lerp(baseGradient, accentGradient, accentMask);
                gradient.rgb *= 1.0 + accentMask * 0.12;

                float4 col = gradient * i.color * _Color;
                col.rgb *= wash;
                col.a *= wash;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= GetClipRectMask(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDHLSL
        }
    }
}
