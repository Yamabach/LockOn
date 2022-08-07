using System;
using System.Collections.Generic;
using System.Linq;
using Modding;
using Modding.Blocks;
using Modding.Modules;
using Modding.Modules.Official;
using Besiege;
using UnityEngine;
using UnityEngine.UI;

namespace LOSpace
{
	/// <summary>
	/// ModEntry
	/// </summary>
	public class Mod : ModEntryPoint
	{
		public GameObject mod;
		/// <summary>
		/// ACM�����[�h����Ă��邩�ǂ���
		/// </summary>
		public static bool ACMLoaded;
		/// <summary>
		/// ACM�����̃f�[�^
		/// </summary>
		public static XmlData AcmConfig;
		/// <summary>
		/// ���b�N�I�����
		/// ����������PlayerId�C�^�[�Q�b�g��PlayerId
		/// </summary>
		public static MessageType LockonType;
		/// <summary>
		/// �^�[�Q�b�g���̍X�V��v�����郁�b�Z�[�W
		/// </summary>
		public static MessageType SetTargetListRequestType;

		public override void OnLoad()
		{
			Log("Load");
			mod = new GameObject("Lock On Mod");
			AddScriptManager.Instance.transform.parent = mod.transform;
			LockOnManager.Instance.transform.parent = mod.transform;
			UnityEngine.Object.DontDestroyOnLoad(mod);
			ACMLoaded = Mods.IsModLoaded(new Guid("A033CF51-D84F-45DE-B9A9-DEF1ED9A6075"));
			AcmConfig = XMLDeserializer.Deserialize();
			//AcmConfig.LogList(); // �f�o�b�O�p

			// ���b�N�I�������󂯎�����ۂ̋���
			// ���b�N�I�������ΏۂɏƏ�������������
			LockonType = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer);
			ModNetworking.Callbacks[LockonType] += new Action<Message>((msg) =>
			{
				int playerid = (int)msg.GetData(0);
				int targetid = (int)msg.GetData(1);
				//Log($"{playerid} -> {targetid}");
				StartingBlockScript startingBlock = null;
				GameObject target = null;
				
				foreach (PlayerData player in LockOnManager.Instance.Players)
				{
					// ���w����Ƃ���
					if (player.machine == null || player.PlayMode == BesiegePlayMode.Spectator)
					{
						continue;
					}

					if (!player.machine.isSimulating || player.machine.LocalSim) // �V�~�����[�V�������̃u���b�N�Ɍ���
					{
						continue;
					}

					// LockOnManager.Instance.TargetList�ł͖��t���[�����X�g���j�������̂ŁC�������ŐV���ɑ�������
					Machine machine = player.machine;
					foreach (BlockBehaviour block in machine.SimulationBlocks)
					{
						var sb = block.GetComponent<StartingBlockScript>();
						if (sb != null)
						{
							if (sb.playerId == playerid)
                            {
								startingBlock = sb;
                            }
							else if (sb.playerId == targetid)
                            {
								target = sb.gameObject;
                            }
						}
					}
				}
				
				if (startingBlock != null)
				{
					startingBlock.ChangeTarget((target == null) ? null : target);
				}
			});

			// �^�[�Q�b�g���̍X�V��v�����ꂽ�ۂ̃��b�Z�[�W
			// NO USE
			SetTargetListRequestType = ModNetworking.CreateMessageType(DataType.Integer);
			ModNetworking.Callbacks[SetTargetListRequestType] += new Action<Message>((msg) =>
			{
				// ���w��ԂȂ牽�����Ȃ�
				if (Machine.Active() == null)
				{
					return;
				}

				// �󂯎����ID���������g�ȊO�Ȃ�CsetTargetList����
				if ((int)msg.GetData(0) == Machine.Active().PlayerID)
                {
					return;
                }
				LockOnManager.Instance.SetTargetList();
			});
		}
		/// <summary>
		/// mod��p���O�֐�
		/// </summary>
		/// <param name="msg"></param>
		public static void Log(string msg)
        {
			Debug.Log("Lock On Mod : " + msg);
        }
		/// <summary>
		/// mod��p�x���֐�
		/// </summary>
		/// <param name="msg"></param>
		public static void Warning(string msg)
        {
			Debug.LogWarning("Lock On Mod : " + msg);
        }
		/// <summary>
		/// mod��p�G���[�֐�
		/// </summary>
		/// <param name="msg"></param>
		public static void Error(string msg)
        {
			Debug.LogError("Lock On Mod : " + msg);
        }
	}
	/// <summary>
	/// �R���|�[�l���g�̃A�^�b�`�p�N���X
	/// </summary>
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
	/// <summary>
	/// �u���b�N�p�R���|�[�l���g���N���X
	/// </summary>
	public abstract class AbstractBlockScript : MonoBehaviour
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
	/// <summary>
	/// �X�^�u��
	/// �^�[�Q�b�g����ԗ����A���̒�����K�؂ȃ^�[�Q�b�g��I�Ԗ���
	/// ��ʂ̃^�[�Q�b�g�̏�Ƀ��b�N�I�����ꂽ���������}�[�J�[��\������
	/// </summary>
	public class StartingBlockScript : AbstractBlockScript
    {
		/// <summary>
		/// ���݂̖ڕW
		/// </summary>
		public GameObject CurrentTarget;
		/// <summary>
		/// ���݂̎����̃`�[��
		/// </summary>
		public MPTeam team;
		/// <summary>
		/// �T�[�o��ł̃v���C���[ID
		/// </summary>
		public int playerId;

		/// <summary>
		/// �I�[�g�G�C����L�������邩�ǂ���
		/// </summary>
		public MKey Activate;
		public bool IsActivated
        {
			private set; get;
        }

		/// <summary>
		/// �G���
		/// </summary>
		public List<Enemy> TargetCandidates
		{
			set; get;
		}

		// UI
		/// <summary>
		/// �G�ɂ�����}�[�N
		/// </summary>
		public Texture markerTexture;
		public Texture markerTexture_red;
		public Texture markerCornerTexture;
		public Texture markerCornerTexture_red;
		public Texture targetTexture;

		// ���b�N�I�����[�h�I��
		/// <summary>
		/// ���b�N�I������ۂɗD�悷�鍀��
		/// </summary>
		public enum LockOnMode
        {
			Nearest,
			MostCentral,
        }
		/// <summary>
		/// ���b�N�I�����[�h
		/// </summary>
		public LockOnMode mode
        {
            get
            {
				return (LockOnMode)LockOnModeMenu.Value;
            }
        }
		/// <summary>
		/// ���b�N�I�����[�h�I��pUI
		/// </summary>
		public MMenu LockOnModeMenu;
		/// <summary>
		/// UI�p���O
		/// </summary>
		public List<string> LockOnModeItems
        {
            get
            {
				return new List<string>
				{
					"Nearest",
					"Most Central",
				};
            }
        }

		/// <summary>
		/// �G�̉�ʓ��ɓ����Ă��邩�̔���
		/// </summary>
		public Camera mainCamera;
		/// <summary>
		/// ��ʂ̑傫��
		/// </summary>
		public Vector2 screenSize;

        public override void SafeAwake()
        {
			base.SafeAwake();
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

			PlayerData player;
			Playerlist.GetPlayer((ushort)playerId, out player);
			team = StatMaster.isMP ? player.team : MPTeam.None;
			//Mod.Log("BB.team = " + team.ToString());

			// �^�[�Q�b�g���X�g�X�V
			//ModNetworking.SendToAll(Mod.SetTargetListRequestType.CreateMessage(playerId));

			// �f�o�b�O�p
			CurrentTarget = null;
			ChangeTarget(null);

			// ���C���J�����ݒ�
			mainCamera = Camera.main;
			screenSize = new Vector2(Screen.width, Screen.height);

			// �G�����X�g�Ɋi�[����
			TargetCandidates = new List<Enemy>();

			// �e�N�X�`���̃��[�h
			markerTexture = ModTexture.GetTexture("marker-green").Texture;
			markerTexture_red = ModTexture.GetTexture("marker-red").Texture;
			markerCornerTexture = ModTexture.GetTexture("marker-corner-green").Texture;
			markerCornerTexture_red = ModTexture.GetTexture("marker-corner-red").Texture;
			targetTexture = ModTexture.GetTexture("lockon-target").Texture;

			// UI�ݒ�
			Activate = BB.AddKey("Auto Aim", "auto-aim", KeyCode.L);
			LockOnModeMenu = BB.AddMenu("lock-on-mode", 0, LockOnModeItems);
		}
        public override void SimulateUpdateAlways()
		{
			// �g�O����
			if (Activate.IsPressed || Activate.EmulationPressed())
			{
				IsActivated = !IsActivated;
			}
		}
        public override void SimulateFixedUpdateAlways()
        {
			SetTargetCandidates();

			// �G�����Ȃ��ꍇ�͉������b�N�I�����Ȃ�
			if (TargetCandidates == null)
            {
				ChangeTarget(null);
            }
			else if (TargetCandidates.Count == 0)
            {
				ChangeTarget(null);
			}

			// �g�O�����L���łȂ��ꍇ�͉������Ȃ�
			if (!IsActivated)
			{
				ChangeTarget(null);
				foreach (Enemy e in TargetCandidates)
                {
					e.Gauge = 0f;
                }
				return;
			}

			// ���b�N�I�������͂��̃u���b�N�������̂��̂ł���ꍇ�ɍs��
			if (playerId == Machine.Active().PlayerID)
			{

				// �J���������ɂ��鎞�Ԃɉ����ă��X�g���̓G�ɃQ�[�W�𗭂߂�
				// �J�����̊O�ɏo���G�̃Q�[�W��0�ɂȂ�
				foreach (Enemy e in TargetCandidates)
				{
					var screenPos = RectTransformUtility.WorldToScreenPoint(mainCamera, e.Target.transform.position);
					// �Ƃ肠������ʓ��ɓ��������Ƃ�z�� �n�`�̉��ɂ����ԂŃ��b�N�I�����O���悤�ɂ͂�����
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

				// ���b�N�I��
				switch (mode)
				{
					// �Q�[�W�����܂����G�̒��ōł��߂��G�����b�N�I��
					default:
					case LockOnMode.Nearest:
						float minSqrDistance = float.PositiveInfinity;
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
						break;
					// �Q�[�W�����܂����G�̒��ōł���ʂ̒��S�ɂ���G�����b�N�I��
					case LockOnMode.MostCentral:
						float minSqrDistance2 = float.PositiveInfinity;
						Enemy mostCentralEnemy = null;
						foreach (Enemy e in TargetCandidates)
                        {
							if (!e.LockOn) continue;

							Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(mainCamera, e.Target.transform.position);
							Vector2 screenCenter = screenSize / 2;
							var sqrDistance = Vector2.SqrMagnitude(screenPos - screenCenter);
							if (sqrDistance < minSqrDistance2)
                            {
								minSqrDistance2 = sqrDistance;
								mostCentralEnemy = e;
                            }
                        }
						ChangeTarget(mostCentralEnemy == null ? null : mostCentralEnemy.Target);
						break;
				}
			}
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
		/// <summary>
		/// �^�[�Q�b�g�}�[�J�[
		/// �f�o�b�O�p
		/// </summary>
		public void OnGUI()
		{
			// �����̂Ƃ���ł����\������
			if (playerId != Machine.Active().PlayerID || !BB.isSimulating || !IsActivated || TargetCandidates.Count == 0)
			{
				return;
			}

			// �^�[�Q�b�g�}�[�J�[
			foreach (Enemy e in TargetCandidates)
            {
				if (e == null)
                {
					continue;
                }
				if (e.Target == null)
                {
					continue;
                }

				// �}�[�J�[
				var tex = e.LockOn ? markerTexture_red : markerTexture;
				Rect targetPos;
				var z = ScreenPoint(e.Target, new Vector2(50, 50), out targetPos);
				GUI.DrawTexture(targetPos, tex, ScaleMode.StretchToFill, true, 0);

				// �R�[�i�[
				var tex2 = e.LockOn ? markerCornerTexture_red : markerCornerTexture;
				Rect targetPos2;
				var z2 = ScreenPoint(e.Target, new Vector2(100 - 30 * e.Gauge, 100 - 30 * e.Gauge), out targetPos2);
				GUI.DrawTexture(targetPos2, tex2, ScaleMode.StretchToFill, true, 0);

				// ���b�N�I�����Ă���ΏۂȂ炳��ɊO�g��\������
				if (CurrentTarget == e.Target)
                {
					Rect targetPos3;
					var z3 = ScreenPoint(e.Target, new Vector2(90, 80), out targetPos3);
					//targetPos3.y -= 50;
					GUI.DrawTexture(targetPos3, targetTexture, ScaleMode.StretchToFill, true, 0);
                }
            }
        }

		/// <summary>
		/// �^�[�Q�b�g�ɂȂ肤��G��S�ă��X�g�Ɋi�[����
		/// �s�x�V�������Ă���ƃ��b�N�I�����Ԃ��v���ł��Ȃ��Ȃ� �^�[�Q�b�g�̃Q�[�W��ێ������܂ܐV���ȃ��X�g�����K�v������
		/// �^�[�Q�b�g���̒�����O������𖞂����Ȃ��Ȃ������̂������i�^�[�Q�b�g���X�g�ɂ��Ȃ����̂�e���j���V���ɑO������𖞂������̂ŁA�܂����ɓ����Ă��Ȃ����̂�������
		/// �����I�ɂ�Update�ɒu�����ɓK�؂ȃ^�C�~���O�ŌĂяo���悤�ɂ�����
		/// </summary>
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
		/// <summary>
		/// �^�[�Q�b�g��ύX����
		/// </summary>
		public void ChangeTarget(GameObject NextTarget)
		{
			if (StatMaster.isClient)
			{
				bool curTargetIsNull = CurrentTarget == null;
				bool nextTargetIsNull = NextTarget == null;
				if (curTargetIsNull && nextTargetIsNull) return;
				else if (curTargetIsNull && !nextTargetIsNull)
				{
					BlockBehaviour bb = NextTarget.GetComponent<BlockBehaviour>();
					if (bb != null)
					{
						ModNetworking.SendToHost(Mod.LockonType.CreateMessage(playerId, (int)bb.ParentMachine.PlayerID));
						//Mod.Log($"{curTargetIsNull} -> {nextTargetIsNull}");
					}
				}
				else if (!curTargetIsNull && nextTargetIsNull)
				{
					ModNetworking.SendToHost(Mod.LockonType.CreateMessage(playerId, -1));
					//Mod.Log($"{curTargetIsNull} -> {nextTargetIsNull}");
				}
				else // !curTargetIsNull && !nextTargetIsNull
				{
					if (CurrentTarget != NextTarget)
					{
						BlockBehaviour bb = NextTarget.GetComponent<BlockBehaviour>();
						if (bb != null)
						{
							ModNetworking.SendToHost(Mod.LockonType.CreateMessage(playerId, (int)bb.ParentMachine.PlayerID));
							//Mod.Log($"{curTargetIsNull} -> {nextTargetIsNull}");
						}
					}
				}
			}
			CurrentTarget = (NextTarget == null) ? null : NextTarget;
		}
		/// <summary>
		/// �ڕW�̉�ʏ�ɂ�����ʒu
		/// </summary>
		/// <param name="target">�ڕW</param>
		/// <param name="scale">��ʃT�C�Y</param>
		/// <returns>��ʂ���݂��ڕW�̐[�x�iz���W�j</returns>
		public float ScreenPoint(GameObject target, Vector2 scale, out Rect result)
        {
			Vector3 point = mainCamera.WorldToScreenPoint(target.transform.position);
			Vector2 pos = new Vector2(point.x - scale.x/2, screenSize.y - point.y - scale.y/2);
			//point.y = screenSize.y - point.y;
			result = point.z > 0 ? new Rect(pos, scale) : new Rect(0, 0, 0, 0);
			return point.z;
        }
		/// <summary>
		/// �G�N���X�irigidbody�͑z�肵�Ȃ��j
		/// </summary>
		public class Enemy
        {
			/// <summary>
			/// �^�[�Q�b�g�̃Q�[���I�u�W�F�N�g
			/// </summary>
			public GameObject Target;
			/// <summary>
			/// ���g�����b�N�I�����Ă��邩�ǂ���
			/// </summary>
			public bool LockOn
            {
                get
                {
					return gauge == 1f;
                }
            }
			/// <summary>
			/// �Q�[�W
			/// 0 ~ 1
			/// </summary>
			private float gauge;
			public float Gauge
			{
				set
				{
					gauge = Utility.Clamp(0f, value, 1f);
				}
                get
                {
					return gauge;
                }
			}
			/// <summary>
			/// �R���X�g���N�^
			/// </summary>
			/// <param name="go">�^�[�Q�b�g</param>
			/// <param name="g">�Q�[�W</param>
			public Enemy(GameObject go, float g=0)
            {
				Target = go;
				gauge = g;
            }
        }
    }
	/// <summary>
	/// �e���ύX���s�������n�u���b�N�̊��N���X
	/// �X�^�u���Ń��b�N�I�������G�̈ړ��ʒu��\�����A���ˊp�x��ύX�������
	/// TODO: �Ə����������Ƀ}�[�J�[��\������
	/// TODO: rigidbody�̔r���i�N���C�A���g�̃Q�[����ɂ�rigidbody���������߁j
	/// </summary>
	public abstract class LockOnBlockScript : AbstractBlockScript
    {
		/// <summary>
		/// �␳�p�x
		/// </summary>
		public Quaternion Correction;
		/// <summary>
		/// ��o�Ă���ʒu�p��
		/// </summary>
		public Transform ProjectileSpawn;
		/// <summary>
		/// ���݂̖ڕW�ʒu
		/// </summary>
		public Vector3 TargetPos = Vector3.zero;
		/// <summary>
		/// 1�t���[���O�̖ڕW�ʒu
		/// </summary>
		public Vector3 TargetPosBefore1 = Vector3.zero;
		/// <summary>
		/// 2�t���[���O�̖ڕW�ʒu
		/// </summary>
		public Vector3 TargetPosBefore2 = Vector3.zero;
		/// <summary>
		/// �d�͂����邩�ǂ���
		/// </summary>
		public bool gravity;
		/// <summary>
		///  �d�͉����x 32.81m/s2
		/// </summary>
		public readonly float g = Physics.gravity.magnitude;
		/// <summary>
		/// �e�̏����x�i1�{�j
		/// </summary>
		public float InitialSpeed
        {
			get; set;
        }
		/// <summary>
		/// �e�̏����x�{��
		/// </summary>
		public float Power
        {
			get; set;
        }
		/// <summary>
		/// 1�t���[���O�̈ʒu
		/// </summary>
		public Vector3 PosBefore1 = Vector3.zero;
		/// <summary>
		/// ���̃u���b�N�̑��x
		/// </summary>
		public Vector3 Velocity
		{
			get
			{
				return (transform.position - PosBefore1) / Time.fixedDeltaTime;
			}
		}
		/// <summary>
		/// ���̃u���b�N�Ƀ��b�N�I�����[�h��K�p���邩�ǂ���
		/// </summary>
		public MToggle Apply;

		/// <summary>
		/// �X�^�u��
		/// </summary>
		public StartingBlockScript startingBlock;

		public override void SafeAwake()
		{
			base.SafeAwake();
			// �e�̏����p�����擾
			SetProjectileSpawn();

			gravity = !StatMaster.GodTools.GravityDisabled;

			// �X�^�u�����擾����
			foreach (BlockBehaviour block in BB.isSimulating ? BB.ParentMachine.SimulationBlocks : BB.ParentMachine.BuildingBlocks)
			{
				startingBlock = block.GetComponent<StartingBlockScript>();
				if (startingBlock != null) break;
			}
			if (startingBlock == null)
            {
				Mod.Error("StartingBlock is null!");
            }

			// UI�ݒ�
			Apply = BB.AddToggle("Apply Auto Aim", "apply", true);
		}
		public override void SimulateFixedUpdateAlways()
		{
			// �e���\��
			if (startingBlock.CurrentTarget != null && Apply.IsActive) // �W�I�����݂���ꍇ ���� �I�[�g�G�C�����K�p����Ă���ꍇ
			{
				SetTarget(startingBlock.CurrentTarget);
				gravity = !StatMaster.GodTools.GravityDisabled;

				ProjectileSpawn.rotation = Rotate(Predict(TargetPos, TargetPosBefore1, TargetPosBefore2, InitialSpeed * Power), -transform.up, 30f);
			}
            else // �W�I�����݂��Ȃ��ꍇ
            {
				ProjectileSpawn.rotation = Rotate(-transform.up);
            }

			// �ڕW�ʒu�X�V
			TargetPosBefore2 = TargetPosBefore1;
			TargetPosBefore1 = TargetPos;
			PosBefore1 = transform.position;
		}
		/// <summary>
		/// �e�̎p�����擾
		/// </summary>
		public virtual void SetProjectileSpawn()
		{
			ProjectileSpawn = transform.FindChild("projective spawn");
		}
		/// <summary>
		/// ���`�\�����~�`�\������I�����A�ڕW�̈ʒu��\������
		/// </summary>
		/// <param name="targetPos">�ڕW�ʒu</param>
		/// <param name="targetPos1">1F�O�̖ڕW�ʒu</param>
		/// <param name="targetPos2">1F�O�̖ڕW�ʒu</param>
		/// <param name="speed">�e�̏���</param>
		/// <param name="limitAngle">���`���~�`����I������臒l 3F�Ԃ̊p�x</param>
		/// <returns></returns>
		public Vector3 Predict(Vector3 targetPos, Vector3 targetPos1, Vector3 targetPos2, float speed, float limitAngle = 0.03f, float limitMove = 0.03f)
        {
			Vector3 predTargetPos;
			if (Mathf.Abs(Vector3.Angle(targetPos - targetPos1, targetPos1 - targetPos2)) < limitAngle && (targetPos - targetPos1).sqrMagnitude > limitMove * limitMove)
			{
				// �~�`�\��
				predTargetPos = CircularPredict(targetPos, targetPos1, targetPos2, speed);
			}
			else
			{
				// ���`�\��
				predTargetPos = LinearPredict(targetPos, targetPos1, speed);
			}
			return predTargetPos;
		}
		/// <summary>
		/// ���`�\��
		/// </summary>
		/// <param name="targetPos">�ڕW�̌��݈ʒu</param>
		/// <param name="targetPrePos">�ڕW�̑��x</param>
		/// <param name="speed">�e�̑���</param>
		/// <returns></returns>
		public virtual Vector3 LinearPredict(Vector3 targetPos, Vector3 targetPrePos, float speed) // ���݂̕W�I�̈ʒu�A���x�A�e�̑���
		{
			float flame2s = Time.fixedDeltaTime; // 0.01f s/frame

			//Unity�̕�����m/s�Ȃ̂�m/flame�ɂ���
			speed = speed * flame2s; // m/frame
			Vector3 v3_Mv = targetPos - targetPrePos; // m/frame
			//Vector3 v3_Mv = (targetPos - targetPrePos) / flame2s;
			Vector3 v3_Pos = targetPos - ProjectileSpawn.position;

			float A = Vector3.SqrMagnitude(v3_Mv) - speed * speed; // m2/frame2 (m2/s2)
			float B = Vector3.Dot(v3_Pos, v3_Mv); // m2/frame (m2/s)
			float C = Vector3.SqrMagnitude(v3_Pos); // m2

            float PredictionFlame; // frame

            //0���֎~
            if (A == 0f && B == 0f) PredictionFlame = 0f;
			else if (A == 0f) PredictionFlame = (-C / B / 2);

			else
			{
				//�������͂ǂ���������Ȃ��̂Ő�Βl�Ŗ�������
				float D = Mathf.Sqrt(Mathf.Abs(B * B - A * C));

				// ����
				PredictionFlame = Utility.PlusMin((-B - D) / A, (-B + D) / A);
				//Mod.Log($"{(-B - D) / A * flame2s}, {(-B + D) / A * flame2s}");
			}
			//Mod.Log((PredictionFlame * flame2s).ToString());
			var result = targetPos + v3_Mv * PredictionFlame;

			// �d�͂̏���
			// �d�͂ŗ����镪�����ڕW�ʒu���グ��
			if (gravity)
			{
				result.y += g * (PredictionFlame * flame2s) * (PredictionFlame * flame2s) / 2f; // ���}�I�ɏd�݂�������
			}

			// �u���b�N�̑��x�̕������␳
			result -= Velocity * PredictionFlame * flame2s;

			return result;
		}
		/// <summary>
		/// �~�`�\��
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="velo"></param>
		/// <param name="angularVelo"></param>
		/// <param name="speed"></param>
		/// <returns></returns>
		public virtual Vector3 CircularPredict(Vector3 targetPos, Vector3 targetPos1, Vector3 targetPos2, float speed) // ���݂̕W�I�̈ʒu�C���x�C�p���x�C�e�̑���
        {
			float flame2s = Time.fixedDeltaTime;

			//Unity�̕�����m/s�Ȃ̂�m/flame�ɂ���
			speed = speed * flame2s;

			//3�_����~�̒��S�_���o��
			Vector3 CenterPosition = Utility.Circumcenter(targetPos, targetPos1, targetPos2);

			//���S�_���猩��1�t���[���̊p���x�Ǝ����o��
			Vector3 axis = Vector3.Cross(targetPos1 - CenterPosition, targetPos - CenterPosition);
			float angle = Vector3.Angle(targetPos1 - CenterPosition, targetPos - CenterPosition);

			//���݈ʒu�Œe�̓��B���Ԃ��o��
			float PredictionFlame = Vector3.Distance(targetPos, ProjectileSpawn.position) / speed;

			//���B���ԕ����ړ������\���ʒu�ōČv�Z���ē��B���Ԃ�␳����B
			for (int i = 0; i < 3; ++i)
			{
				PredictionFlame = Vector3.Distance(Utility.RotateToPosition(targetPos, CenterPosition, axis, angle * PredictionFlame), ProjectileSpawn.position) / speed;
			}
			var result = Utility.RotateToPosition(targetPos, CenterPosition, axis, angle * PredictionFlame);

			// �d�͂̏���
			// �d�͂ŗ����镪�����ڕW�ʒu���グ��
			if (gravity)
			{
				result.y += g * (PredictionFlame * flame2s) * (PredictionFlame * flame2s) / 2f; // ���}�I�ɏd�݂�������
			}

			// �u���b�N�̑��x�̕������␳
			result -= Velocity * PredictionFlame * flame2s;

			return result;
		}
		/// <summary>
		/// �ڕW�������悤�ɉ�]����
		/// </summary>
		/// <param name="to">�ڕW�ʒu</param>
		/// <param name="defaultRot">�����ȏꍇ�̌���</param>
		/// <param name="limitAngle">��]�\�ȏ���p�x</param>
		/// <returns></returns>
		public virtual Quaternion Rotate(Vector3 to, Vector3 defaultRot, float limitAngle = 30f) // ���ʂ̌����ɒ��ӁI
		{
			return Vector3.Angle(defaultRot, to - ProjectileSpawn.position) < limitAngle ? Quaternion.LookRotation(to - ProjectileSpawn.position) : Quaternion.LookRotation(defaultRot);
		}
		/// <summary>
		/// �ڕW�������悤�ɉ�]����
		/// </summary>
		/// <returns></returns>
		public virtual Quaternion Rotate(Vector3 defaultRot)
        {
			return Quaternion.LookRotation(defaultRot);
        }
		/// <summary>
		/// �ڕW���߂�
		/// </summary>
		public virtual void SetTarget(Vector3 pos)
        {
			TargetPos = (pos == null) ? Vector3.zero : pos;
        }
		/// <summary>
		/// �ڕW���߂�
		/// </summary>
		/// <param name="nextTarget"></param>
		public virtual void SetTarget(GameObject nextTarget)
        {
			if (nextTarget != null)
			{
				SetTarget(nextTarget.transform.position);
			}
            else
            {
				SetTarget(Vector3.zero);
            }
        }
		/// <summary>
		/// �ڕW�����߂�
		/// ���݂��Ȃ����vector3.zero���Z�b�g����
		/// </summary>
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
				SetTarget(Vector3.zero);
				return;
            }
			if (startingBlock.CurrentTarget == null)
            {
				SetTarget(Vector3.zero);
				return;
            }
			SetTarget(startingBlock.CurrentTarget);
        }
	}
	/// <summary>
	/// Cannon�n
	/// �t�����ɒe���o��g���u������
	/// </summary>
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
			InitialSpeed = (Cannon.shrapnel ? 1f : 1f);
			Power = Cannon.boltSpeed;
			ProjectileSpawn = Cannon.boltObject;
		}
        public override void SimulateFixedUpdateAlways()
        {
			if (startingBlock.CurrentTarget != null)
			{
				SetTarget(startingBlock.CurrentTarget);
				gravity = !StatMaster.GodTools.GravityDisabled;

				// �e���\��
				var predTargetPos = Predict(TargetPos, TargetPosBefore1, TargetPosBefore2, InitialSpeed * Power);
				if (Cannon.shrapnel)
				{
					Cannon.boltSpawnRot = Rotate(predTargetPos, -transform.up); // �g�U�C�̔��˕�����ύX // �t
				}
				else
				{
					// Cannon�ł���ꍇ�̋���
					Cannon.boltSpawnRot = Rotate(predTargetPos, -transform.up); // �g�U�C�̔��˕�����ύX // �t
				}
			}
            else
            {
				Cannon.boltObject.rotation = Rotate(-transform.up);
            }

			// �ڕW�ʒu�X�V
			TargetPosBefore2 = TargetPosBefore1;
			TargetPosBefore1 = TargetPos;
			PosBefore1 = transform.position;
		}
        public override void SetProjectileSpawn()
        {
			ProjectileSpawn = null;
        }
		/*
        public void SetInitialSpeed()
        {
			InitialSpeed = Cannon.boltSpeed * (Cannon.shrapnel ? 1f : 1f);
        }
		*/
    }
	/// <summary>
	/// Crossbow
	/// </summary>
	public class CrossbowScript : LockOnBlockScript
    {
		public CrossBowBlock Crossbow;
		public LineRenderer line; // �f�o�b�O�p

        public override void SafeAwake()
        {
			//Mod.Log("Crossbow Script");
			base.SafeAwake();
			Crossbow = GetComponent<CrossBowBlock>();
			InitialSpeed = 75f;
			Power = Crossbow.power;
		}
    }
	/// <summary>
	/// Flamethrower
	/// </summary>
	public class FlamethrowerScript : LockOnBlockScript
    {
		/// <summary>
		/// �΂̃G�t�F�N�g
		/// </summary>
		public Transform Fire;
		//public LineRenderer line; // �f�o�b�O�p
		public override void SafeAwake()
        {
			//Mod.Log("Flamethrower Script");
			Fire = transform.FindChild("Fire");

			base.SafeAwake();

			// ��ɏd�͂̉e�����󂯂Ȃ�
			gravity = false;

			InitialSpeed = 500f;
			Power = 1f;
		}
        public override void SimulateFixedUpdateAlways()
        {
			if (startingBlock.CurrentTarget != null)
			{
				SetTarget(startingBlock.CurrentTarget);

				// �e���\��
				Correction = Rotate(Predict(TargetPos, TargetPosBefore1, TargetPosBefore2, InitialSpeed * Power), transform.forward);
			}
            else
            {
				Correction = Rotate(transform.forward);
			}
			Fire.rotation = Correction;
			ProjectileSpawn.rotation = Correction;
		}
		public override void SetProjectileSpawn()
		{
			ProjectileSpawn = transform.FindChild("FireTrigger");
		}
	}

	// mod�Œǉ�����镐��mod�ɑ΂���
	// �V���[�e�B���O���W���[���ł̓u���b�N��.ShootingDirectionVisual(Clone)�Ƃ������O�̎q�I�u�W�F�N�g�Ŕ��˕����𐧌䂵�Ă���͗l
	// ACM�̏ꍇ�̓u���b�N��.AdShootingVisual(Clone) �Ƃ������O�̎q�I�u�W�F�N�g�Ŕ��˕����𐧌䂵�Ă���͗l
	/// <summary>
	/// �������W���[������mod�u���b�N
	/// </summary>
	public class ModAddedBlocksScript : LockOnBlockScript
    {
		/// <summary>
		/// �����̃V���[�e�B���O���W���[��
		/// </summary>
		public ShootingModuleBehaviour shootingModule;
		/// <summary>
		/// �e�̕������w�肷��Q�[���I�u�W�F�N�g����
		/// </summary>
		public List<Transform> ProjectileVis;
		public readonly string originalShootingModuleName = "ShootingDirectionVisual(Clone)";
		public readonly string acmShootingModuleName = "AdShootingVisual(Clone)";
		/// <summary>
		/// �e�̕����̏����l
		/// </summary>
		public List<GameObject> defaultForward;
		//public LineRenderer line; // �f�o�b�O�p

		// �����̌v�Z
		//public MSlider PowerSlider;
		//public float power;

		/// <summary>
		/// ACM�����ǂ���
		/// </summary>
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

			// v = f * 1 / m
			InitialSpeed = 1f / shootingModule.Module.ProjectileInfo.Mass; // 1 / m
			Power = shootingModule.GetSlider(shootingModule.Module.PowerSlider).Value; // f
		}
        public override void SimulateFixedUpdateAlways()
		{
			// �e���\��
			if (startingBlock.CurrentTarget != null) // �W�I�����݂���ꍇ
			{
				SetTarget(startingBlock.CurrentTarget);
				gravity = !StatMaster.GodTools.GravityDisabled;

				Vector3 predTargetPos = Predict(TargetPos, TargetPosBefore1, TargetPosBefore2, InitialSpeed * Power);
				for (int i = 0; i < ProjectileVis.Count; i++)
				{
					ProjectileSpawn = ProjectileVis[i];
					ProjectileVis[i].rotation = Rotate(predTargetPos, defaultForward[i].transform.forward);
				}
			}
			else // �W�I�����݂��Ȃ��ꍇ
			{
				for (int i = 0; i < ProjectileVis.Count; i++)
				{
					ProjectileSpawn = ProjectileVis[i];
					ProjectileVis[i].rotation = Rotate(defaultForward[i].transform.forward);
				}
			}

			// �f�o�b�O�p
			//line.SetPositions(new Vector3[] { ProjectileVis[0].position, ProjectileVis[0].forward * 100f });

			// �ڕW�ʒu�X�V
			TargetPosBefore2 = TargetPosBefore1;
			TargetPosBefore1 = TargetPos;
			PosBefore1 = transform.position;
		}
        public override void SetProjectileSpawn()
        {
			ProjectileSpawn = null;
        }
	}
	/// <summary>
	/// ACM���u���b�N
	/// </summary>
	public class AdShootingBlocksScript : LockOnBlockScript
    {
		/// <summary>
		/// �e�̕������w�肷��Q�[���I�u�W�F�N�g����
		/// </summary>
		public List<Transform> ProjectileVis;
		public readonly string acmShootingModuleName = "AdShootingVisual(Clone)";
		/// <summary>
		/// �e�̕����̏����l
		/// </summary>
		public List<GameObject> defaultForward;
		/// <summary>
		/// xml�f�[�^
		/// </summary>
		XmlBlock xmlBlock;

		/// <summary>
		/// �����̌v�Z
		/// </summary>
		public XDataHolder adBlockData;

		public override void SafeAwake()
		{
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

			base.SafeAwake();

			/*
			foreach (XData x in adBlockData.ReadAll())
            {
				Mod.Log(x.Key);
            }
			*/

			if (Mod.AcmConfig.Find(name, out xmlBlock))
            {
				InitialSpeed = 1f / xmlBlock.Mass;
            }
            else
            {
				Mod.Warning($"{name} does not exist in AcmProjectileMass.xml! disabled itself");
				InitialSpeed = 1f;
				enabled = false;
				return;
			}

			// block��XML��������擾
			adBlockData = BlockInfo.FromBlockBehaviour(BB).BlockData;
			Power = adBlockData.HasKey("bmt-power") ? adBlockData.ReadFloat("bmt-power") : 1f;
		}
		public override void SimulateFixedUpdateAlways()
		{
			if (startingBlock.CurrentTarget != null)
			{
				SetTarget(startingBlock.CurrentTarget);
				gravity = !StatMaster.GodTools.GravityDisabled && xmlBlock.Gravity;

				// �e���\��
				//var predTargetPos = Predict(Target, TargetVelo, 1f * power); // �b��I�ɏ���������
				Vector3 predTargetPos = Predict(TargetPos, TargetPosBefore1, TargetPosBefore2, InitialSpeed * Power);
				for (int i = 0; i < ProjectileVis.Count; i++)
				{
					ProjectileSpawn = ProjectileVis[i];
					ProjectileVis[i].rotation = Rotate(predTargetPos, defaultForward[i].transform.forward);
				}
			}
			else
			{
				for (int i = 0; i < ProjectileVis.Count; i++)
				{
					ProjectileSpawn = ProjectileVis[i];
					ProjectileVis[i].rotation = Rotate(defaultForward[i].transform.forward);
				}
			}

			// �ڕW�ʒu�X�V
			TargetPosBefore2 = TargetPosBefore1;
			TargetPosBefore1 = TargetPos;
			PosBefore1 = transform.position;
		}
		public override void SetProjectileSpawn()
		{
			ProjectileSpawn = null;
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

		// �W�I�i�X�^�u���j
		public List<StartingBlockScript> TargetList;
		public List<PlayerData> Players, PlayersPast; // �I���̃v���C���[�ꗗ

		public void Awake()
        {
			TargetList = new List<StartingBlockScript>();
        }
		public void FixedUpdate()
        {
			Players = Playerlist.Players;

			// �v���C���[���X�g�X�V �v���C���[�̐����ω���������
			// �Ƃ肠��������擾�������Ă݂�
			SetTargetList();
			PlayersPast = new List<PlayerData>(Players);
		}

		/// <summary>
		/// �I���̃^�[�Q�b�g���擾���CTargetList�Ɋi�[����
		/// </summary>
		public void SetTargetList()
        {
			TargetList = new List<StartingBlockScript>();
			foreach (PlayerData player in Players)
            {
				// ���w����Ƃ��߁H
				if (player.machine == null || player.PlayMode == BesiegePlayMode.Spectator)
                {
					continue;
                }

				if (!player.machine.isSimulating || player.machine.LocalSim) // �V�~�����[�V�������̃u���b�N�Ɍ���
                {
					continue;
                }

				Machine machine = player.machine;
				foreach (BlockBehaviour block in machine.SimulationBlocks)
				{
					var sb = block.GetComponent<StartingBlockScript>();
					if (sb != null)
					{
						TargetList.Add(sb);
						break;
					}
				}
			}
        }
    }
}