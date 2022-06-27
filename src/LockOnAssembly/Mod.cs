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
		public static bool ACMLoaded; // ACMがロードされているかどうか

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
	public abstract class AbstractBlockScript : MonoBehaviour //ブロック基本
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
	// スタブロ
	public class StartingBlockScript : AbstractBlockScript
    {
		public GameObject CurrentTarget; // 現在の目標
		public MPTeam team; // 現在のチーム
		public int playerId; // サーバーID
		public Rigidbody rigid; // スタブロのrigidbody

		// 敵候補
		public List<Enemy> TargetCandidates;

		// UI
		public Texture markerTexture; // 敵にかかるマーク
		public Texture debugTextureRed; // デバッグ用
		public Texture debugTextureGreen;

		// 敵の画面内に入っているかの判定
		public Camera mainCamera;
		public Vector2 screenSize;

        public override void SafeAwake()
        {
			rigid = BB.noRigidbody ? null : BB.Rigidbody;
			//GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>();

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
			//team = StatMaster.isMP ? BB.Team : MPTeam.None;
			//Mod.Log("BB.team = " + team.ToString());

			PlayerData player;
			Playerlist.GetPlayer((ushort)playerId, out player);
			team = StatMaster.isMP ? player.team : MPTeam.None;
			Mod.Log("BB.team = " + team.ToString());

			// デバッグ用
			//ChangeTarget(LockOnManager.Instance.DebugTarget);
			CurrentTarget = null;
			ChangeTarget(null);

			// メインカメラ設定
			mainCamera = Camera.main;
			screenSize = new Vector2(Screen.width, Screen.height);

			// 敵をリストに格納する
			TargetCandidates = new List<Enemy>();
			//SetTargetCandidates();

			// テクスチャのロード
			markerTexture = ModTexture.GetTexture("marker-green").Texture;

			// デバッグ用テクスチャの設定
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

			// 敵がいない場合はデバッグ用ボールをターゲットにする
			if (TargetCandidates == null)
            {
				ChangeTarget(null);
            }
			else if (TargetCandidates.Count == 0)
            {
				ChangeTarget(null);
            }

			// カメラ中央にいる時間に応じてリスト内の敵にゲージを溜める
			// カメラの外に出た敵のゲージは0になる
			foreach (Enemy e in TargetCandidates)
            {
				var screenPos = RectTransformUtility.WorldToScreenPoint(mainCamera, e.Target.transform.position);
				// とりあえず画面内に入ったことを想定
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

			// ゲージが溜まった敵の中で最も近い敵をロックオン
			float minSqrDistance = float.PositiveInfinity;
			//Enemy mostNearestEnemy = new Enemy(LockOnManager.Instance.DebugTarget); // 暫定
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
		public void OnGUI()
		{
			// 自分のところでだけ表示する
			if (playerId != Machine.Active().PlayerID)
			{
				return;
			}

			// シミュ中のみ
			if (!BB.isSimulating)
            {
				return;
            }

			#region // デバッグ用GUI
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

			// ターゲットマーカー
			//var targetPos = ScreenPoint(CurrentTarget);
			//GUI.DrawTexture(new Rect(targetPos, new Vector2(100, 100)), debugTextureRed);
			// 矩形を描画
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

		// ターゲットになりうる敵を全てリストに格納する
		// 都度新しくしているとロックオン時間を計測できなくなる ターゲットのゲージを保持したまま新たなリストを作る必要がある
		// ターゲット候補の中から前提条件を満たさなくなったものを除く（ターゲットリストにいないものを弾く）→新たに前提条件を満たすもので、まだ候補に入っていないものを加える
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
		// ターゲットを変更する
		public void ChangeTarget(GameObject NextTarget)
        {
			CurrentTarget = (NextTarget == null) ? null : NextTarget;
        }
		
		// 目標の画面上における位置 // point.zを返り値にしてRectをoutにした方が良さそう
		public Rect ScreenPoint(GameObject target, Vector2 scale)
        {
			Vector3 point = mainCamera.WorldToScreenPoint(target.transform.position);
			Vector2 pos = new Vector2(point.x - scale.x/2, screenSize.y - point.y - scale.y/2);
			//point.y = screenSize.y - point.y;
			return point.z > 0 ? new Rect(pos, scale) : new Rect(0, 0, 0, 0);
        }

		// 敵クラス
		public class Enemy
        {
			public GameObject Target; // ターゲットのゲームオブジェクト
			public bool LockOn // 自分がロックオンしているかどうか
            {
                get
                {
					return gauge == 1f;
                }
            }
			private float gauge; // ゲージ 0f~1f の値をとる
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
	// 基本
	public abstract class LockOnBlockScript : AbstractBlockScript
    {
		public Quaternion Correction; // 補正角度
		public Transform ProjectileSpawn; // 矢が出てくる位置姿勢
		public Vector3 Target; // 目標位置
		public Vector3 TargetVelo; // 目標の速度
		public Vector3 TargetAngularVelo; // 目標の角速度
		public bool gravity; // ゴッドツール使用時かどうか
		public readonly float g = 32.81f; // 重力加速度
		public float initialSpeed; // 弾の初期速度

		// スタブロ
		public StartingBlockScript startingBlock;

		public override void SafeAwake()
		{
			// 弾の初期姿勢を取得
			SetProjectileSpawn();

			gravity = !StatMaster.GodTools.GravityDisabled;

			// スタブロを取得する
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

			// 弾の初期速度設定
			SetInitialSpeed();
		}
		public override void SimulateFixedUpdateAlways()
		{
			// 弾道予測
			if (startingBlock.CurrentTarget != null) // 標的が存在する場合
			{
				SetTarget();
				//SetTarget(LockOnManager.Instance.DebugTargetPos, LockOnManager.Instance.DebugTargetRigid.velocity);
				gravity = !StatMaster.GodTools.GravityDisabled;

				Vector3 predTargetPos;
				if (TargetAngularVelo.sqrMagnitude > Mathf.Pow(0.03f, 2)) // 円形予測
				{
					predTargetPos = CircularPredict(Target, TargetVelo, TargetAngularVelo, initialSpeed);
				}
				else // 線形予測
				{
					predTargetPos = LinearPredict(Target, TargetVelo, initialSpeed);
				}
				ProjectileSpawn.rotation = Rotate(predTargetPos);
			}
            else // 標的が存在しない場合
            {
				ProjectileSpawn.rotation = Rotate();
            }
		}
		// 弾の姿勢を取得
		public virtual void SetProjectileSpawn()
		{
			ProjectileSpawn = transform.FindChild("projective spawn");
		}
		// 弾の初期速度設定
		public virtual void SetInitialSpeed()
        {
			initialSpeed = 1f;
        }
		// 弾道予測
		public virtual Vector3 LinearPredict(Vector3 pos, Vector3 velo, float speed = 1f) // 現在の標的の位置、速度、弾の速さ
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
		public virtual Vector3 LinearPredict(Rigidbody rigid, float speed = 1f)
        {
			return LinearPredict(rigid.position, rigid.velocity, speed);
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
			for (int i = 0; i<3; ++i)
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
		public virtual Vector3 CircularPredict(Rigidbody rigid, float speed = 1f)
        {
			return CircularPredict(rigid.position, rigid.velocity, rigid.angularVelocity, speed);
        }
		public virtual Quaternion Rotate(Vector3 to, float limitAngle = 30f) // 正面の向きに注意！
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
				//var rigid = nextTarget.GetComponent<Rigidbody>() ?? nextTarget.AddComponent<Rigidbody>(); // クライアントのスタブロはrigid==nullになる
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
				// スタブロを取得する
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
	// Cannon系 // 逆向きに弾が出るトラブルあり
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

				// 弾道予測
				var predTargetPos = LinearPredict(Target, TargetVelo, initialSpeed);
				if (Cannon.shrapnel)
				{
					Cannon.boltSpawnRot = Rotate(predTargetPos); // 拡散砲の発射方向を変更
				}
				else
				{
					// Cannonである場合の挙動
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
		public LineRenderer line; // デバッグ用

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
				// 弾の初期速度設定
				SetInitialSpeed();
			}

			// 弾の初期姿勢を取得
			SetProjectileSpawn();

			gravity = !StatMaster.GodTools.GravityDisabled;

			// スタブロを取得する
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
		public Transform Fire; // 火のエフェクト
		//public LineRenderer line; // デバッグ用
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

				// 弾道予測
				Vector3 predTargetPos;
				if (TargetAngularVelo.sqrMagnitude > Mathf.Pow(0.03f, 2)) // 円形予測
				{
					predTargetPos = CircularPredict(Target, TargetVelo, TargetAngularVelo, initialSpeed);
				}
				else // 線形予測
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
        // 弾道予測
        public override Vector3 LinearPredict(Vector3 pos, Vector3 velo, float speed = 100f) // 現在の標的の位置、速度、矢の早さ
		{
			// 射撃する位置から見た現在の標的の位置
			Vector3 deltaPos = pos - transform.position;

			// 二次方程式を解き、目標の位置を予測する
			float t = Utility.SolveEquation(velo.sqrMagnitude - speed * speed, Vector3.Dot(velo, deltaPos), deltaPos.sqrMagnitude);
			Vector3 targetpos = pos + velo * t;

			return targetpos;
		}
		public override Vector3 CircularPredict(Vector3 pos, Vector3 velo, Vector3 angularVelo, float speed = 1f) // 現在の標的の位置，速度，角速度，弾の速さ
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

	// modで追加される武装modに対して
	// シューティングモジュールではブロック名.ShootingDirectionVisual(Clone)という名前の子オブジェクトで発射方向を制御している模様
	// ACMの場合はブロック名.AdShootingVisual(Clone) という名前の子オブジェクトで発射方向を制御している模様
	public class ModAddedBlocksScript : LockOnBlockScript
    {
		public ShootingModuleBehaviour shootingModule; // 公式のシューティングモジュール
		public List<Transform> ProjectileVis; // 弾の方向を指定するゲームオブジェクトたち
		public readonly string originalShootingModuleName = "ShootingDirectionVisual(Clone)";
		public readonly string acmShootingModuleName = "AdShootingVisual(Clone)";
		public List<GameObject> defaultForward; // 弾の方向の初期値
		//public LineRenderer line; // デバッグ用

		// 初速の計算
		//public MSlider PowerSlider;
		//public float power;

		// ACM製かどうか
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
		}
        public override void SimulateFixedUpdateAlways()
		{
			// 弾道予測
			if (startingBlock.CurrentTarget != null) // 標的が存在する場合
			{
				SetTarget();
				gravity = !StatMaster.GodTools.GravityDisabled;

				Vector3 predTargetPos;
				if (TargetAngularVelo.sqrMagnitude > Mathf.Pow(0.03f, 2)) // 円形予測
				{
					predTargetPos = CircularPredict(Target, TargetVelo, TargetAngularVelo, initialSpeed);
				}
				else // 線形予測
				{
					predTargetPos = LinearPredict(Target, TargetVelo, initialSpeed);
				}
				for (int i = 0; i < ProjectileVis.Count; i++)
				{
					ProjectileVis[i].rotation = Rotate(predTargetPos, defaultForward[i].transform.forward);
				}
			}
			else // 標的が存在しない場合
			{
				for (int i = 0; i < ProjectileVis.Count; i++)
				{
					ProjectileVis[i].rotation = Rotate(defaultForward[i].transform.forward);
				}
			}

			// デバッグ用
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
        // 弾道予測
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
		public List<Transform> ProjectileVis; // 弾の方向を指定するゲームオブジェクトたち
		public readonly string acmShootingModuleName = "AdShootingVisual(Clone)";
		public List<GameObject> defaultForward; // 弾の方向の初期値

		// ミサイルとチャフなら無効化
		public bool isMissile = false;
		public bool isChaffLauncher = false;

		// 初速の計算
		public XDataHolder adBlockData;
		public float power;

		public override void SafeAwake()
		{
			// XMLから情報を取得 ミサイルかチャフなら何もしない
			adBlockData = BlockInfo.FromBlockBehaviour(BB).BlockData;
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

			// ミサイル，チャフ，非砲系ならオフにする
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
		// 弾道予測
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
		//public Vector3 ProjectileDirection; // 弾の発射方向を一律で変更（デバッグ用）

		// 標的（スタブロ）
		public List<StartingBlockScript> TargetList;
		public List<PlayerData> Players, PlayersPast; // 鯖内のプレイヤー一覧

		// 標的（デバッグ用）
		//public GameObject DebugTarget;
		//public Vector3 DebugTargetPos;
		//public Rigidbody DebugTargetRigid;

		// クロスボウ検証用
		public float crossbowPower = 10f;

		// メッセージ（デバッグ用）
		public string message0="", message1 = "", message2 = "", message3 = "";

		public void Awake()
        {
			TargetList = new List<StartingBlockScript>();

			// デバッグ用
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

			// プレイヤーリスト更新 プレイヤーの数が変化した時に
			// とりあえず毎回取得し直してみる
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
				// デバッグ用 弾の発射方向を一律で変更する
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

				//GUILayout.Label("倍率 = " + crossbowPower);
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

		// 鯖内のターゲットを取得する
		public void SetTargetList()
        {
			TargetList = new List<StartingBlockScript>();
			foreach (PlayerData player in Players)
            {
				if (!player.machine.isSimulating || player.machine.LocalSim) continue; // シミュレーション中のブロックに限る
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
