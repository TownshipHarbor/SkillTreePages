using System;
using System.Collections.Generic;
using UnityEngine;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppTMPro;
using GameStream = Il2CppAlta.Serialization.Stream;
using Object = UnityEngine.Object;

namespace SkillTreePagesQuest
{
    internal static class Plat
    {
        internal static List<string> Prepare()
        {
            List<string> missing = new List<string>();
            return missing;
        }

        internal static T As<T>(Object o) where T : Object
        {
            return o == null ? null : o.TryCast<T>();
        }

        internal static long Key(Object o)
        {
            return o == null ? 0 : (long)o.Pointer;
        }

        internal static bool Same(Object a, Object b)
        {
            if (a == null || b == null) return false;
            return Key(a) == Key(b);
        }

        internal static T Add<T>(GameObject g) where T : Component
        {
            return g.AddComponent(Il2CppType.Of<T>()).TryCast<T>();
        }

        internal static List<T> All<T>(Component c) where T : Component
        {
            List<T> list = new List<T>();
            if (c == null) return list;
            foreach (Component x in c.GetComponentsInChildren(Il2CppType.Of<T>(), true))
            {
                T t = x == null ? null : x.TryCast<T>();
                if (t != null) list.Add(t);
            }
            return list;
        }

        internal static List<Material> Materials(Renderer r)
        {
            List<Material> list = new List<Material>();
            if (r == null) return list;
            foreach (Material m in r.sharedMaterials) if (m != null) list.Add(m);
            return list;
        }

        internal static List<Object> Loaded<T>() where T : Object
        {
            List<Object> list = new List<Object>();
            foreach (Object o in Resources.FindObjectsOfTypeAll(Il2CppType.Of<T>())) list.Add(o);
            return list;
        }

        internal static GameStream Clone(GameStream s)
        {
            Il2CppSystem.Object o = s.Clone();
            return o == null ? null : o.TryCast<GameStream>();
        }

        internal static uint TotalBits(GameStream s)
        {
            Il2CppAlta.Serialization.StreamReader r = s.TryCast<Il2CppAlta.Serialization.StreamReader>();
            if (r == null || r.reader == null) return 0;
            return r.reader.TotalBits;
        }

        internal static uint Bits(GameStream s, int count)
        {
            uint v = 0;
            s.SerializeBits(ref v, count);
            return v;
        }

        internal static int CrystalCount(BoughtSkillVisualizer v)
        {
            return v.crystalCount;
        }

        internal static void SetCrystalCount(BoughtSkillVisualizer v, int n)
        {
            v.crystalCount = n;
        }

        internal static bool HasHolderPrefab(BoughtSkillVisualizer v)
        {
            return v.holderPrefab != null;
        }

        internal static void PlaceOrbHolders(BoughtSkillVisualizer v)
        {
            v.PlaceOrbHolders();
        }

        internal static ProgressionShrine ShrineOf(BoughtSkillVisualizer v)
        {
            return v.fillVisualizerObject;
        }

        internal static float Radius(BoughtSkillVisualizer v)
        {
            return v.radius;
        }

        internal static float Vertical(BoughtSkillVisualizer v)
        {
            return v.verticalOffset;
        }

        internal static Quaternion Offset(BoughtSkillVisualizer v)
        {
            return v.offsetRotation;
        }

        internal sealed class Holders
        {
            readonly Il2CppReferenceArray<BoughtSkillVisualizer.OrbHolderHandle> _a;

            internal Holders(BoughtSkillVisualizer v)
            {
                _a = v.placedHolders;
            }

            internal int Count { get { return _a == null ? 0 : _a.Length; } }

            internal Component Holder(int i)
            {
                BoughtSkillVisualizer.OrbHolderHandle h = _a[i];
                return h == null ? null : h.holder;
            }

            internal uint Hash(int i)
            {
                BoughtSkillVisualizer.OrbHolderHandle h = _a[i];
                return h == null ? 0u : h.SlotHash;
            }

            internal bool HasOrb(int i)
            {
                BoughtSkillVisualizer.OrbHolderHandle h = _a[i];
                return h != null && h.Orb != null;
            }
        }

        internal static int BarBitsAfter(BoughtSkillVisualizer v, ProgressionShrine shrine)
        {
            if (shrine == null) return -1;
            ProfessionsProgressShrine p = As<ProfessionsProgressShrine>(shrine);
            if (p != null)
            {
                if (!Same(p.boughtSkillsVisualizer, v)) return -1;
                int count;
                try { count = BarCount(p); }
                catch (Exception) { count = All<ExperienceFillVisualizer>(p).Count; }
                return count * Wire.BarBits;
            }
            SkillShrine k = As<SkillShrine>(shrine);
            if (k == null) return -1;
            List<SkillShrineVisualizer> list = new List<SkillShrineVisualizer>();
            Il2CppReferenceArray<SkillShrineVisualizer> arr = k.visualizers;
            if (arr == null) return -1;
            for (int i = 0; i < arr.Length; i++) list.Add(arr[i]);
            int at = -1;
            for (int i = 0; i < list.Count; i++) if (Same(list[i], v)) at = i;
            if (at < 0) return -1;
            int bits = 0;
            for (int i = at + 1; i < list.Count; i++)
            {
                SkillShrineVisualizer x = list[i];
                if (x == null) return -1;
                if (As<ExperienceFillVisualizer>(x) != null) bits += Wire.BarBits;
                else if (As<PathModelDisplay>(x) == null && As<SkillOrbScaler>(x) == null) return -1;
            }
            return bits;
        }

        static int BarCount(ProfessionsProgressShrine p)
        {
            Il2CppSystem.Collections.Generic.Dictionary<ProgressionPath, ExperienceFillVisualizer> bars = p.availablePointsVisualizers;
            if (bars == null) throw new InvalidOperationException("no bar list");
            return bars.Count;
        }

        internal static PlayerController User(ProgressionShrine shrine)
        {
            PlayerProgressionManager m = shrine == null ? null : shrine.CurrentManager;
            if (m == null) return null;
            Player p = m.Player;
            return p == null ? null : p.PlayerController;
        }

        internal static void FixCanBuy(PlayerProgressionManager m, int limit)
        {
            m.canBuyMoreSkills = Wire.CanBuy(m.currentlyOwnedSkillsCount, limit);
        }

        static bool _fontTried;
        static TMP_FontAsset _font;

        static TMP_FontAsset Font()
        {
            if (_fontTried) return _font;
            _fontTried = true;
            try { _font = TMP_Settings.defaultFontAsset; }
            catch (Exception) { _font = null; }
            if (_font == null)
            {
                foreach (Object o in Loaded<TMP_FontAsset>())
                {
                    _font = As<TMP_FontAsset>(o);
                    if (_font != null) break;
                }
            }
            return _font;
        }

        internal static Component Label(GameObject g, string text, float size, Color color)
        {
            TextMeshPro t = Add<TextMeshPro>(g);
            TMP_FontAsset f = Font();
            if (f != null) t.font = f;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.enableWordWrapping = false;
            t.text = text;
            return t;
        }

        internal static void SetText(Component label, string text)
        {
            TextMeshPro t = As<TextMeshPro>(label);
            if (t != null && t.text != text) t.text = text;
        }

        internal static Mesh NewMesh(string name, Vector3[] v, Vector3[] n, Vector2[] uv, List<int[]> subs)
        {
            Mesh m = new Mesh();
            m.name = name;
            m.vertices = new Il2CppStructArray<Vector3>(v);
            m.normals = new Il2CppStructArray<Vector3>(n);
            m.uv = new Il2CppStructArray<Vector2>(uv);
            m.subMeshCount = subs.Count;
            for (int i = 0; i < subs.Count; i++) m.SetTriangles(new Il2CppStructArray<int>(subs[i]), i);
            m.RecalculateBounds();
            m.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return m;
        }

        internal static Texture2D Swatch(Color c)
        {
            Texture2D t = new Texture2D(1, 1, TextureFormat.RGBA32, false, false);
            byte[] px = new byte[] { Byte(c.r), Byte(c.g), Byte(c.b), 255 };
            t.LoadRawTextureData(new Il2CppStructArray<byte>(px));
            t.Apply(false, true);
            t.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return t;
        }

        static byte Byte(float f)
        {
            return (byte)Mathf.RoundToInt(Mathf.Clamp01(f) * 255f);
        }
    }

    internal sealed class StreamBits : IBits
    {
        readonly GameStream _s;
        readonly uint _total;

        internal StreamBits(GameStream s)
        {
            _s = s;
            _total = Plat.TotalBits(s);
        }

        public uint Index { get { return _s.BitIndex; } }
        public uint Total { get { return _total; } }
        public uint Read(int count) { return Plat.Bits(_s, count); }
    }
}
