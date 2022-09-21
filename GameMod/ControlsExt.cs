using HarmonyLib;
using Overload;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace GameMod
{
    internal class ControlsExt
    {
        public static int MAX_ARRAY_SIZE = Enum.GetValues(typeof(CCInputExt)).Cast<int>().Max() + 1;

        public static string GetInputName(CCInputExt cc)
        {
            switch (cc)
            {
                case CCInputExt.ToggleLoadoutPrimary:
                    return Loc.LS("TOGGLE LOADOUT PRIMARY");
                default:
                    return Loc.LS("UNKNOWN");
            }
        }
    }

    public enum CCInputExt
    {
        ToggleLoadoutPrimary = 60
    };

    [HarmonyPatch(typeof(Controls), "ClearKBMouse")]
    internal class ControlsExt_Controls_ClearKBMouse
    {
        static void Postfix()
        {
            Controls.m_input_kc[0, (int)CCInputExt.ToggleLoadoutPrimary] = KeyCode.LeftBracket;
            Controls.m_input_kc[1, (int)CCInputExt.ToggleLoadoutPrimary] = KeyCode.None;
        }
    }

    [HarmonyPatch(typeof(PlayerShip), "UpdateReadImmediateControls")]
    internal class ControlsExt_PlayerShip_UpdateReadImmediateControls
    {
        static void Postfix(PlayerShip __instance)
        {
            if (Controls.JustPressed((CCInput)CCInputExt.ToggleLoadoutPrimary) && GameplayManager.IsMultiplayerActive)
            {
                MPLoadouts.ToggleLoadoutPrimary(__instance.c_player);
            }
        }
    }

    [HarmonyPatch(typeof(Player), MethodType.Constructor)]
    internal class ControlsExt_Player_Constructor
    {
        static void Postfix(Player __instance)
        {
            Array.Resize<int>(ref __instance.m_input_count, ControlsExt.MAX_ARRAY_SIZE);
        }
    }

    [HarmonyPatch(typeof(Controls), "InitControl")]
    internal class ControlsExt_Controls_InitControl
    {
        static void Prefix()
        {
            Controls.m_input_joy = new RWInput[2, ControlsExt.MAX_ARRAY_SIZE];
            Controls.m_input_kc = new KeyCode[2, ControlsExt.MAX_ARRAY_SIZE];
            Controls.m_input_count = new int[ControlsExt.MAX_ARRAY_SIZE];
        }
    }

    [HarmonyPatch]
    internal class ControlsExt_PatchArraySizes
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Controls), "UpdateDevice");
            yield return AccessTools.Method(typeof(Controls), "ClearKBMouse");
            yield return AccessTools.Method(typeof(Controls), "ClearJoystickSlotsMenu");
            yield return AccessTools.Method(typeof(Controls), "ClearControlsForController");
            yield return AccessTools.Method(typeof(PlayerShip), "UpdateReadImmediateControls");
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            foreach (var code in codes)
            {
                if (code.opcode == OpCodes.Ldc_I4 && (int)code.operand == 59)
                    code.operand = ControlsExt.MAX_ARRAY_SIZE;

                if (code.opcode == OpCodes.Ldc_I4_S && (sbyte)code.operand == 59)
                    code.operand = (sbyte)ControlsExt.MAX_ARRAY_SIZE;

                yield return code;
            }
        }
    }
}
