Shader "Overlays/WorldGridOverlay"
{
    Properties
    {
        _Color("Minor Line Color", Color) = (1,1,1,0.10)
        _MajorColor("Major Line Color", Color) = (1,1,1,0.18)
        _Background("Background Tint", Color) = (0,0,0,0) // usually transparent
        _StepA("Step A (world units)", Float) = 10
        _StepB("Step B (world units)", Float) = 100
        _Blend("LOD Blend 0..1", Range(0,1)) = 0
        _MajorEvery("Major every N lines", Float) = 10
        _WorldPerPixel("World units per screen pixel", Float) = 1.0
        _PxThickness("Line thickness (px)", Float) = 1.0
        _Origin("Grid Origin (world XY)", Vector) = (0,0,0,0)
        _MajorPxMul("Major line thickness mul", Float) = 1.75
    }
    SubShader
    {
        Tags { "Queue"="Overlay+100" "RenderType"="Transparent" "IgnoreProjector"="True" }
        ZTest Always
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex: POSITION; };
            struct v2f { float4 pos: SV_POSITION; float3 wpos: TEXCOORD0; };

            float4 _Background;
            float4 _Color, _MajorColor;
            float _WorldPerPixel;
            float _StepA, _StepB, _Blend, _MajorEvery, _PxThickness;
            float4 _Origin;
            float _MajorPxMul;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz; // world XY drives the grid
                return o;
            }

            // distance in world units to nearest grid line for cell size 'step'
            float distToGrid(float2 w, float step)
            {
                // align to origin: subtract _Origin.xy so lines hit (0,0) when origin=(0,0)
                float2 p = (w - _Origin.xy) / step;   // in cell coordinates
                float2 f = frac(p);                   // 0..1
                float2 dEdge = min(f, 1.0 - f) * step;// world distance to nearest line on each axis
                return min(dEdge.x, dEdge.y);         // nearest line, any axis
            }

            float lineMaskPx(float2 w, float step, float pxWorld)
            {
                float d  = distToGrid(w, step);
                float dd = fwidth(d);
                return smoothstep(pxWorld + dd, 0.0, d);
            }

            float majorMaskPx(float2 w, float step, float everyN, float pxWorld)
            {
                float majorStep = step * everyN;
                float d  = distToGrid(w, majorStep);
                float dd = fwidth(d);
                // thicker major lines
                return smoothstep(pxWorld * _MajorPxMul + dd, 0.0, d);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Base background (usually transparent)
                float4 col = _Background;

                float2 w = i.wpos.xy;

                // convert desired pixel thickness to world units
                float pxWorld = _PxThickness * _WorldPerPixel;

                float mA = lineMaskPx(w, _StepA, pxWorld);
                float mB = lineMaskPx(w, _StepB, pxWorld);
                float minor = lerp(mA, mB, _Blend);

                float MA = majorMaskPx(w, _StepA, _MajorEvery, pxWorld);
                float MB = majorMaskPx(w, _StepB, _MajorEvery, pxWorld);
                float major = lerp(MA, MB, _Blend);

                // Composite: major overrides minor
                float4 cMinor = _Color * minor;
                float4 cMajor = _MajorColor * major;

                return col + cMinor + cMajor;
            }
            ENDHLSL
        }
    }
}
