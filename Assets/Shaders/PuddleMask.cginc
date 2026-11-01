#ifndef PIXEL_LAB_PUDDLE_MASK
#define PIXEL_LAB_PUDDLE_MASK
// Same integer hash and basin equation as PuddleSurface.cs.
uint PuddleHash(int2 cell)
{
    uint h=asuint(cell.x)*374761393u+asuint(cell.y)*668265263u+7919u;
    h=(h^(h>>13))*1274126177u;return h^(h>>16);
}
float PuddleSeed(int2 cell){return (PuddleHash(cell)&65535u)/65535.0;}
float LabPuddleMask(float3 position,float normalY,float wetness,float snow)
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
    return pool*step(.2,seed)*smoothstep(.88,.99,normalY)*(1-saturate(snow*2))*smoothstep(.38,.6,position.y);
}
#endif
