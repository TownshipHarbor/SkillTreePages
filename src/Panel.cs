using System;
using System.Collections.Generic;
using UnityEngine;
using Il2Cpp;
using Object = UnityEngine.Object;

namespace SkillTreePagesQuest
{
    internal sealed class Panel
    {
        static readonly Color LabelColor = new Color(0.5f, 0.9f, 1f);
        const float Reach = 0.08f;

        readonly bool _ruins;
        readonly GameObject _root;
        readonly Transform _prev, _next;
        readonly Transform _prevFace, _nextFace;
        readonly Vector3 _prevRest, _nextRest;
        readonly Component _owned, _pages, _counter;
        readonly GameObject _prevButton, _nextButton;
        readonly List<GameObject> _markers = new List<GameObject>();
        readonly List<Renderer> _markerRenderers = new List<Renderer>();
        readonly List<Object> _made = new List<Object>();
        readonly List<GameObject> _plain = new List<GameObject>();
        readonly Dictionary<string, Component> _texts = new Dictionary<string, Component>();
        Material _glow, _muted;
        bool _wasTouching;
        float _nextPulse;

        internal bool Model { get; private set; }

        internal static Panel Create(BoughtSkillVisualizer v)
        {
            Transform t = v.transform;
            while (t != null && t.name != Pack.ParentName && t.name != "ruins_visualise_base") t = t.parent;
            return t != null ? new Panel(t) : new Panel(v.transform, Plat.Radius(v));
        }

        Panel(Transform parent)
        {
            _ruins = true;
            Material stone = null;
            Color light = new Color(0.75f, 1.22f, 1.38f);
            foreach (Renderer r in Plat.All<Renderer>(parent))
            {
                foreach (Material m in Plat.Materials(r))
                {
                    string n = m.name;
                    if (n.StartsWith("Ruins_02_glow_m", StringComparison.Ordinal)) stone = m;
                    if (n.StartsWith("Ruins_shrine_handtouch_LOD0", StringComparison.Ordinal) && m.HasProperty("_EmissionColor")) light = m.GetColor("_EmissionColor");
                }
            }
            if (Pack.Ok && stone != null)
            {
                MakeMaterials(stone, light);
                _root = Build(parent, stone);
                Model = true;
            }
            else
            {
                _root = new GameObject("SkillTreePages Navigation");
                _root.transform.SetParent(parent, false);
                if (Pack.Nodes.Count > 0)
                {
                    PackNode top = Pack.Nodes[0];
                    _root.transform.localPosition = top.Position;
                    _root.transform.localRotation = top.Rotation;
                    _root.transform.localScale = top.Scale;
                }
                Core.Log("shrine panel uses text arrows (" + (stone == null ? "no shrine stone material" : "no panel model") + ")");
            }
            _prev = Anchor("PreviousTouch", Pack.Previous);
            _next = Anchor("NextTouch", Pack.Next);
            _prevFace = _root.transform.Find("PreviousButton/PressFace");
            _nextFace = _root.transform.Find("NextButton/PressFace");
            if (_prevFace != null) _prevRest = _prevFace.localPosition;
            if (_nextFace != null) _nextRest = _nextFace.localPosition;
            Transform b = _root.transform.Find("PreviousButton");
            if (b != null) _prevButton = b.gameObject;
            b = _root.transform.Find("NextButton");
            if (b != null) _nextButton = b.gameObject;
            for (int i = 0; i < Wire.PageSize; i++)
            {
                Transform m = _root.transform.Find("PageMarkers/Page_" + (i + 1));
                List<Renderer> rs = Plat.All<Renderer>(m);
                _markers.Add(m == null ? null : m.gameObject);
                _markerRenderers.Add(rs.Count > 0 ? rs[0] : null);
            }
            if (Model)
            {
                _owned = Lookup("OwnedCount");
                _pages = Lookup("PageCount");
            }
            else
            {
                Text(_prev.gameObject, "<", 0.288f, LabelColor);
                Text(_next.gameObject, ">", 0.288f, LabelColor);
                _owned = Text(Child(_root.transform, "OwnedCount", new Vector3(0f, 0.03f, -0.09f)), "", 0.288f, LabelColor);
                _pages = Text(Child(_root.transform, "PageCount", new Vector3(0f, -0.03f, -0.09f)), "", 0.288f, LabelColor);
            }
            _root.SetActive(false);
        }

        Panel(Transform visualizer, float radius)
        {
            _ruins = false;
            GameObject p = Child(visualizer, "SkillTreePages Previous", new Vector3(-radius - 0.13f, 0f, 0f));
            GameObject n = Child(visualizer, "SkillTreePages Next", new Vector3(radius + 0.13f, 0f, 0f));
            GameObject c = Child(visualizer, "SkillTreePages Skills", new Vector3(0f, -radius - 0.14f, 0f));
            _prev = p.transform;
            _next = n.transform;
            Text(p, "Previous", 0.384f, LabelColor);
            Text(n, "Next", 0.384f, LabelColor);
            _counter = Text(c, "Skills", 0.384f, LabelColor);
            _plain.Add(p);
            _plain.Add(n);
            _plain.Add(c);
            foreach (GameObject g in _plain) g.SetActive(false);
        }

        void MakeMaterials(Material stone, Color light)
        {
            Shader color = Shader.Find("Unlit/Color");
            Shader texture = color == null ? Shader.Find("Unlit/Texture") : null;
            _glow = Tint(color, texture, stone, light);
            _muted = Tint(color, texture, stone, light * 0.18f);
        }

        Material Tint(Shader color, Shader texture, Material stone, Color c)
        {
            Material m;
            if (color != null)
            {
                m = new Material(color);
                m.color = c;
            }
            else if (texture != null)
            {
                m = new Material(texture);
                Texture2D t = Plat.Swatch(c);
                _made.Add(t);
                m.mainTexture = t;
            }
            else
            {
                m = new Material(stone);
                m.color = c;
            }
            m.hideFlags = HideFlags.DontUnloadUnusedAsset;
            _made.Add(m);
            return m;
        }

        GameObject Build(Transform parent, Material stone)
        {
            List<GameObject> made = new List<GameObject>();
            for (int i = 0; i < Pack.Nodes.Count; i++)
            {
                PackNode d = Pack.Nodes[i];
                GameObject g = new GameObject(d.Name);
                made.Add(g);
                g.transform.SetParent(d.Parent < 0 ? parent : made[d.Parent].transform, false);
                if (d.Mesh >= 0)
                {
                    MeshFilter f = Plat.Add<MeshFilter>(g);
                    f.sharedMesh = Pack.Mesh(d.Mesh);
                    MeshRenderer r = Plat.Add<MeshRenderer>(g);
                    r.sharedMaterial = d.Material == Pack.Glow ? _glow : stone;
                }
                if (d.Text != null)
                {
                    Component c = Text(g, d.Text, d.TextSize, d.TextColor);
                    if (c != null) _texts[d.Name] = c;
                }
                g.transform.localPosition = d.Position;
                g.transform.localRotation = d.Rotation;
                g.transform.localScale = d.Scale;
                g.SetActive(d.Active);
            }
            return made[0];
        }

        Component Lookup(string name)
        {
            Component c;
            return _texts.TryGetValue(name, out c) ? c : null;
        }

        Transform Anchor(string name, Vector3 local)
        {
            return Child(_root.transform, name, local).transform;
        }

        static GameObject Child(Transform parent, string name, Vector3 local)
        {
            GameObject g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = local;
            return g;
        }

        static Component Text(GameObject g, string text, float size, Color color)
        {
            try { return Plat.Label(g, text, size, color); }
            catch (Exception e)
            {
                Core.Error("panel text " + g.name + ": " + e.Message);
                return null;
            }
        }

        internal void Show(bool open, int page, int extent, int owned, int limit)
        {
            int count = Wire.PageCount(extent);
            if (!_ruins)
            {
                _plain[0].SetActive(open && extent > Wire.PageSize);
                _plain[1].SetActive(open && extent > Wire.PageSize);
                _plain[2].SetActive(open);
                if (_counter != null) Plat.SetText(_counter, "Skills " + (page + 1) + " / " + count + "\nTouch Previous / Next");
                return;
            }
            _root.SetActive(open);
            if (_owned != null) Plat.SetText(_owned, owned + " / " + limit);
            if (_pages != null) Plat.SetText(_pages, (page + 1) + " / " + count);
            if (_prevButton != null) _prevButton.SetActive(count > 1);
            if (_nextButton != null) _nextButton.SetActive(count > 1);
            for (int i = 0; i < Wire.PageSize; i++)
            {
                if (_markers[i] == null) continue;
                _markers[i].SetActive(i < count);
                if (_markerRenderers[i] != null && _glow != null) _markerRenderers[i].sharedMaterial = i == page ? _glow : _muted;
            }
        }

        internal void Face()
        {
            if (_ruins) return;
            PlayerController local = PlayerController.Current;
            Transform head = local == null ? null : local.Head;
            if (head == null) return;
            foreach (GameObject g in _plain)
            {
                if (!g.activeSelf) continue;
                Vector3 d = g.transform.position - head.position;
                if (d.sqrMagnitude > 1e-6f) g.transform.rotation = Quaternion.LookRotation(d);
            }
        }

        internal void Feedback(PlayerController player, bool enabled)
        {
            if (!_ruins) return;
            Controller left = player == null ? null : player.LeftController;
            Controller right = player == null ? null : player.RightController;
            bool lp = enabled && Touches(left, _prev), rp = enabled && Touches(right, _prev);
            bool ln = enabled && Touches(left, _next), rn = enabled && Touches(right, _next);
            bool previous = lp || rp, next = ln || rn;
            bool touching = previous || next;
            if (touching && !_wasTouching && previous != next && Time.unscaledTime >= _nextPulse)
            {
                _nextPulse = Time.unscaledTime + 0.5f;
                PlayerController local = PlayerController.Current;
                if (player != null && Plat.Same(player, local) && player.IsLocalPlayer)
                {
                    if (lp || ln) left.TriggerHaptic(0.35f, 0.045f, 90f);
                    if (rp || rn) right.TriggerHaptic(0.35f, 0.045f, 90f);
                }
            }
            _wasTouching = touching;
            Animate(_prevFace, _prevRest, previous && !next);
            Animate(_nextFace, _nextRest, next && !previous);
        }

        static bool Touches(Controller hand, Transform target)
        {
            return hand != null && target != null && (hand.transform.position - target.position).sqrMagnitude < Reach * Reach;
        }

        static void Animate(Transform face, Vector3 rest, bool pressed)
        {
            if (face == null) return;
            face.localPosition = Vector3.MoveTowards(face.localPosition, rest + (pressed ? new Vector3(0f, 0f, 0.012f) : Vector3.zero), Time.unscaledDeltaTime * 0.12f);
        }

        internal void Destroy()
        {
            if (_root != null) Object.Destroy(_root);
            foreach (GameObject g in _plain) if (g != null) Object.Destroy(g);
            foreach (Object o in _made) if (o != null) Object.Destroy(o);
            _made.Clear();
        }
    }
}
