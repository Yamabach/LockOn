using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Xml.Serialization;
using Modding;
using Modding.Blocks;
using Modding.Modules;
using Modding.Modules.Official;
using Modding.Serialization;
using Besiege;
using UnityEngine;
using UnityEngine.UI;

namespace LOSpace
{
    /// <summary>
    /// XMLを読み込む
    /// </summary>
    public class XMLDeserializer
    {
        public static readonly string FileName = "AcmProjectileMass.xml";
        private static readonly string ResourcesPath = "Resources/Xml/";
        /// <summary>
        /// ACM武装のデータをデシリアライズする
        /// </summary>
        /// <returns></returns>
        public static XmlData Deserialize()
        {
            Mod.Log("Loaded " + FileName + " from resources folder");
            return Modding.ModIO.DeserializeXml<XmlData>(ResourcesPath + FileName);
        }
    }
    /// <summary>
    /// ACMブロックのデータが格納されたリストを持つクラス
    /// </summary>
    [XmlRoot("data")]
    public class XmlData : Element
    {
        /// <summary>
        /// ACMのブロックのデータ
        /// </summary>
        [XmlArray("blocks")]
        [XmlArrayItem("block")]
        public List<XmlBlock> AcmBlocks { get; set; }

        /// <summary>
        /// デバッグ用
        /// AcmBlocksの中身を全てプリントする
        /// </summary>
        public void LogList()
        {
            foreach (XmlBlock b in AcmBlocks)
            {
                Mod.Log($"name={b.Name}, id={b}, mass={b.Mass}");
            }
        }
        /// <summary>
        /// AcmBlocksの中から指定したidを持つブロックを取り出す
        /// </summary>
        /// <param name="id">ブロックのid（"global"-"local"）</param>
        /// <param name="result">指定したブロック</param>
        /// <returns>指定したidを持つブロックが存在したかどうか</returns>
        public bool Find(string id, out XmlBlock result)
        {
            result = null;
            //result = (XmlBlock)AcmBlocks.Select(x => x.ToString() == id);
            foreach (XmlBlock x in AcmBlocks)
            {
                if (x.ToString() == id)
                {
                    result = x;
                }
            }
            return result != null;
        }
    }
    /// <summary>
    /// ACMブロックのデータ
    /// </summary>
	[Serializable]
	public class XmlBlock
    {
        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlElement("guid")]
        public string ModId { get; set; }
        [XmlElement("local_id")]
        public string LocalId { get; set; }
        [XmlElement("projectile_mass")]
        public float Mass { get; set; }

        public override string ToString()
        {
            return $"{ModId}-{LocalId}";
        }
    }
}