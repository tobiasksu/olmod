using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Overload;
using UnityEngine;

namespace GameMod
{

    [HarmonyPatch(typeof(Item), "OnTriggerEnter")]
    class MPWeaponBalance_Item_OnTriggerEnter
    {
        static int CreeperDropAmount(Item item)
        {
            // Stock game dropped 12 on pickup in MP, change to 6 (leave other modes to default inherited 12)
            return GameplayManager.IsMultiplayerActive ? 6 : item.m_amount;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            int state = 0;
            foreach (var code in codes)
            {
                // Patch MISSILE_CREEPER drop amounts for MP
                if (code.opcode == OpCodes.Ldfld && code.operand == AccessTools.Field(typeof(Item), "m_amount"))
                {
                    state++;
                    if (state == 4)
                    {
                        code.opcode = OpCodes.Call;
                        code.operand = AccessTools.Method(typeof(MPWeaponBalance_Item_OnTriggerEnter), "CreeperDropAmount");
                    }
                }


                yield return code;
            }
        }
    }
}
