#ifndef PIXEL_LAB_WORLD_MOTION
#define PIXEL_LAB_WORLD_MOTION
float4 _LabEnvironment; // enabled, daylight, wetness, snow
float4 _LabWind; // world x/z direction, strength, unused
float _LabWeatherTime, _LabWaterMotion;
float4 _LabWindParams; // mode: 1 tree, 2 grass, 3 flag, 4 water; local anchor, inverse extent, phase
void LabDeformVertex(inout float4 vertex,inout float3 normal,float2 uv)
{
    if(_LabEnvironment.x<.5)return;
    float3 world=mul(unity_ObjectToWorld,vertex).xyz;
    float t=_LabWeatherTime;
    float mode=_LabWindParams.x;
    if(mode>3.5)
    {
        float a=world.x*.85+t*1.1,b=world.z*1.3-t*1.45;
        world.y+=(sin(a)*.045+sin(b)*.025)*_LabWaterMotion;
        float3 waterNormal=normalize(float3(-cos(a)*.03825*_LabWaterMotion,1,-cos(b)*.0325*_LabWaterMotion));
        normal=normalize(mul(waterNormal,(float3x3)unity_ObjectToWorld));
    }
    else if(mode>.5)
    {
        float h=saturate((vertex.y-_LabWindParams.y)*_LabWindParams.z);
        float wave=sin(world.x*.51+world.z*.39+t*1.9+_LabWindParams.w)+.32*sin(t*3.7+world.z*.8);
        float weight=h*h;
        float amplitude=mode<1.5?.20:.16;
        if(mode>2.5)
        {
            float pin=saturate((vertex.x-_LabWindParams.y)*_LabWindParams.z);
            weight=pin*pin;amplitude=.30;
            wave=sin(pin*7.5-t*4.4+_LabWindParams.w)+.28*sin(pin*13-t*6);
        }
        float2 direction=normalize(_LabWind.xy+float2(.001,0));
        world.xz+=direction*wave*weight*amplitude*_LabWind.z;
        world.y+=abs(wave)*weight*.025*_LabWind.z;
    }
    vertex=mul(unity_WorldToObject,float4(world,1));
}
#endif
