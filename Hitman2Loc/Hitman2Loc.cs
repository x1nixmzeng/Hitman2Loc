using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Collections; // for stack

namespace Hitman2Loc
{
    class Hitman2Loc
    {
        ///////////////////////////////////////////////////////////////////////

        Node root;
        Options runtimeOptions;

        ///////////////////////////////////////////////////////////////////////

        public Hitman2Loc(Options opts)
        {
            root = null;
            runtimeOptions = opts;
        }

        ///////////////////////////////////////////////////////////////////////

        void ReadXmlNode_Node(ref XmlReader xr, ref Node node)
        {
            bool done = false;

            while (false == done && xr.Read())
            {
                switch (xr.NodeType)
                {
                    case XmlNodeType.Element:

                        switch (xr.Name)
                        {
                            case "children":
                                {
                                    int num = ReadXmlNode_Attr(ref xr, "count");
                                    ReadXmlNode_Children(ref xr, ref node, num);
                                }
                                break;

                            case "strings":
                                {
                                    int num = ReadXmlNode_Attr(ref xr, "count");
                                    ReadXmlNode_Strings(ref xr, ref node, num);
                                }
                                break;

                            default:
                                throw new Exception("Unknown XML data in <node> node");
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (xr.Name == "node")
                        {
                            done = true;
                        }
                        else
                        {
                            new Exception("Unknown XML node");
                        }

                        break;

                    default:
                        break;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        int ReadXmlNode_Attr(ref XmlReader xr, string name)
        {
            string attr_str = xr.GetAttribute(name);
            int result = 0;
            if (attr_str != null)
            {
                int.TryParse(attr_str, out result);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        void ReadXmlNode_Children(ref XmlReader xr, ref Node node, int count)
        {
            bool done = false;

            while (false == done && xr.Read())
            {
                switch (xr.NodeType)
                {
                    case XmlNodeType.Element:

                        switch (xr.Name)
                        {
                            case "node":
                                {
                                    Node child = new Node(xr.GetAttribute("name"));
                                    node.Children.Add(child);

                                    // fix for '<node name="NotEquipped"/>' data
                                    if (false == xr.IsEmptyElement)
                                    {
                                        ReadXmlNode_Node(ref xr, ref child);
                                    }

                                    --count;

                                }
                                break;

                            default:
                                throw new Exception("Unknown XML data in <children> node");
                        }

                        break;

                    case XmlNodeType.EndElement:
                        switch (xr.Name)
                        {
                            case "children":
                                {
                                    if (count != 0)
                                    {
                                        new Exception("Missing children in XML data");
                                    }

                                    done = true;
                                }
                                break;
                        }

                        break;

                    default:
                        break;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        void ReadXmlNode_Strings(ref XmlReader xr, ref Node node, int count)
        {
            bool done = false;

            while (false == done && xr.Read())
            {
                switch (xr.NodeType)
                {
                    case XmlNodeType.Element:

                        switch (xr.Name)
                        {
                            case "string":
                                ReadXmlNode_String(ref xr, ref node);
                                --count;
                                break;

                            default:
                                throw new Exception("Unknown XML data in <strings> node");
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (xr.Name == "strings")
                        {
                            if (count != 0)
                            {
                                new Exception("Missing strings in XML data");
                            }

                            done = true;
                        }
                        else
                        {
                            new Exception("Unknown XML node");
                        }

                        break;

                    default:
                        break;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        void ReadXmlNode_String(ref XmlReader xr, ref Node node)
        {
            bool done = false;

            while (false == done && xr.Read())
            {
                switch (xr.NodeType)
                {
                    case XmlNodeType.Text:
                        node.TailStrs.Add(xr.Value);
                        break;

                    case XmlNodeType.EndElement:
                        if (xr.Name == "string")
                        {
                            done = true;
                        }
                        else
                        {
                            new Exception("Unknown XML node");
                        }

                        break;

                    default:
                        break;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ReadXml(string file_name)
        {
            bool valid = true;

            if (false == File.Exists(file_name))
            {
                valid = false;
            }
            else
            {
                StreamReader src = new StreamReader(file_name);
                XmlReader xml = XmlReader.Create(src);

                if (xml.Read())
                {
                    if ((xml.NodeType == XmlNodeType.Element)
                        && (xml.Name == "node"))
                    {
                        root = new Node();
                        ReadXmlNode_Node(ref xml, ref root);
                    }
                    else
                    {
                        throw new Exception("Unknown XML data");
                    }
                }

                src.Close();
            }

            return valid;
        }

        ///////////////////////////////////////////////////////////////////////

        int ReadLocChild(BinaryReader br, int max_len, ref Node root)
        {
            Node n = null;
            int read_len = 0;

            if (root == null)
            {
                // will only happen once on the first node

                n = new Node();
                root = n;
            }
            else
            {
                // get the node name
                using (var s = new StringStore().Read(br))
                {
                    n = new Node(s.Value);
                    read_len += s.ReadSize;
                }

                root.Children.Add(n);
            }

            // strange special case, unsure how these are even used
            if( n.Name == "formatstring" )
            {
                int count = (int)br.ReadByte();
                read_len += sizeof(byte);

                for (int i = 0; i < count -1; ++i)
                {
                    using (var s = new StringStore().Read(br))
                    {
#if DEBUG
                        Console.WriteLine("Read formatting value \"{0}\"", s.Value);
#endif

                        n.TailStrs.Add(s.Value);
                        read_len += s.ReadSize;
                    }
                }
            }

            // if a node has children, this loop will happen once
            // if the node has tail strings this can happen several times
            while (read_len < max_len)
            {
                var sizes = new List<int>();

                // read the size hints
                int count = (int)br.ReadByte();
                read_len += sizeof(byte);

                if (count > 1)
                {
                    int last = 0;

                    for (int i = 0; i < count - 1; ++i)
                    {
                        last = br.ReadInt32();

#if DEBUG
                        if( last > (int)br.BaseStream.Length)
                        {
                            throw new Exception("Invalid tail string offset");
                        }
#endif
                        sizes.Add(last);

                        read_len += sizeof(int);
                    }
                }

                // add the final known size hint
                sizes.Add(max_len - read_len);

                if (count > 1)
                {
#if DEBUG
                    var ftell = (int)br.BaseStream.Position;
#endif

                    int len = 0;

                    for (int i = 0; i < sizes.Count; ++i)
                    {
                        if (i == 0)
                        {
                            len = sizes[i];
                        }
                        else
                        {
                            len = sizes[i] - sizes[i - 1];
                        }

#if DEBUG
                        if (i > 0)
                        {
                            if (br.BaseStream.Position != ftell + sizes[i - 1])
                            {
                                throw new Exception("not all data read from node");
                            }
                        }
#endif

                        read_len += ReadLocChild(br, len, ref n);
                    }
                }
                else if (count > 0)
                {
                    // read just the 1 this pass

                    using (var s = new StringStore().Read(br))
                    {
                        n.TailStrs.Add(s.Value);
                        read_len += s.ReadSize;
                    }
                }
                else
                {
                    // some items have no text!
                }
            }

#if DEBUG
            if (read_len != max_len)
            {
                throw new Exception("failed to completely parse this node");
            }
#endif

            return read_len;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ReadLoc(string file_name)
        {
            bool valid = true;

            if (false == File.Exists(file_name))
            {
                valid = false;
            }
            else
            {
                Stream fh = File.OpenRead(file_name);
                BinaryReader br = new BinaryReader(fh);

                ReadLocChild(br, (int)br.BaseStream.Length, ref root);

                valid &= (br.BaseStream.Position == br.BaseStream.Length);

                fh.Close();
            }

            return valid;
        }

        ///////////////////////////////////////////////////////////////////////

        public void WriteXmlNode(ref XmlTextWriter xtw, Node root)
        {
            xtw.WriteStartElement("node");

            if (root.Name.Length > 0)
            {
                xtw.WriteAttributeString("name", root.Name);
            }

            if (root.Children.Count > 0)
            {
                xtw.WriteStartElement("children");
                xtw.WriteAttributeString("count", root.Children.Count.ToString());

                for (int i = 0; i < root.Children.Count; ++i)
                {
                    WriteXmlNode(ref xtw, root.Children[i]);
                }

                xtw.WriteEndElement();
            }

            if (root.TailStrs.Count > 0)
            {
                xtw.WriteStartElement("strings");
                xtw.WriteAttributeString("count", root.TailStrs.Count.ToString());

                for (int i = 0; i < root.TailStrs.Count; ++i)
                {
                    xtw.WriteStartElement("string");
                    xtw.WriteString(root.TailStrs[i]);
                    xtw.WriteEndElement();
                }

                xtw.WriteEndElement();
            }

            xtw.WriteEndElement();
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteXml(string file_name)
        {
            if (root == null)
            {
                return false;
            }

            XmlTextWriter xtw = new XmlTextWriter(file_name, runtimeOptions.Encoding);

            bool success = false;

            if (xtw.BaseStream.CanWrite)
            {
                WriteXmlNode(ref xtw, root);
                success = true;
            }

            xtw.Close();
            return success;
        }

        ///////////////////////////////////////////////////////////////////////

        private int CalcCStringLength(string str)
        {
            int len = runtimeOptions.Encoding.GetByteCount(str);

            if( len == 0 )
            {
                return 0;
            }

            // with null-terminator
            return len + sizeof(byte);            
        }

        ///////////////////////////////////////////////////////////////////////

        void CalcLocSizeOnce(Node root)
        {
#if DEBUG
            if( root.Size != 0 )
            {
                throw new Exception("Duplicate parsing");
            }
#endif

            int len = 0;
            int name_enc = CalcCStringLength(root.Name);

            if (name_enc > 0)
            {
                len += name_enc;
            }

            if (root.Name == "formatstring")
            {
                len += sizeof(byte);

                for (int i = 0; i < root.TailStrs.Count; ++i)
                {
                    len += CalcCStringLength(root.TailStrs[i]);
                }
            }
            else if (root.Children.Count == 0 && root.TailStrs.Count == 0)
            {
                len += sizeof(byte);
            }
            else
            {
                if (root.Children.Count > 0)
                {
                    len += sizeof(byte);

                    if (root.Children.Count > 1)
                    {
                        len += (sizeof(int) * (root.Children.Count - 1));
                    }

                    for (int i = 0; i < root.Children.Count; ++i)
                    {
                        CalcLocSizeOnce(root.Children[i]);
                        len += root.Children[i].Size;
                    }
                }

                if (root.TailStrs.Count > 0)
                {
                    for (int i = 0; i < root.TailStrs.Count; ++i)
                    {
                        len += sizeof(byte);
                        len += CalcCStringLength(root.TailStrs[i]);
                    }
                }
            }

#if DEBUG
            Console.WriteLine(string.Format("{0} == {1}", root.Name, len));
#endif

            root.Size = len;
        }

        ///////////////////////////////////////////////////////////////////////

        private void WriteLocChild(BinaryWriter bw, Node root)
        {
            int name_enc = CalcCStringLength(root.Name);

            if (name_enc > 0)
            {
                bw.Write(runtimeOptions.Encoding.GetBytes(root.Name));
                bw.Write((byte)0);
            }

            if( root.Name == "formatstring")
            {
                bw.Write((byte)((root.TailStrs.Count + 1) & 0xFF));

                for (int i = 0; i < root.TailStrs.Count; ++i)
                {
                    bw.Write(runtimeOptions.Encoding.GetBytes(root.TailStrs[i]));
                    bw.Write((byte)0);
                }
            }
            else if (root.Children.Count == 0 && root.TailStrs.Count == 0)
            {
                bw.Write((byte)0);
            }
            else
            {
                if (root.Children.Count > 0)
                {
                    bw.Write((byte)(root.Children.Count & 0xFF));

                    if (root.Children.Count > 1)
                    {
                        int node_size = root.Children[0].Size;

                        for(int i = 1; i < root.Children.Count; ++i)
                        {
                            bw.Write(node_size);
                            node_size += root.Children[i].Size;
                        }
                    }

                    for (int i = 0; i < root.Children.Count; ++i)
                    {
                        WriteLocChild(bw, root.Children[i]);
                    }
                }

                if (root.TailStrs.Count > 0)
                {
                    for (int i = 0; i < root.TailStrs.Count; ++i)
                    {
                        bw.Write((byte)1);
                        bw.Write(runtimeOptions.Encoding.GetBytes(root.TailStrs[i]));
                        bw.Write((byte)0);
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteLoc(string file_name)
        {
            bool valid = true;

            Stream fh = File.OpenWrite(file_name);
            if (fh.CanWrite)
            {
                BinaryWriter bw = new BinaryWriter(fh);

                CalcLocSizeOnce(root);

                WriteLocChild(bw, root);
            }
            fh.Close();

            return valid;
        }

        ///////////////////////////////////////////////////////////////////////
    }
}
