using UnityEngine;

public static class ShaderParam
{
    public static readonly int _MainTex = Shader.PropertyToID("_MainTex");
    public static readonly int _Alpha = Shader.PropertyToID("_Alpha");
    public static readonly int _CutOff = Shader.PropertyToID("_CutOff");
    public static readonly int _BaseCutoff = Shader.PropertyToID("_BaseCutoff");
    public static readonly int _TopCutoff = Shader.PropertyToID("_TopCutoff");
    public static readonly int _OverlayCol = Shader.PropertyToID("_OverlayCol");
    public static readonly int _LerpSpeed = Shader.PropertyToID("_LerpSpeed");
    public static readonly int _LUTStrength = Shader.PropertyToID("_LUTStrength");
    public static readonly int _LerpColour = Shader.PropertyToID("_LerpColour");
    public static readonly int _MultiplyColour = Shader.PropertyToID("_MultiplyColour");
    public static readonly int _AddColour = Shader.PropertyToID("_AddColour");
    public static readonly int _Strength = Shader.PropertyToID("_Strength");

    public static readonly int _DeformDirection = Shader.PropertyToID("_DeformDirection");
    public static readonly int _DeformTranslate = Shader.PropertyToID("_DeformTranslate");
    public static readonly int _DeformRoundness = Shader.PropertyToID("_DeformRoundness");
    public static readonly int _DeformAmplification = Shader.PropertyToID("_DeformAmplification");
    public static readonly int _PinchScalar = Shader.PropertyToID("_PinchScalar");

    public static readonly int _WORLDPOS = Shader.PropertyToID("_WORLDPOS");
    public static readonly int _BLOCKWIDTH = Shader.PropertyToID("_BLOCKWIDTH");
}
