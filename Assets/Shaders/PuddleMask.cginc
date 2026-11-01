#ifndef PIXEL_LAB_PUDDLE_MASK
#define PIXEL_LAB_PUDDLE_MASK
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
    int2 cell=(int2)floor(position.xz/6);
    float seed=PuddleSeed(cell),second=PuddleSeed(cell+int2(73,19));
    float2 center=float2(.25+seed*.5,.25+second*.5);
    float2 offset=frac(position.xz/6)-center;
    float radius=length(offset*float2(.8+second*.65,.8+seed*.65));
    float shore=sin(position.x*1.7+sin(position.z*2.1))*.045+sin(position.z*4.7+position.x*.5)*.02;
    float fill=smoothstep(.22,.95,wetness);
    float pool=(1-smoothstep(.15+fill*.13,.175+fill*.13,radius+shore))*fill;
    // No altitude band: gentle hills receive pools at their own surface height.
    float gate=smoothstep(.88,.99,normalY)*(1-saturate(snow*2))*smoothstep(.38,.6,position.y);
    // A broader, feathered moisture stain follows the SAME basin, rather than
    // a second set of unrelated circles. It appears earlier and dries later.
    float edge=.15+fill*.13;
    float damp=(1-smoothstep(edge+.025,edge+.20,radius+shore*.65))*step(.2,seed);
    // Union the nearest neighbouring basins so the wider stain never stops on a cell edge.
    int2 side=int2(frac(position.x/6)<.5?-1:1,frac(position.z/6)<.5?-1:1);
    damp=max(damp,LabDampCell(position.xz/6,cell+int2(side.x,0),edge,shore));
    damp=max(damp,LabDampCell(position.xz/6,cell+int2(0,side.y),edge,shore));
    damp=max(damp,LabDampCell(position.xz/6,cell+side,edge,shore));
    damp*=smoothstep(.015,.45,wetness);
    return float2(pool*step(.2,seed),damp)*gate;
}
float LabPuddleMask(float3 position,float normalY,float wetness,float snow){return LabPuddleRegions(position,normalY,wetness,snow).x;}
#endif
