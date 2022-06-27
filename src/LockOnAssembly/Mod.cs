using System;
using System.Collections.Generic;
using Modding;
using Modding.Blocks;
using Modding.Modules;
using Modding.Modules.Official;
using Besiege;
using UnityEngine;
using UnityEngine.UI;

namespace LOSpace
{
	public class Mod : ModEntryPoint
	{
		public GameObject mod;
		public static bool ACMLoaded; // ACM�����[�h����Ă��邩�ǂ���

		public override void OnLoad()
		{
			Log("Load");
			mod = new GameObject("Lock On Mod");
			AddScriptManager.Instance.transform.parent = mod.transform;
			LockOnManager.Instance.transform.parent = mod.transform;
			UnityEngine.Object.DontDestroyOnLoad(mod);
			ACMLoaded = Mods.IsModLoaded(new Guid("A033CF51-D84F-45DE-B9A9-DEF1ED9A6075"));
		}
		public static void Log(string msg)
        {
			Debug.Log("Lock On Mod : " + msg);
        }
		public static void Warning(string msg)
        {
			Debug.LogWarning("Lock On Mod : " + msg);
        }
		public static void Error(string msg)
        {
			Debug.LogError("Lock On Mod : " + msg);
        }
	}
	public class AddScriptManager : SingleInstance<AddScriptManager>
	{
		public override string Name { get { return "Add Script Manager"; } }
		public bool isFirstFrame = true;
		public PlayerMachineInfo PMI;
		public Dictionary<int, Type> BlockDict;
		public void Awake()
		{
			BlockDict = new Dictionary<int, Type>
			{
                {(int)BlockType.StartingBlock, typeof(StartingBlockScript) },
				{(int)BlockType.Cannon, typeof(CannonScript) },
				{(int)BlockType.ShrapnelCannon, typeof(CannonScript) },
				{(int)BlockType.Crossbow, typeof(CrossbowScript) },
				{(int)BlockType.Flamethrower, typeof(FlamethrowerScript) },
			};
			Events.OnBlockInit += new Action<Block>(AddScript);
			/*
			foreach (BlockBehaviour block in Machine.Active().BuildingBlocks)
            {
				if (block.BlockID == (int)BlockType.StartingBlock && block.GetComponent<StartingBlockScript>() == null)
                {
					block.gameObject.AddComponent<StartingBlockScript>();
                }
            }
			*/
		}
		public void AddScript(Block block)
		{
			BlockBehaviour internalObject = block.BuildingBlock.InternalObject;
			if (BlockDict.ContainsKey(internalObject.BlockID))
			{
				Type type = BlockDict[internalObject.BlockID];
				try
				{
					if (internalObject.GetComponent(type) == null)
					{
						internalObject.gameObject.AddComponent(type);
					}
				}
				catch
				{
					ModConsole.Log("AddScript Error.");
				}
				return;
			}

			if (internalObject.name.Length > 32) // Mod�u���b�N
			{
				try
				{
					if (internalObject.GetComponent<ModAddedBlocksScript>() == null)
					{
						internalObject.gameObject.AddComponent<ModAddedBlocksScript>();
					}
				}
				catch
				{
					ModConsole.Log("AddSctipt to mod block Error.");
				}
				return;
			}
		}
	}
	public abstract class AbstractBlockScript : MonoBehaviour //�u���b�N��{
	{
		[Obsolete]
		public Action<XDataHolder> BlockDataLoadEvent;
		[Obsolete]
		public Action<XDataHolder> BlockDataSaveEvent;
		public Action BlockPropertiseChangedEvent;
		public bool isFirstFrame;
		public BlockBehaviour BB { internal set; get; }
		public bool CombatUtilities { set; get; }


		private void Awake()
		{
			BB = GetComponent<BlockBehaviour>();
			SafeAwake();
			ChangedProperties();
			try
			{
				BlockPropertiseChangedEvent();
			}
			catch
			{

			}
			DisplayInMapper(CombatUtilities);
		}
		private void Update()
		{
			if (BB.isSimulating)
			{
				if (isFirstFrame)
				{
					isFirstFrame = false;
					if (CombatUtilities)
					{
						OnSimulateStart();
					}
					if (!StatMaster.isClient)
					{
						ChangeParameter();
					}
				}
				if (CombatUtilities)
				{
					if (StatMaster.isHosting)
					{
						SimulateUpdateHost();
					}
					if (StatMaster.isClient)
					{
						SimulateUpdateClient();
					}
					SimulateUpdateAlways();
				}
			}
			else
			{
				if (CombatUtilities)
				{
					BuildingUpdate();
				}
				isFirstFrame = true;
			}
		}
		private void FixedUpdate()
		{
			if (CombatUtilities && BB.isSimulating && !isFirstFrame)
			{
				SimulateFixedUpdateAlways();
			}
		}
		private void LastUpdate()
		{
			if (CombatUtilities && BB.isSimulating && !isFirstFrame)
			{
				SimulateLateUpdateAlways();
			}
		}

		[Obsolete]
		private void SaveConfiguration(PlayerMachineInfo pmi)
		{
			ConsoleController.ShowMessage("On save en");
			if (pmi != null)
			{
				foreach (Modding.Blocks.BlockInfo current in pmi.Blocks)
				{
					if (current.Guid == BB.Guid)
					{
						XDataHolder data = current.Data;
						try
						{
							BlockDataSaveEvent(data);
						}
						catch
						{

						}
						this.SaveConfiguration(data);
						break;
					}
				}
			}
		}
		[Obsolete]
		private void LoadConfiguration()
		{
			ConsoleController.ShowMessage("On load en");
			if (SingleInstance<AddScriptManager>.Instance.PMI != null)
			{
				foreach (Modding.Blocks.BlockInfo current in SingleInstance<AddScriptManager>.Instance.PMI.Blocks)
				{
					if (current.Guid == BB.Guid)
					{
						XDataHolder data = current.Data;
						try
						{
							BlockDataLoadEvent(data);
						}
						catch { }
						LoadConfiguration(data);
						break;
					}
				}
			}
		}
		[Obsolete]
		public virtual void SaveConfiguration(XDataHolder BlockData) { }
		[Obsolete]
		public virtual void LoadConfiguration(XDataHolder BlockData) { }
		public virtual void SafeAwake() { }
		public virtual void OnSimulateStart() { }
		public virtual void SimulateUpdateHost() { }
		public virtual void SimulateUpdateClient() { }
		public virtual void SimulateUpdateAlways() { }
		public virtual void SimulateFixedUpdateAlways() { }
		public virtual void SimulateLateUpdateAlways() { }
		public virtual void BuildingUpdate() { }
		public virtual void DisplayInMapper(bool value) { }
		public virtual void ChangedProperties() { }
		public virtual void ChangeParameter() { }
		public static void SwitchMatalHardness(int Hardness, ConfigurableJoint CJ)
		{
			if (Hardness != 1)
			{
				if (Hardness != 2)
				{
					CJ.projectionMode = JointProjectionMode.None;
				}
				else
				{
					CJ.projectionMode = JointProjectionMode.PositionAndRotation;
					CJ.projectionAngle = 0f;
				}
			}
			else
			{
				CJ.projectionMode = JointProjectionMode.PositionAndRotation;
				CJ.projectionAngle = 0.5f;
			}
		}
		public static void SwitchWoodHardness(int Hardness, ConfigurableJoint CJ)
		{
			switch (Hardness)
			{
				case 0:
					CJ.projectionMode = JointProjectionMode.PositionAndRotation;
					CJ.projectionAngle = 10f;
					CJ.projectionDistance = 5f;
					return;
				case 2:
					CJ.projectionMode = JointProjectionMode.PositionAndRotation;
					CJ.projectionAngle = 5f;
					CJ.projectionDistance = 2.5f;
					return;
				case 3:
					CJ.projectionMode = JointProjectionMode.PositionAndRotation;
					CJ.projectionAngle = 0f;
					CJ.projectionDistance = 0f;
					return;
				default:
					CJ.projectionMode = JointProjectionMode.None;
					CJ.projectionDistance = 0f;
					CJ.projectionAngle = 0f;
					return;
			}
		}
		public AbstractBlockScript()
		{
			CombatUtilities = true;
			isFirstFrame = true;
		}

	}
	// �X�^�u��
	public class StartingBlockScript : AbstractBlockScript
    {
		public GameObject CurrentTarget; // ���݂̖ڕW
		public MPTeam team; // ���݂̃`�[��
		public int playerId; // �T�[�o�[ID
		public Rigidbody rigid; // �X�^�u����rigidbody

		// �G���
		public List<Enemy> TargetCandidates;

		// UI
		public Texture markerTexture; // �G�ɂ�����}�[�N
		public Texture debugTextureRed; // �f�o�b�O�p
		public Texture debugTextureGreen;

		// �G�̉�ʓ��ɓ����Ă��邩�̔���
		public Camera mainCamera;
		public Vector2 screenSize;

        public override void SafeAwake()
        {
			rigid = BB.noRigidbody ? null : BB.Rigidbody;
			//GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>();

			// �T�[�o�[ID�擾
			playerId = BB.ParentMachine.PlayerID;

			// ���g�����X�g�ɖ�����΃��X�g�ɒǉ�����
			#region ���X�g�ǉ��֌W ���R�����g�A�E�g
			/*
			bool exists = false;
			if (LockOnManager.Instance.TargetList != null)
			{
				foreach (StartingBlockScript sb in LockOnManager.Instance.TargetList)
				{
					if (sb.playerId == playerId) // ���g�����X�g�ɂ���ꍇ�͉������Ȃ�
					{
						exists = true;
						break;
					}
				}
			}
			if (!exists)
			{
				LockOnManager.Instance.TargetList.Add(this);
				Mod.Log("Added myself to LockOnManager.Instance.TargetList. playerId = " + playerId);
			}
			if (LockOnManager.Instance.TargetList.Count == 0)
            {
				LockOnManager.Instance.TargetList.Add(this);
            }
			*/
			#endregion
			//team = StatMaster.isMP ? BB.Team : MPTeam.None;
			//Mod.Log("BB.team = " + team.ToString());

			PlayerData player;
			Playerlist.GetPlayer((ushort)playerId, out player);
			team = StatMaster.isMP ? player.team : MPTeam.None;
			Mod.Log("BB.team = " + team.ToString());

			// �f�o�b�O�p
			//ChangeTarget(LockOnManager.Instance.DebugTarget);
			CurrentTarget = null;
			ChangeTarget(null);

			// ���C���J�����ݒ�
			mainCamera = Camera.main;
			screenSize = new Vector2(Screen.width, Screen.height);

			// �G�����X�g�Ɋi�[����
			TargetCandidates = new List<Enemy>();
			//SetTargetCandidates();

			// �e�N�X�`���̃��[�h
			markerTexture = ModTexture.GetTexture("marker-green").Texture;

			// �f�o�b�O�p�e�N�X�`���̐ݒ�
			var tex = new Texture2D(1, 1);
			tex.SetPixel(0, 0, new Color(1, 0, 0, 0.3f));
			tex.Apply();
			debugTextureRed = tex;
			var tex2 = new Texture2D(1, 1);
			tex2.SetPixel(0, 0, new Color(0, 1, 0, 0.3f));
			tex2.Apply();
			debugTextureGreen = tex2;
		}
		public override void SimulateFixedUpdateAlways()
        {
			SetTargetCandidates();

			// �G�����Ȃ��ꍇ�̓f�o�b�O�p�{�[�����^�[�Q�b�g�ɂ���
			if (TargetCandidates == null)
            {
				ChangeTarget(null);
            }
			else if (TargetCandidates.Count == 0)
            {
				ChangeTarget(null);
            }

			// �J���������ɂ��鎞�Ԃɉ����ă��X�g���̓G�ɃQ�[�W�𗭂߂�
			// �J�����̊O�ɏo���G�̃Q�[�W��0�ɂȂ�
			foreach (Enemy e in TargetCandidates)
            {
				var screenPos = RectTransformUtility.WorldToScreenPoint(mainCamera, e.Target.transform.position);
				// �Ƃ肠������ʓ��ɓ��������Ƃ�z��
				if (0 < screenPos.x && screenPos.x < screenSize.x && 0 < screenPos.y && screenPos.y < screenSize.y)
                {
					// 1�b���炢�Ń��b�N�I���ł���悤�ɂ���
                    e.Gauge += 0.01f;
                }
                else
                {
					e.Gauge = 0f;
                }
            }

			// �Q�[�W�����܂����G�̒��ōł��߂��G�����b�N�I��
			float minSqrDistance = float.PositiveInfinity;
			//Enemy mostNearestEnemy = new Enemy(LockOnManager.Instance.DebugTarget); // �b��
			Enemy mostNearestEnemy = null;
			foreach (Enemy e in TargetCandidates)
            {
				if (!e.LockOn) continue; // �Q�[�W�����܂��Ă��Ȃ���Ή������Ȃ�
				var sqrDistance = Vector3.SqrMagnitude(e.Target.transform.position - transform.position);
				if (sqrDistance < minSqrDistance)
                {
					minSqrDistance = sqrDistance;
					mostNearestEnemy = e;
                }
            }
			ChangeTarget(mostNearestEnemy == null ? null : mostNearestEnemy.Target);
		}

        // �V�~���J�n���ƏI�����Ƀ��X�g���X�V
		/*
        public override void OnSimulateStart()
        {
			LockOnManager.Instance.SetTargetList();
        }
		public void OnDestroy()
        {
			LockOnManager.Instance.SetTargetList();
        }
		*/

        // �f�o�b�O�pGUI
        public Rect debugWindowRect = new Rect(100, 100, 200, 150);
		public int debugWindowId = ModUtility.GetWindowId();
		public void OnGUI()
		{
			// �����̂Ƃ���ł����\������
			if (playerId != Machine.Active().PlayerID)
			{
				return;
			}

			// �V�~�����̂�
			if (!BB.isSimulating)
            {
				return;
            }

			#region // �f�o�b�O�pGUI
			/*
			debugWindowRect = GUI.Window(debugWindowId, debugWindowRect, (id) =>
			{
				GUILayout.Label("CurrentTarget = " + CurrentTarget != null ? CurrentTarget.name : "None");
				GUILayout.Label("team = " + team);
				GUILayout.Label("playerId = " + playerId);
				if (TargetCandidates != null)
				{
					GUILayout.Label("TargetCandidates.Count = " + TargetCandidates.Count);
					foreach (Enemy e in TargetCandidates)
					{
						GUILayout.BeginHorizontal();
						GUILayout.Label(e.Target.name);
						GUILayout.Label(e.Target.transform.position.ToString());
						GUILayout.Label(e.Gauge.ToString());
						GUILayout.EndHorizontal();
					}
				}
				GUI.DragWindow();
			}, "Starting Block Script");
			*/
			#endregion

			// �^�[�Q�b�g�}�[�J�[
			//var targetPos = ScreenPoint(CurrentTarget);
			//GUI.DrawTexture(new Rect(targetPos, new Vector2(100, 100)), debugTextureRed);
			// ��`��`��
			//var rect = new Rect(12, 12, 300, 100);
			//GUI.DrawTexture(rect, markerTexture, ScaleMode.StretchToFill, true, 0);
			if (TargetCandidates == null)
            {
				return;
            }
			if (TargetCandidates.Count == 0)
            {
				return;
            }
			foreach (Enemy e in TargetCandidates)
            {
				if (e == null)
                {
					continue;
                }
				var tex = e.LockOn ? debugTextureRed : debugTextureGreen;
				var targetPos = ScreenPoint(e.Target, new Vector2(50, 50));
				GUI.DrawTexture(targetPos, tex, ScaleMode.StretchToFill, true, 0);
            }
        }

		// �^�[�Q�b�g�ɂȂ肤��G��S�ă��X�g�Ɋi�[����
		// �s�x�V�������Ă���ƃ��b�N�I�����Ԃ��v���ł��Ȃ��Ȃ� �^�[�Q�b�g�̃Q�[�W��ێ������܂ܐV���ȃ��X�g�����K�v������
		// �^�[�Q�b�g���̒�����O������𖞂����Ȃ��Ȃ������̂������i�^�[�Q�b�g���X�g�ɂ��Ȃ����̂�e���j���V���ɑO������𖞂������̂ŁA�܂����ɓ����Ă��Ȃ����̂�������
		public void SetTargetCandidates()
        {
			var ret = new List<Enemy>();
			if (LockOnManager.Instance.TargetList.Count == 0)
            {
				TargetCandidates = ret;
				return;
            }
			foreach (StartingBlockScript sb in LockOnManager.Instance.TargetList)
            {
				foreach (Enemy e in TargetCandidates)
                {
					// �c���g
					if (sb.gameObject == e.Target)
                    {
						ret.Add(new Enemy(e.Target, e.Gauge));
                    }
                }
				if ((sb.team != team || sb.team == MPTeam.None) && sb != this) // �^�[�Q�b�g���ɂ��邽�߂̑O�����
                {
					var candidate = new Enemy(sb.gameObject);
					bool exist = false;
					foreach (Enemy e in TargetCandidates)
                    {
						if (candidate.Target == e.Target)
                        {
							exist = true;
                        }
                    }
					if (!exist)
                    {
						ret.Add(candidate);
					}
				}
            }
			TargetCandidates = new List<Enemy>(ret);
        }
		// �^�[�Q�b�g��ύX����
		public void ChangeTarget(GameObject NextTarget)
        {
			CurrentTarget = (NextTarget == null) ? null : NextTarget;
        }
		
		// �ڕW�̉�ʏ�ɂ�����ʒu // point.z��Ԃ�l�ɂ���Rect��out�ɂ��������ǂ�����
		public Rect ScreenPoint(GameObject target, Vector2 scale)
        {
			Vector3 point = mainCamera.WorldToScreenPoint(target.transform.position);
			Vector2 pos = new Vector2(point.x - scale.x/2, screenSize.y - point.y - scale.y/2);
			//point.y = screenSize.y - point.y;
			return point.z > 0 ? new Rect(pos, scale) : new Rect(0, 0, 0, 0);
        }

		// �G�N���X
		public class Enemy
        {
			public GameObject Target; // �^�[�Q�b�g�̃Q�[���I�u�W�F�N�g
			public bool LockOn // ���������b�N�I�����Ă��邩�ǂ���
            {
                get
                {
					return gauge == 1f;
                }
            }
			private float gauge; // �Q�[�W 0f~1f �̒l���Ƃ�
			public float Gauge
			{
				set
				{
					if (value < 0) { value = 0f; }
					else if (1f < value) { value = 1f; }
					gauge = value;
				}
                get
                {
					return gauge;
                }
			}
			public Enemy(GameObject go, float g=0)
            {
				Target = go;
				gauge = g;
            }
        }
    }
	// ��{
	public abstract class LockOnBlockScript : AbstractBlockScript
    {
		public Quaternion Correction; // �␳�p�x
		public Transform ProjectileSpawn; // ��o�Ă���ʒu�p��
		public Vector3 Target; // �ڕW�ʒu
		public Vector3 TargetVelo; // �ڕW�̑��x
		public Vector3 TargetAngularVelo; // �ڕW�̊p���x
		public bool gravity; // �S�b�h�c�[���g�p�����ǂ���
		public readonly float g = 32.81f; // �d�͉����x
		public float initialSpeed; // �e�̏������x

		// �X�^�u��
		public StartingBlockScript startingBlock;

		public override void SafeAwake()
		{
			// �e�̏����p�����擾
			SetProjectileSpawn();

			gravity = !StatMaster.GodTools.GravityDisabled;

			// �X�^�u�����擾����
			if (BB.isSimulating)
			{
				foreach (BlockBehaviour block in BB.ParentMachine.SimulationBlocks)
				{
					startingBlock = block.GetComponent<StartingBlockScript>();
					if (startingBlock != null) break;
				}
			}
            else
            {
				startingBlock = null;
            }

			// �e�̏������x�ݒ�
			SetInitialSpeed();
		}
		public override void SimulateFixedUpdateAlways()
		{
			// �e���\��
			if (startingBlock.CurrentTarget != null) // �W�I�����݂���ꍇ
			{
				SetTarget();
				//SetTarget(LockOnManager.Instance.DebugTargetPos, LockOnManager.Instance.DebugTargetRigid.velocity);
				gravity = !StatMaster.GodTools.GravityDisabled;

				Vector3 predTargetPos;
				if (TargetAngularVelo.sqrMagnitude > Mathf.Pow(0.03f, 2)) // �~�`�\��
				{
					predTargetPos = CircularPredict(Target, TargetVelo, TargetAngularVelo, initialSpeed);
				}
				else // ���`�\��
				{
					predTargetPos = LinearPredict(Target, TargetVelo, initialSpeed);
				}
				ProjectileSpawn.rotation = Rotate(predTargetPos);
			}
            else // �W�I�����݂��Ȃ��ꍇ
            {
				ProjectileSpawn.rotation = Rotate();
            }
		}
		// �e�̎p�����擾
		public virtual void SetProjectileSpawn()
		{
			ProjectileSpawn = transform.FindChild("projective spawn");
		}
		// �e�̏������x�ݒ�
		public virtual void SetInitialSpeed()
        {
			initialSpeed = 1f;
        }
		// �e���\��
		public virtual Vector3 LinearPredict(Vector3 pos, Vector3 velo, float speed = 1f) // ���݂̕W�I�̈ʒu�A���x�A�e�̑���
		{
			// �ˌ�����ʒu���猩�����݂̕W�I�̈ʒu
			Vector3 deltaPos = pos - transform.position;

			// �񎟕������������A�ڕW�̈ʒu��\������
			float t = Utility.SolveEquation(velo.sqrMagnitude - speed * speed, Vector3.Dot(velo, deltaPos), deltaPos.sqrMagnitude);
			Vector3 targetpos = pos + velo * t;

			// �d�͂̏���
			if (gravity)
			{
				targetpos.y += g * t * t / 2;
			}

			return targetpos;
		}
		public virtual Vector3 LinearPredict(Rigidbody rigid, float speed = 1f)
        {
			return LinearPredict(rigid.position, rigid.velocity, speed);
        }
		public virtual Vector3 CircularPredict(Vector3 pos, Vector3 velo, Vector3 angularVelo, float speed = 1f) // ���݂̕W�I�̈ʒu�C���x�C�p���x�C�e�̑���
        {
			// 3�_����~�̒��S�_���o��
			Vector3 radius = Vector3.Cross(velo, angularVelo) / Vector3.SqrMagnitude(angularVelo);
			Vector3 centerPos = pos - radius;

			// ���S�_���猩��1�t���[���̊p���x�Ǝ����o��
			float angle = angularVelo.magnitude;
			Vector3 axis = angularVelo / angle;

			// ���݈ʒu�Œe�̓��B���Ԃ��o��
			float predictionFlame = Vector3.Distance(pos, transform.position) / speed;

			// ���B���ԕ����ړ������\���ʒu�ōČv�Z���ē��B���Ԃ�␳����
			for (int i = 0; i<3; ++i)
            {
				predictionFlame = Vector3.Distance(Utility.RotateToPosition(pos, centerPos, axis, angle * predictionFlame), transform.position) / speed;
            }

			var targetpos = Utility.RotateToPosition(pos, centerPos, axis, angle * predictionFlame);

			// �d�͂̏���
			if (gravity)
			{
				targetpos.y += g * predictionFlame * predictionFlame / 2;
			}

			return targetpos;
        }
		public virtual Vector3 CircularPredict(Rigidbody rigid, float speed = 1f)
        {
			return CircularPredict(rigid.position, rigid.velocity, rigid.angularVelocity, speed);
        }
		public virtual Quaternion Rotate(Vector3 to, float limitAngle = 30f) // ���ʂ̌����ɒ��ӁI
		{
			Quaternion ret;
			float angle = Vector3.Angle(-transform.up, to - transform.position);
			if (angle < limitAngle)
			{
				ret = Quaternion.LookRotation(to - transform.position);
			}
			else
			{
				ret = Quaternion.LookRotation(-transform.up);
			}
			return ret;
		}
		public virtual Quaternion Rotate()
        {
			return Quaternion.LookRotation(-transform.up);
        }
		// �ڕW���߂�
		public virtual void SetTarget(Vector3 pos, Vector3 velo, Vector3 angularVelo)
        {
			Target = pos == null ? Vector3.zero : pos;
			TargetVelo = velo == null ? Vector3.zero : velo;
			TargetAngularVelo = angularVelo == null ? Vector3.zero : angularVelo;
        }
		public virtual void SetTarget(GameObject nextTarget)
        {
			if (nextTarget != null)
			{
				//var rigid = nextTarget.GetComponent<Rigidbody>() ?? nextTarget.AddComponent<Rigidbody>(); // �N���C�A���g�̃X�^�u����rigid==null�ɂȂ�
				var block = nextTarget.GetComponent<BlockBehaviour>();
				var rigid = block.noRigidbody ? null : block.Rigidbody;
				if (rigid == null)
                {
					SetTarget(Vector3.zero, Vector3.zero, Vector3.zero);
					return;
                }
				SetTarget(rigid.position, rigid.velocity, rigid.angularVelocity);
			}
            else
            {
				SetTarget(Vector3.zero, Vector3.zero, Vector3.zero);
            }
        }
		public virtual void SetTarget()
        {
			if (startingBlock == null)
            {
				// �X�^�u�����擾����
				foreach (BlockBehaviour block in BB.ParentMachine.SimulationBlocks)
				{
					startingBlock = block.GetComponent<StartingBlockScript>();
					if (startingBlock != null) break;
				}
			}
			if (startingBlock == null)
            {
				SetTarget(Vector3.zero, Vector3.zero, Vector3.zero);
				return;
            }
			SetTarget(startingBlock.CurrentTarget == null ? null : startingBlock.CurrentTarget);
        }
	}
	// Cannon�n // �t�����ɒe���o��g���u������
	public class CannonScript : LockOnBlockScript
    {
		public CanonBlock Cannon;

        public override void SafeAwake()
        {
			//Mod.Log("Cannon Script");
			Cannon = GetComponent<CanonBlock>();
			if (Cannon == null)
            {
				Mod.Error("could not get CanonBlock!");
				return;
            }
			base.SafeAwake();
        }
        public override void SimulateFixedUpdateAlways()
        {
			if (startingBlock.CurrentTarget != null)
			{
				SetTarget();
				gravity = !StatMaster.GodTools.GravityDisabled;

				// �e���\��
				var predTargetPos = LinearPredict(Target, TargetVelo, initialSpeed);
				if (Cannon.shrapnel)
				{
					Cannon.boltSpawnRot = Rotate(predTargetPos); // �g�U�C�̔��˕�����ύX
				}
				else
				{
					// Cannon�ł���ꍇ�̋���
				}
			}
            else
            {
				Cannon.boltSpawnRot = Rotate();
            }
		}
        public override void SetProjectileSpawn()
        {
			ProjectileSpawn = null;
        }
        public override void SetInitialSpeed()
        {
			initialSpeed = Cannon.boltSpeed * (Cannon.shrapnel ? 1f : 1f);
        }
    }
	// Crossbow
	public class CrossbowScript : LockOnBlockScript
    {
		public CrossBowBlock Crossbow;
		public LineRenderer line; // �f�o�b�O�p

        public override void SafeAwake()
        {
			//Mod.Log("Crossbow Script");

			Crossbow = GetComponent<CrossBowBlock>();
			if (Crossbow == null)
			{
				Mod.Error("could not get CrossbowBlock!");
				initialSpeed = 1f;
			}
            else
            {
				// �e�̏������x�ݒ�
				SetInitialSpeed();
			}

			// �e�̏����p�����擾
			SetProjectileSpawn();

			gravity = !StatMaster.GodTools.GravityDisabled;

			// �X�^�u�����擾����
			if (BB.isSimulating)
			{
				foreach (BlockBehaviour block in BB.ParentMachine.SimulationBlocks)
				{
					startingBlock = block.GetComponent<StartingBlockScript>();
					if (startingBlock != null) break;
				}
			}
			else
			{
				startingBlock = null;
			}
		}
        public override void SetInitialSpeed()
        {
			if (Crossbow.PowerSlider != null)
			{
				initialSpeed = Crossbow.PowerSlider.Value * 80f;
			}
            else
            {
				initialSpeed = 1f * 80f;
            }
        }
    }
	// Flamethrower
	public class FlamethrowerScript : LockOnBlockScript
    {
		public Transform Fire; // �΂̃G�t�F�N�g
		//public LineRenderer line; // �f�o�b�O�p
		public override void SafeAwake()
        {
			//Mod.Log("Flamethrower Script");
			Fire = transform.FindChild("Fire");

			base.SafeAwake();
		}
        public override void SimulateFixedUpdateAlways()
        {
			if (startingBlock.CurrentTarget != null)
			{
				SetTarget();

				// �e���\��
				Vector3 predTargetPos;
				if (TargetAngularVelo.sqrMagnitude > Mathf.Pow(0.03f, 2)) // �~�`�\��
				{
					predTargetPos = CircularPredict(Target, TargetVelo, TargetAngularVelo, initialSpeed);
				}
				else // ���`�\��
				{
					predTargetPos = LinearPredict(Target, TargetVelo, initialSpeed);
				}
				Correction = Rotate(predTargetPos);
				Fire.rotation = Correction;
				ProjectileSpawn.rotation = Correction;
			}
            else
            {
				Correction = Rotate();
				Fire.rotation = Correction;
				ProjectileSpawn.rotation = Correction;
            }
		}
		public override void SetProjectileSpawn()
		{
			ProjectileSpawn = transform.FindChild("FireTrigger");
		}
        public override void SetInitialSpeed()
        {
			initialSpeed = 500f;
        }
        // �e���\��
        public override Vector3 LinearPredict(Vector3 pos, Vector3 velo, float speed = 100f) // ���݂̕W�I�̈ʒu�A���x�A��̑���
		{
			// �ˌ�����ʒu���猩�����݂̕W�I�̈ʒu
			Vector3 deltaPos = pos - transform.position;

			// �񎟕������������A�ڕW�̈ʒu��\������
			float t = Utility.SolveEquation(velo.sqrMagnitude - speed * speed, Vector3.Dot(velo, deltaPos), deltaPos.sqrMagnitude);
			Vector3 targetpos = pos + velo * t;

			return targetpos;
		}
		public override Vector3 CircularPredict(Vector3 pos, Vector3 velo, Vector3 angularVelo, float speed = 1f) // ���݂̕W�I�̈ʒu�C���x�C�p���x�C�e�̑���
		{
			// 3�_����~�̒��S�_���o��
			Vector3 radius = Vector3.Cross(velo, angularVelo) / Vector3.SqrMagnitude(angularVelo);
			Vector3 centerPos = pos - radius;

			// ���S�_���猩��1�t���[���̊p���x�Ǝ����o��
			float angle = angularVelo.magnitude;
			Vector3 axis = angularVelo / angle;

			// ���݈ʒu�Œe�̓��B���Ԃ��o��
			float predictionFlame = Vector3.Distance(pos, transform.position) / speed;

			// ���B���ԕ����ړ������\���ʒu�ōČv�Z���ē��B���Ԃ�␳����
			for (int i = 0; i < 3; ++i)
			{
				predictionFlame = Vector3.Distance(Utility.RotateToPosition(pos, centerPos, axis, angle * predictionFlame), transform.position) / speed;
			}

			return Utility.RotateToPosition(pos, centerPos, axis, angle * predictionFlame);
		}

		public override Quaternion Rotate(Vector3 to, float limitAngle = 30f)
		{
			Quaternion ret;
			float angle = Vector3.Angle(transform.forward, to - transform.position);
			if (angle < limitAngle)
			{
				ret = Quaternion.LookRotation(to - transform.position);
			}
			else
			{
				ret = Quaternion.LookRotation(transform.forward);
			}
			return ret;
		}
        public override Quaternion Rotate()
        {
			return Quaternion.LookRotation(transform.forward);
        }
    }

	// mod�Œǉ�����镐��mod�ɑ΂���
	// �V���[�e�B���O���W���[���ł̓u���b�N��.ShootingDirectionVisual(Clone)�Ƃ������O�̎q�I�u�W�F�N�g�Ŕ��˕����𐧌䂵�Ă���͗l
	// ACM�̏ꍇ�̓u���b�N��.AdShootingVisual(Clone) �Ƃ������O�̎q�I�u�W�F�N�g�Ŕ��˕����𐧌䂵�Ă���͗l
	public class ModAddedBlocksScript : LockOnBlockScript
    {
		public ShootingModuleBehaviour shootingModule; // �����̃V���[�e�B���O���W���[��
		public List<Transform> ProjectileVis; // �e�̕������w�肷��Q�[���I�u�W�F�N�g����
		public readonly string originalShootingModuleName = "ShootingDirectionVisual(Clone)";
		public readonly string acmShootingModuleName = "AdShootingVisual(Clone)";
		public List<GameObject> defaultForward; // �e�̕����̏����l
		//public LineRenderer line; // �f�o�b�O�p

		// �����̌v�Z
		//public MSlider PowerSlider;
		//public float power;

		// ACM�����ǂ���
		public bool fromACM = false;

		public override void SafeAwake()
        {
			// �e�̔��˕������擾
			ProjectileVis = new List<Transform>();
			foreach (Transform child in transform)
            {
				if (child.name == originalShootingModuleName)
                {
					ProjectileVis.Add(child);
					fromACM = false;
                }
				else if (child.name == acmShootingModuleName)
                {
					ProjectileVis.Add(child);
					fromACM = true;
                }
			}

			if (fromACM)
            {
				// �ʂ̃R���|�[�l���g��\��t���Ă�������A�N�e�B�u�ɂ���
				if (GetComponent<AdShootingBlocksScript>() == null)
				{
					gameObject.AddComponent<AdShootingBlocksScript>();
				}
				enabled = false;
				return;
			}
			//Mod.Log("Mod-added Blocks Script");
			if (ProjectileVis.Count > 0 && shootingModule.IsSimulating)
			{
				defaultForward = new List<GameObject>();
				foreach (Transform t in ProjectileVis)
                {
					GameObject go = new GameObject("Projectile Default Vis");
					go.transform.parent = transform;
					go.transform.localPosition = t.localPosition;
					go.transform.localRotation = t.localRotation;
					defaultForward.Add(go);
                }
			}

			shootingModule = GetComponent<ShootingModuleBehaviour>(); // �擾�ł��� // ACM�͕ʂ̃N���X���g���Ă���͗l
			if (shootingModule == null)
			{
				//Mod.Warning("could not get shootingModule");
				enabled = false;
				return;
			}

			// �����v�Z
			//PowerSlider = shootingModule.GetSlider(shootingModule.Module.PowerSlider);
			//power = 100f * PowerSlider.Value;
			//SetInitialSpeed();

			base.SafeAwake();
		}
        public override void SimulateFixedUpdateAlways()
		{
			// �e���\��
			if (startingBlock.CurrentTarget != null) // �W�I�����݂���ꍇ
			{
				SetTarget();
				gravity = !StatMaster.GodTools.GravityDisabled;

				Vector3 predTargetPos;
				if (TargetAngularVelo.sqrMagnitude > Mathf.Pow(0.03f, 2)) // �~�`�\��
				{
					predTargetPos = CircularPredict(Target, TargetVelo, TargetAngularVelo, initialSpeed);
				}
				else // ���`�\��
				{
					predTargetPos = LinearPredict(Target, TargetVelo, initialSpeed);
				}
				for (int i = 0; i < ProjectileVis.Count; i++)
				{
					ProjectileVis[i].rotation = Rotate(predTargetPos, defaultForward[i].transform.forward);
				}
			}
			else // �W�I�����݂��Ȃ��ꍇ
			{
				for (int i = 0; i < ProjectileVis.Count; i++)
				{
					ProjectileVis[i].rotation = Rotate(defaultForward[i].transform.forward);
				}
			}

			// �f�o�b�O�p
			//line.SetPositions(new Vector3[] { ProjectileVis[0].position, ProjectileVis[0].forward * 100f });
		}
        public override void SetProjectileSpawn()
        {
			ProjectileSpawn = null;
        }
        public override void SetInitialSpeed()
        {
			initialSpeed = 100f * shootingModule.GetSlider(shootingModule.Module.PowerSlider).Value;
		}
        // �e���\��
        public Quaternion Rotate(Vector3 to, Vector3 defaultForward, float limitAngle = 30f)
		{
			Quaternion ret;
			float angle = Vector3.Angle(defaultForward, to - transform.position);
			if (angle < limitAngle)
			{
				ret = Quaternion.LookRotation(to - transform.position);
			}
			else
			{
				ret = Quaternion.LookRotation(defaultForward);
			}
			return ret;
		}
		public Quaternion Rotate(Vector3 defaultForward)
        {
			return Quaternion.LookRotation(defaultForward);
		}
	}

	public class AdShootingBlocksScript : LockOnBlockScript
    {
		public List<Transform> ProjectileVis; // �e�̕������w�肷��Q�[���I�u�W�F�N�g����
		public readonly string acmShootingModuleName = "AdShootingVisual(Clone)";
		public List<GameObject> defaultForward; // �e�̕����̏����l

		// �~�T�C���ƃ`���t�Ȃ疳����
		public bool isMissile = false;
		public bool isChaffLauncher = false;

		// �����̌v�Z
		public XDataHolder adBlockData;
		public float power;

		public override void SafeAwake()
		{
			// XML��������擾 �~�T�C�����`���t�Ȃ牽�����Ȃ�
			adBlockData = BlockInfo.FromBlockBehaviour(BB).BlockData;
			if (adBlockData.HasKey("isChaff"))
			{
				isChaffLauncher = adBlockData.ReadBool("isChaff");
			}
			if (adBlockData.HasKey("useBeacon"))
			{
				isMissile = adBlockData.ReadBool("useBeacon");
			}

			// �e�̔��˕�����\���Q�[���I�u�W�F�N�g���擾
			ProjectileVis = new List<Transform>();
			foreach (Transform child in transform)
			{
				if (child.name == acmShootingModuleName)
				{
					ProjectileVis.Add(child);
				}
			}

			// �f�t�H���g�̔��˕�����ۂQ�[���I�u�W�F�N�g�𐶐�
			if (ProjectileVis.Count > 0 && BB.isSimulating)
			{
				defaultForward = new List<GameObject>();
				foreach (Transform t in ProjectileVis)
				{
					GameObject go = new GameObject("Projectile Default Vis");
					go.transform.parent = transform;
					go.transform.localPosition = t.localPosition;
					go.transform.localRotation = t.localRotation;
					defaultForward.Add(go);
				}
			}

			// �~�T�C���C�`���t�C��C�n�Ȃ�I�t�ɂ���
			if (isMissile || isChaffLauncher || ProjectileVis.Count == 0)
			{
				enabled = false;
			}

			base.SafeAwake();
		}
		public override void SimulateFixedUpdateAlways()
		{
			if (startingBlock.CurrentTarget != null)
			{
				SetTarget();
				gravity = !StatMaster.GodTools.GravityDisabled;

				// �e���\��
				//var predTargetPos = Predict(Target, TargetVelo, 1f * power); // �b��I�ɏ���������
				Vector3 predTargetPos;
				if (TargetAngularVelo.sqrMagnitude > Mathf.Pow(0.03f, 2)) // �~�`�\��
				{
					predTargetPos = CircularPredict(Target, TargetVelo, TargetAngularVelo, power);
				}
				else // ���`�\��
				{
					predTargetPos = LinearPredict(Target, TargetVelo, power);
				}
				for (int i = 0; i < ProjectileVis.Count; i++)
				{
					ProjectileVis[i].rotation = Rotate(predTargetPos, defaultForward[i].transform.forward);
				}
			}
			else
			{
				for (int i = 0; i < ProjectileVis.Count; i++)
				{
					ProjectileVis[i].rotation = Rotate(defaultForward[i].transform.forward);
				}
			}
		}
		public override void SetProjectileSpawn()
		{
			ProjectileSpawn = null;
		}
		public override void SetInitialSpeed()
		{

			if (adBlockData.HasKey("PowerSlider"))
			{
				power = 100f * adBlockData.ReadFloat("PowerSlider");
				Mod.Log("power = " + power);
			}
            else
            {
				power = 0f;
            }
		}
		// �e���\��
		public Quaternion Rotate(Vector3 to, Vector3 defaultForward, float limitAngle = 30f)
		{
			Quaternion ret;
			float angle = Vector3.Angle(defaultForward, to - transform.position);
			if (angle < limitAngle)
			{
				ret = Quaternion.LookRotation(to - transform.position);
			}
			else
			{
				ret = Quaternion.LookRotation(defaultForward);
			}
			return ret;
		}
		public Quaternion Rotate(Vector3 defaultForward)
		{
			return Quaternion.LookRotation(defaultForward);
		}
	}

	/*
	public class AdShootingBlocksScript : ModBlockBehaviour //BlockModuleBehaviour<AdShootingProp>
    {
		public List<Transform> ProjectileVis; // �e�̕������w�肷��Q�[���I�u�W�F�N�g����
		public readonly string acmShootingModuleName = "AdShootingVisual(Clone)";
		public List<GameObject> defaultForward; // �e�̕����̏����l
		public Quaternion Correction; // �␳�p�x
		public Vector3 Target; // �ڕW�ʒu
		public Vector3 TargetVelo; // �ڕW�̑��x
		public Vector3 TargetAngularVelo; // �ڕW�̊p���x
		public bool gravity; // �S�b�h�c�[���g�p�����ǂ���
		public readonly float g = 32.81f; // �d�͉����x

		// �~�T�C���ƃ`���t�Ȃ疳����
		public bool isMissile = false;
		public bool isChaffLauncher = false;

		// �����̌v�Z
		public XDataHolder adBlockData;
		public float power;

		// �X�^�[�e�B���O�u���b�N
		public StartingBlockScript startingBlock;

		public override void SafeAwake()
        {
			//Mod.Log("Ad-Shooting Blocks Script");

			// XML��������擾
			adBlockData = BlockInfo.FromBlockBehaviour(BlockBehaviour).BlockData;
			if (adBlockData.HasKey("PowerSlider"))
			{
				power = 100f * adBlockData.ReadFloat("PowerSlider");
				Mod.Log("power = " + power);
			}
			if (adBlockData.HasKey("isChaff"))
            {
				isChaffLauncher = adBlockData.ReadBool("isChaff");
            }
			if (adBlockData.HasKey("useBeacon"))
            {
				isMissile = adBlockData.ReadBool("useBeacon");
            }

			// �e�̔��˕�����\���Q�[���I�u�W�F�N�g���擾
			ProjectileVis = new List<Transform>();
			foreach (Transform child in transform)
			{
				if (child.name == acmShootingModuleName)
				{
					ProjectileVis.Add(child);
				}
			}

			// �f�t�H���g�̔��˕�����ۂQ�[���I�u�W�F�N�g�𐶐�
			if (ProjectileVis.Count > 0 && IsSimulating)
			{
				defaultForward = new List<GameObject>();
				foreach (Transform t in ProjectileVis)
				{
					GameObject go = new GameObject("Projectile Default Vis");
					go.transform.parent = transform;
					go.transform.localPosition = t.localPosition;
					go.transform.localRotation = t.localRotation;
					defaultForward.Add(go);
				}
			}

			// �~�T�C���C�`���t�C��C�n�Ȃ�I�t�ɂ���
			if (isMissile || isChaffLauncher || ProjectileVis.Count == 0)
            {
				enabled = false;
            }

			// �X�^�u�����擾����
			if (BlockBehaviour.isSimulating)
			{
				foreach (BlockBehaviour block in BlockBehaviour.ParentMachine.SimulationBlocks)
				{
					startingBlock = block.GetComponent<StartingBlockScript>();
					if (startingBlock != null) break;
				}
			}
			else
			{
				startingBlock = null;
			}
		}
		
		public override void SimulateFixedUpdateAlways()
		{
			if (startingBlock.CurrentTarget != null)
			{
				SetTarget();
				gravity = !StatMaster.GodTools.GravityDisabled;

				// �e���\��
				//var predTargetPos = Predict(Target, TargetVelo, 1f * power); // �b��I�ɏ���������
				Vector3 predTargetPos;
				if (TargetAngularVelo.sqrMagnitude > Mathf.Pow(0.03f, 2)) // �~�`�\��
				{
					predTargetPos = CircularPredict(Target, TargetVelo, TargetAngularVelo, power);
				}
				else // ���`�\��
				{
					predTargetPos = LinearPredict(Target, TargetVelo, power);
				}

				for (int i = 0; i < ProjectileVis.Count; i++)
				{
					ProjectileVis[i].rotation = Rotate(predTargetPos, defaultForward[i].transform.forward);
				}
			}
            else
            {
				for (int i = 0; i < ProjectileVis.Count; i++)
				{
					ProjectileVis[i].rotation = Rotate(defaultForward[i].transform.forward);
				}
			}
		}

		// �e���\��
		public Vector3 LinearPredict(Vector3 pos, Vector3 velo, float speed = 500f) // ���݂̕W�I�̈ʒu�A���x�A��̑���
		{
			// �ˌ�����ʒu���猩�����݂̕W�I�̈ʒu
			Vector3 deltaPos = pos - transform.position;

			// �񎟕������������A�ڕW�̈ʒu��\������
			float t = Utility.SolveEquation(velo.sqrMagnitude - speed * speed, Vector3.Dot(velo, deltaPos), deltaPos.sqrMagnitude);
			Vector3 targetpos = pos + velo * t;

			// �d�͂̏���
			if (gravity)
			{
				targetpos.y += g * t * t / 2;
			}

			return targetpos;
		}
		public virtual Vector3 CircularPredict(Vector3 pos, Vector3 velo, Vector3 angularVelo, float speed = 1f) // ���݂̕W�I�̈ʒu�C���x�C�p���x�C�e�̑���
		{
			// 3�_����~�̒��S�_���o��
			Vector3 radius = Vector3.Cross(velo, angularVelo) / Vector3.SqrMagnitude(angularVelo);
			Vector3 centerPos = pos - radius;

			// ���S�_���猩��1�t���[���̊p���x�Ǝ����o��
			float angle = angularVelo.magnitude;
			Vector3 axis = angularVelo / angle;

			// ���݈ʒu�Œe�̓��B���Ԃ��o��
			float predictionFlame = Vector3.Distance(pos, transform.position) / speed;

			// ���B���ԕ����ړ������\���ʒu�ōČv�Z���ē��B���Ԃ�␳����
			for (int i = 0; i < 3; ++i)
			{
				predictionFlame = Vector3.Distance(Utility.RotateToPosition(pos, centerPos, axis, angle * predictionFlame), transform.position) / speed;
			}

			var targetpos = Utility.RotateToPosition(pos, centerPos, axis, angle * predictionFlame);

			// �d�͂̏���
			if (gravity)
			{
				targetpos.y += g * predictionFlame * predictionFlame / 2;
			}

			return targetpos;
		}
		public Quaternion Rotate(Vector3 to, Vector3 defaultForward, float limitAngle = 30f)
		{
			Quaternion ret;
			float angle = Vector3.Angle(defaultForward, to - transform.position);
			if (angle < limitAngle)
			{
				ret = Quaternion.LookRotation(to - transform.position);
			}
			else
			{
				ret = Quaternion.LookRotation(defaultForward);
			}
			return ret;
		}
		public Quaternion Rotate(Vector3 defaultForward)
        {
			return Quaternion.LookRotation(defaultForward);
        }

		// �ڕW���߂�
		public virtual void SetTarget(Vector3 pos, Vector3 velo, Vector3 angularVelo)
		{
			Target = pos == null ? Vector3.zero : pos;
			TargetVelo = velo == null ? Vector3.zero : velo;
			TargetAngularVelo = angularVelo == null ? Vector3.zero : angularVelo;
		}
		public virtual void SetTarget(GameObject nextTarget)
		{
			if (nextTarget != null)
			{
				var rigid = nextTarget.GetComponent<Rigidbody>();
				SetTarget(rigid.position, rigid.velocity, rigid.angularVelocity);
			}
			else
			{
				SetTarget(Vector3.zero, Vector3.zero, Vector3.zero);
			}
		}
		public void SetTarget()
		{
			SetTarget(startingBlock.CurrentTarget);
		}
	}
	*/

	public class LockOnManager : SingleInstance<LockOnManager>
    {
		public override string Name => "Lock On Manager";
		//public Vector3 ProjectileDirection; // �e�̔��˕������ꗥ�ŕύX�i�f�o�b�O�p�j

		// �W�I�i�X�^�u���j
		public List<StartingBlockScript> TargetList;
		public List<PlayerData> Players, PlayersPast; // �I���̃v���C���[�ꗗ

		// �W�I�i�f�o�b�O�p�j
		//public GameObject DebugTarget;
		//public Vector3 DebugTargetPos;
		//public Rigidbody DebugTargetRigid;

		// �N���X�{�E���ؗp
		public float crossbowPower = 10f;

		// ���b�Z�[�W�i�f�o�b�O�p�j
		public string message0="", message1 = "", message2 = "", message3 = "";

		public void Awake()
        {
			TargetList = new List<StartingBlockScript>();

			// �f�o�b�O�p
			/*
			DebugTarget = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			DebugTarget.transform.parent = transform;
			DebugTargetPos = new Vector3(5, 0, 30);
			DebugTargetRigid = DebugTarget.GetComponent<Rigidbody>() ?? DebugTarget.AddComponent<Rigidbody>();
			DebugTargetRigid.useGravity = false;
			Collider col;
			if (col = DebugTarget.GetComponent<Collider>())
            {
				Destroy(col);
            }
			*/
        }
		public void FixedUpdate()
        {
			//DebugTarget.transform.position = DebugTargetPos;
			Players = Playerlist.Players;

			// �v���C���[���X�g�X�V �v���C���[�̐����ω���������
			// �Ƃ肠��������擾�������Ă݂�
			SetTargetList();
			PlayersPast = new List<PlayerData>(Players);
		}

		// GUI
		public Rect windowRect = new Rect(0, 0, 250, 400);
		public int windowId = ModUtility.GetWindowId();
		/*
		public void OnGUI()
        {
			windowRect = GUI.Window(windowId, windowRect, (windowId) =>
			{
				// �f�o�b�O�p �e�̔��˕������ꗥ�ŕύX����
				//GUILayout.Label("Projectile Direction (Debug)");
				//ProjectileDirection.x = GUILayout.HorizontalSlider(ProjectileDirection.x, -180f, 180f);
				//ProjectileDirection.y = GUILayout.HorizontalSlider(ProjectileDirection.y, -180f, 180f);
				//ProjectileDirection.z = GUILayout.HorizontalSlider(ProjectileDirection.z, -180f, 180f);

				//GUILayout.Label("Target Position (Debug)");
				//DebugTargetPos.x = GUILayout.HorizontalSlider(DebugTargetPos.x, -50, 50);
				//DebugTargetPos.y = GUILayout.HorizontalSlider(DebugTargetPos.y, -50, 50);
				//DebugTargetPos.z = GUILayout.HorizontalSlider(DebugTargetPos.z, -50, 50);

				GUILayout.Label("Target List (" + TargetList.Count + ") :");
				foreach (StartingBlockScript sb in TargetList)
                {
					GUILayout.Label("id = " + sb.playerId + ", pos = " + sb.transform.position.ToString());
                }

				//GUILayout.Label("�{�� = " + crossbowPower);
				//crossbowPower = GUILayout.HorizontalSlider(crossbowPower, 10, 1000);

				GUILayout.Label("Message");
				GUILayout.Label(message0);
				GUILayout.Label(message1);
				GUILayout.Label(message2);
				GUILayout.Label(message3);

				GUI.DragWindow();
			}, "Lock On Mod");
        }
		*/

		// �I���̃^�[�Q�b�g���擾����
		public void SetTargetList()
        {
			TargetList = new List<StartingBlockScript>();
			foreach (PlayerData player in Players)
            {
				if (!player.machine.isSimulating || player.machine.LocalSim) continue; // �V�~�����[�V�������̃u���b�N�Ɍ���
				Machine machine = player.machine;
				foreach (BlockBehaviour block in machine.SimulationBlocks)
				{
					var sb = block.GetComponent<StartingBlockScript>();
					if (sb != null)
					{
						TargetList.Add(sb);
					}
				}
			}
        }
    }

	
}
