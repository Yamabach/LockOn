using System;
using UnityEngine;

namespace LOSpace{
	public static class Utility
	{
		// �v���X�̍ŏ��l��Ԃ��i�����}�C�i�X�Ȃ�0�j
		public static float PlusMin(float a, float b)
		{
			if (a < 0 && b < 0) return 0f;
			if (a < 0) return b;
			if (b < 0) return a;
			return a < b ? a : b;
		}

		// 2��������������
		// At^2 + 2Bt + C = 0
		public static float SolveEquation(float A, float halfB, float C)
		{
			// 0����֎~����
			float t = 0;
			if (A == 0 && halfB == 0) t = 0; // t = none
			else if (A == 0) t = -C / halfB; // t = -C/B
			else // t = alpha, beta
			{
				// �����𖳎�
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

		// �O�p�`�̒��_3�_�̈ʒu����O�S�̈ʒu��Ԃ�
		public static Vector3 Circumcenter(Vector3 posA, Vector3 posB, Vector3 posC)
		{
			// 3�ӂ̒�����2��
			float edgeA = Vector3.SqrMagnitude(posB - posC);
			float edgeB = Vector3.SqrMagnitude(posC - posA);
			float edgeC = Vector3.SqrMagnitude(posA - posB);

			// �d�S���W�n�Ōv�Z
			float a = edgeA * (-edgeA + edgeB + edgeC);
			float b = edgeB * (edgeA - edgeB + edgeC);
			float c = edgeC * (edgeA + edgeB - edgeC);

			if (a + b + c == 0) return (posA + posB + posC) / 3; // 0����֎~����
			return (posA * a + posB * b + posC * c) / (a + b + c);
		}

		// �ڕW�ʒu���Z���^�[�ʒu�Ŏ��Ɗp�x�ŉ�]�������l��Ԃ�
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