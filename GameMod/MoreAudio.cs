using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace GameMod
{
    static class MoreAudio
    {
        public static IEnumerable<CodeInstruction> ChangeMaxSources(IEnumerable<CodeInstruction> cs)
        {
            //int n = 0;
            foreach (var c in cs)
            {
                if (c.opcode == OpCodes.Ldc_I4 && (int)c.operand == 128)
                {
                    c.operand = 512;
                    //n++;
                }
                yield return c;
            }
            //Debug.Log(n);
        }
    }

    [HarmonyPatch]
    class MoreAudioIterator
    {
        static MethodBase TargetMethod()
        {
            foreach (var x in typeof(UnityAudio).GetNestedTypes(BindingFlags.NonPublic))
                if (x.Name.Contains("LoadSoundEffects"))
                {
                    //UnityEngine.Debug.Log("Found LoadSoundEffects iterator " + x.Name);
                    return x.GetMethod("MoveNext");
                }
            return null;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> cs) { return MoreAudio.ChangeMaxSources(cs); }
    }

    [HarmonyPatch(typeof(UnityAudio), "LoadSoundEffects")]
    class MoreAudio1 { static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> cs) { return MoreAudio.ChangeMaxSources(cs); } }

    [HarmonyPatch(typeof(UnityAudio), "InitializeForNewLevel")]
    class MoreAudio2 { static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> cs) { return MoreAudio.ChangeMaxSources(cs); } }

    [HarmonyPatch(typeof(UnityAudio), "AdjustSFXPitch")]
    class MoreAudio3 { static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> cs) { return MoreAudio.ChangeMaxSources(cs); } }

    [HarmonyPatch(typeof(UnityAudio), "UpdateAudio")]
    class MoreAudio4 { static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> cs) { return MoreAudio.ChangeMaxSources(cs); } }

    [HarmonyPatch(typeof(UnityAudio), "PauseAllSounds")]
    class MoreAudio5 { static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> cs) { return MoreAudio.ChangeMaxSources(cs); } }

    [HarmonyPatch(typeof(UnityAudio), "RestartSoundsFromLoadGame")]
    class MoreAudio6 { static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> cs) { return MoreAudio.ChangeMaxSources(cs); } }

    [HarmonyPatch(typeof(UnityAudio), "Serialize")]
    class MoreAudio7 { static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> cs) { return MoreAudio.ChangeMaxSources(cs); } }

    [HarmonyPatch(typeof(UnityAudio), "Deserialize")]
    class MoreAudio8 { static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> cs) { return MoreAudio.ChangeMaxSources(cs); } }
    
    /// I don't believe this is properly processing in Harmony2 as it was in Harmony1, handling it in MoreAudio12 now
    [HarmonyPatch(typeof(UnityAudio), MethodType.Constructor)]
    class MoreAudio9 { static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> cs) { return MoreAudio.ChangeMaxSources(cs); } }

    [HarmonyPatch(typeof(UnityAudio), "FindNextOpenAudioSlot")]
    class MoreAudio10 { static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> cs) { return MoreAudio.ChangeMaxSources(cs); } }

    [HarmonyPatch(typeof(UnityAudio), "InitAudio")]
    class MoreAudio12
    {
        static void Prefix(UnityAudio __instance, ref UnityEngine.GameObject[] ___m_a_object, ref UnityEngine.AudioSource[] ___m_a_source, ref float[] ___m_a_original_pitch, ref bool[] ___m_a_was_playing)
        {
            ___m_a_object = new UnityEngine.GameObject[512];
            ___m_a_original_pitch = new float[512];
            ___m_a_source = new UnityEngine.AudioSource[512];
            ___m_a_was_playing = new bool[512];
        }
    }
}
