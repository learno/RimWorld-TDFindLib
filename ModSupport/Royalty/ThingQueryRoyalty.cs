﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TD_Find_Lib;
using Verse;
using RimWorld;

namespace TDFindLib_Royalty
{
	// It is a bit roundabout to have a project for this when there is no new dll dependency but, eh, organization I guess.
	public class ThingQueryRoyalTitle : ThingQueryDropDown<RoyalTitleDef>
	{
		public override bool AppliesDirectlyTo(Thing thing)
		{
			Pawn pawn = thing as Pawn;
			if (pawn == null || pawn.royalty == null) return false;

			if (sel == null)
				return pawn.royalty.AllTitlesForReading.Count == 0;

			return pawn.royalty?.HasTitle(sel) ?? false;
		}

		public override string NullOption() => "None".Translate();
	}

	public class ThingQueryPsyfocus : ThingQueryFloatRange
	{
		public override bool AppliesDirectlyTo(Thing thing)
		{
			Pawn pawn = thing as Pawn;
			if (pawn == null || pawn.psychicEntropy == null) return false;


			return sel.Includes(pawn.psychicEntropy.CurrentPsyfocus);
		}
	}

	public class ThingQueryEntropyValue : ThingQueryFloatRange
	{
		public override float Max => 80f;   //Base 30 * max buff of 2.667
		public override ToStringStyle Style => ToStringStyle.Integer;

		public override bool AppliesDirectlyTo(Thing thing)
		{
			Pawn pawn = thing as Pawn;
			if (pawn == null || pawn.psychicEntropy == null) return false;


			return sel.Includes(pawn.psychicEntropy.EntropyValue);
		}
	}

	[StaticConstructorOnStartup]
	public static class ExpansionHider
	{
		static ExpansionHider()
		{
			if (!ModsConfig.RoyaltyActive)
				foreach (ThingQueryDef def in DefDatabase<ThingQueryDef>.AllDefsListForReading)
					if (def.mod == "Ludeon.Rimworld.Royalty")
						def.devOnly = true;
		}
	}
}
