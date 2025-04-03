using Photon.Pun;
using UnityEngine;
using UnityEngine.AI;

using StoneBaneEnemy;
namespace StoneBaneEnemy.AI;

public class EnemyStoneBane : MonoBehaviour
{
	public enum State
	{
		Spawn = 0,
		Idle = 1,
		Roam = 2,
		Investigate = 3,
		Notice = 4,
		GoToPlayer = 5,
		Sneak = 6,
		ChargeAttack = 7,
		DelayAttack = 8,
		Attack = 9,
		Stun = 10,
		Leave = 11,
		Despawn = 12,
		HandSwingAttack = 13,
		HandSwingAttackTriggered = 14,
		Grab = 15
	}

	public State currentState;
	
	public Transform visionTransform;

	public float stateTimer;

	public EnemyStoneBaneAnim animator;

	public ParticleSystem particleDeathImpact;

	public ParticleSystem particleDeathBitsFar;

	public ParticleSystem particleDeathBitsShort;

	public ParticleSystem particleDeathSmoke;

	public SpringQuaternion rotationSpring;

	private Quaternion rotationTarget;

	private bool stateImpulse = true;

	internal PlayerAvatar targetPlayer;

	public Enemy enemy;

	private PhotonView photonView;

	private Vector3 agentDestination;

	private Vector3 backToNavMeshPosition;

	private Vector3 stuckAttackTarget;

	private Vector3 targetPosition;

	private float visionTimer;

	private bool visionPrevious;

	public Transform feetTransform;

	public Transform followParentTransform;

	public AnimationCurve followParentCurve;

	private float followParentLerp;

	private float grabAggroTimer;

	private int attackCount;

	private int attacks;
	
	private bool attackImpulse;

	private void Awake()
	{
		photonView = GetComponent<PhotonView>();
	}

	private void Update()
	{
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			FloatingAnimation();
			if (enemy.CurrentState == EnemyState.Despawn && !enemy.IsStunned() && currentState == State.Idle)
			{
				UpdateState(State.Despawn);
			}
			if (enemy.IsStunned())
			{
				UpdateState(State.Stun);
			}
			switch (currentState)
			{
			case State.Spawn:
				StateSpawn();
				break;
			case State.Idle:
				StateIdle();
				break;
			case State.Roam:
				StateRoam();
				break;
			case State.Investigate:
				StateInvestigate();
				break;
			case State.Notice:
				StateNotice();
				break;
			case State.GoToPlayer:
				StateGoToPlayer();
				break;
			case State.Sneak:
				StateSneak();
				break;
			case State.Stun:
				StateStun();
				break;
			case State.Leave:
				StateLeave();
				break;
			case State.Despawn:
				StateDespawn();
				break;
			case State.HandSwingAttack:
				StateHandSwingAttack();
				break;
			}
			RotationLogic();
			TimerLogic();
		}

		if (currentState == State.HandSwingAttackTriggered && (bool)targetPlayer)
		{
			if (targetPlayer.isLocal)
			{
				PlayerController.instance.InputDisable(0.1f);
				CameraAim.Instance.AimTargetSet(visionTransform.position, 0.1f, 5f, base.gameObject, 90);
				CameraZoom.Instance.OverrideZoomSet(100f, 0.1f, 5f, 5f, base.gameObject, 50);
				Color color = new Color(0.4f, 0f, 0f, 1f);
				PostProcessing.Instance.VignetteOverride(color, 0.75f, 1f, 3.5f, 2.5f, 0.5f, base.gameObject);
			}
			if (attackImpulse)
			{
				if (targetPlayer.isLocal)
				{
					targetPlayer.physGrabber.ReleaseObject();
					CameraGlitch.Instance.PlayLong();
				}
				attackImpulse = false;
				animator.OnHandSwing();
			}
		}
		else
		{
			attackImpulse = true;
		}
	}

	public void StateSpawn()
	{
		if (stateImpulse)
		{
			stateImpulse = false;
			stateTimer = 1f;
		}
		stateTimer -= Time.deltaTime;
		if (stateTimer <= 0f)
		{
			UpdateState(State.Idle);
		}
	}

	public void StateIdle()
	{
		if (stateImpulse)
		{
			stateImpulse = false;
			stateTimer = 1f;
			enemy.NavMeshAgent.Warp(feetTransform.position);
			enemy.NavMeshAgent.ResetPath();
		}
		if (!SemiFunc.EnemySpawnIdlePause())
		{
			stateTimer -= Time.deltaTime;
			if (stateTimer <= 0f)
			{
				UpdateState(State.Roam);
			}
			if (SemiFunc.EnemyForceLeave(enemy))
			{
				UpdateState(State.Leave);
			}
		}
	}

	public void StateRoam()
	{
		if (stateImpulse)
		{
			stateTimer = 5f;
			bool flag = false;
			LevelPoint levelPoint = SemiFunc.LevelPointGet(base.transform.position, 10f, 25f);
			if (!levelPoint)
			{
				levelPoint = SemiFunc.LevelPointGet(base.transform.position, 0f, 999f);
			}
			if ((bool)levelPoint && NavMesh.SamplePosition(levelPoint.transform.position + Random.insideUnitSphere * 3f, out var hit, 5f, -1) && Physics.Raycast(hit.position, Vector3.down, 5f, LayerMask.GetMask("Default")))
			{
				agentDestination = hit.position;
				flag = true;
			}
			if (!flag)
			{
				return;
			}
			enemy.Rigidbody.notMovingTimer = 0f;
			stateImpulse = false;
		}
		else
		{
			enemy.NavMeshAgent.SetDestination(agentDestination);
			if (enemy.Rigidbody.notMovingTimer > 3f)
			{
				stateTimer -= Time.deltaTime;
			}
			if (stateTimer <= 0f || Vector3.Distance(base.transform.position, agentDestination) < 1f)
			{
				UpdateState(State.Idle);
			}
		}
		if (SemiFunc.EnemyForceLeave(enemy))
		{
			UpdateState(State.Leave);
		}
	}

	public void StateInvestigate()
	{
		if (stateImpulse)
		{
			stateImpulse = false;
			stateTimer = 5f;
			enemy.Rigidbody.notMovingTimer = 0f;
		}
		else
		{
			enemy.NavMeshAgent.SetDestination(agentDestination);
			if (enemy.Rigidbody.notMovingTimer > 2f)
			{
				stateTimer -= Time.deltaTime;
			}
			if (stateTimer <= 0f)
			{
				UpdateState(State.Idle);
				return;
			}
			if (Vector3.Distance(base.transform.position, agentDestination) < 2f)
			{
				UpdateState(State.Idle);
			}
		}
		if (SemiFunc.EnemyForceLeave(enemy))
		{
			UpdateState(State.Leave);
		}
	}

	public void StateNotice()
	{
		if (stateImpulse)
		{
			stateImpulse = false;
			stateTimer = 1f;
			enemy.NavMeshAgent.Warp(feetTransform.position);
			enemy.NavMeshAgent.ResetPath();
		}
		stateTimer -= Time.deltaTime;
		if (stateTimer <= 0f)
		{
			if (Vector3.Distance(feetTransform.position, targetPlayer.transform.position) < 2.0f)
			{
				UpdateState(State.HandSwingAttack);
			}
			else
			{
				UpdateState(State.GoToPlayer);
			}
		}
	}

	public void StateGoToPlayer()
	{
		if (!targetPlayer)
		{
			UpdateState(State.Idle);
			return;
		}
		if (stateImpulse)
		{
			stateImpulse = false;
			stateTimer = 2f;
		}
		targetPosition = targetPlayer.transform.position;
		enemy.NavMeshAgent.SetDestination(targetPosition);
		enemy.NavMeshAgent.OverrideAgent(2f, enemy.NavMeshAgent.DefaultAcceleration, 0.2f);
		stateTimer -= Time.deltaTime;
		if (stateTimer <= 0f)
		{
			UpdateState(State.Idle);
		}
		else if (Vector3.Distance(feetTransform.position, enemy.NavMeshAgent.GetPoint()) < 2f && stateTimer > 1.5f)
		{
			UpdateState(State.HandSwingAttack);
		}
	}

	public void StateSneak()
	{
		if (!targetPlayer)
		{
			UpdateState(State.Idle);
			return;
		}
		if (stateImpulse)
		{
			stateImpulse = false;
			stateTimer = 2f;
			enemy.Rigidbody.notMovingTimer = 0f;
			enemy.NavMeshAgent.Warp(feetTransform.position);
			enemy.NavMeshAgent.ResetPath();
		}
		targetPosition = targetPlayer.transform.position;
		enemy.NavMeshAgent.SetDestination(targetPosition);
		enemy.NavMeshAgent.OverrideAgent(1.5f, enemy.NavMeshAgent.DefaultAcceleration, 0.2f);
		stateTimer -= Time.deltaTime;
		if (stateTimer <= 0f)
		{
			UpdateState(State.Idle);
		}
		else if (Vector3.Distance(feetTransform.position, enemy.NavMeshAgent.GetPoint()) < 2f || enemy.OnScreen.OnScreenAny)
		{
			Debug.Log("this notice1 get triggerd");
			UpdateState(State.Notice);
		}
	}
	public void StateChargeAttack()
	{
		if (stateImpulse)
		{
			stateImpulse = false;
			stateTimer = 7f;
			enemy.NavMeshAgent.Warp(feetTransform.position);
			enemy.NavMeshAgent.ResetPath();
		}
		stateTimer -= Time.deltaTime;
		if (stateTimer <= 0f)
		{
			UpdateState(State.DelayAttack);
		}
	}

	public void StateDelayAttack()
	{
		if (stateImpulse)
		{
			stateImpulse = false;
			stateTimer = 3f;
		}
		stateTimer -= Time.deltaTime;
		if (stateTimer <= 0f)
		{
			UpdateState(State.HandSwingAttack);
		}
	}

	public void StateHandSwingAttack()
	{
		if (stateImpulse)
		{
			attacks++;
			stateTimer = 1.5f;
			stateImpulse = false;
			enemy.NavMeshAgent.ResetPath();
			enemy.NavMeshAgent.Warp(enemy.Rigidbody.transform.position);
		}
		stateTimer -= Time.deltaTime;
		if (stateTimer <= 0f)
		{
			Debug.Log("I'm attacking fuah fuah fuah");
			animator.animator.SetTrigger("HandSwing");
			if (attacks >= 3 || Random.Range(0f, 1f) <= 0.1f)
			{
				attacks = 0;
				UpdateState(State.Leave); // here besides that must be the trigger to start another attack after certain delay, but it will be implemented latar
			}
			else
			{
				UpdateState(State.Idle);
			}
		}
	}
	
	public void StateAttack()
	{
		if (stateImpulse)
		{
			stateImpulse = false;
			stateTimer = 2f;
			attackCount++;
		}
		stateTimer -= Time.deltaTime;
		if (stateTimer <= 0f)
		{
			if (attackCount >= 3 || Random.Range(0f, 1f) <= 0.3f)
			{
				attackCount = 0;
				UpdateState(State.Leave);
			}
			else
			{
				UpdateState(State.Idle);
			}
		}
	}

	public void StateStun()
	{
		enemy.NavMeshAgent.Disable(0.1f);
		base.transform.position = enemy.Rigidbody.transform.position;
		if (!enemy.IsStunned())
		{
			UpdateState(State.Idle);
		}
	}

	public void StateLeave()
	{
		if (stateImpulse)
		{
			stateImpulse = false;
			stateTimer = 5f;
			bool flag = false;
			LevelPoint levelPoint = SemiFunc.LevelPointGetPlayerDistance(base.transform.position, 30f, 50f);
			if (!levelPoint)
			{
				levelPoint = SemiFunc.LevelPointGetFurthestFromPlayer(base.transform.position, 5f);
			}
			if ((bool)levelPoint && NavMesh.SamplePosition(levelPoint.transform.position + Random.insideUnitSphere * 3f, out var hit, 5f, -1) && Physics.Raycast(hit.position, Vector3.down, 5f, LayerMask.GetMask("Default")))
			{
				agentDestination = hit.position;
				flag = true;
			}
			if (!flag)
			{
				return;
			}
		}
		if (enemy.Rigidbody.notMovingTimer > 3f)
		{
			stateTimer -= Time.deltaTime;
		}
		enemy.NavMeshAgent.SetDestination(agentDestination);
		enemy.NavMeshAgent.OverrideAgent(1.5f, enemy.NavMeshAgent.DefaultAcceleration, 0.2f);
		if (Vector3.Distance(base.transform.position, agentDestination) < 1f || stateTimer <= 0f)
		{
			UpdateState(State.Idle);
		}
	}

	public void StateDespawn()
	{
		if (stateImpulse)
		{
			stateImpulse = false;
			enemy.NavMeshAgent.Warp(feetTransform.position);
			enemy.NavMeshAgent.ResetPath();
		}
	}

	public void OnSpawn()
	{
		Debug.Log("I'm spawned, fuah fuah fuah");
		if (SemiFunc.IsMasterClientOrSingleplayer() && SemiFunc.EnemySpawn(enemy))
		{
			UpdateState(State.Spawn);
		}
	}

	public void OnHurt()
	{
		animator.sfxHurt.Play(animator.transform.position);
		if (SemiFunc.IsMasterClientOrSingleplayer() && currentState == State.Leave)
		{
			UpdateState(State.Idle);
		}
	}

	public void OnDeath()
	{
		particleDeathImpact.transform.position = enemy.CenterTransform.position;
		particleDeathImpact.Play();
		particleDeathBitsFar.transform.position = enemy.CenterTransform.position;
		particleDeathBitsFar.Play();
		particleDeathBitsShort.transform.position = enemy.CenterTransform.position;
		particleDeathBitsShort.Play();
		particleDeathSmoke.transform.position = enemy.CenterTransform.position;
		particleDeathSmoke.Play();
		animator.SfxDeath();
		GameDirector.instance.CameraShake.ShakeDistance(3f, 3f, 10f, base.transform.position, 0.5f);
		GameDirector.instance.CameraImpact.ShakeDistance(3f, 3f, 10f, base.transform.position, 0.05f);
		if (SemiFunc.IsMasterClientOrSingleplayer())
		{
			enemy.EnemyParent.Despawn();
		}
	}

	public void OnInvestigate()
	{
		if (SemiFunc.IsMasterClientOrSingleplayer() && (currentState == State.Idle || currentState == State.Roam || currentState == State.Investigate))
		{
			agentDestination = enemy.StateInvestigate.onInvestigateTriggeredPosition;
			UpdateState(State.Investigate);
		}
	}

	public void OnVision()
	{
		if (enemy.CurrentState == EnemyState.Despawn)
		{
			return;
		}
		if (currentState == State.Roam || currentState == State.Idle || currentState == State.Investigate)
		{
			targetPlayer = enemy.Vision.onVisionTriggeredPlayer;
			if (!enemy.OnScreen.OnScreenAny)
			{
				UpdateState(State.Sneak);
			}
			else
			{			
				Debug.Log("this notice2 get triggerd");
				UpdateState(State.Notice);
			}
			if (GameManager.Multiplayer())
			{
				photonView.RPC("TargetPlayerRPC", RpcTarget.All, targetPlayer.photonView.ViewID);
			}
		}
		else if ((currentState == State.GoToPlayer || currentState == State.Sneak) && targetPlayer == enemy.Vision.onVisionTriggeredPlayer)
		{
			stateTimer = 2f;
		}
	}

	public void OnGrabbed()
	{
		if (!SemiFunc.IsMasterClientOrSingleplayer() || grabAggroTimer > 0f || currentState != State.Leave)
		{
			return;
		}
		grabAggroTimer = 60f;
		PlayerAvatar onGrabbedPlayerAvatar = enemy.Rigidbody.onGrabbedPlayerAvatar;
		if (onGrabbedPlayerAvatar.transform.position.y - enemy.transform.position.y > 1.15f || onGrabbedPlayerAvatar.transform.position.y - enemy.transform.position.y < -1f)
		{
			return;
		}
		targetPlayer = onGrabbedPlayerAvatar;
		if (!enemy.IsStunned())
		{
			if (GameManager.Multiplayer())
			{
				photonView.RPC("NoticeRPC", RpcTarget.All, targetPlayer.photonView.ViewID);
			}
			else
			{
				NoticeRPC(targetPlayer.photonView.ViewID);
			}
		}
		UpdateState(State.Notice);
	}

	private void UpdateState(State _state)
	{
		if (currentState != _state)
		{
			enemy.Rigidbody.notMovingTimer = 0f;
			currentState = _state;
			stateImpulse = true;
			stateTimer = 0f;
			if (GameManager.Multiplayer())
			{
				photonView.RPC("UpdateStateRPC", RpcTarget.All, currentState);
			}
			else
			{
				UpdateStateRPC(currentState);
			}
		}
	}

	private void FloatingAnimation()
	{
		float num = 0.1f;
		float num2 = 0.4f;
		float t = followParentCurve.Evaluate(followParentLerp);
		float num3 = Mathf.Lerp(0f - num, num, t);
		float num4 = 0f;
		Vector3 localPosition = new Vector3(followParentTransform.localPosition.x, num3 + num4, followParentTransform.localPosition.z);
		followParentLerp += Time.deltaTime * num2;
		if (followParentLerp > 1f)
		{
			followParentLerp = 0f;
		}
		followParentTransform.localPosition = localPosition;
	}

	private void RotationLogic()
	{
		if (currentState == State.Notice)
		{
			if ((bool)targetPlayer && Vector3.Distance(targetPlayer.transform.position, enemy.Rigidbody.transform.position) > 0.1f)
			{
				rotationTarget = Quaternion.LookRotation(targetPlayer.transform.position - enemy.Rigidbody.transform.position);
				rotationTarget.eulerAngles = new Vector3(0f, rotationTarget.eulerAngles.y, 0f);
			}
		}
		else if (enemy.NavMeshAgent.AgentVelocity.normalized.magnitude > 0.1f)
		{
			rotationTarget = Quaternion.LookRotation(enemy.NavMeshAgent.AgentVelocity.normalized);
			rotationTarget.eulerAngles = new Vector3(0f, rotationTarget.eulerAngles.y, 0f);
		}
		base.transform.rotation = SemiFunc.SpringQuaternionGet(rotationSpring, rotationTarget);
	}

	private void TimerLogic()
	{
		visionTimer -= Time.deltaTime;
	}

	[PunRPC]
	private void UpdateStateRPC(State _state)
	{
		currentState = _state;
		if (currentState == State.Spawn)
		{
			animator.OnSpawn();
		}
	}

	[PunRPC]
	private void TargetPlayerRPC(int _playerID)
	{
		foreach (PlayerAvatar player in GameDirector.instance.PlayerList)
		{
			if (player.photonView.ViewID == _playerID)
			{
				targetPlayer = player;
			}
		}
	}

	[PunRPC]
	private void NoticeRPC(int _playerID)
	{
		animator.NoticeSet(_playerID);
	}
}
