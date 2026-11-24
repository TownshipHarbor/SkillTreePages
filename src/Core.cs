using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using Il2Cpp;
using GameStream = Il2CppAlta.Serialization.Stream;

[assembly: MelonInfo(typeof(SkillTreePagesQuest.Core), SkillTreePagesQuest.Core.Name, SkillTreePagesQuest.Core.Version, "caleb, ported by Shadow")]
[assembly: MelonGame("Alta", "Att_Quest")]

namespace SkillTreePagesQuest
{
    public sealed class Core : MelonMod
    {
        internal const string Name = "SkillTreePagesQuest";
        internal const string Version = "0.1.0";
        static MelonLogger.Instance _log;
        static bool _running, _reset;
        static readonly Dictionary<string, string> Last = new Dictionary<string, string>();

        internal static void Log(string m) { if (_log != null) _log.Msg(m); }
        internal static void Error(string m) { if (_log != null) _log.Error(m); }

        internal static void Once(string slot, Exception e)
        {
            string m = e.GetType().Name + ": " + e.Message;
            string was;
            if (Last.TryGetValue(slot, out was) && was == m) return;
            Last[slot] = m;
            Error(slot + " threw: " + e);
        }

        public override void OnInitializeMelon()
        {
            _log = LoggerInstance;
            try
            {
                List<string> missing = Plat.Prepare();
                if (missing.Count > 0)
                {
                    Error("this game build lacks " + string.Join(", ", missing.ToArray()) + "; Skill Tree Pages stays off.");
                    return;
                }
                try { Pack.Read(); }
                catch (Exception e) { Error("panel model unreadable, text arrows only: " + e.Message); }
                HarmonyLib.Harmony h = new HarmonyLib.Harmony("skilltreepages.quest");
                if (Patch(h, typeof(BoughtSkillVisualizer), "Serialize", "Before", "After", typeof(GameStream), typeof(PlayerProgressionManager)) == 0)
                {
                    Error("the shrine hook is missing; Skill Tree Pages stays off.");
                    return;
                }
                int extra = Patch(h, typeof(PlayerProgressionManager), "IsSlotAvailable", "Allowed", null);
                extra += Patch(h, typeof(JoinServerPipeline), "ConnectionApprovedByServer", null, "Joined");
                _running = true;
                Log(Version + " started (" + (1 + extra) + "/3 hooks), waiting for a server that runs Skill Tree Pages");
            }
            catch (Exception e)
            {
                Error("failed to start: " + e);
            }
        }

        int Patch(HarmonyLib.Harmony h, Type type, string method, string prefix, string postfix, params Type[] args)
        {
            try
            {
                MethodInfo target = args.Length > 0 ? AccessTools.Method(type, method, args) : AccessTools.Method(type, method);
                if (target == null) throw new MissingMethodException(type.Name + "." + method);
                h.Patch(target, prefix == null ? null : new HarmonyMethod(typeof(Core), prefix), postfix == null ? null : new HarmonyMethod(typeof(Core), postfix));
                return 1;
            }
            catch (Exception e)
            {
                Error("hook " + type.Name + "." + method + " failed: " + e.Message);
                return 0;
            }
        }

        static void Before(BoughtSkillVisualizer __instance, GameStream stream)
        {
            if (!_running) return;
            try
            {
                if (!stream.IsReading || NetworkSceneManager.IsServer) return;
                Shrine.Read(__instance, stream);
            }
            catch (Exception e) { Once("shrine read", e); }
        }

        static void After(BoughtSkillVisualizer __instance, GameStream stream)
        {
            if (!_running) return;
            try
            {
                if (stream.IsReading && !NetworkSceneManager.IsServer) Shrine.Changed(__instance);
            }
            catch (Exception e) { Once("shrine after", e); }
        }

        static void Allowed(PlayerProgressionManager __instance)
        {
            if (!_running || !Shrine.PagesServer || Shrine.RemoteLimit <= 0) return;
            try
            {
                if (!NetworkSceneManager.IsServer) Plat.FixCanBuy(__instance, Shrine.RemoteLimit);
            }
            catch (Exception e) { Once("skill cap", e); }
        }

        static void Joined()
        {
            _reset = true;
        }

        public override void OnLateUpdate()
        {
            if (!_running) return;
            try
            {
                if (_reset)
                {
                    _reset = false;
                    Shrine.Reset();
                }
                Shrine.Tick();
            }
            catch (Exception e) { Once("tick", e); }
        }
    }
}
