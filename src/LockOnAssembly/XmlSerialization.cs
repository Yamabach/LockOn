using UnityEngine;
using Modding;
using Modding.Modules;
using Modding.Serialization;
using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace LOSpace
{
	/// <summary>
	/// ACM製ブロックであるかどうかを判断するためのクラス
	/// </summary>
	[Reloadable, XmlRoot("AdShootingProp")]
	public class AdShootingProp : BlockModule
	{
		[Serializable]
		public class ShootingState : Element
		{
			[DefaultValue(true), XmlElement("Projectile")]
			public bool Projectileflag = true;
			[RequireToValidate, XmlElement("Mesh")]
			public MeshReference Mesh;
			[RequireToValidate, XmlElement("Texture")]
			public ResourceReference Texture;
			[RequireToValidate, XmlArray("Colliders"), XmlArrayItem("BoxCollider", typeof(BoxModCollider)), XmlArrayItem("SphereCollider", typeof(SphereModCollider)), XmlArrayItem("CapsuleCollider", typeof(CapsuleModCollider))]
			public ModCollider[] Colliders;
			[XmlElement("Mass")]
			public float Mass;
			[DefaultValue(false), XmlElement("IgnoreGravity")]
			public bool IgnoreGravity;
			[DefaultValue(0f), XmlElement("Drag")]
			public float Drag;
			[DefaultValue(5f), XmlElement("AngularDrag")]
			public float AngularDrag = 5f;
			[DefaultValue(0f), XmlElement("Buoyancy")]
			public float Buoyancy = 0f;
			[DefaultValue(0.6f), XmlElement("FrictionStr")]
			public float FrictionStr = 0.6f;
			[DefaultValue(0f), XmlElement("BounceStr")]
			public float BounceStr = 0f;
			[DefaultValue(CombineType.Average), XmlElement("FrictionCombineType")]
			public CombineType FriCombType;
			[DefaultValue(CombineType.Average), XmlElement("BounceCombineType")]
			public CombineType BounceComType;
			[DefaultValue(100f), XmlElement("EntityDamage")]
			public float EntityDamage = 100f;
			[DefaultValue(1f), XmlElement("BlockDamage")]
			public float BlockDamage = 1f;
			[DefaultValue(CollisionType.Discrete), XmlElement("CollisionTypeS")]
			public CollisionType CollisionTypeS;
			[XmlElement("Attaches")]
			public bool Attaches;
		}
		public int ProjectileId;
		[Reloadable, XmlElement("ProjectileStart")]
		public TransformValues ProjectileStart;
		[DefaultValue(false), XmlElement("ShowPlaceholderProjectile")]
		public bool ShowPlaceholderProjectile;
		[DefaultValue(false), XmlElement("PlaceholderProjectileUseCollider")]
		public bool PlaceholderProjectileUseCollider;
		[Reloadable, XmlElement("DefaultAmmo")]
		public int DefaultAmmo;
		[Reloadable, DefaultValue(AmmoType.All), XmlElement("AmmoType")]
		public AmmoType AmmoType;
		[Reloadable, DefaultValue(true), XmlElement("SupportsExplosionGodTool")]
		public bool SupportsExplosionGodTool = true;
		[Reloadable, DefaultValue(false), XmlElement("ProjectilesExplode")]
		public bool ProjectilesExplode;
		[Reloadable, DefaultValue(3f), XmlElement("ExplodeRadius")]
		public float ExplodeRadius;
		[Reloadable, DefaultValue(10f), XmlElement("ExplodePower")]
		public float ExplodePower;
		[Reloadable, DefaultValue(0f), XmlElement("ExplodeUpPower")]
		public float ExplodeUpPower;
		[CanBeEmpty, RequireToValidate, XmlElement("AssetBundleName")]
		public ResourceReference AssetBundleName;
		[Reloadable, DefaultValue(false), XmlElement("useDefaultAsset")]
		public bool useDefaultAsset;
		[CanBeEmpty, DefaultValue(null), XmlElement("ExplodeEffect")]
		public string ExplodeEffect;
		[CanBeEmpty, DefaultValue(null), XmlElement("ShotFlashPosition")]
		public TransformValues ShotFlashPosition;
		[DefaultValue(null), XmlElement("PurgeVector")]
		public ModVector3 PurgeVector;
		[DefaultValue(0f), XmlElement("PurgePower")]
		public float PurgePower;
		[DefaultValue(0f), XmlElement("DelayTime")]
		public float DelayTime;
		[CanBeEmpty, DefaultValue(null), XmlElement("ShotFlashEffect")]
		public string ShotFlashEffect;
		[CanBeEmpty, DefaultValue(null), XmlElement("TrailEffect")]
		public string TrailEffect;
		[CanBeEmpty, DefaultValue(null), XmlElement("BulletEffect")]
		public string BulletEffect;
		[CanBeEmpty, DefaultValue(null), XmlElement("ChaffEffect")]
		public string ChaffEffect;
		[Reloadable, DefaultValue(false), XmlElement("useBooster")]
		public bool useBooster;
		[Reloadable, DefaultValue(false), XmlElement("useJamReducer")]
		public bool useJamReducer;
		[Reloadable, DefaultValue(false), XmlElement("useTimefuse")]
		public bool useTimefuse;
		[Reloadable, DefaultValue(false), XmlElement("useDelayTimer")]
		public bool useDelayTimer;
		[Reloadable, DefaultValue(false), XmlElement("useThrustDelayTimer")]
		public bool useThrustDelayTimer;
		[Reloadable, DefaultValue(false), XmlElement("useDelay")]
		public bool useDelay;
		[Reloadable, DefaultValue(false), XmlElement("useBeacon")]
		public bool useBeacon;
		[Reloadable, DefaultValue(false), XmlElement("useBurstShot")]
		public bool useBurstShot;
		[Reloadable, DefaultValue(false), XmlElement("useFreezingAttack")]
		public bool useFreezingAttack;
		[CanBeEmpty, DefaultValue(null), XmlElement("GuidType")]
		public string Guidtype;
		[Reloadable, DefaultValue(false), XmlElement("isChaff")]
		public bool isChaff;
		[Reloadable, DefaultValue(0.5f), XmlElement("GuidRatio")]
		public float GuidRatio;
		[Reloadable, DefaultValue(false), XmlElement("useExplodeRotation")]
		public bool useExplodeRotation;
		[Reloadable, DefaultValue(false), XmlElement("ProjectilesDespawnImmediately")]
		public bool ProjectilesDespawnImmediately;
		[RequireToValidate, XmlElement("FireKey")]
		public MKeyReference FireKey;
		[RequireToValidate, XmlElement("PowerSlider")]
		public MSliderReference PowerSlider;
		[RequireToValidate, XmlElement("RateOfFireSlider")]
		public MSliderReference RateOfFireSlider;
		[RequireToValidate, DefaultValue(null), XmlElement("TimefuseSlider")]
		public MSliderReference TimefuseSlider;
		[RequireToValidate, DefaultValue(null), XmlElement("DelayTimerSlider")]
		public MSliderReference DelayTimerSlider;
		[RequireToValidate, XmlElement("HoldToShootToggle")]
		public MToggleReference HoldToShootToggle;
		[RequireToValidate, DefaultValue(null), XmlElement("ThrustDelayTimerSlider")]
		public MSliderReference ThrustDelayTimerSlider;
		[Reloadable, DefaultValue(0.1f), XmlElement("RecoilMultiplier")]
		public float RecoilMultiplier = 0.1f;
		[Reloadable, DefaultValue(0.05f), XmlElement("RandomInterval")]
		public float RandomInterval = 0.05f;
		[Reloadable, DefaultValue(0.01f), XmlElement("RandomDiffusion")]
		public float RandomDiffusion = 0.01f;
		[Reloadable, DefaultValue(0.05f), XmlElement("RandomFuseInterval")]
		public float RandomFuseInterval = 0.05f;
		[Reloadable, DefaultValue(0f), XmlElement("FuseDelayTime")]
		public float FuseDelayTime = 0f;
		[Reloadable, DefaultValue(2f), XmlElement("RateOfBurst")]
		public float RateOfBurst = 2f;
		[DefaultValue(3), XmlElement("BurstShotNum")]
		public int BurstShotNum = 3;
		[DefaultValue(10), XmlElement("PoolSize")]
		public int PoolSize = 10;
		[CanBeEmpty, RequireToValidate, DefaultValue(null), XmlArray("Sounds"), XmlArrayItem("AudioClip", typeof(ResourceReference))]
		public object[] Sounds;
		[CanBeEmpty, RequireToValidate, DefaultValue(null), XmlArray("HitSounds"), XmlArrayItem("AudioClip", typeof(ResourceReference))]
		public object[] HitSounds;
		[CanBeEmpty, RequireToValidate, DefaultValue(null), XmlArray("ProjectileSounds"), XmlArrayItem("AudioClip", typeof(ResourceReference))]
		public object[] ProjectileSounds;
		[Reloadable, RequireToValidate, XmlElement("ShootingState")]
		public AdShootingProp.ShootingState Shootingstateinfo;
	}
	/// <summary>
	/// 摩擦タイプ
	/// </summary>
	public enum CombineType
    {
		Average,
		Minimum,
		Maximum,
		Multiply
    }
	/// <summary>
	/// 衝突タイプ
	/// </summary>
	public enum CollisionType
    {
		Discrete,
		Continuous,
		ContinuousDynamic
    }
	/// <summary>
	/// ACM上でのVector3
	/// </summary>
	[Serializable]
	public struct ModVector3
    {
		[XmlAttribute]
		public float x;
		[XmlAttribute]
		public float y;
		[XmlAttribute]
		public float z;
        public override string ToString()
        {
			return string.Format("({0}, {1}, {2})", x, y, z);
        }
		public static implicit operator UnityEngine.Vector3(ModVector3 sV)
        {
			return new UnityEngine.Vector3
			{
				x = sV.x,
				y = sV.y,
				z = sV.z
			};
        }
    }

	public class AdShootingBehaviour : BlockModuleBehaviour<AdShootingProp>
    {
		public float projectileMass;

        public override void SafeAwake()
        {
			Mod.Log("AdShootingBehaviour safeawake");
			base.SafeAwake();

			// 値を取得
			projectileMass = (Module.Shootingstateinfo.Mass);
			Mod.Log(projectileMass.ToString());
        }
    }
}