Shader "UI/ENP/RoundedRectLiquidEdge"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BaseColorA ("Base Color A", Color) = (0.12,0.72,1,1)
        _BaseColorB ("Base Color B", Color) = (0.58,0.22,1,1)
        _AccentColor ("Accent Color", Color) = (1,0.78,0.38,1)
        _Intensity ("Global Intensity", Range(0,2)) = 1
        _Thickness ("Thickness", Float) = 62
        _Softness ("Softness", Range(0,1)) = 0.56
        _CenterClear ("Center Clear", Range(0,1)) = 0.76
        _Roundness ("Roundness", Float) = 36
        _AmbientMotionIntensity ("Ambient Motion Intensity", Range(0,1)) = 0.2
        _HighlightZoneIntensity ("Highlight Zone Intensity", Range(0,2)) = 1
        _HighlightColorShift ("Highlight Color Shift", Range(0,1)) = 0.34
        _HighlightShapeInfluence ("Highlight Shape Influence", Range(0,2)) = 1
        _HighlightIntensityInfluence ("Highlight Intensity Influence", Range(0,2)) = 1
        _HighlightColorInfluence ("Highlight Color Influence", Range(0,2)) = 1
        _BottomDominance ("Bottom Dominance", Range(0,2)) = 1
        _SideSupportAmount ("Side Support Amount", Range(0,2)) = 0.46
        _TopSuppression ("Top Suppression", Range(0,1)) = 0.92
        _TimeSeed ("Time Seed", Float) = 0
        _RectSize ("Rect Size", Vector) = (100,100,0,0)
        _RectCenter ("Rect Center", Vector) = (0,0,0,0)

        _BottomStrengthA ("Bottom Strength A", Vector) = (0,0,0,0)
        _BottomStrengthB ("Bottom Strength B", Vector) = (0,0,0,0)
        _LeftStrength ("Left Strength", Vector) = (0,0,0,0)
        _RightStrength ("Right Strength", Vector) = (0,0,0,0)
        _BottomDriftA ("Bottom Drift A", Vector) = (0,0,0,0)
        _BottomDriftB ("Bottom Drift B", Vector) = (0,0,0,0)
        _LeftDrift ("Left Drift", Vector) = (0,0,0,0)
        _RightDrift ("Right Drift", Vector) = (0,0,0,0)
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
            Name "RoundedRectLiquidEdge"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

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
            float _Thickness;
            float _Softness;
            float _CenterClear;
            float _Roundness;
            float _AmbientMotionIntensity;
            float _HighlightZoneIntensity;
            float _HighlightColorShift;
            float _HighlightShapeInfluence;
            float _HighlightIntensityInfluence;
            float _HighlightColorInfluence;
            float _BottomDominance;
            float _SideSupportAmount;
            float _TopSuppression;
            float _TimeSeed;
            float4 _RectSize;
            float4 _RectCenter;
            float4 _BottomStrengthA;
            float4 _BottomStrengthB;
            float4 _LeftStrength;
            float4 _RightStrength;
            float4 _BottomDriftA;
            float4 _BottomDriftB;
            float4 _LeftDrift;
            float4 _RightDrift;
            float4 _BottomAccentA;
            float4 _BottomAccentB;
            float4 _LeftAccent;
            float4 _RightAccent;
            float4 _ClipRect;

            float SdRoundedBox(float2 p, float2 halfSize, float radius)
            {
                float2 q = abs(p) - halfSize + radius;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            float Smooth01(float value)
            {
                float t = saturate(value);
                return t * t * (3.0 - 2.0 * t);
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

            float Fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                v += ValueNoise(p) * a;
                p = p * 2.13 + 17.7;
                a *= 0.5;
                v += ValueNoise(p) * a;
                p = p * 2.07 + 9.2;
                a *= 0.5;
                v += ValueNoise(p) * a;
                return v;
            }

            float Select3(float4 values, int index)
            {
                if (index <= 0) return values.x;
                if (index == 1) return values.y;
                return values.z;
            }

            float Select5(float4 valuesA, float4 valuesB, int index)
            {
                if (index <= 0) return valuesA.x;
                if (index == 1) return valuesA.y;
                if (index == 2) return valuesA.z;
                if (index == 3) return valuesA.w;
                return valuesB.x;
            }

            float SampleSegment3(float coord, float4 values)
            {
                float scaled = saturate(coord) * 2.0;
                int index = (int)floor(scaled);
                float blend = Smooth01(scaled - index);
                if (index >= 2) return values.z;
                return lerp(Select3(values, index), Select3(values, index + 1), blend);
            }

            float SampleSegment5(float coord, float4 valuesA, float4 valuesB)
            {
                float scaled = saturate(coord) * 4.0;
                int index = (int)floor(scaled);
                float blend = Smooth01(scaled - index);
                if (index >= 4) return valuesB.x;
                return lerp(Select5(valuesA, valuesB, index), Select5(valuesA, valuesB, index + 1), blend);
            }

            float ProximityWeight(float distanceToSide, float range)
            {
                return Smooth01(1.0 - saturate(distanceToSide / max(range, 0.0001)));
            }

            float BandProfile(float edgeDistance, float reachPx, float softness)
            {
                float normalized = saturate(1.0 - edgeDistance / max(reachPx, 0.0001));
                float outer = smoothstep(0.0, lerp(0.035, 0.28, softness), normalized);
                float power = lerp(4.6, 1.25, softness);
                return pow(normalized, power) * outer;
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
                float halfMin = min(halfSize.x, halfSize.y);
                float radius = min(max(_Roundness, 0.0), max(halfMin - 0.0001, 0.0));

                float sd = SdRoundedBox(p, halfSize, radius);
                float aa = max(fwidth(sd), 0.75);
                float insideMask = saturate((-sd + aa) / aa);
                float edgeDistance = max(-sd, 0.0);

                float distLeft = p.x + halfSize.x;
                float distRight = halfSize.x - p.x;
                float distBottom = p.y + halfSize.y;
                float distTop = halfSize.y - p.y;
                float reachBase = min(max(_Thickness, 0.0), halfMin * saturate(1.0 - _CenterClear));
                float weightRange = max(reachBase * 1.55, 1.0);

                float bottomWeightRaw = ProximityWeight(distBottom, weightRange) * lerp(0.72, 1.62, saturate(_BottomDominance));
                float leftWeightRaw = ProximityWeight(distLeft, weightRange) * saturate(_SideSupportAmount);
                float rightWeightRaw = ProximityWeight(distRight, weightRange) * saturate(_SideSupportAmount);
                float topWeightRaw = ProximityWeight(distTop, weightRange) * (1.0 - saturate(_TopSuppression)) * 0.22;
                float weightSum = bottomWeightRaw + leftWeightRaw + rightWeightRaw + topWeightRaw + 0.0001;
                float bottomWeight = bottomWeightRaw / weightSum;
                float leftWeight = leftWeightRaw / weightSum;
                float rightWeight = rightWeightRaw / weightSum;
                float topWeight = topWeightRaw / weightSum;

                float bottomStrength = SampleSegment5(uv.x, _BottomStrengthA, _BottomStrengthB);
                float leftStrength = SampleSegment3(uv.y, _LeftStrength);
                float rightStrength = SampleSegment3(uv.y, _RightStrength);
                float bottomDrift = SampleSegment5(uv.x, _BottomDriftA, _BottomDriftB);
                float leftDrift = SampleSegment3(uv.y, _LeftDrift);
                float rightDrift = SampleSegment3(uv.y, _RightDrift);
                float bottomAccent = SampleSegment5(uv.x, _BottomAccentA, _BottomAccentB);
                float leftAccent = SampleSegment3(uv.y, _LeftAccent);
                float rightAccent = SampleSegment3(uv.y, _RightAccent);

                float strength = bottomStrength * bottomWeight + leftStrength * leftWeight + rightStrength * rightWeight + topWeight * 0.03;
                float drift = bottomDrift * bottomWeight + leftDrift * leftWeight + rightDrift * rightWeight;
                float lobe = saturate(bottomAccent * bottomWeight + leftAccent * leftWeight * 0.42 + rightAccent * rightWeight * 0.42);
                float lobeMask = smoothstep(0.0, 0.72, lobe);

                float pathCoord = uv.x * bottomWeight + uv.y * (leftWeight + rightWeight) + (1.0 - uv.x) * topWeight;
                float ambient = Fbm(float2(pathCoord * 5.7 + _TimeSeed * 0.031, uv.y * 1.9 - _TimeSeed * 0.019));
                float micro = Fbm(float2(pathCoord * 15.0 - _TimeSeed * 0.047, edgeDistance * 0.019 + _TimeSeed * 0.011));
                float ambientShape = (ambient - 0.5) * _AmbientMotionIntensity * 0.18;
                float filament = smoothstep(0.58, 0.91, micro) * _AmbientMotionIntensity * 0.18;

                float baseReach = reachBase * saturate(0.62 + strength * 0.58 + drift * 0.22 + ambientShape);
                baseReach = max(baseReach, 0.001);

                float accentShapePush = lobeMask * _HighlightZoneIntensity * _HighlightShapeInfluence * lerp(0.09, 0.3, bottomWeight);
                float accentReach = reachBase * saturate(0.64 + strength * 0.5 + accentShapePush);
                accentReach = max(accentReach, baseReach);

                float band = BandProfile(edgeDistance, baseReach, _Softness);
                float accentBand = BandProfile(edgeDistance, accentReach, saturate(_Softness * 0.68 + 0.035));

                float2 normalizedPos = abs(p) / max(halfSize, float2(1.0, 1.0));
                float centerMetric = saturate(max(normalizedPos.x, normalizedPos.y));
                float clearStart = lerp(0.0, 0.92, saturate(_CenterClear));
                float centerMask = smoothstep(clearStart, 1.0, centerMetric);

                float edgeEnergy = band * insideMask * centerMask;
                edgeEnergy *= saturate(strength + filament);

                float accentEdge = accentBand * insideMask * centerMask * lobeMask;
                accentEdge *= clamp(_HighlightZoneIntensity, 0.0, 2.0) * _HighlightIntensityInfluence * lerp(0.42, 1.18, bottomWeight);

                float localHue = Fbm(float2(pathCoord * 3.4 + 3.0, _TimeSeed * 0.013 + uv.x * 0.7));
                float4 baseColor = lerp(_BaseColorA, _BaseColorB, saturate(localHue * 0.78 + bottomWeight * 0.18));
                float accentRidge = smoothstep(0.12, 0.82, accentBand) * (1.0 - smoothstep(0.7, 1.0, edgeDistance / max(accentReach, 0.0001)));
                float accentMask = saturate(accentEdge * accentRidge * 1.72);
                float4 liftedAccent = lerp(_AccentColor, float4(0.92, 0.98, 1.0, _AccentColor.a), bottomWeight * 0.28);
                float4 colorMix = lerp(baseColor, liftedAccent, saturate(accentMask * _HighlightColorShift * _HighlightColorInfluence));
                colorMix.rgb *= 1.0 + accentMask * lerp(0.24, 0.52, bottomWeight) + filament * 0.08;

                float globalIntensity = clamp(_Intensity, 0.0, 2.0);
                float alpha = globalIntensity * edgeEnergy;
                alpha += globalIntensity * accentMask * lerp(0.18, 0.56, bottomWeight);
                alpha = saturate(alpha);

                fixed4 col = colorMix * i.color * _Color;
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
