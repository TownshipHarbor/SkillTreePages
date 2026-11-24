using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

namespace SkillTreePagesQuest
{
    internal sealed class PackMesh
    {
        internal string Name;
        internal Vector3[] Vertices;
        internal Vector3[] Normals;
        internal Vector2[] Uv;
        internal List<int[]> Subs = new List<int[]>();
        internal Mesh Built;
    }

    internal sealed class PackNode
    {
        internal string Name;
        internal int Parent;
        internal bool Active;
        internal Vector3 Position;
        internal Quaternion Rotation;
        internal Vector3 Scale;
        internal int Mesh;
        internal int Material;
        internal string Text;
        internal float TextSize;
        internal Color TextColor;
    }

    internal static class Pack
    {
        internal const int Stone = 1;
        internal const int Glow = 2;
        internal static string ParentName = "Ruins Visual Base";
        internal static Vector3 Previous = new Vector3(-0.257f, 0f, -0.085f);
        internal static Vector3 Next = new Vector3(0.257f, 0f, -0.085f);
        internal static readonly List<PackMesh> Meshes = new List<PackMesh>();
        internal static readonly List<PackNode> Nodes = new List<PackNode>();
        internal static bool Ok;

        internal static void Read()
        {
            using (Stream s = typeof(Pack).Assembly.GetManifestResourceStream("SkillTreePagesQuest.panel.pack"))
            {
                if (s == null) throw new InvalidDataException("panel.pack is missing from the DLL");
                byte[] head = new byte[4];
                if (s.Read(head, 0, 4) != 4) throw new InvalidDataException("panel.pack is cut short");
                int size = BitConverter.ToInt32(head, 0);
                byte[] body = new byte[size];
                using (DeflateStream d = new DeflateStream(s, CompressionMode.Decompress))
                {
                    int got = 0;
                    while (got < size)
                    {
                        int n = d.Read(body, got, size - got);
                        if (n <= 0) throw new InvalidDataException("panel.pack is cut short");
                        got += n;
                    }
                }
                Parse(new BinaryReader(new MemoryStream(body)));
            }
            Ok = Nodes.Count > 0;
        }

        static void Parse(BinaryReader r)
        {
            if (Encoding.ASCII.GetString(r.ReadBytes(4)) != "STP1") throw new InvalidDataException("panel.pack has the wrong header");
            ParentName = Text(r);
            Previous = V3(r);
            Next = V3(r);
            int meshes = r.ReadInt32();
            for (int i = 0; i < meshes; i++)
            {
                PackMesh m = new PackMesh();
                m.Name = Text(r);
                int n = r.ReadInt32();
                m.Vertices = new Vector3[n];
                m.Normals = new Vector3[n];
                m.Uv = new Vector2[n];
                for (int k = 0; k < n; k++) m.Vertices[k] = V3(r);
                for (int k = 0; k < n; k++) m.Normals[k] = V3(r);
                for (int k = 0; k < n; k++) m.Uv[k] = new Vector2(r.ReadSingle(), r.ReadSingle());
                int subs = r.ReadInt32();
                for (int k = 0; k < subs; k++)
                {
                    int[] idx = new int[r.ReadInt32()];
                    for (int j = 0; j < idx.Length; j++)
                    {
                        idx[j] = r.ReadInt32();
                        if (idx[j] < 0 || idx[j] >= n) throw new InvalidDataException("panel.pack mesh " + m.Name + " has a bad index");
                    }
                    m.Subs.Add(idx);
                }
                Meshes.Add(m);
            }
            int nodes = r.ReadInt32();
            for (int i = 0; i < nodes; i++)
            {
                PackNode d = new PackNode();
                d.Name = Text(r);
                d.Parent = r.ReadInt32();
                d.Active = r.ReadByte() != 0;
                d.Position = V3(r);
                d.Rotation = new Quaternion(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                d.Scale = V3(r);
                d.Mesh = r.ReadInt32();
                d.Material = r.ReadByte();
                if (r.ReadByte() != 0)
                {
                    d.Text = Text(r);
                    d.TextSize = r.ReadSingle();
                    d.TextColor = new Color(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                }
                if (d.Parent >= i || (i > 0 && d.Parent < 0)) throw new InvalidDataException("panel.pack node " + d.Name + " has a bad parent");
                if (d.Mesh >= Meshes.Count) throw new InvalidDataException("panel.pack node " + d.Name + " has a bad mesh");
                Nodes.Add(d);
            }
        }

        internal static Mesh Mesh(int i)
        {
            PackMesh m = Meshes[i];
            if (m.Built == null) m.Built = Plat.NewMesh(m.Name, m.Vertices, m.Normals, m.Uv, m.Subs);
            return m.Built;
        }

        static string Text(BinaryReader r)
        {
            int n = r.ReadInt32();
            return Encoding.UTF8.GetString(r.ReadBytes(n));
        }

        static Vector3 V3(BinaryReader r)
        {
            return new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        }
    }
}
