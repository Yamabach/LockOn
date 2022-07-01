using System;
using UnityEngine;

namespace LOSpace{
	public static class Utility
	{
		// プラスの最小値を返す（両方マイナスなら0）
		public static float PlusMin(float a, float b)
		{
			if (a < 0 && b < 0) return 0f;
			if (a < 0) return b;
			if (b < 0) return a;
			return a < b ? a : b;
		}

		// 2次方程式を解く
		// At^2 + 2Bt + C = 0
		public static float SolveEquation(float A, float halfB, float C)
		{
			// 0割り禁止処理
			float t = 0;
			if (A == 0 && halfB == 0) t = 0; // t = none
			else if (A == 0) t = -C / halfB; // t = -C/B
			else // t = alpha, beta
			{
				// 虚数解無視
				float D = halfB * halfB - A * C;
				if (D < 0)
				{
					t = 0;
				}
				else
				{
					float E = Mathf.Sqrt(D);
					t = PlusMin((-halfB - E) / A, (-halfB + E) / A);
				}
			}
			return t;
		}

		// 三角形の頂点3点の位置から外心の位置を返す
		public static Vector3 Circumcenter(Vector3 posA, Vector3 posB, Vector3 posC)
		{
			// 3辺の長さの2乗
			float edgeA = Vector3.SqrMagnitude(posB - posC);
			float edgeB = Vector3.SqrMagnitude(posC - posA);
			float edgeC = Vector3.SqrMagnitude(posA - posB);

			// 重心座標系で計算
			float a = edgeA * (-edgeA + edgeB + edgeC);
			float b = edgeB * (edgeA - edgeB + edgeC);
			float c = edgeC * (edgeA + edgeB - edgeC);

			if (a + b + c == 0) return (posA + posB + posC) / 3; // 0割り禁止処理
			return (posA * a + posB * b + posC * c) / (a + b + c);
		}

		// 目標位置をセンター位置で軸と角度で回転させた値を返す
		public static Vector3 RotateToPosition(Vector3 v3_target, Vector3 v3_center, Vector3 v3_axis, float f_angle)
		{
			return Quaternion.AngleAxis(f_angle, v3_axis) * (v3_target - v3_center) + v3_center;
		}
		public static Vector3 RotateToPosition(Vector3 v3_target, Vector3 v3_center, Vector3 v3_angularVelo)
		{
			return Quaternion.Euler(v3_angularVelo) * (v3_target - v3_center) + v3_center;
		}
	}
}