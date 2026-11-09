#ifndef PIXEL_LAB_WIND
#define PIXEL_LAB_WIND
#include "LabEnvironment.hlsl"
// Vertex motion for trees, grass, flags and water waves. Call it first in every pass's vertex
// shader (forward, shadow, depth) so the moving surface and its shadow stay together.
// params: _LabWindParams per renderer, set by WorldSurfaceMotion
//   x mode 1 tree, 2 grass, 3 flag, 4 water; y local anchor; z inverse extent; w phase
// waterFlow: 0 weather clock, 1 smooth water clock. waveScale: water wave height multiplier.
void LabDeformVertex(inout float4 vertex,inout float3 normal,float4 params,float waterFlow,float waveScale)
{
    if(_LabEnvironment.x<.5)return;
    float3 world=mul(UNITY_MATRIX_M,vertex).xyz;
    float t=_LabWeatherTime;
    float mode=params.x;
    if(mode>3.5)
    {
        t=lerp(t,_LabWaterTime,waterFlow);
        float a=world.x*.85+t*1.1,b=world.z*1.3-t*1.45;
        float c=(world.x+world.z)*2.1+t*1.7;
        world.y+=(sin(a)*.045+sin(b)*.025+sin(c)*.008*waterFlow)*_LabWaterMotion*waveScale;
        float dx=cos(a)*.03825+cos(c)*.0168*waterFlow,dz=cos(b)*.0325+cos(c)*.0168*waterFlow;
        float3 waterNormal=normalize(float3(-dx*_LabWaterMotion*waveScale,1,-dz*_LabWaterMotion*waveScale));
        normal=normalize(mul(waterNormal,(float3x3)UNITY_MATRIX_M));
    }
    else if(mode>.5)
    {
        float h=saturate((vertex.y-params.y)*params.z);
        float wave=sin(world.x*.51+world.z*.39+t*1.9+params.w)+.32*sin(t*3.7+world.z*.8);
        float weight=h*h;
        float amplitude=mode<1.5?.20:.16;
        if(mode>2.5)
        {
            float pin=saturate((vertex.x-params.y)*params.z);
            weight=pin*pin;amplitude=.30;
            wave=sin(pin*7.5-t*4.4+params.w)+.28*sin(pin*13-t*6);
        }
        float2 direction=normalize(_LabWind.xy+float2(.001,0));
        world.xz+=direction*wave*weight*amplitude*_LabWind.z;
        world.y+=abs(wave)*weight*.025*_LabWind.z;
    }
    vertex=mul(UNITY_MATRIX_I_M,float4(world,1));
}
#endif
