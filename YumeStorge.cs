using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace YumeStorge
{
    public interface IYumeElement
    {
        IYumeElement Read(BinaryReader reader);
        void Write(BinaryWriter writer);

        void Set(object value);
        object Get();
    }

    public class YumeFile
    {
        public readonly YumeRoot root;
        private readonly string fileName;
        private DESHelper des;

        public YumeFile(string fileName, YumeRoot root, string key, string iv)
        {
            this.root = root;
            this.fileName = fileName;
            des = new DESHelper(key, iv);
        }

        private FileStream Create(FileMode fileMode)
        {
            return new FileStream(fileName + ".yume",
                fileMode, FileAccess.ReadWrite, FileShare.None);
        }

        public void ToFile()
        {
            using (FileStream fileStream = Create(FileMode.Create))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(ms))
                    {
                        root.Write(writer);
                        writer.Flush();

                        using (BinaryWriter bw = new BinaryWriter(fileStream))
                        {
                            byte[] data = des.Encrypt(ms.ToArray());
                            bw.Write(data.Length);
                            bw.Write(data);
                            bw.Flush();
                        }
                    }
                }
            }
        }

        public void FormFile()
        {
            // 打开文件
            using (FileStream fileStream = Create(FileMode.Open))
            {
                using (BinaryReader br = new BinaryReader(fileStream))
                {
                    byte[] data = des.Decrypt(br.ReadBytes(br.ReadInt32()));

                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        ms.Seek(0, SeekOrigin.Begin);

                        using (BinaryReader binaryReader = new BinaryReader(ms))
                        {
                            root.Read(binaryReader);
                        }
                    }
                }
            }
        }
    }

    public class YumeRoot : IYumeElement
    {
        private Dictionary<string, IYumeElement> elements = new Dictionary<string, IYumeElement>();

        public IYumeElement Get(string name)
        {
            return elements.ContainsKey(name) ? elements[name] : null;
        }

        public void Remove(string key)
        {
            elements.Remove(key);
        }

        public void Set(string name, IYumeElement element)
        {
            if (!elements.ContainsKey(name))
            {
                elements.Add(name, element);
            }
            else
            {
                elements[name] = element;
            }
        }

        public bool Has(string name)
        {
            return elements.ContainsKey(name);
        }

        public IYumeElement Read(BinaryReader reader)
        {
            elements.Clear();

            while (true)
            {
                IYumeElement element = YumeUtils.Instance.GetType(reader.ReadByte());

                if (element == null) return this;

                elements.Add(reader.ReadString(), element.Read(reader));
            }
        }

        public void Write(BinaryWriter writer)
        {
            foreach (var entry in elements)
            {
                writer.Write(YumeUtils.Instance.GetId(entry.Value));
                writer.Write(entry.Key);
                entry.Value.Write(writer);
            }

            writer.Write((byte)0);
        }

        public void Set(object value)
        {

        }

        public object Get()
        {
            return elements;
        }

        public IYumeElement this[string key]
        {
            get { return Get(key); }
            set { Set(key, value); }
        }
    }

    public class YumeArray : IYumeElement
    {
        public readonly List<IYumeElement> values = new List<IYumeElement>();

        public object Get()
        {
            return values;
        }

        public IYumeElement Read(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                values.Add(YumeUtils.Instance.GetType(reader.ReadByte()).Read(reader));
            }
            return this;
        }

        public void Set(object value)
        {

        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(values.Count());
            foreach (var entry in values)
            {
                writer.Write(YumeUtils.Instance.GetId(entry));
                entry.Write(writer);
            }
        }
    }

    public class YumeInt : IYumeElement
    {
        public int value;

        public YumeInt(int value)
        {
            this.value = value;
        }

        public YumeInt()
        {
            value = 0;
        }

        public IYumeElement Read(BinaryReader reader)
        {
            value = reader.ReadInt32();
            return this;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(value);
        }

        public void Set(object value)
        {
            this.value = (int)value;
        }

        public object Get()
        {
            return value;
        }
    }

    public class YumeDouble : IYumeElement
    {
        public double value;

        public YumeDouble(double value)
        {
            this.value = value;
        }

        public YumeDouble()
        {
            value = 0;
        }

        public IYumeElement Read(BinaryReader reader)
        {
            value = reader.ReadDouble();
            return this;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(value);
        }

        public void Set(object value)
        {
            this.value = (double)value;
        }

        public object Get()
        {
            return value;
        }
    }

    public class YumeString : IYumeElement
    {
        public string value;

        public YumeString(string value)
        {
            this.value = value;
        }

        public YumeString()
        {
            value = "";
        }

        public IYumeElement Read(BinaryReader reader)
        {
            value = reader.ReadString();
            return this;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(value);
        }

        public void Set(object value)
        {
            this.value = value.ToString();
        }

        public object Get()
        {
            return this.value;
        }
    }

    public class YumeBool : IYumeElement
    {
        private bool value;

        public YumeBool(bool value)
        {
            this.value = value;
        }

        public YumeBool()
        {
            value = false;
        }

        public object Get()
        {
            return value;
        }

        public IYumeElement Read(BinaryReader reader)
        {
            value = reader.ReadBoolean();
            return this;
        }

        public void Set(object value)
        {
            if (value is bool b)
            {
                this.value = b;
            }
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(value);
        }
    }

    internal class DESHelper
    {
        private readonly ICryptoTransform encryptor;
        private readonly ICryptoTransform decryptor;

        public DESHelper(string key, string iv)
        {
            byte[] byKey = Encoding.UTF8.GetBytes(key);
            byte[] byIV = Encoding.UTF8.GetBytes(iv);

            DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();

            encryptor = cryptoProvider.CreateEncryptor(byKey, byIV);
            decryptor = cryptoProvider.CreateDecryptor(byKey, byIV);
        }

        public byte[] Encrypt(byte[] inDate)
        {
            return Transform(inDate, encryptor);
        }

        public byte[] Decrypt(byte[] inDate)
        {
            return Transform(inDate, decryptor);
        }

        private byte[] Transform(byte[] inDate, ICryptoTransform crypto)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, crypto, CryptoStreamMode.Write))
                {
                    cs.Write(inDate, 0, inDate.Length);
                    cs.FlushFinalBlock();
                }

                return ms.ToArray();
            }
        }
    }
}
