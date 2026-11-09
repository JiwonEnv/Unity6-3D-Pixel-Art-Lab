using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelArtLab
{
    // Look settings of one shader technique module (Lab*.hlsl), published as _Lab shader globals.
    // Swap the asset in SurfaceStyleSet to restyle a project without touching HLSL.
    // Colors are authored in sRGB like any Unity color field; SetGlobalColor converts them.
    public abstract class SurfaceStyle : ScriptableObject
    {
        public abstract void Push();
        protected virtual void OnValidate()=>SurfaceStyleSet.RefreshActive();

        static readonly Dictionary<Type,SurfaceStyle> defaults=new Dictionary<Type,SurfaceStyle>();
        // A hidden instance with the built-in values, used when a slot is left empty.
        public static T Default<T>() where T:SurfaceStyle
        {
            if(!defaults.TryGetValue(typeof(T),out var style)||!style){style=CreateInstance<T>();style.hideFlags=HideFlags.HideAndDontSave;defaults[typeof(T)]=style;}
            return (T)style;
        }
    }
}
