using System;
using UnityEngine;
namespace StoneBaneEnemy.AI;
public class StoneBaneLine : MonoBehaviour
{
	public Transform lineTarget;

	private LineRenderer lineRenderer;

	private PhysGrabObject physGrabObject;

	internal StoneBaneAttackLogic stonebaneAttack;

	internal bool outro;

	public Material redMaterial;

	internal bool redMaterialSet;

	private void Start()
	{
		lineRenderer = GetComponent<LineRenderer>();
		lineRenderer.widthMultiplier = 0f;
		physGrabObject = lineTarget.GetComponent<PhysGrabObject>();
	}

	private void Update()
	{
		if ((bool)lineTarget)
		{
			Vector3 position = base.transform.position;
			Vector3 vector = lineTarget.position;
			if ((bool)physGrabObject)
			{
				vector = physGrabObject.midPoint;
			}
			Vector3[] array = new Vector3[20];
			for (int i = 0; i < 20; i++)
			{
				float num = (float)i / 20f;
				array[i] = Vector3.Lerp(position, vector, num) + Vector3.up * Mathf.Sin(num * MathF.PI) * 1f;
				float num2 = 1f - Mathf.Abs(num - 0.5f) * 2f;
				float num3 = 1f;
				if (stonebaneAttack.state == StoneBaneAttackLogic.StoneBaneAttackState.stop)
				{
					num2 *= 3f;
					num3 = 2f;
				}
				array[i] += Vector3.right * Mathf.Sin(Time.time * (30f * num3) + (float)i) * 0.02f * num2;
				array[i] += Vector3.forward * Mathf.Cos(Time.time * (30f * num3) + (float)i) * 0.02f * num2;
			}
			lineRenderer.material.mainTextureOffset = new Vector2(Time.time * 2f, 0f);
			lineRenderer.positionCount = 20;
			lineRenderer.SetPositions(array);
		}
		else
		{
			outro = true;
		}
		if (stonebaneAttack.state == StoneBaneAttackLogic.StoneBaneAttackState.stop && !redMaterialSet)
		{
			lineRenderer.material = redMaterial;
			redMaterialSet = true;
		}
		if (!outro)
		{
			if (lineRenderer.widthMultiplier < 0.195f)
			{
				lineRenderer.widthMultiplier = Mathf.Lerp(lineRenderer.widthMultiplier, 0.2f, Time.deltaTime * 2f);
			}
			else
			{
				lineRenderer.widthMultiplier = 0.2f;
			}
		}
		else if (lineRenderer.widthMultiplier > 0.005f)
		{
			lineRenderer.widthMultiplier = Mathf.Lerp(lineRenderer.widthMultiplier, 0f, Time.deltaTime * 2f);
		}
		else
		{
			lineRenderer.widthMultiplier = 0f;
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}
}
