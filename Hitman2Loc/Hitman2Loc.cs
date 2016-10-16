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

        public struct Options { }

        Options runtimeOptions;

        ///////////////////////////////////////////////////////////////////////

        public Hitman2Loc(Options opts)
        {
            root = null;
            runtimeOptions = opts;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ReadXml(string file_name)
        {
            bool valid = false;

            // stub

            return valid;
        }

        ///////////////////////////////////////////////////////////////////////

        int ReadLocChild(BinaryReader br, int max_len, ref Node root)
        {
            Node n = null;
            int read_len = 0;

            if( root == null )
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
                else if(count > 0)
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
            if (read_len != max_len )
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

            if(false == File.Exists(file_name))
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

            if( root.Name.Length > 0 )
            {
                xtw.WriteAttributeString("name", root.Name);
            }
            
            if( root.Children.Count > 0 )
            {
                xtw.WriteStartElement("children");
                xtw.WriteAttributeString("count", root.Children.Count.ToString());

                for(int i=0; i <root.Children.Count; ++i)
                {
                    WriteXmlNode(ref xtw, root.Children[i]);
                }

                xtw.WriteEndElement();
            }

            if( root.TailStrs.Count > 0 )
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
            if( root == null )
            {
                return false;
            }

            XmlTextWriter xtw = new XmlTextWriter(file_name, Encoding.UTF8);

            bool success = false;

            if(xtw.BaseStream.CanWrite)
            {
                WriteXmlNode(ref xtw, root);
                success = true;
            }

            xtw.Close();
            return success;
        }

        ///////////////////////////////////////////////////////////////////////

        private int WriteLocChild(BinaryWriter bw, ref Node root)
        {
            int len = 0;

            byte children = (byte)(root.Children.Count & 0xFF);

            // stub

            return len;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WriteLoc(string file_name)
        {
            bool valid = false;

            // stub

            return valid;
        }

        ///////////////////////////////////////////////////////////////////////
    }
}
