﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HeightMapGenerator {

	public static HeightMap GenerateHeightMap(int width, int height, HeightMapSettings settings, Vector2 sampleCentre, float jMultiplier) {
		float[,] values = Noise.GenerateNoiseMap (width, height, settings.noiseSettings, sampleCentre);
		// Debug.Log ("Width: " 
		AnimationCurve heightCurve_threadsafe = new AnimationCurve (settings.heightCurve.keys);

		float minValue = float.MaxValue;
		float maxValue = float.MinValue;
		float multiplier;
		int offset = 20;
		float tempi;
		float tempj;

		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {
				if (i > offset && i < width - offset && j > offset && j < height - offset) {
					tempi = i / (width/2.0f);
					if (i > width / 2)
						tempi = System.Math.Abs(i - width) / (width/2);

					tempj = j / (height/2.0f);
					if (tempj > height / 2)
						tempj = System.Math.Abs(j - height) / (height/2);
					multiplier = heightCurve_threadsafe.Evaluate (values [i, j]) * settings.heightMultiplier + (jMultiplier * tempi * tempj);
				}
				else
					multiplier = heightCurve_threadsafe.Evaluate (values [i, j]) * settings.heightMultiplier;
				values [i, j] *= multiplier;

				if (values [i, j] > maxValue) {
					maxValue = values [i, j];
				}
				if (values [i, j] < minValue) {
					minValue = values [i, j];
				}
			}
		}

		return new HeightMap (values, minValue, maxValue);
	}

}

public struct HeightMap {
	public readonly float[,] values;
	public readonly float minValue;
	public readonly float maxValue;

	public HeightMap (float[,] values, float minValue, float maxValue)
	{
		this.values = values;
		this.minValue = minValue;
		this.maxValue = maxValue;
	}
}

