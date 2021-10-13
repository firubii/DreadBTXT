using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace DreadBTXT
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 1)
            {
                string path = Path.GetFullPath(args[1]);
                string outPath = path.EndsWith(".txt") ? path.Remove(path.Length - 4) + ".xml" : path.Remove(path.Length - 4) + ".txt";

                if (args.Contains("-o"))
                {
                    outPath = args[args.ToList().IndexOf("-o") + 1];
                }

                if (args[0] == "-d")
                {
                    byte[] version;
                    Dictionary<string, string> entries = new Dictionary<string, string>();

                    using (BinaryReader reader = new BinaryReader(new FileStream(path, FileMode.Open, FileAccess.Read)))
                    {
                        if (Encoding.UTF8.GetString(reader.ReadBytes(4)) != "BTXT")
                        {
                            Console.WriteLine("Invalid Binary TXT file!");
                            return;
                        }

                        version = reader.ReadBytes(4);
                        while (reader.BaseStream.Position < reader.BaseStream.Length)
                        {
                            List<byte> name = new List<byte>();
                            while (true)
                            {
                                byte b = reader.ReadByte();
                                if (b == 0)
                                    break;

                                name.Add(b);
                            }

                            List<byte> text = new List<byte>();
                            while (true)
                            {
                                byte[] s = reader.ReadBytes(2);
                                if (BitConverter.ToInt16(s, 0) == 0)
                                    break;

                                text.AddRange(s);
                            }

                            string entryName = Encoding.UTF8.GetString(name.ToArray());
                            string entryText = Encoding.Unicode.GetString(text.ToArray());

                            entries.Add(entryName, entryText);
                        }
                    }

                    XmlDocument xml = new XmlDocument();
                    xml.AppendChild(xml.CreateXmlDeclaration("1.0", "utf-8", ""));

                    XmlElement root = xml.CreateElement("BTXT");
                    root.SetAttribute("version", $"{version[0]}.{version[1]}.{version[2]}.{version[3]}");

                    foreach (KeyValuePair<string, string> pair in entries)
                    {
                        XmlElement entry = xml.CreateElement("Entry");
                        entry.SetAttribute("name", pair.Key);
                        entry.InnerText = pair.Value;

                        root.AppendChild(entry);
                    }

                    xml.AppendChild(root);

                    xml.Save(outPath);
                }
                else if (args[0] == "-a")
                {
                    XmlDocument xml = new XmlDocument();
                    xml.Load(path);

                    XmlElement root = xml["BTXT"];
                    string[] v = root.GetAttribute("version").Split('.');
                    byte[] version = new byte[] { byte.Parse(v[0]), byte.Parse(v[1]), byte.Parse(v[2]), byte.Parse(v[3]) };

                    Dictionary<string, string> entries = new Dictionary<string, string>();
                    for (int i = 0; i < root.ChildNodes.Count; i++)
                    {
                        entries.Add(root.ChildNodes[i].Attributes["name"].Value, root.ChildNodes[i].InnerText);
                    }

                    using (BinaryWriter writer = new BinaryWriter(new FileStream(outPath, FileMode.Create, FileAccess.Write)))
                    {
                        writer.Write("BTXT".ToCharArray());
                        writer.Write(version);
                        foreach (KeyValuePair<string, string> pair in entries)
                        {
                            writer.Write(Encoding.UTF8.GetBytes(pair.Key));
                            writer.Write((byte)0);
                            writer.Write(Encoding.Unicode.GetBytes(pair.Value));
                            writer.Write((short)0);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Usage: DreadBTXT.exe <-d|-a> <file path> [options]" +
                                    "\n" +
                                    "\nOptions:" +
                                    "\n  -o <path>: Sets the output filepath");
                }
            }
        }
    }
}
