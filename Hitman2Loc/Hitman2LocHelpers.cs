using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;

namespace Hitman2Loc
{
    ///////////////////////////////////////////////////////////////////////

    public struct StringStore : IDisposable
    {
        public string Value;

        private int _ValueSize;
        public int ReadSize { get { return _ValueSize; } }
        public int WriteSize { get { return _ValueSize; } }

        public void Dispose() { }

        public StringStore Read(BinaryReader br)
        {
            var raw = new List<byte>();

            // this is reading a null-terminated string
            for (byte val = br.ReadByte(); val != 0; val = br.ReadByte())
            {
                raw.Add(val);
            }

            Value = Encoding.UTF8.GetString(raw.ToArray());
            _ValueSize = raw.Count + 1;

            return this;
        }

        public StringStore Write(BinaryWriter bw)
        {
            byte[] raw = Encoding.UTF8.GetBytes(Value);

            bw.Write(raw, 0, raw.Length);
            bw.Write((byte)0);

            _ValueSize = raw.Length + 1;

            return this;
        }
    }

    ///////////////////////////////////////////////////////////////////////

    public class Options
    {
        public Encoding Encoding;

        public Options()
        {
            // defaults

            Encoding = Encoding.UTF8;
        }
    }

    ///////////////////////////////////////////////////////////////////////

    class Node
    {
        public string Name { get; private set; }
        public int Size;

        public List<Node> Children { get; private set; }
        public List<string> TailStrs { get; private set; }

        public Node(string _name)
        {
            Name = _name;
            Size = 0;

            Children = new List<Node>();
            TailStrs = new List<string>();
        }

        public Node()
        {
            Name = "";
            Size = 0;

            Children = new List<Node>();
            TailStrs = new List<string>();
        }
    }

    ///////////////////////////////////////////////////////////////////////
}
