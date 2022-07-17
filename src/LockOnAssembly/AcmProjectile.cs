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
    /// XML��ǂݍ���
    /// </summary>
    public class XMLDeserializer
    {
        public static readonly string FileName = "AcmProjectileMass.xml";
        private static readonly string ResourcesPath = "Resources/Xml/";
        /// <summary>
        /// ACM�����̃f�[�^���f�V���A���C�Y����
        /// </summary>
        /// <returns></returns>
        public static XmlData Deserialize()
        {
            Mod.Log("Loaded " + FileName + " from resources folder");
            return Modding.ModIO.DeserializeXml<XmlData>(ResourcesPath + FileName);
        }
    }
    /// <summary>
    /// ACM�u���b�N�̃f�[�^���i�[���ꂽ���X�g�����N���X
    /// </summary>
    [XmlRoot("data")]
    public class XmlData : Element
    {
        /// <summary>
        /// ACM�̃u���b�N�̃f�[�^
        /// </summary>
        [XmlArray("blocks")]
        [XmlArrayItem("block")]
        public List<XmlBlock> AcmBlocks { get; set; }

        /// <summary>
        /// �f�o�b�O�p
        /// AcmBlocks�̒��g��S�ăv�����g����
        /// </summary>
        public void LogList()
        {
            foreach (XmlBlock b in AcmBlocks)
            {
                Mod.Log($"name={b.Name}, id={b}, mass={b.Mass}");
            }
        }
        /// <summary>
        /// AcmBlocks�̒�����w�肵��id�����u���b�N�����o��
        /// </summary>
        /// <param name="id">�u���b�N��id�i"global"-"local"�j</param>
        /// <param name="result">�w�肵���u���b�N</param>
        /// <returns>�w�肵��id�����u���b�N�����݂������ǂ���</returns>
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
    /// ACM�u���b�N�̃f�[�^
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