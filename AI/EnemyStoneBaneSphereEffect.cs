using UnityEngine;

namespace StoneBaneEnemy.AI;

public class EnemyStoneBaneSphereEffect : MonoBehaviour
{
	public enum StoneBaneSphereEffectState
	{
		levitate = 0,
		stop = 1,
		smash = 2
	}

	private MeshRenderer meshRenderer;

	private Light lightSphere;

	private StoneBaneAttackLogic stonebaneAttack;

	private bool stateStart = true;

	private float originalScale;

	private Color originalLightColor;

	private float originalLightIntensity;

	private float originalLightRange;

	private int myChildNumber;

	private Color originalMaterialColor;

	internal StoneBaneSphereEffectState state;

	private void Start()
	{
		meshRenderer = GetComponent<MeshRenderer>();
		lightSphere = GetComponentInChildren<Light>();
		stonebaneAttack = GetComponentInParent<StoneBaneAttackLogic>();
		originalScale = base.transform.localScale.x;
		myChildNumber = base.transform.GetSiblingIndex();
		originalMaterialColor = meshRenderer.material.color;
		if ((bool)lightSphere)
		{
			originalLightColor = lightSphere.color;
			originalLightIntensity = lightSphere.intensity;
			originalLightRange = lightSphere.range;
		}
	}

	private void StateMachine()
	{
		switch (state)
		{
		case StoneBaneSphereEffectState.levitate:
			StateLevitate();
			break;
		case StoneBaneSphereEffectState.stop:
			StateStop();
			break;
		case StoneBaneSphereEffectState.smash:
			StateSmash();
			break;
		}
	}

	private void StateLevitate()
	{
		if (stateStart)
		{
			base.transform.localScale = new Vector3(originalScale, originalScale, originalScale);
			meshRenderer.material.color = originalMaterialColor;
			if ((bool)lightSphere)
			{
				lightSphere.color = originalLightColor;
				lightSphere.intensity = originalLightIntensity;
				lightSphere.range = originalLightRange;
			}
			stateStart = false;
		}
		PulseEffect();
	}

	private void StateStop()
	{
		if (stateStart)
		{
			stateStart = false;
		}
		StopEffect();
	}

	private void StateSmash()
	{
		if (stateStart)
		{
			stateStart = false;
		}
	}

	private void Update()
	{
		StateMachine();
		if (stonebaneAttack.state == StoneBaneAttackLogic.StoneBaneAttackState.levitate || stonebaneAttack.state == StoneBaneAttackLogic.StoneBaneAttackState.start)
		{
			StateSet(StoneBaneSphereEffectState.levitate);
		}
		if (stonebaneAttack.state == StoneBaneAttackLogic.StoneBaneAttackState.stop)
		{
			StateSet(StoneBaneSphereEffectState.stop);
		}
		if (stonebaneAttack.state == StoneBaneAttackLogic.StoneBaneAttackState.smash)
		{
			StateSet(StoneBaneSphereEffectState.smash);
		}
	}

	private void StateSet(StoneBaneSphereEffectState _state)
	{
		if (state != _state)
		{
			state = _state;
			stateStart = true;
		}
	}

	private void PulseEffect()
	{
		if (base.transform.parent.transform.localScale == Vector3.zero)
		{
			return;
		}
		base.transform.localScale += new Vector3(1f, 1f, 1f) * Time.deltaTime * 2f;
		Color color = meshRenderer.material.color;
		if (base.transform.localScale.magnitude > 10f)
		{
			color.a -= 1f * Time.deltaTime;
			if ((bool)lightSphere)
			{
				lightSphere.intensity = 4f * color.a;
			}
		}
		meshRenderer.material.color = color;
		if ((bool)lightSphere)
		{
			lightSphere.range = base.transform.localScale.x * 2.8f;
		}
		meshRenderer.material.mainTextureOffset += new Vector2(0.1f, 0.1f) * Time.deltaTime;
		if (color.a <= 0f)
		{
			if ((bool)lightSphere)
			{
				lightSphere.intensity = 4f;
			}
			if ((bool)lightSphere)
			{
				lightSphere.range = 0f;
			}
			base.transform.localScale = Vector3.zero;
			color.a = 1f;
			meshRenderer.material.color = color;
		}
	}

	private void StopEffect()
	{
		if ((bool)lightSphere)
		{
			Color red = Color.red;
			float num = 8f;
			float num2 = 15f;
			lightSphere.color = Color.Lerp(lightSphere.color, red, Time.deltaTime * 10f);
			lightSphere.intensity = Mathf.Lerp(lightSphere.intensity, num, Time.deltaTime * 10f);
			lightSphere.range = Mathf.Lerp(lightSphere.range, num2, Time.deltaTime * 10f);
		}
		base.transform.localScale = Vector3.Lerp(base.transform.localScale, Vector3.one, Time.deltaTime * 10f);
		base.transform.localScale += new Vector3(0.4f, 0.4f, 0.4f) * Mathf.Sin((Time.time + (float)(myChildNumber * 10)) * (float)myChildNumber * 20f) * (0.1f + (float)myChildNumber / 10f);
		meshRenderer.material.color = Color.Lerp(meshRenderer.material.color, Color.red, Time.deltaTime * 10f);
	}
}
