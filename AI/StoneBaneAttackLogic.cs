using System.Collections.Generic;
using UnityEngine;
namespace StoneBaneEnemy.AI;
public class StoneBaneAttackLogic : MonoBehaviour
{
	public enum StoneBaneAttackState
	{
		start = 0,
		levitate = 1,
		stop = 2,
		smash = 3,
		end = 4,
		inactive = 5
	}

	public GameObject linePrefab;

	public ParticleSystem upParticle;

	public ParticleSystem downParticle;

	public EnemyStoneBane controller;

	public PhysGrabObject enemyStoneBanePhysGrabObject;

	internal int damage = 50;

	internal StoneBaneAttackState state = StoneBaneAttackState.inactive;

	private bool stateStart = true;

	public Transform sphereEffects;

	public Light attackLight;

	private float range = 4f;

	private List<PlayerAvatar> capturedPlayerAvatars = new List<PlayerAvatar>();

	private List<PhysGrabObject> capturedPhysGrabObjects = new List<PhysGrabObject>();

	private List<StoneBaneLine> stonebaneLines = new List<StoneBaneLine>();

	private float checkTimer;

	private int particleCount;

	private float tumblePhysObjectCheckTimer;

	private void StateMachine()
	{
		if (controller.currentState == EnemyStoneBane.State.ChargeAttack || controller.currentState == EnemyStoneBane.State.DelayAttack || controller.currentState == EnemyStoneBane.State.Attack)
		{
			switch (controller.currentState)
			{
			case EnemyStoneBane.State.ChargeAttack:
				if (state != StoneBaneAttackState.levitate)
				{
					StateSet(StoneBaneAttackState.start);
				}
				break;
			case EnemyStoneBane.State.Stun:
				StateSet(StoneBaneAttackState.end);
				break;
			}
		}
		else
		{
			StateSet(StoneBaneAttackState.end);
		}
		switch (state)
		{
		case StoneBaneAttackState.start:
			StateStart();
			break;
		case StoneBaneAttackState.levitate:
			StateLevitate();
			break;
		case StoneBaneAttackState.stop:
			StateStop();
			break;
		case StoneBaneAttackState.smash:
			StateSmash();
			break;
		case StoneBaneAttackState.end:
			StateEnd();
			break;
		case StoneBaneAttackState.inactive:
			StateInactive();
			break;
		}
	}

	private void Reset()
	{
		checkTimer = 0f;
		particleCount = 0;
		tumblePhysObjectCheckTimer = 0f;
		foreach (StoneBaneLine stonebaneLine in stonebaneLines)
		{
			if ((bool)stonebaneLine)
			{
				stonebaneLine.outro = true;
			}
		}
		capturedPlayerAvatars.Clear();
		capturedPhysGrabObjects.Clear();
		stonebaneLines.Clear();
		sphereEffects.localScale = Vector3.zero;
		attackLight.intensity = 0f;
		sphereEffects.gameObject.SetActive(value: false);
	}

	private void StateInactive()
	{
		if (stateStart)
		{
			Reset();
			stateStart = false;
		}
	}

	private void StateEnd()
	{
		if (stateStart)
		{
			foreach (StoneBaneLine stonebaneLine in stonebaneLines)
			{
				if ((bool)stonebaneLine)
				{
					stonebaneLine.outro = true;
				}
			}
			stateStart = false;
		}
		if (sphereEffects.gameObject.activeSelf)
		{
			sphereEffects.localScale = Vector3.Lerp(sphereEffects.localScale, Vector3.zero, Time.deltaTime * 20f);
			attackLight.intensity = Mathf.Lerp(attackLight.intensity, 0f, Time.deltaTime * 20f);
			if (sphereEffects.localScale.x < 0.01f)
			{
				StateSet(StoneBaneAttackState.inactive);
			}
		}
		else
		{
			StateSet(StoneBaneAttackState.inactive);
		}
	}

	private void StateStart()
	{
		if (stateStart)
		{
			Reset();
			sphereEffects.gameObject.SetActive(value: true);
			stateStart = false;
		}
		sphereEffects.localScale = Vector3.Lerp(sphereEffects.localScale, Vector3.one * 1.2f, Time.deltaTime * 6f);
		attackLight.intensity = 4f * sphereEffects.localScale.magnitude;
		if (sphereEffects.localScale.x > 1.19f)
		{
			attackLight.intensity = 4f;
			sphereEffects.localScale = Vector3.one * 1.2f;
			StateSet(StoneBaneAttackState.levitate);
		}
	}

	private void StateLevitate()
	{
		if (stateStart)
		{
			stateStart = false;
			GetAllWithinRange();
		}
		if (checkTimer > 0.35f)
		{
			GetAllWithinRange();
			checkTimer = 0f;
		}
		checkTimer += Time.deltaTime;
		if (!SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		foreach (PlayerAvatar capturedPlayerAvatar in capturedPlayerAvatars)
		{
			capturedPlayerAvatar.tumble.TumbleOverrideTime(2f);
			PlayerTumble(capturedPlayerAvatar);
		}
		foreach (PhysGrabObject capturedPhysGrabObject in capturedPhysGrabObjects)
		{
			if ((bool)capturedPhysGrabObject && capturedPhysGrabObject.isEnemy)
			{
				Enemy enemy = capturedPhysGrabObject.GetComponent<EnemyRigidbody>().enemy;
				if ((bool)enemy && enemy.HasStateStunned && enemy.Type < EnemyType.Heavy)
				{
					enemy.StateStunned.Set(4f);
				}
			}
			capturedPhysGrabObject.OverrideZeroGravity();
		}
	}

	private void StateStop()
	{
		if (stateStart)
		{
			checkTimer = 0f;
			stateStart = false;
		}
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			foreach (PlayerAvatar capturedPlayerAvatar in capturedPlayerAvatars)
			{
				if ((bool)capturedPlayerAvatar)
				{
					capturedPlayerAvatar.tumble.TumbleOverrideTime(2f);
				}
			}
		}
		checkTimer += Time.deltaTime;
		if (checkTimer > 0.35f)
		{
			RemoveAllOutOfRange();
			checkTimer = 0f;
		}
	}

	private void StateSmash()
	{
		if (stateStart)
		{
			GameDirector.instance.CameraShake.ShakeDistance(6f, 3f, 8f, base.transform.position, 0.1f);
			GameDirector.instance.CameraImpact.ShakeDistance(8f, 3f, 8f, base.transform.position, 0.1f);
			foreach (PhysGrabObject capturedPhysGrabObject in capturedPhysGrabObjects)
			{
				if ((bool)capturedPhysGrabObject)
				{
					downParticle.transform.position = capturedPhysGrabObject.midPoint;
					downParticle.Emit(1);
				}
			}
			if (SemiFunc.IsMasterClientOrSingleplayer())
			{
				foreach (PlayerAvatar capturedPlayerAvatar in capturedPlayerAvatars)
				{
					if ((bool)capturedPlayerAvatar && capturedPlayerAvatar.tumble.isTumbling)
					{
						capturedPlayerAvatar.tumble.TumbleOverrideTime(2f);
						capturedPlayerAvatar.tumble.ImpactHurtSet(2f, damage);
					}
				}
				foreach (PhysGrabObject capturedPhysGrabObject2 in capturedPhysGrabObjects)
				{
					if ((bool)capturedPhysGrabObject2 && (bool)capturedPhysGrabObject2 && (bool)capturedPhysGrabObject2.rb && !capturedPhysGrabObject2.rb.isKinematic)
					{
						capturedPhysGrabObject2.rb.AddForce(Vector3.down * 30f, ForceMode.Impulse);
					}
				}
			}
			foreach (StoneBaneLine stonebaneLine in stonebaneLines)
			{
				if ((bool)stonebaneLine)
				{
					stonebaneLine.outro = true;
				}
			}
			stonebaneLines.Clear();
			capturedPlayerAvatars.Clear();
			capturedPhysGrabObjects.Clear();
			stateStart = false;
		}
		sphereEffects.localScale = Vector3.Lerp(sphereEffects.localScale, Vector3.zero, Time.deltaTime * 2f);
		if (sphereEffects.localScale.x > 0.5f)
		{
			attackLight.intensity = Mathf.Lerp(attackLight.intensity, 20f, Time.deltaTime * 60f);
		}
		else
		{
			attackLight.intensity = 20f * sphereEffects.localScale.magnitude;
		}
		if (sphereEffects.localScale.x < 0.01f)
		{
			StateSet(StoneBaneAttackState.inactive);
		}
	}

	private void RemoveAllOutOfRange()
	{
		for (int num = capturedPlayerAvatars.Count - 1; num >= 0; num--)
		{
			PlayerAvatar playerAvatar = capturedPlayerAvatars[num];
			if (!playerAvatar)
			{
				capturedPlayerAvatars.RemoveAt(num);
			}
			else if (Vector3.Distance(new Vector3(playerAvatar.transform.position.x, base.transform.position.y, playerAvatar.transform.position.z), base.transform.position) > range * 1.2f)
			{
				capturedPlayerAvatars.RemoveAt(num);
				foreach (StoneBaneLine stonebaneLine in stonebaneLines)
				{
					if ((bool)stonebaneLine && stonebaneLine.lineTarget == playerAvatar.PlayerVisionTarget.VisionTransform)
					{
						stonebaneLine.outro = true;
					}
				}
			}
		}
		for (int num2 = capturedPhysGrabObjects.Count - 1; num2 >= 0; num2--)
		{
			PhysGrabObject physGrabObject = capturedPhysGrabObjects[num2];
			if (!physGrabObject)
			{
				capturedPhysGrabObjects.RemoveAt(num2);
			}
			else if (Vector3.Distance(new Vector3(physGrabObject.transform.position.x, base.transform.position.y, physGrabObject.transform.position.z), base.transform.position) > range * 1.2f)
			{
				capturedPhysGrabObjects.RemoveAt(num2);
			}
		}
	}

	private void StateLevitateFixed()
	{
		if (state != StoneBaneAttackState.levitate)
		{
			return;
		}
		if (tumblePhysObjectCheckTimer > 1f)
		{
			foreach (PlayerAvatar capturedPlayerAvatar in capturedPlayerAvatars)
			{
				if (capturedPlayerAvatar.tumble.isTumbling)
				{
					PhysGrabObject physGrabObject = capturedPlayerAvatar.tumble.physGrabObject;
					if (!capturedPhysGrabObjects.Contains(physGrabObject))
					{
						capturedPhysGrabObjects.Add(physGrabObject);
					}
				}
			}
			tumblePhysObjectCheckTimer = 0f;
		}
		else
		{
			tumblePhysObjectCheckTimer += Time.fixedDeltaTime;
		}
		foreach (PhysGrabObject capturedPhysGrabObject in capturedPhysGrabObjects)
		{
			if ((bool)capturedPhysGrabObject)
			{
				float num = 10f;
				if ((bool)capturedPhysGrabObject.GetComponent<PlayerTumble>())
				{
					num = 20f;
				}
				if ((bool)capturedPhysGrabObject && (bool)capturedPhysGrabObject.rb && !capturedPhysGrabObject.rb.isKinematic)
				{
					capturedPhysGrabObject.rb.AddForce(Vector3.up * Time.fixedDeltaTime * num, ForceMode.Force);
					capturedPhysGrabObject.rb.AddTorque(Vector3.up * Time.fixedDeltaTime * 0.2f, ForceMode.Force);
					capturedPhysGrabObject.rb.AddTorque(Vector3.left * Time.fixedDeltaTime * 0.1f, ForceMode.Force);
					capturedPhysGrabObject.rb.velocity = Vector3.Lerp(capturedPhysGrabObject.rb.velocity, new Vector3(0f, capturedPhysGrabObject.rb.velocity.y, 0f), Time.fixedDeltaTime * 2f);
				}
			}
		}
		if (particleCount < capturedPhysGrabObjects.Count)
		{
			if ((bool)capturedPhysGrabObjects[particleCount])
			{
				Vector3 position = capturedPhysGrabObjects[particleCount].transform.position;
				Vector3 vector = Random.insideUnitSphere * 2f;
				vector.y = 0f - Mathf.Abs(vector.y);
				position += vector;
				upParticle.transform.position = position;
				upParticle.Emit(1);
			}
			particleCount++;
		}
		else
		{
			particleCount = 0;
		}
	}

	private void StateStopFixed()
	{
		if (state != StoneBaneAttackState.stop || !SemiFunc.IsMasterClientOrSingleplayer())
		{
			return;
		}
		foreach (PhysGrabObject capturedPhysGrabObject in capturedPhysGrabObjects)
		{
			if (!capturedPhysGrabObject || !capturedPhysGrabObject.rb || capturedPhysGrabObject.rb.isKinematic)
			{
				continue;
			}
			capturedPhysGrabObject.OverrideZeroGravity();
			if (capturedPhysGrabObject.isEnemy)
			{
				Enemy enemy = capturedPhysGrabObject.GetComponent<EnemyRigidbody>().enemy;
				if ((bool)enemy && enemy.HasStateStunned && enemy.Type < EnemyType.Heavy)
				{
					enemy.StateStunned.Set(4f);
				}
			}
			capturedPhysGrabObject.rb.velocity = Vector3.Lerp(capturedPhysGrabObject.rb.velocity, Vector3.zero, Time.deltaTime * 2f);
		}
	}

	private void FixedUpdate()
	{
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			StateLevitateFixed();
			StateStopFixed();
		}
	}

	private void Update()
	{
		StateMachine();
	}

	public void StateSet(StoneBaneAttackState _state)
	{
		if (state != _state)
		{
			state = _state;
			stateStart = true;
		}
	}

	public void GetAllWithinRange()
	{
		RemoveAllOutOfRange();
		foreach (PlayerAvatar item in SemiFunc.PlayerGetAllPlayerAvatarWithinRange(range, base.transform.position))
		{
			if (!capturedPlayerAvatars.Contains(item))
			{
				capturedPlayerAvatars.Add(item);
				PlayerTumble(item);
				StoneBaneLine component = Object.Instantiate(linePrefab, base.transform.position, Quaternion.identity, base.transform).GetComponent<StoneBaneLine>();
				component.lineTarget = item.PlayerVisionTarget.VisionTransform;
				component.stonebaneAttack = this;
				stonebaneLines.Add(component);
			}
		}
		foreach (PhysGrabObject item2 in SemiFunc.PhysGrabObjectGetAllWithinRange(range, base.transform.position))
		{
			if (!(item2 == enemyStoneBanePhysGrabObject) && !capturedPhysGrabObjects.Contains(item2))
			{
				capturedPhysGrabObjects.Add(item2);
			}
		}
	}

	private void PlayerTumble(PlayerAvatar _player)
	{
		if (SemiFunc.IsMasterClientOrSingleplayer() && (bool)_player && !_player.isDisabled)
		{
			if (!_player.tumble.isTumbling)
			{
				_player.tumble.TumbleRequest(_isTumbling: true, _playerInput: false);
			}
			_player.tumble.TumbleOverrideTime(2f);
		}
	}

	private void OnEnable()
	{
		StateSet(StoneBaneAttackState.inactive);
	}

	private void OnDisable()
	{
		StateSet(StoneBaneAttackState.inactive);
		StateInactive();
	}
}
