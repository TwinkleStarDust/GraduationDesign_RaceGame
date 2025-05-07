Shader "Hidden/PampelGames/RoadConstructor/InvisibleShader"  
{  
    SubShader  
    {  
        Pass  
        {  
            ZWrite Off  
            Blend SrcAlpha OneMinusSrcAlpha            ColorMask 0  
        }  
    }
}