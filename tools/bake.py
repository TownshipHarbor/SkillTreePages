import json
import struct
import sys
import zlib

import UnityPy
from UnityPy.helpers import MeshHelper

CUBE_PATH = 10202
STONE, GLOW = 1, 2


def cube():
    faces = [
        ((0, 0, 1), (1, 0, 0), (0, 1, 0)),
        ((0, 0, -1), (-1, 0, 0), (0, 1, 0)),
        ((0, 1, 0), (1, 0, 0), (0, 0, -1)),
        ((0, -1, 0), (1, 0, 0), (0, 0, 1)),
        ((1, 0, 0), (0, 0, -1), (0, 1, 0)),
        ((-1, 0, 0), (0, 0, 1), (0, 1, 0)),
    ]
    verts, normals, uvs, tris = [], [], [], []
    for n, u, v in faces:
        base = len(verts)
        for su, sv in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
            verts.append(tuple(0.5 * (n[i] + su * u[i] + sv * v[i]) for i in range(3)))
            normals.append(n)
            uvs.append(((su + 1) / 2, (sv + 1) / 2))
        tris += [base, base + 2, base + 1, base, base + 3, base + 2]
    return {"name": "Cube", "v": verts, "n": normals, "uv": uvs, "subs": [tris]}


def read_mesh(m):
    h = MeshHelper.MeshHandler(m)
    h.process()
    subs = [[i for t in sub for i in t] for sub in h.get_triangles()]
    n = list(h.m_Normals or [])
    uv = list(h.m_UV0 or [])
    v = list(h.m_Vertices)
    if len(n) != len(v):
        raise SystemExit("mesh " + m.m_Name + " has no normals")
    if len(uv) != len(v):
        uv = [(0.0, 0.0)] * len(v)
    return {"name": m.m_Name, "v": v, "n": n, "uv": [tuple(x[:2]) for x in uv], "subs": subs}


def component(go, kind):
    for c in go.m_Component:
        ref = c.component if hasattr(c, "component") else c
        if ref.path_id == 0 or ref.file_id != 0:
            continue
        obj = ref.read()
        if obj.object_reader.type.name == kind:
            return obj
    return None


def main(bundle, placement, out):
    env = UnityPy.load(bundle)
    roots = [o.read() for o in env.objects if o.type.name == "Transform" and o.read().m_Father.path_id == 0]
    if len(roots) != 1:
        raise SystemExit("expected one root transform, found " + str(len(roots)))
    meshes, mesh_index, nodes = [], {}, []

    def mesh_of(filt):
        ref = filt.m_Mesh
        key = (ref.file_id, ref.path_id)
        if key not in mesh_index:
            if ref.file_id != 0:
                if ref.path_id != CUBE_PATH:
                    raise SystemExit("unknown built in mesh " + str(key))
                meshes.append(cube())
            else:
                meshes.append(read_mesh(ref.read()))
            mesh_index[key] = len(meshes) - 1
        return mesh_index[key]

    def walk(tr, parent):
        go = tr.m_GameObject.read()
        node = {
            "name": go.m_Name,
            "parent": parent,
            "active": bool(go.m_IsActive),
            "p": (tr.m_LocalPosition.x, tr.m_LocalPosition.y, tr.m_LocalPosition.z),
            "r": (tr.m_LocalRotation.x, tr.m_LocalRotation.y, tr.m_LocalRotation.z, tr.m_LocalRotation.w),
            "s": (tr.m_LocalScale.x, tr.m_LocalScale.y, tr.m_LocalScale.z),
            "mesh": -1,
            "mat": 0,
            "text": None,
        }
        filt = component(go, "MeshFilter")
        text = component(go, "TextMesh")
        if filt is not None:
            node["mesh"] = mesh_of(filt)
            node["mat"] = GLOW if go.m_Name.startswith("ArrowStroke") or go.m_Name == "Crystal" else STONE
        if text is not None:
            c = text.m_Color
            node["text"] = (text.m_Text, text.m_FontSize * text.m_CharacterSize, (c.r, c.g, c.b, c.a))
        nodes.append(node)
        me = len(nodes) - 1
        for ch in tr.m_Children:
            walk(ch.read(), me)

    walk(roots[0], -1)
    place = json.load(open(placement, encoding="utf-8"))

    body = bytearray(b"STP1")

    def i32(x):
        body.extend(struct.pack("<i", x))

    def f32(*xs):
        body.extend(struct.pack("<" + "f" * len(xs), *xs))

    def text(s):
        b = s.encode("utf-8")
        i32(len(b))
        body.extend(b)

    def vec(d):
        return (d["x"], d["y"], d["z"])

    text(place["parentName"])
    f32(*vec(place["previous"]))
    f32(*vec(place["next"]))
    i32(len(meshes))
    for m in meshes:
        text(m["name"])
        i32(len(m["v"]))
        for x in m["v"]:
            f32(*x)
        for x in m["n"]:
            f32(*x)
        for x in m["uv"]:
            f32(*x)
        i32(len(m["subs"]))
        for s in m["subs"]:
            i32(len(s))
            for i in s:
                i32(i)
    i32(len(nodes))
    for n in nodes:
        text(n["name"])
        i32(n["parent"])
        body.append(1 if n["active"] else 0)
        f32(*n["p"])
        f32(*n["r"])
        f32(*n["s"])
        i32(n["mesh"])
        body.append(n["mat"])
        if n["text"] is None:
            body.append(0)
        else:
            body.append(1)
            text(n["text"][0])
            f32(n["text"][1])
            f32(*n["text"][2])
    c = zlib.compressobj(9, zlib.DEFLATED, -15)
    packed = c.compress(bytes(body)) + c.flush()
    with open(out, "wb") as f:
        f.write(struct.pack("<i", len(body)))
        f.write(packed)
    print("baked", len(meshes), "meshes,", len(nodes), "nodes,", len(body), "bytes ->", len(packed) + 4, "bytes")


if __name__ == "__main__":
    if len(sys.argv) != 4:
        raise SystemExit("usage: bake.py <shrine-navigation bundle> <shrine-placement.json> <out panel.pack>")
    main(sys.argv[1], sys.argv[2], sys.argv[3])
