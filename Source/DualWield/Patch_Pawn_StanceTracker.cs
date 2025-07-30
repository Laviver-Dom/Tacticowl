using HarmonyLib;
using Verse;
using System.Reflection.Emit;
using System.Collections.Generic;
using Settings = Tacticowl.ModSettings_Tacticowl;

namespace Tacticowl.DualWield
{
	[HarmonyPatch(typeof(Pawn_StanceTracker), nameof(Pawn_StanceTracker.FullBodyBusy), MethodType.Getter)]
	class Patch_Pawn_StanceTracker_FullBodyBusy
	{
		static bool Prepare()
        {
            return Settings.dualWieldEnabled;
        }
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			foreach (var instruction in instructions) yield return instruction;
			var stanceBusyMethod = AccessTools.Property(typeof(Stance), nameof(Stance.StanceBusy)).GetGetMethod();
			var offhandBusyMethod = AccessTools.Method(
				typeof(Patch_Pawn_StanceTracker_FullBodyBusy),
				nameof(Patch_Pawn_StanceTracker_FullBodyBusy.OffHandStanceBusy)
			);
			// The original statement is
			// FullBodyBusy => this.stunner.Stunned || this.curStance.StanceBusy;

			// New statement, full body busy 
			// FullBodyBusy => thus.stunner.Stunned || this.curStance.StanceBusy || OffHandStanceBusy
			object returnTrueOperand = null;
			foreach (var instruction in instructions)
			{
				yield return instruction;
				if (instruction.opcode == OpCodes.Brtrue_S)
				{
					returnTrueOperand = instruction.operand;
				}
				if (instruction.opcode == OpCodes.Callvirt && instruction.OperandIs(stanceBusyMethod))
				{
					yield return new CodeInstruction(OpCodes.Brtrue_S, returnTrueOperand);
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Call, offhandBusyMethod);
					yield return new CodeInstruction(OpCodes.Ret);
				}
			}
		}
		public static bool OffHandStanceBusy(Pawn_StanceTracker __instance)
		{
			Pawn pawn = __instance.pawn;
			if (pawn.HasOffHand())
			{
				var stancesOffHand = pawn.GetOffHandStance();
				if (stancesOffHand is Stance_Cooldown && !pawn.RunsAndGuns()) return stancesOffHand.StanceBusy;
			}
			return false;
		}
	}
}