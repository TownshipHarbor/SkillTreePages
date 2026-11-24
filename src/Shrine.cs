using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Il2Cpp;
using GameStream = Il2CppAlta.Serialization.Stream;

namespace SkillTreePagesQuest
{
    internal sealed class Shrine
    {
        static readonly Dictionary<long, Shrine> All = new Dictionary<long, Shrine>();
        static readonly List<long> Gone = new List<long>();
        static bool _mismatch;
        static int _told, _copies;

        internal static bool PagesServer { get; private set; }
        internal static int RemoteLimit { get; private set; }

        readonly BoughtSkillVisualizer _v;
        ProgressionShrine _shrine;
        Panel _panel;
        bool _panelFailed, _arranged, _dirty = true, _wasOpen;
        int _page, _extent = Wire.PageSize, _limit = Wire.PageSize;
        ulong _mask;
        readonly List<Renderer> _hiddenRenderers = new List<Renderer>();
        readonly List<bool> _rendererStates = new List<bool>();
        readonly List<Collider> _hiddenColliders = new List<Collider>();
        readonly List<bool> _colliderStates = new List<bool>();

        Shrine(BoughtSkillVisualizer v)
        {
            _v = v;
        }

        internal static void Reset()
        {
            foreach (Shrine s in All.Values) s.Dispose();
            All.Clear();
            PagesServer = false;
            RemoteLimit = 0;
            _mismatch = false;
            _told = 0;
            _copies = 0;
        }

        internal static void Read(BoughtSkillVisualizer v, GameStream stream)
        {
            int rest = Plat.BarBitsAfter(v, Plat.ShrineOf(v));
            Header h;
            Shape shape;
            List<GameStream> copies = new List<GameStream>();
            try
            {
                shape = Wire.Classify(delegate
                {
                    GameStream c = Plat.Clone(stream);
                    if (c == null) throw new InvalidOperationException("the shrine message could not be copied");
                    copies.Add(c);
                    return new StreamBits(c);
                }, rest, PagesServer ? _copies : 0, out h);
            }
            finally
            {
                foreach (GameStream c in copies) c.Dispose();
            }
            if (shape == Shape.PagesLoose && !PagesServer) shape = Shape.Stock;
            if (shape == Shape.Stock)
            {
                if (_told < 3)
                {
                    _told++;
                    Core.Log("shrine message read as stock: " + Describe(stream, rest));
                }
                if (PagesServer && !_mismatch)
                {
                    _mismatch = true;
                    Core.Error("a shrine message on this Skill Tree Pages server did not match the page layout (bits after " + rest + ")");
                }
                return;
            }
            if (!PagesServer)
            {
                PagesServer = true;
                _copies = h.Copies;
                Core.Log("this server runs Skill Tree Pages: up to " + h.Limit + " learned skills (page header x" + h.Copies + ")");
            }
            for (int i = 0; i < h.Copies * 3; i++) Plat.Bits(stream, 32);
            RemoteLimit = h.Limit;
            Get(v).Receive(h);
        }

        static string Describe(GameStream stream, int rest)
        {
            GameStream copy = Plat.Clone(stream);
            if (copy == null) return "no copy";
            try
            {
                uint start = copy.BitIndex, total = Plat.TotalBits(copy);
                StringBuilder b = new StringBuilder("bit " + start + " of " + total + ", bars after " + rest + ", head");
                for (int i = 0; i < 6 && copy.BitIndex + 32 <= total; i++) b.Append(" " + Plat.Bits(copy, 32).ToString("x8"));
                return b.ToString();
            }
            finally { copy.Dispose(); }
        }

        internal static void Changed(BoughtSkillVisualizer v)
        {
            Shrine s;
            if (All.TryGetValue(Plat.Key(v), out s)) s._dirty = true;
        }

        internal static void Tick()
        {
            Gone.Clear();
            foreach (KeyValuePair<long, Shrine> e in All)
            {
                bool alive;
                try { alive = e.Value.Update(); }
                catch (Exception ex)
                {
                    Core.Once("shrine tick", ex);
                    alive = true;
                }
                if (!alive)
                {
                    e.Value.Dispose();
                    Gone.Add(e.Key);
                }
            }
            foreach (long k in Gone) All.Remove(k);
        }

        static Shrine Get(BoughtSkillVisualizer v)
        {
            long k = Plat.Key(v);
            Shrine s;
            if (!All.TryGetValue(k, out s))
            {
                s = new Shrine(v);
                All[k] = s;
            }
            return s;
        }

        void Receive(Header h)
        {
            Convert();
            _shrine = Plat.ShrineOf(_v);
            _page = h.Page;
            _extent = h.Extent;
            _limit = h.Limit;
            _dirty = true;
            if (_panel != null || _panelFailed) return;
            try { _panel = Panel.Create(_v); }
            catch (Exception e)
            {
                _panelFailed = true;
                Core.Error("shrine panel failed: " + e);
            }
        }

        void Convert()
        {
            if (Plat.CrystalCount(_v) != Wire.Capacity || new Plat.Holders(_v).Count != Wire.Capacity)
            {
                if (!Plat.HasHolderPrefab(_v)) throw new InvalidOperationException("this shrine has no orb holder prefab to make 64 holders from");
                Plat.SetCrystalCount(_v, Wire.Capacity);
                Plat.PlaceOrbHolders(_v);
                _arranged = false;
                Plat.Holders made = new Plat.Holders(_v);
                if (made.Count != Wire.Capacity || made.Holder(0) == null) throw new InvalidOperationException("the shrine made " + made.Count + " orb holders, not " + Wire.Capacity);
            }
            if (!_arranged) Arrange();
        }

        void Arrange()
        {
            Plat.Holders hs = new Plat.Holders(_v);
            Transform t = _v.transform;
            float radius = Plat.Radius(_v), vertical = Plat.Vertical(_v);
            Quaternion offset = Plat.Offset(_v);
            for (int i = 0; i < hs.Count; i++)
            {
                Component holder = hs.Holder(i);
                if (holder == null) continue;
                Vector3 radial = Quaternion.AngleAxis(-180f + 360f * (i % Wire.PageSize) / Wire.PageSize, t.forward) * t.up * radius;
                holder.transform.SetPositionAndRotation(t.position + radial + Vector3.up * vertical, Quaternion.LookRotation(-radial, Vector3.up) * offset);
            }
            _arranged = true;
        }

        bool Update()
        {
            if (_v == null) return false;
            bool open = _shrine != null && _shrine.IsOpen;
            int pages = Wire.PageCount(_extent);
            if (_panel != null)
            {
                PlayerController user = open ? Plat.User(_shrine) : null;
                bool enabled = open && pages > 1 && !Busy();
                _panel.Feedback(user, enabled);
            }
            if (open)
            {
                ulong m = Mask();
                if (m != _mask)
                {
                    _mask = m;
                    _dirty = true;
                }
            }
            if (_dirty || open != _wasOpen)
            {
                Apply(open);
                _dirty = false;
                _wasOpen = open;
            }
            if (_panel != null) _panel.Face();
            return true;
        }

        bool Busy()
        {
            try { return _v.IsBeingInteracted(); }
            catch (Exception) { return false; }
        }

        ulong Mask()
        {
            Plat.Holders hs = new Plat.Holders(_v);
            ulong m = 0;
            for (int i = 0; i < hs.Count && i < 64; i++) if (hs.HasOrb(i)) m |= 1UL << i;
            return m;
        }

        void Apply(bool open)
        {
            Restore();
            Plat.Holders hs = new Plat.Holders(_v);
            int owned = 0;
            for (int i = 0; i < hs.Count; i++) if (hs.Hash(i) != 0) owned++;
            if (_panel != null) _panel.Show(open, _page, _extent, owned, _limit);
            for (int i = 0; i < hs.Count; i++)
            {
                if (open && Wire.Visible(i, _page, _extent)) continue;
                Component holder = hs.Holder(i);
                if (holder == null) continue;
                foreach (Renderer r in Plat.All<Renderer>(holder))
                {
                    _hiddenRenderers.Add(r);
                    _rendererStates.Add(r.enabled);
                    r.enabled = false;
                }
                foreach (Collider c in Plat.All<Collider>(holder))
                {
                    _hiddenColliders.Add(c);
                    _colliderStates.Add(c.enabled);
                    c.enabled = false;
                }
            }
        }

        void Restore()
        {
            for (int i = 0; i < _hiddenRenderers.Count; i++) if (_hiddenRenderers[i] != null) _hiddenRenderers[i].enabled = _rendererStates[i];
            for (int i = 0; i < _hiddenColliders.Count; i++) if (_hiddenColliders[i] != null) _hiddenColliders[i].enabled = _colliderStates[i];
            _hiddenRenderers.Clear();
            _rendererStates.Clear();
            _hiddenColliders.Clear();
            _colliderStates.Clear();
        }

        void Dispose()
        {
            try { Restore(); }
            catch (Exception) { }
            if (_panel != null) _panel.Destroy();
            _panel = null;
        }
    }
}
