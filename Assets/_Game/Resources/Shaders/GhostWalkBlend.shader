Shader "Mitzi/GhostWalkBlend"
{
    Properties
    {
        _TextureA("Previous",2D)="white"{}
        _TextureB("Current",2D)="white"{}
        _Blend("Blend",Range(0,1))=1
        _Tint("Tint",Color)=(1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off Lighting Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _TextureA,_TextureB;
            sampler2D _Motion;
            float4 _RectA,_RectB,_BoundsA,_BoundsB,_Tint;
            float4 _MotionBounds;
            float _Blend,_UseMotion;
            struct appdata { float4 vertex:POSITION; };
            struct v2f { float4 vertex:SV_POSITION;float2 local:TEXCOORD0; };
            v2f vert(appdata v) { v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.local=v.vertex.xy;return o; }
            float4 samplePose(sampler2D texSampler,float2 local,float4 bounds,float4 rect)
            {
                float2 uv=(local-bounds.xy)/bounds.zw;
                float inside=step(0,uv.x)*step(0,uv.y)*step(uv.x,1)*step(uv.y,1);
                float4 c=tex2D(texSampler,rect.xy+saturate(uv)*rect.zw);
                c.a*=inside;c.rgb*=c.a;return c;
            }
            float4 frag(v2f i):SV_Target
            {
                float2 a=i.local,b=i.local;
                if(_UseMotion>.5)
                {
                    // Inverse-warp both poses toward their shared intermediate paw positions.
                    for(int n=0;n<1;n++)
                    {
                        a=i.local-_Blend*tex2D(_Motion,(a-_MotionBounds.xy)/_MotionBounds.zw).rg;
                        b=i.local-(1-_Blend)*tex2D(_Motion,(b-_MotionBounds.xy)/_MotionBounds.zw).ba;
                    }
                }
                // Blend in one pass so the opaque torso never becomes translucent.
                float4 c=lerp(samplePose(_TextureA,a,_BoundsA,_RectA),samplePose(_TextureB,b,_BoundsB,_RectB),_Blend);
                c.rgb*=_Tint.rgb*_Tint.a;c.a*=_Tint.a;return c;
            }
            ENDCG
        }
    }
}
