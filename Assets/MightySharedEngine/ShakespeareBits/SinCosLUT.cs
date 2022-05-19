
using UnityEngine;
using System;
using System.Collections;

public sealed class SinCosLUT
{
	
	public static float DEFAULT_PRECISION = 0.25f;
	
	private static SinCosLUT DEFAULT_INSTANCE;
	
	public static SinCosLUT GetDefaultInstance ()
	{
		if (DEFAULT_INSTANCE == null) {
			DEFAULT_INSTANCE = new SinCosLUT ();
		}
		return DEFAULT_INSTANCE;
	}
	
	public static float Sine(float v)
	{
		return GetDefaultInstance().Sin(v);
	}
	
	public static float Cosine(float v)
	{
		return GetDefaultInstance().Cos(v);
	}
	
	
	private float[] sinLUT;
	
	private float precision;
	
	private int period;
	private int quadrant;
	
	private float deg2rad;
	private float rad2deg;
	
	public SinCosLUT ()
	{
		Setup (DEFAULT_PRECISION);
	}
	
	public SinCosLUT (float precision)
	{
		Setup (precision);
	}
	
	public void Setup (float precision)
	{
		this.precision = precision;
		this.period = (int)(360 / precision);
		this.quadrant = period >> 2;
		this.deg2rad = (float)(Math.PI / 180.0) * precision;
		this.rad2deg = (float)(180.0 / Math.PI) / precision;
		this.sinLUT = new float[period];
		for (int i = 0; i < period; i++) {
			sinLUT [i] = (float)Math.Sin (i * deg2rad);
		}
	}
	
	
	public float Cos (float theta)
	{
		while (theta < 0) {
			theta += Mathf.PI * 2f;
		}
		return sinLUT [((int)(theta * rad2deg) + quadrant) % period];
	}
	
	public int GetPeriod ()
	{
		return period;
	}
	
	public float GetPrecision ()
	{
		return precision;
	}
	
	public float[] GetSinLUT ()
	{
		return sinLUT;
	}
	
	public float Sin (float theta)
	{
		while (theta < 0) {
			theta += Mathf.PI * 2f;
		}
		return sinLUT [(int)(theta * rad2deg) % period];
	}
}
