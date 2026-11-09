#ifndef PIXEL_LAB_PUDDLE_MASK
#define PIXEL_LAB_PUDDLE_MASK
#include "LabEnvironment.hlsl"
#include "LabPlanarReflection.hlsl"
// PuddleStyle (SurfaceStyleSet)
float4 _LabPuddleShape; // x cell size (m), y ring density, z ring speed, w ring strength
float4 _LabPuddleDamp;  // rgb damp soil tint, a damp roughness
float4 _LabPuddlePool;  // x pool roughness, y pool reflection, z grazing reflection, w damp reflection
float4 _LabPuddleLevel; // x lowest height, y full height
// Rain puddles on ground: stable world-space basins that fill with wetness and dry
// afterwards, a damp soil ring around them, rain rings and a planar reflection inside.
// Same integer hash and basin equation as PuddleSurface.cs.
uint PuddleHash(int2 cell)
{
    uint h=asuint(cell.x)*374761393u+asuint(cell.y)*668265263u+7919u;
    h=(h^(h>>13))*1274126177u;return h^(h>>16);
}
float PuddleSeed(int2 cell){return (PuddleHash(cell)&65535u)/65535.0;}
float LabDampCell(float2 coordinate,int2 cell,float edge,float shore)
{
    float seed=PuddleSeed(cell),second=PuddleSeed(cell+int2(73,19));
    float2 offset=coordinate-float2(cell)-float2(.25+seed*.5,.25+second*.5);
    float radius=length(offset*float2(.8+second*.65,.8+seed*.65));
    return (1-smoothstep(edge+.025,edge+.20,radius+shore*.65))*step(.2,seed);
}
float2 LabPuddleRegions(float3 position,float normalY,float wetness,float snow)
{
    int2 cell=(int2)floor(position.xz/_LabPuddleShape.x);
    float seed=PuddleSeed(cell),second=PuddleSeed(cell+int2(73,19));
    float2 center=float2(.25+seed*.5,.25+second*.5);
    float2 offset=frac(position.xz/_LabPuddleShape.x)-center;
    float radius=length(offset*float2(.8+second*.65,.8+seed*.65));
    float shore=sin(position.x*1.7+sin(position.z*2.1))*.045+sin(position.z*4.7+position.x*.5)*.02;
    float fill=smoothstep(.22,.95,wetness);
    float pool=(1-smoothstep(.15+fill*.13,.175+fill*.13,radius+shore))*fill;
    // No altitude band: gentle hills receive pools at their own surface height.
    float gate=smoothstep(.88,.99,normalY)*(1-saturate(snow*2))*smoothstep(_LabPuddleLevel.x,_LabPuddleLevel.y,position.y);
    // A broader, feathered moisture stain follows the SAME basin, rather than
    // a second set of unrelated circles. It appears earlier and dries later.
    float edge=.15+fill*.13;
    float damp=(1-smoothstep(edge+.025,edge+.20,radius+shore*.65))*step(.2,seed);
    // Union the nearest neighbouring basins so the wider stain never stops on a cell edge.
    int2 side=int2(frac(position.x/_LabPuddleShape.x)<.5?-1:1,frac(position.z/_LabPuddleShape.x)<.5?-1:1);
    damp=max(damp,LabDampCell(position.xz/_LabPuddleShape.x,cell+int2(side.x,0),edge,shore));
    damp=max(damp,LabDampCell(position.xz/_LabPuddleShape.x,cell+int2(0,side.y),edge,shore));
    damp=max(damp,LabDampCell(position.xz/_LabPuddleShape.x,cell+side,edge,shore));
    damp*=smoothstep(.015,.45,wetness);
    return float2(pool*step(.2,seed),damp)*gate;
}
float LabPuddleMask(float3 position,float normalY,float wetness,float snow){return LabPuddleRegions(position,normalY,wetness,snow).x;}
struct LabPuddle{float pool;float wetGround;};
// strength: material _Puddles, wetStrength: _PuddleWetStrength. Edits the surface in place.
LabPuddle LabApplyPuddles(float3 worldPos,float geometryNormalY,float strength,float wetStrength,inout half3 albedo,inout half rough,inout half3 normalTS)
{
    LabPuddle p;p.pool=0;p.wetGround=0;
    if(strength>.001)
    {
        float2 regions=LabPuddleRegions(worldPos,geometryNormalY,_LabEnvironment.z,_LabEnvironment.w);
        p.pool=strength*regions.x;
        p.wetGround=strength*wetStrength*regions.y*(1-p.pool);
        albedo*=lerp(half3(1,1,1),_LabPuddleDamp.rgb,p.wetGround);
        rough=lerp(rough,_LabPuddleDamp.a,p.wetGround);
        normalTS=normalize(lerp(normalTS,half3(0,0,1),p.wetGround*.25));
        float2 rippleCell=floor(worldPos.xz*_LabPuddleShape.y);
        float rippleAge=frac(_LabWaterTime*_LabPuddleShape.z+frac(sin(dot(rippleCell,float2(43.1,17.7)))*1327.8));
        float2 rippleOffset=frac(worldPos.xz*_LabPuddleShape.y)-.5;
        float ring=sin((length(rippleOffset)-rippleAge*.7)*65)*exp(-abs(length(rippleOffset)-rippleAge*.7)*35)*(1-rippleAge);
        normalTS=normalize(lerp(normalTS,half3(normalize(rippleOffset+.001)*ring*_LabPuddleShape.w*_LabEnvironment.z,1),p.pool));
        rough=lerp(rough,_LabPuddlePool.x,p.pool);
    }
    return p;
}
// reflected: _PuddleReflection sampled at LabPuddleReflectionUV. Pools mirror strongly,
// damp soil only at grazing angles.
void LabPuddleReflect(LabPuddle p,half3 reflected,float3 geometryNormal,float3 view,inout half3 albedo,inout half3 emission)
{
    float grazing=1-saturate(dot(geometryNormal,view));
    float amount=p.pool*(_LabPuddlePool.y+_LabPuddlePool.z*pow(grazing,4))*(1-_LabReflectionDisabled);
    amount+=p.wetGround*_LabPuddlePool.w*pow(grazing,2)*(1-_LabReflectionDisabled);
    albedo*=1-amount*.8;
    emission+=reflected*amount;
}
#endif
