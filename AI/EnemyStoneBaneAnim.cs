using UnityEngine;


namespace StoneBaneEnemy.AI;



public class EnemyStoneBaneAnim : MonoBehaviour
{
	private int animStunned = Animator.StringToHash("stunned");

	private int animDespawning = Animator.StringToHash("despawning");

	private int animAttacking = Animator.StringToHash("attacking");

	private int animStun = Animator.StringToHash("Stun");

	private int animNotice = Animator.StringToHash("Notice");

	private int animChargeAttack = Animator.StringToHash("ChargeAttack");

	private int animDelayAttack = Animator.StringToHash("DelayAttack");
	
	private int animHandSwing = Animator.StringToHash("HandSwing");

	private int animAttack = Animator.StringToHash("Attack");

	public Enemy enemy;

	public EnemyStoneBane  controller;

	public StoneBaneAttackLogic attackLogic;

	public float springSpeedMultiplier = 1f;

	public float springDampingMultiplier = 1f;

	public SpringQuaternion springHead;

	private float springHeadSpeed;

	private float springHeadDamping;

	public Transform springHeadTarget;

	public Transform springHeadSource;

	public SpringQuaternion springLegL;

	private float springLegLSpeed;

	private float springLegLDamping;

	public Transform springLegLTarget;

	public Transform springLegLSource;

	public SpringQuaternion springLegR;

	private float springLegRSpeed;

	private float springLegRDamping;

	public Transform springLegRTarget;

	public Transform springLegRSource;

	public SpringQuaternion springArmL;

	private float springArmLSpeed;

	private float springArmLDamping;

	public Transform springArmLTarget;

	public Transform springArmLSource;

	public SpringQuaternion springArmR;

	private float springArmRSpeed;

	private float springArmRDamping;

	public Transform springArmRTarget;

	public Transform springArmRSource;

	[Header("One Shots")]
	public Sound sfxChargeAttackStart;

	public Sound sfxDelayAttackLocal;

	public Sound sfxDelayAttackGlobal;

	public Sound sfxAttackUpLocal;

	public Sound sfxAttackUpGlobal;

	public Sound sfxAttackDownLocal;

	public Sound sfxAttackDownGlobal;

	public Sound sfxMoveShort;

	public Sound sfxMoveLong;

	public Sound sfxHurt;

	public Sound sfxDeath;

	[Header("Loops")]
	public Sound sfxChargeAttackLoop;

	public Sound sfxDelayAttackLoop;

	public Sound sfxStunnedLoop;

	[Header("Animation Booleans")]
	public bool sfxChargeAttackLoopPlaying;

	public bool sfxDelayAttackLoopPlaying;

	internal Animator animator;

	private bool idling;

	private bool stunned;

	private bool stunImpulse;

	private bool noticeImpulse;

	private bool delayAttackImpulse;

	private bool attackImpulse;

	private bool chargeAttackImpulse;

	private void Awake()
	{
		animator = GetComponent<Animator>();
		animator.keepAnimatorStateOnDisable = true;
		springHeadSpeed = springHead.speed;
		springHeadDamping = springHead.damping;
		springLegLSpeed = springLegL.speed;
		springLegLDamping = springLegL.damping;
		springLegRSpeed = springLegR.speed;
		springLegRDamping = springLegR.damping;
		springArmLSpeed = springArmL.speed;
		springArmLDamping = springArmL.damping;
		springArmRSpeed = springArmR.speed;
		springArmRDamping = springArmR.damping;
	}

	private void Update()
	{
		SpringLogic();
		if (enemy.Rigidbody.frozen)
		{
			animator.speed = 0f;
		}
		else
		{
			animator.speed = 1f;
		}
		if (controller.currentState == EnemyStoneBane.State.Stun)
		{
			if (stunImpulse)
			{
				Debug.Log("Im here stunned yo");
				animator.SetTrigger(animStun);
				stunImpulse = false;
			}
			animator.SetBool(animStunned, value: true);
			stunned = true;
		}
		else
		{
			animator.SetBool(animStunned, value: false);
			stunImpulse = true;
			stunned = false;
		}
		SfxStunnedLoop();
		if (controller.currentState == EnemyStoneBane.State.Notice)
		{
			if (noticeImpulse)
			{
				Debug.Log("Im here notice");
				animator.SetTrigger(animNotice);
				noticeImpulse = false;
			}
		}
		else
		{
			noticeImpulse = true;
		}
		if (controller.currentState == EnemyStoneBane.State.ChargeAttack)
		{
			if (chargeAttackImpulse)
			{
				animator.SetTrigger(animChargeAttack);
				chargeAttackImpulse = false;
				sfxChargeAttackStart.Play(base.transform.position);
			}
		}
		else
		{
			chargeAttackImpulse = true;
		}
		SfxChargeAttackLoop();
		if (controller.currentState == EnemyStoneBane.State.DelayAttack)
		{
			if (delayAttackImpulse)
			{
				animator.SetTrigger(animDelayAttack);
				delayAttackImpulse = false;
			}
		}
		else
		{
			delayAttackImpulse = true;
		}
		SfxDelayAttackLoop();
		if (controller.currentState == EnemyStoneBane.State.Attack)
		{
			if (attackImpulse)
			{
				animator.SetTrigger(animAttack);
				attackImpulse = false;
			}
		}
		else
		{
			attackImpulse = true;
		}
		if (controller.currentState == EnemyStoneBane.State.Despawn)
		{
			animator.SetBool(animDespawning, value: true);
		}
		else
		{
			animator.SetBool(animDespawning, value: false);
		}
	}

	public void OnHandSwing()
	{
		animator.SetTrigger(animHandSwing);
	}
	
	public void OnSpawn()
	{
		animator.Play("Spawn", 0, 0f);
	}

	public void NoticeSet(int _playerID)
	{
		animator.SetTrigger(animNotice);
	}

	private void SpringLogic()
	{
		springHead.speed = springHeadSpeed * springSpeedMultiplier;
		springHead.damping = springHeadDamping * springDampingMultiplier;
		springHeadSource.rotation = SemiFunc.SpringQuaternionGet(springHead, springHeadTarget.transform.rotation);
		springLegL.speed = springLegLSpeed * springSpeedMultiplier;
		springLegL.damping = springLegLDamping * springDampingMultiplier;
		springLegLSource.rotation = SemiFunc.SpringQuaternionGet(springLegL, springLegLTarget.transform.rotation);
		springLegR.speed = springLegRSpeed * springSpeedMultiplier;
		springLegR.damping = springLegRDamping * springDampingMultiplier;
		springLegRSource.rotation = SemiFunc.SpringQuaternionGet(springLegR, springLegRTarget.transform.rotation);
		springArmL.speed = springArmLSpeed * springSpeedMultiplier;
		springArmL.damping = springArmLDamping * springDampingMultiplier;
		springArmLSource.rotation = SemiFunc.SpringQuaternionGet(springArmL, springArmLTarget.transform.rotation);
		springArmR.speed = springArmRSpeed * springSpeedMultiplier;
		springArmR.damping = springArmRDamping * springDampingMultiplier;
		springArmRSource.rotation = SemiFunc.SpringQuaternionGet(springArmR, springArmRTarget.transform.rotation);
	}

	public void CheckForGrab()
	{
		// this will be called at the end of the swinging animation
		Debug.Log("Checking grab");
	}
	
	public void OnSwingComplete()
	{
		// Called at end of swing animation
		if (controller.currentState == EnemyStoneBane.State.Grab)
		{
			//StartCrushing();
			Debug.Log("start crushing");
		}
		else
		{
			Debug.Log("Back to idle");
		}
	}
	
	
	public void Despawn()
	{
		enemy.EnemyParent.Despawn();
	}

	public void DelayAttack()
	{
		attackLogic.StateSet(StoneBaneAttackLogic.StoneBaneAttackState.stop);
		SfxDelayAttack();
	}

	public void HandSwingAttack()
	{
		Debug.Log("Im the attacking animation fuah fuah fuah fuah");
	}

	public void SfxDelayAttack()
	{
		sfxDelayAttackLocal.Play(base.transform.position);
		sfxDelayAttackGlobal.Play(base.transform.position);
	}

	public void SfxAttackUp()
	{
		sfxAttackUpLocal.Play(base.transform.position);
		sfxAttackUpGlobal.Play(base.transform.position);
	}

	public void SfxAttackDown()
	{
		sfxAttackDownLocal.Play(base.transform.position);
		sfxAttackDownGlobal.Play(base.transform.position);
	}

	public void SfxMoveShort()
	{
		sfxMoveShort.Play(base.transform.position);
	}

	public void SfxMoveLong()
	{
		sfxMoveLong.Play(base.transform.position);
	}

	public void SfxHurt()
	{
		sfxHurt.Play(base.transform.position);
	}

	public void SfxDeath()
	{
		sfxDeath.Play(base.transform.position);
	}

	public void SfxChargeAttackLoop()
	{
		sfxChargeAttackLoop.PlayLoop(sfxChargeAttackLoopPlaying, 5f, 5f);
	}

	public void SfxDelayAttackLoop()
	{
		sfxDelayAttackLoop.PlayLoop(sfxDelayAttackLoopPlaying, 5f, 5f);
	}

	public void SfxStunnedLoop()
	{
		sfxStunnedLoop.PlayLoop(stunned, 5f, 5f);
	}
}
