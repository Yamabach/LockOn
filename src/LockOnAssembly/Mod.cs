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
		/// ACMがロードされているかどうか
		/// </summary>
		public static bool ACMLoaded;
		/// <summary>
		/// ACM武装のデータ
		/// </summary>
		public static XmlData AcmConfig;
		/// <summary>
		/// ロックオン情報
		/// 撃った方のPlayerId，ターゲットのPlayerId
		/// </summary>
		public static MessageType LockonType;
		/// <summary>
		/// ターゲット候補の更新を要請するメッセージ
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
			//AcmConfig.LogList(); // デバッグ用

			// ロックオン情報を受け取った際の挙動
			// ロックオンした対象に照準を向けさせる
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
					// 見学いるとだめ
					if (player.machine == null || player.PlayMode == BesiegePlayMode.Spectator)
					{
						continue;
					}

					if (!player.machine.isSimulating || player.machine.LocalSim) // シミュレーション中のブロックに限る
					{
						continue;
					}

					// LockOnManager.Instance.TargetListでは毎フレームリストが破棄されるので，こっちで新たに走査する
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

			// ターゲット候補の更新を要請された際のメッセージ
			// NO USE
			SetTargetListRequestType = ModNetworking.CreateMessageType(DataType.Integer);
			ModNetworking.Callbacks[SetTargetListRequestType] += new Action<Message>((msg) =>
			{
				// 見学状態なら何もしない
				if (Machine.Active() == null)
				{
					return;
				}

				// 受け取ったIDが自分自身以外なら，setTargetListする
				if ((int)msg.GetData(0) == Machine.Active().PlayerID)
                {
					return;
                }
				LockOnManager.Instance.SetTargetList();
			});
		}
		/// <summary>
		/// mod専用ログ関数
		/// </summary>
		/// <param name="msg"></param>
		public static void Log(string msg)
        {
			Debug.Log("Lock On Mod : " + msg);
        }
		/// <summary>
		/// mod専用警告関数
		/// </summary>
		/// <param name="msg"></param>
		public static void Warning(string msg)
        {
			Debug.LogWarning("Lock On Mod : " + msg);
        }
		/// <summary>
		/// mod専用エラー関数
		/// </summary>
		/// <param name="msg"></param>
		public static void Error(string msg)
        {
			Debug.LogError("Lock On Mod : " + msg);
        }
	}
	/// <summary>
	/// コンポーネントのアタッチ用クラス
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

			if (internalObject.name.Length > 32) // Modブロック
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
	/// ブロック用コンポーネント基底クラス
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
	/// スタブロ
	/// ターゲット候補を網羅し、その中から適切なターゲットを選ぶ役割
	/// 画面のターゲットの上にロックオンされたかを示すマーカーを表示する
	/// </summary>
	public class StartingBlockScript : AbstractBlockScript
    {
		/// <summary>
		/// 現在の目標
		/// </summary>
		public GameObject CurrentTarget;
		/// <summary>
		/// 現在の自分のチーム
		/// </summary>
		public MPTeam team;
		/// <summary>
		/// サーバ上でのプレイヤーID
		/// </summary>
		public int playerId;

		/// <summary>
		/// オートエイムを有効化するかどうか
		/// </summary>
		public MKey Activate;
		public bool IsActivated
        {
			private set; get;
        }

		/// <summary>
		/// 敵候補
		/// </summary>
		public List<Enemy> TargetCandidates
		{
			set; get;
		}

		// UI
		/// <summary>
		/// 敵にかかるマーク
		/// </summary>
		public Texture markerTexture;
		public Texture markerTexture_red;
		public Texture markerCornerTexture;
		public Texture markerCornerTexture_red;
		public Texture targetTexture;

		// ロックオンモード選択
		/// <summary>
		/// ロックオンする際に優先する項目
		/// </summary>
		public enum LockOnMode
        {
			Nearest,
			MostCentral,
        }
		/// <summary>
		/// ロックオンモード
		/// </summary>
		public LockOnMode mode
        {
            get
            {
				return (LockOnMode)LockOnModeMenu.Value;
            }
        }
		/// <summary>
		/// ロックオンモード選択用UI
		/// </summary>
		public MMenu LockOnModeMenu;
		/// <summary>
		/// UI用名前
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
		/// 敵の画面内に入っているかの判定
		/// </summary>
		public Camera mainCamera;
		/// <summary>
		/// 画面の大きさ
		/// </summary>
		public Vector2 screenSize;

        public override void SafeAwake()
        {
			base.SafeAwake();
			// サーバーID取得
			playerId = BB.ParentMachine.PlayerID;

			// 自身がリストに無ければリストに追加する
			#region リスト追加関係 一回コメントアウト
			/*
			bool exists = false;
			if (LockOnManager.Instance.TargetList != null)
			{
				foreach (StartingBlockScript sb in LockOnManager.Instance.TargetList)
				{
					if (sb.playerId == playerId) // 自身がリストにある場合は何もしない
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

			// ターゲットリスト更新
			//ModNetworking.SendToAll(Mod.SetTargetListRequestType.CreateMessage(playerId));

			// デバッグ用
			CurrentTarget = null;
			ChangeTarget(null);

			// メインカメラ設定
			mainCamera = Camera.main;
			screenSize = new Vector2(Screen.width, Screen.height);

			// 敵をリストに格納する
			TargetCandidates = new List<Enemy>();

			// テクスチャのロード
			markerTexture = ModTexture.GetTexture("marker-green").Texture;
			markerTexture_red = ModTexture.GetTexture("marker-red").Texture;
			markerCornerTexture = ModTexture.GetTexture("marker-corner-green").Texture;
			markerCornerTexture_red = ModTexture.GetTexture("marker-corner-red").Texture;
			targetTexture = ModTexture.GetTexture("lockon-target").Texture;

			// UI設定
			Activate = BB.AddKey("Auto Aim", "auto-aim", KeyCode.L);
			LockOnModeMenu = BB.AddMenu("lock-on-mode", 0, LockOnModeItems);
		}
        public override void SimulateUpdateAlways()
		{
			// トグル化
			if (Activate.IsPressed || Activate.EmulationPressed())
			{
				IsActivated = !IsActivated;
			}
		}
        public override void SimulateFixedUpdateAlways()
        {
			SetTargetCandidates();

			// 敵がいない場合は何もロックオンしない
			if (TargetCandidates == null)
            {
				ChangeTarget(null);
            }
			else if (TargetCandidates.Count == 0)
            {
				ChangeTarget(null);
			}

			// トグルが有効でない場合は何もしない
			if (!IsActivated)
			{
				ChangeTarget(null);
				foreach (Enemy e in TargetCandidates)
                {
					e.Gauge = 0f;
                }
				return;
			}

			// ロックオン処理はこのブロックが自分のものである場合に行う
			if (playerId == Machine.Active().PlayerID)
			{

				// カメラ中央にいる時間に応じてリスト内の敵にゲージを溜める
				// カメラの外に出た敵のゲージは0になる
				foreach (Enemy e in TargetCandidates)
				{
					var screenPos = RectTransformUtility.WorldToScreenPoint(mainCamera, e.Target.transform.position);
					// とりあえず画面内に入ったことを想定 地形の奥にいる状態でロックオンを外すようにはしたい
					if (0 < screenPos.x && screenPos.x < screenSize.x && 0 < screenPos.y && screenPos.y < screenSize.y)
					{
						// 1秒くらいでロックオンできるようにする
						e.Gauge += 0.01f;
					}
					else
					{
						e.Gauge = 0f;
					}
				}

				// ロックオン
				switch (mode)
				{
					// ゲージが溜まった敵の中で最も近い敵をロックオン
					default:
					case LockOnMode.Nearest:
						float minSqrDistance = float.PositiveInfinity;
						Enemy mostNearestEnemy = null;
						foreach (Enemy e in TargetCandidates)
						{
							if (!e.LockOn) continue; // ゲージが溜まっていなければ何もしない

							var sqrDistance = Vector3.SqrMagnitude(e.Target.transform.position - transform.position);
							if (sqrDistance < minSqrDistance)
							{
								minSqrDistance = sqrDistance;
								mostNearestEnemy = e;
							}
						}
						ChangeTarget(mostNearestEnemy == null ? null : mostNearestEnemy.Target);
						break;
					// ゲージが溜まった敵の中で最も画面の中心にいる敵をロックオン
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

        // シミュ開始時と終了時にリストを更新
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

        // デバッグ用GUI
        public Rect debugWindowRect = new Rect(100, 100, 200, 150);
		public int debugWindowId = ModUtility.GetWindowId();
		/// <summary>
		/// ターゲットマーカー
		/// デバッグ用
		/// </summary>
		public void OnGUI()
		{
			// 自分のところでだけ表示する
			if (playerId != Machine.Active().PlayerID || !BB.isSimulating || !IsActivated || TargetCandidates.Count == 0)
			{
				return;
			}

			// ターゲットマーカー
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

				// マーカー
				var tex = e.LockOn ? markerTexture_red : markerTexture;
				Rect targetPos;
				var z = ScreenPoint(e.Target, new Vector2(50, 50), out targetPos);
				GUI.DrawTexture(targetPos, tex, ScaleMode.StretchToFill, true, 0);

				// コーナー
				var tex2 = e.LockOn ? markerCornerTexture_red : markerCornerTexture;
				Rect targetPos2;
				var z2 = ScreenPoint(e.Target, new Vector2(100 - 30 * e.Gauge, 100 - 30 * e.Gauge), out targetPos2);
				GUI.DrawTexture(targetPos2, tex2, ScaleMode.StretchToFill, true, 0);

				// ロックオンしている対象ならさらに外枠を表示する
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
		/// ターゲットになりうる敵を全てリストに格納する
		/// 都度新しくしているとロックオン時間を計測できなくなる ターゲットのゲージを保持したまま新たなリストを作る必要がある
		/// ターゲット候補の中から前提条件を満たさなくなったものを除く（ターゲットリストにいないものを弾く）→新たに前提条件を満たすもので、まだ候補に入っていないものを加える
		/// 将来的にはUpdateに置かずに適切なタイミングで呼び出すようにしたい
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
					// 残留組
					if (sb.gameObject == e.Target)
                    {
						ret.Add(new Enemy(e.Target, e.Gauge));
                    }
                }
				if ((sb.team != team || sb.team == MPTeam.None) && sb != this) // ターゲット候補にするための前提条件
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
		/// ターゲットを変更する
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
		/// 目標の画面上における位置
		/// </summary>
		/// <param name="target">目標</param>
		/// <param name="scale">画面サイズ</param>
		/// <returns>画面からみた目標の深度（z座標）</returns>
		public float ScreenPoint(GameObject target, Vector2 scale, out Rect result)
        {
			Vector3 point = mainCamera.WorldToScreenPoint(target.transform.position);
			Vector2 pos = new Vector2(point.x - scale.x/2, screenSize.y - point.y - scale.y/2);
			//point.y = screenSize.y - point.y;
			result = point.z > 0 ? new Rect(pos, scale) : new Rect(0, 0, 0, 0);
			return point.z;
        }
		/// <summary>
		/// 敵クラス（rigidbodyは想定しない）
		/// </summary>
		public class Enemy
        {
			/// <summary>
			/// ターゲットのゲームオブジェクト
			/// </summary>
			public GameObject Target;
			/// <summary>
			/// 自身がロックオンしているかどうか
			/// </summary>
			public bool LockOn
            {
                get
                {
					return gauge == 1f;
                }
            }
			/// <summary>
			/// ゲージ
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
			/// コンストラクタ
			/// </summary>
			/// <param name="go">ターゲット</param>
			/// <param name="g">ゲージ</param>
			public Enemy(GameObject go, float g=0)
            {
				Target = go;
				gauge = g;
            }
        }
    }
	/// <summary>
	/// 弾道変更を行う武装系ブロックの基底クラス
	/// スタブロでロックオンした敵の移動位置を予測し、発射角度を変更する役割
	/// TODO: 照準が当たる先にマーカーを表示する
	/// TODO: rigidbodyの排除（クライアントのゲーム上にはrigidbodyが無いため）
	/// </summary>
	public abstract class LockOnBlockScript : AbstractBlockScript
    {
		/// <summary>
		/// 補正角度
		/// </summary>
		public Quaternion Correction;
		/// <summary>
		/// 矢が出てくる位置姿勢
		/// </summary>
		public Transform ProjectileSpawn;
		/// <summary>
		/// 現在の目標位置
		/// </summary>
		public Vector3 TargetPos = Vector3.zero;
		/// <summary>
		/// 1フレーム前の目標位置
		/// </summary>
		public Vector3 TargetPosBefore1 = Vector3.zero;
		/// <summary>
		/// 2フレーム前の目標位置
		/// </summary>
		public Vector3 TargetPosBefore2 = Vector3.zero;
		/// <summary>
		/// 重力があるかどうか
		/// </summary>
		public bool gravity;
		/// <summary>
		///  重力加速度 32.81m/s2
		/// </summary>
		public readonly float g = Physics.gravity.magnitude;
		/// <summary>
		/// 弾の初速度（1倍）
		/// </summary>
		public float InitialSpeed
        {
			get; set;
        }
		/// <summary>
		/// 弾の初速度倍率
		/// </summary>
		public float Power
        {
			get; set;
        }
		/// <summary>
		/// 1フレーム前の位置
		/// </summary>
		public Vector3 PosBefore1 = Vector3.zero;
		/// <summary>
		/// このブロックの速度
		/// </summary>
		public Vector3 Velocity
		{
			get
			{
				return (transform.position - PosBefore1) / Time.fixedDeltaTime;
			}
		}
		/// <summary>
		/// このブロックにロックオンモードを適用するかどうか
		/// </summary>
		public MToggle Apply;

		/// <summary>
		/// スタブロ
		/// </summary>
		public StartingBlockScript startingBlock;

		public override void SafeAwake()
		{
			base.SafeAwake();
			// 弾の初期姿勢を取得
			SetProjectileSpawn();

			gravity = !StatMaster.GodTools.GravityDisabled;

			// スタブロを取得する
			foreach (BlockBehaviour block in BB.isSimulating ? BB.ParentMachine.SimulationBlocks : BB.ParentMachine.BuildingBlocks)
			{
				startingBlock = block.GetComponent<StartingBlockScript>();
				if (startingBlock != null) break;
			}
			if (startingBlock == null)
            {
				Mod.Error("StartingBlock is null!");
            }

			// UI設定
			Apply = BB.AddToggle("Apply Auto Aim", "apply", true);
		}
		public override void SimulateFixedUpdateAlways()
		{
			// 弾道予測
			if (startingBlock.CurrentTarget != null && Apply.IsActive) // 標的が存在する場合 かつ オートエイムが適用されている場合
			{
				SetTarget(startingBlock.CurrentTarget);
				gravity = !StatMaster.GodTools.GravityDisabled;

				ProjectileSpawn.rotation = Rotate(Predict(TargetPos, TargetPosBefore1, TargetPosBefore2, InitialSpeed * Power), -transform.up, 30f);
			}
            else // 標的が存在しない場合
            {
				ProjectileSpawn.rotation = Rotate(-transform.up);
            }

			// 目標位置更新
			TargetPosBefore2 = TargetPosBefore1;
			TargetPosBefore1 = TargetPos;
			PosBefore1 = transform.position;
		}
		/// <summary>
		/// 弾の姿勢を取得
		/// </summary>
		public virtual void SetProjectileSpawn()
		{
			ProjectileSpawn = transform.FindChild("projective spawn");
		}
		/// <summary>
		/// 線形予測か円形予測かを選択し、目標の位置を予測する
		/// </summary>
		/// <param name="targetPos">目標位置</param>
		/// <param name="targetPos1">1F前の目標位置</param>
		/// <param name="targetPos2">1F前の目標位置</param>
		/// <param name="speed">弾の初速</param>
		/// <param name="limitAngle">線形か円形かを選択する閾値 3F間の角度</param>
		/// <returns></returns>
		public Vector3 Predict(Vector3 targetPos, Vector3 targetPos1, Vector3 targetPos2, float speed, float limitAngle = 0.03f, float limitMove = 0.03f)
        {
			Vector3 predTargetPos;
			if (Mathf.Abs(Vector3.Angle(targetPos - targetPos1, targetPos1 - targetPos2)) < limitAngle && (targetPos - targetPos1).sqrMagnitude > limitMove * limitMove)
			{
				// 円形予測
				predTargetPos = CircularPredict(targetPos, targetPos1, targetPos2, speed);
			}
			else
			{
				// 線形予測
				predTargetPos = LinearPredict(targetPos, targetPos1, speed);
			}
			return predTargetPos;
		}
		/// <summary>
		/// 線形予測
		/// </summary>
		/// <param name="targetPos">目標の現在位置</param>
		/// <param name="targetPrePos">目標の速度</param>
		/// <param name="speed">弾の速さ</param>
		/// <returns></returns>
		public virtual Vector3 LinearPredict(Vector3 targetPos, Vector3 targetPrePos, float speed) // 現在の標的の位置、速度、弾の速さ
		{
			float flame2s = Time.fixedDeltaTime; // 0.01f s/frame

			//Unityの物理はm/sなのでm/flameにする
			speed = speed * flame2s; // m/frame
			Vector3 v3_Mv = targetPos - targetPrePos; // m/frame
			//Vector3 v3_Mv = (targetPos - targetPrePos) / flame2s;
			Vector3 v3_Pos = targetPos - ProjectileSpawn.position;

			float A = Vector3.SqrMagnitude(v3_Mv) - speed * speed; // m2/frame2 (m2/s2)
			float B = Vector3.Dot(v3_Pos, v3_Mv); // m2/frame (m2/s)
			float C = Vector3.SqrMagnitude(v3_Pos); // m2

            float PredictionFlame; // frame

            //0割禁止
            if (A == 0f && B == 0f) PredictionFlame = 0f;
			else if (A == 0f) PredictionFlame = (-C / B / 2);

			else
			{
				//虚数解はどうせ当たらないので絶対値で無視した
				float D = Mathf.Sqrt(Mathf.Abs(B * B - A * C));

				// 時間
				PredictionFlame = Utility.PlusMin((-B - D) / A, (-B + D) / A);
				//Mod.Log($"{(-B - D) / A * flame2s}, {(-B + D) / A * flame2s}");
			}
			//Mod.Log((PredictionFlame * flame2s).ToString());
			var result = targetPos + v3_Mv * PredictionFlame;

			// 重力の処理
			// 重力で落ちる分だけ目標位置を上げる
			if (gravity)
			{
				result.y += g * (PredictionFlame * flame2s) * (PredictionFlame * flame2s) / 2f; // 応急的に重みを下げる
			}

			// ブロックの速度の分だけ補正
			result -= Velocity * PredictionFlame * flame2s;

			return result;
		}
		/// <summary>
		/// 円形予測
		/// </summary>
		/// <param name="pos"></param>
		/// <param name="velo"></param>
		/// <param name="angularVelo"></param>
		/// <param name="speed"></param>
		/// <returns></returns>
		public virtual Vector3 CircularPredict(Vector3 targetPos, Vector3 targetPos1, Vector3 targetPos2, float speed) // 現在の標的の位置，速度，角速度，弾の速さ
        {
			float flame2s = Time.fixedDeltaTime;

			//Unityの物理はm/sなのでm/flameにする
			speed = speed * flame2s;

			//3点から円の中心点を出す
			Vector3 CenterPosition = Utility.Circumcenter(targetPos, targetPos1, targetPos2);

			//中心点から見た1フレームの角速度と軸を出す
			Vector3 axis = Vector3.Cross(targetPos1 - CenterPosition, targetPos - CenterPosition);
			float angle = Vector3.Angle(targetPos1 - CenterPosition, targetPos - CenterPosition);

			//現在位置で弾の到達時間を出す
			float PredictionFlame = Vector3.Distance(targetPos, ProjectileSpawn.position) / speed;

			//到達時間分を移動した予測位置で再計算して到達時間を補正する。
			for (int i = 0; i < 3; ++i)
			{
				PredictionFlame = Vector3.Distance(Utility.RotateToPosition(targetPos, CenterPosition, axis, angle * PredictionFlame), ProjectileSpawn.position) / speed;
			}
			var result = Utility.RotateToPosition(targetPos, CenterPosition, axis, angle * PredictionFlame);

			// 重力の処理
			// 重力で落ちる分だけ目標位置を上げる
			if (gravity)
			{
				result.y += g * (PredictionFlame * flame2s) * (PredictionFlame * flame2s) / 2f; // 応急的に重みを下げる
			}

			// ブロックの速度の分だけ補正
			result -= Velocity * PredictionFlame * flame2s;

			return result;
		}
		/// <summary>
		/// 目標を向くように回転する
		/// </summary>
		/// <param name="to">目標位置</param>
		/// <param name="defaultRot">無効な場合の向き</param>
		/// <param name="limitAngle">回転可能な上限角度</param>
		/// <returns></returns>
		public virtual Quaternion Rotate(Vector3 to, Vector3 defaultRot, float limitAngle = 30f) // 正面の向きに注意！
		{
			return Vector3.Angle(defaultRot, to - ProjectileSpawn.position) < limitAngle ? Quaternion.LookRotation(to - ProjectileSpawn.position) : Quaternion.LookRotation(defaultRot);
		}
		/// <summary>
		/// 目標を向くように回転する
		/// </summary>
		/// <returns></returns>
		public virtual Quaternion Rotate(Vector3 defaultRot)
        {
			return Quaternion.LookRotation(defaultRot);
        }
		/// <summary>
		/// 目標を定める
		/// </summary>
		public virtual void SetTarget(Vector3 pos)
        {
			TargetPos = (pos == null) ? Vector3.zero : pos;
        }
		/// <summary>
		/// 目標を定める
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
		/// 目標を決める
		/// 存在しなければvector3.zeroをセットする
		/// </summary>
		public virtual void SetTarget()
        {
			if (startingBlock == null)
            {
				// スタブロを取得する
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
	/// Cannon系
	/// 逆向きに弾が出るトラブルあり
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

				// 弾道予測
				var predTargetPos = Predict(TargetPos, TargetPosBefore1, TargetPosBefore2, InitialSpeed * Power);
				if (Cannon.shrapnel)
				{
					Cannon.boltSpawnRot = Rotate(predTargetPos, -transform.up); // 拡散砲の発射方向を変更 // 逆
				}
				else
				{
					// Cannonである場合の挙動
					Cannon.boltSpawnRot = Rotate(predTargetPos, -transform.up); // 拡散砲の発射方向を変更 // 逆
				}
			}
            else
            {
				Cannon.boltObject.rotation = Rotate(-transform.up);
            }

			// 目標位置更新
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
		public LineRenderer line; // デバッグ用

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
		/// 火のエフェクト
		/// </summary>
		public Transform Fire;
		//public LineRenderer line; // デバッグ用
		public override void SafeAwake()
        {
			//Mod.Log("Flamethrower Script");
			Fire = transform.FindChild("Fire");

			base.SafeAwake();

			// 常に重力の影響を受けない
			gravity = false;

			InitialSpeed = 500f;
			Power = 1f;
		}
        public override void SimulateFixedUpdateAlways()
        {
			if (startingBlock.CurrentTarget != null)
			{
				SetTarget(startingBlock.CurrentTarget);

				// 弾道予測
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

	// modで追加される武装modに対して
	// シューティングモジュールではブロック名.ShootingDirectionVisual(Clone)という名前の子オブジェクトで発射方向を制御している模様
	// ACMの場合はブロック名.AdShootingVisual(Clone) という名前の子オブジェクトで発射方向を制御している模様
	/// <summary>
	/// 公式モジュール製のmodブロック
	/// </summary>
	public class ModAddedBlocksScript : LockOnBlockScript
    {
		/// <summary>
		/// 公式のシューティングモジュール
		/// </summary>
		public ShootingModuleBehaviour shootingModule;
		/// <summary>
		/// 弾の方向を指定するゲームオブジェクトたち
		/// </summary>
		public List<Transform> ProjectileVis;
		public readonly string originalShootingModuleName = "ShootingDirectionVisual(Clone)";
		public readonly string acmShootingModuleName = "AdShootingVisual(Clone)";
		/// <summary>
		/// 弾の方向の初期値
		/// </summary>
		public List<GameObject> defaultForward;
		//public LineRenderer line; // デバッグ用

		// 初速の計算
		//public MSlider PowerSlider;
		//public float power;

		/// <summary>
		/// ACM製かどうか
		/// </summary>
		public bool fromACM = false;

		public override void SafeAwake()
        {
			// 弾の発射方向を取得
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
				// 別のコンポーネントを貼り付けてこちらを非アクティブにする
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

			shootingModule = GetComponent<ShootingModuleBehaviour>(); // 取得できた // ACMは別のクラスを使っている模様
			if (shootingModule == null)
			{
				//Mod.Warning("could not get shootingModule");
				enabled = false;
				return;
			}

			// 初速計算
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
			// 弾道予測
			if (startingBlock.CurrentTarget != null) // 標的が存在する場合
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
			else // 標的が存在しない場合
			{
				for (int i = 0; i < ProjectileVis.Count; i++)
				{
					ProjectileSpawn = ProjectileVis[i];
					ProjectileVis[i].rotation = Rotate(defaultForward[i].transform.forward);
				}
			}

			// デバッグ用
			//line.SetPositions(new Vector3[] { ProjectileVis[0].position, ProjectileVis[0].forward * 100f });

			// 目標位置更新
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
	/// ACM製ブロック
	/// </summary>
	public class AdShootingBlocksScript : LockOnBlockScript
    {
		/// <summary>
		/// 弾の方向を指定するゲームオブジェクトたち
		/// </summary>
		public List<Transform> ProjectileVis;
		public readonly string acmShootingModuleName = "AdShootingVisual(Clone)";
		/// <summary>
		/// 弾の方向の初期値
		/// </summary>
		public List<GameObject> defaultForward;
		/// <summary>
		/// xmlデータ
		/// </summary>
		XmlBlock xmlBlock;

		/// <summary>
		/// 初速の計算
		/// </summary>
		public XDataHolder adBlockData;

		public override void SafeAwake()
		{
			// 弾の発射方向を表すゲームオブジェクトを取得
			ProjectileVis = new List<Transform>();
			foreach (Transform child in transform)
			{
				if (child.name == acmShootingModuleName)
				{
					ProjectileVis.Add(child);
				}
			}

			// デフォルトの発射方向を保つゲームオブジェクトを生成
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

			// blockのXMLから情報を取得
			adBlockData = BlockInfo.FromBlockBehaviour(BB).BlockData;
			Power = adBlockData.HasKey("bmt-power") ? adBlockData.ReadFloat("bmt-power") : 1f;
		}
		public override void SimulateFixedUpdateAlways()
		{
			if (startingBlock.CurrentTarget != null)
			{
				SetTarget(startingBlock.CurrentTarget);
				gravity = !StatMaster.GodTools.GravityDisabled && xmlBlock.Gravity;

				// 弾道予測
				//var predTargetPos = Predict(Target, TargetVelo, 1f * power); // 暫定的に初速を仮定
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

			// 目標位置更新
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
		public List<Transform> ProjectileVis; // 弾の方向を指定するゲームオブジェクトたち
		public readonly string acmShootingModuleName = "AdShootingVisual(Clone)";
		public List<GameObject> defaultForward; // 弾の方向の初期値
		public Quaternion Correction; // 補正角度
		public Vector3 Target; // 目標位置
		public Vector3 TargetVelo; // 目標の速度
		public Vector3 TargetAngularVelo; // 目標の角速度
		public bool gravity; // ゴッドツール使用時かどうか
		public readonly float g = 32.81f; // 重力加速度

		// ミサイルとチャフなら無効化
		public bool isMissile = false;
		public bool isChaffLauncher = false;

		// 初速の計算
		public XDataHolder adBlockData;
		public float power;

		// スターティングブロック
		public StartingBlockScript startingBlock;

		public override void SafeAwake()
        {
			//Mod.Log("Ad-Shooting Blocks Script");

			// XMLから情報を取得
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

			// 弾の発射方向を表すゲームオブジェクトを取得
			ProjectileVis = new List<Transform>();
			foreach (Transform child in transform)
			{
				if (child.name == acmShootingModuleName)
				{
					ProjectileVis.Add(child);
				}
			}

			// デフォルトの発射方向を保つゲームオブジェクトを生成
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

			// ミサイル，チャフ，非砲系ならオフにする
			if (isMissile || isChaffLauncher || ProjectileVis.Count == 0)
            {
				enabled = false;
            }

			// スタブロを取得する
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

				// 弾道予測
				//var predTargetPos = Predict(Target, TargetVelo, 1f * power); // 暫定的に初速を仮定
				Vector3 predTargetPos;
				if (TargetAngularVelo.sqrMagnitude > Mathf.Pow(0.03f, 2)) // 円形予測
				{
					predTargetPos = CircularPredict(Target, TargetVelo, TargetAngularVelo, power);
				}
				else // 線形予測
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

		// 弾道予測
		public Vector3 LinearPredict(Vector3 pos, Vector3 velo, float speed = 500f) // 現在の標的の位置、速度、矢の早さ
		{
			// 射撃する位置から見た現在の標的の位置
			Vector3 deltaPos = pos - transform.position;

			// 二次方程式を解き、目標の位置を予測する
			float t = Utility.SolveEquation(velo.sqrMagnitude - speed * speed, Vector3.Dot(velo, deltaPos), deltaPos.sqrMagnitude);
			Vector3 targetpos = pos + velo * t;

			// 重力の処理
			if (gravity)
			{
				targetpos.y += g * t * t / 2;
			}

			return targetpos;
		}
		public virtual Vector3 CircularPredict(Vector3 pos, Vector3 velo, Vector3 angularVelo, float speed = 1f) // 現在の標的の位置，速度，角速度，弾の速さ
		{
			// 3点から円の中心点を出す
			Vector3 radius = Vector3.Cross(velo, angularVelo) / Vector3.SqrMagnitude(angularVelo);
			Vector3 centerPos = pos - radius;

			// 中心点から見た1フレームの角速度と軸を出す
			float angle = angularVelo.magnitude;
			Vector3 axis = angularVelo / angle;

			// 現在位置で弾の到達時間を出す
			float predictionFlame = Vector3.Distance(pos, transform.position) / speed;

			// 到達時間分を移動した予測位置で再計算して到達時間を補正する
			for (int i = 0; i < 3; ++i)
			{
				predictionFlame = Vector3.Distance(Utility.RotateToPosition(pos, centerPos, axis, angle * predictionFlame), transform.position) / speed;
			}

			var targetpos = Utility.RotateToPosition(pos, centerPos, axis, angle * predictionFlame);

			// 重力の処理
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

		// 目標を定める
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

		// 標的（スタブロ）
		public List<StartingBlockScript> TargetList;
		public List<PlayerData> Players, PlayersPast; // 鯖内のプレイヤー一覧

		public void Awake()
        {
			TargetList = new List<StartingBlockScript>();
        }
		public void FixedUpdate()
        {
			Players = Playerlist.Players;

			// プレイヤーリスト更新 プレイヤーの数が変化した時に
			// とりあえず毎回取得し直してみる
			SetTargetList();
			PlayersPast = new List<PlayerData>(Players);
		}

		/// <summary>
		/// 鯖内のターゲットを取得し，TargetListに格納する
		/// </summary>
		public void SetTargetList()
        {
			TargetList = new List<StartingBlockScript>();
			foreach (PlayerData player in Players)
            {
				// 見学いるとだめ？
				if (player.machine == null || player.PlayMode == BesiegePlayMode.Spectator)
                {
					continue;
                }

				if (!player.machine.isSimulating || player.machine.LocalSim) // シミュレーション中のブロックに限る
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