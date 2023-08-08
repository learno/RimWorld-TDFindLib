﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Verse;
using RimWorld;
using UnityEngine;

namespace TD_Find_Lib
{
	public enum QueryPawnProperty
	{
		IsColonist
	, IsFreeColonist
	, IsPrisonerOfColony
	, IsSlaveOfColony
	, IsPrisoner
	, IsSlave
	, IsColonyMech
	, Downed
	, Dead
	, HasPsylink
	}
	public class ThingQueryBasicProperty : ThingQueryDropDown<QueryPawnProperty>
	{
		public override bool AppliesDirectlyTo(Thing thing) =>
			thing is Pawn pawn ? sel switch
			{
				QueryPawnProperty.IsColonist => pawn.IsColonist
			, QueryPawnProperty.IsFreeColonist => pawn.IsFreeColonist
			, QueryPawnProperty.IsPrisonerOfColony => pawn.IsPrisonerOfColony
			, QueryPawnProperty.IsSlaveOfColony => pawn.IsSlaveOfColony
			, QueryPawnProperty.IsPrisoner => pawn.IsPrisoner
			, QueryPawnProperty.IsSlave => pawn.IsSlave
			, QueryPawnProperty.IsColonyMech => pawn.IsColonyMech
			, QueryPawnProperty.Downed => pawn.Downed
			, QueryPawnProperty.Dead => pawn.Dead
			, QueryPawnProperty.HasPsylink => pawn.HasPsylink
			,	_ => false
			} : false;
	}

	public class ThingQueryDying : ThingQueryIntRange
	{
		public override int Min => 0;
		public override int Max => GenDate.TicksPerDay * 2;
		public override Func<int, string> Writer => ticks => $"{ticks * 1f / GenDate.TicksPerHour:0.0}";

		public ThingQueryDying() => selByRef.max = GenDate.TicksPerDay;


		public override bool AppliesDirectlyTo(Thing thing)
		{
			Pawn pawn = thing as Pawn;
			if (pawn == null) return false;

			int eta = HealthUtility.TicksUntilDeathDueToBloodLoss(pawn);

			// Unbounded range includes >max when the max is set to max...
			// but we don't actually want to incude IntMax
			if (eta == int.MaxValue)
				return false;

			return sel.Includes(eta);
		}


		public override bool DrawMain(Rect rect, bool locked, Rect fullRect)
		{
			if (base.DrawMain(rect, locked, fullRect))
			{
				DirtyLabel();
				return true;
			}
			return false;
		}

		protected override string MakeLabel()
		{
			string rangeLabel = sel.min == 0 ? "" : $"{sel.min.ToStringTicksToPeriod(shortForm: true)} - ";//notranslate
			
			if (sel.max == sel.absRange.max)
				rangeLabel += "AnyLower".Translate();
			else 
				rangeLabel += sel.max.ToStringTicksToPeriod(shortForm: true);

			return "TimeToDeath".Translate(rangeLabel).CapitalizeFirst();
		}
	}

	public class ThingQuerySkill : ThingQueryDropDown<SkillDef>
	{
		IntRangeUB skillRange = new(SkillRecord.MinLevel, SkillRecord.MaxLevel);
		int passion = 4;

		static string[] passionText = new string[]
		{ "PassionNone", "PassionMinor", "PassionMajor", "TD.AnyPassion", "TD.IgnorePassion" };//notranslate
		public static string GetPassionText(int x) => passionText[x].Translate();
		public static Color GetPassionFloatColor(int x) =>
			x switch { 0 => Color.red, 3 => Color.yellow, _ => Color.white };


		public ThingQuerySkill()
		{
			sel = SkillDefOf.Animals;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref skillRange.range, "skillRange");
			Scribe_Values.Look(ref passion, "passion");
		}
		protected override ThingQuery Clone()
		{
			ThingQuerySkill clone = (ThingQuerySkill)base.Clone();
			clone.skillRange = skillRange;
			clone.passion = passion;
			return clone;
		}
		public override int ExtraOptionsCount => 1;
		public override string NameForExtra(int ex) => "TD.AnyOption".Translate();

		public override bool AppliesDirectlyTo(Thing thing)
		{
			Pawn_SkillTracker skills = (thing as Pawn)?.skills;
			if (skills == null) return false;

			return extraOption == 1 ? skills.skills.Any(AppliesRecord) :
				skills.GetSkill(sel) is SkillRecord rec && AppliesRecord(rec);
		}

		private bool AppliesRecord(SkillRecord rec) =>
			!rec.TotallyDisabled &&
			skillRange.Includes(rec.Level) &&
			(passion == 4 ? true :
			 passion == 3 ? rec.passion != Passion.None :
			 (int)rec.passion == passion);


		public override bool DrawCustom(Rect fullRect)
		{
			if (row.ButtonText(GetPassionText(passion)))
			{
				DoFloatOptions(Enumerable.Range(0, 5).Select(
					p => new FloatMenuOptionAndRefresh(GetPassionText(p), () => passion = p, this, GetPassionFloatColor(p)) as FloatMenuOption).ToList());
			}

			return TDWidgets.IntRangeUB(fullRect.RightHalfClamped(row.FinalX), id, ref skillRange);
		}
	}

	public class ThingQueryTrait : ThingQueryDropDown<TraitDef>
	{
		int traitDegree;

		public ThingQueryTrait()
		{
			sel = TraitDefOf.Beauty;  //Todo: beauty shows even if it's not on map
		}
		protected override void PostChosen()
		{
			traitDegree = sel.degreeDatas.First().degree;
		}

		public override string NameFor(TraitDef def) =>
			def.degreeDatas.Count == 1
				? def.degreeDatas.First().LabelCap
				: def.GetLabel() + "*";//TraitDefs don't have labels

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref traitDegree, "traitDegree");
		}
		protected override ThingQuery Clone()
		{
			ThingQueryTrait clone = (ThingQueryTrait)base.Clone();
			clone.traitDegree = traitDegree;
			return clone;
		}

		public override bool AppliesDirectlyTo(Thing thing)
		{
			Pawn pawn = thing as Pawn;
			if (pawn == null) return false;

			return pawn.story?.traits.GetTrait(sel) is Trait trait &&
				trait.Degree == traitDegree;
		}

		public override IEnumerable<TraitDef> AvailableOptions() =>
			ContentsUtility.AvailableInGame(t => (t as Pawn)?.story?.traits.allTraits.Select(tr => tr.def));

		public override bool Ordered => true;

		public override bool DrawCustom(Rect fullRect)
		{
			if (sel == null) return false;

			if (sel.degreeDatas.Count > 1)
			{ 
				RowButtonFloatMenu(sel.DataAtDegree(traitDegree),
					sel.degreeDatas,
					deg => deg.LabelCap,
					newValue => traitDegree = newValue.degree);
			}
			return false;
		}
	}

	public class ThingQueryThought: ThingQueryDropDown<ThoughtDef>
	{
		IntRange stageRange;	//Indexes of orderedStages
		List<int> orderedStages = new();

		// There is a confusing translation between stage index and ordered index.
		// The xml defines stages inconsistently so we order them to orderedStages
		// The selection is done with the ordered index
		// But of course this has to be translated from and to the actual stage index

		public ThingQueryThought()
		{
			sel = ThoughtDefOf.AteWithoutTable;
		}


		// stageI = orderedStages[orderI], so
		// gotta reverse index search to find orderI from stageI
		private int OrderedIndex(int stageI) =>
			orderedStages.IndexOf(stageI);

		private bool Includes(int stageI) =>
			stageRange.Includes(OrderedIndex(stageI));


		// Multistage UI is only shown when when there's >1 stage
		// Some hidden stages are not shown (unless you have godmode on)
		public static bool VisibleStage(ThoughtStage stage) =>
			DebugSettings.godMode || (stage?.visible ?? false);

		public static bool ShowMultistage(ThoughtDef def) =>
			def.stages.Count(VisibleStage) > 1;

		public List<int> SelectableStages =>
			orderedStages.Where(i => VisibleStage(sel.stages[i])).ToList();

		protected override void PostProcess()
		{
			orderedStages.Clear();
			if (sel == null) return;

			orderedStages.AddRange(Enumerable.Range(0, sel.stages.Count)
				.OrderBy(i => sel.stages[i] == null ? i : i + 1000 * sel.stages[i].baseOpinionOffset + 1000000 * sel.stages[i].baseMoodEffect));
		}

		protected override void PostChosen()
		{
			stageRange = new IntRange(0, SelectableStages.Count - 1);
		}

		public override void ExposeData()
		{
			base.ExposeData();

			Scribe_Values.Look(ref stageRange, "stageRange");
		}
		protected override ThingQuery Clone()
		{
			ThingQueryThought clone = (ThingQueryThought)base.Clone();
			clone.stageRange = stageRange;
			return clone;
		}

		public override bool AppliesDirectlyTo(Thing thing)
		{
			Pawn pawn = thing as Pawn;
			if (pawn == null) return false;

			if (pawn.needs?.TryGetNeed<Need_Mood>() is Need_Mood mood)
			{
				//memories
				if (mood.thoughts.memories.Memories.Any(t => t.def == sel && Includes(t.CurStageIndex)))
					return true;

				//situational
				List<Thought> thoughts = new();
				mood.thoughts.situational.AppendMoodThoughts(thoughts);
				if (thoughts.Any(t => t.def == sel && Includes(t.CurStageIndex)))
					return true;
			}
			return false;
		}

		const string goodColor = "#d0ffd0", badColor ="#ffd0d0";
		public static void AppendEffect(StringBuilder sb, ThoughtStage stage)
		{
			int mood = (int)stage.baseMoodEffect;
			if (mood != 0)
			{
				bool good = mood > 0;
				sb.Append("<color=");
				sb.Append(good ? goodColor : badColor);
				sb.Append(">");

				sb.Append(" (");
				if (good)
					sb.Append("+");
				sb.Append(mood);
				sb.Append(" ");
				sb.Append("TD.Mood".Translate());
				sb.Append(")");

				sb.Append("</color>");
			}

			int opinion = (int)stage.baseOpinionOffset;
			if (opinion != 0)
			{
				bool good = opinion > 0;
				sb.Append("<color=");
				sb.Append(good ? goodColor : badColor);
				sb.Append(">");

				sb.Append(" (");
				if (opinion > 0)
					sb.Append("+");
				sb.Append(opinion);
				sb.Append(" ");
				sb.Append("TD.Opinion".Translate());
				sb.Append(")");

				sb.Append("</color>");
			}
		}

		public override string NameFor(ThoughtDef def)
		{
			StringBuilder sb = new();
			ThoughtStage stage = def.stages.FirstOrDefault(d => d?.label != null);
			string label =
				def.label?.CapitalizeFirst() ??
				stage?.label.CapitalizeFirst() ??
				"???";

			label = Regex.Replace(label, "{\\d}", "_");
			sb.Append(label);

			if (ShowMultistage(def))
				sb.Append("*");
			else if (stage != null)
				AppendEffect(sb, stage);

			if(def.gender != Gender.None)
			{
				sb.Append(" (");
				sb.Append(def.gender == Gender.Male ? "Male".Translate() : "Female".Translate());
				sb.Append(")");
			}

			return sb.ToString();
		}

		public override string TipForSel()
		{
			StringBuilder sb = new();
			if (!ShowMultistage(sel))
			{
				sb.AppendLine(GetSelLabel());

				if (sel.stages?[0]?.description != null)
					sb.AppendLine(sel.stages[0].description);
			}
			sb.Append("<color=#808080>(");
			sb.Append(sel.defName); // .SplitCamelCase()); actually looks worse 
			sb.Append(")</color>");

			return sb.ToString();
		}

		public override IEnumerable<ThoughtDef> AvailableOptions() =>
			ContentsUtility.AvailableInGame(ThoughtsForThing);

		public override bool Ordered => true;

		private string NameForStage(int stageI)
		{
			ThoughtStage stage = sel.stages[stageI];
			if (stage == null || !stage.visible)
				return "TD.Invisible".Translate();

			StringBuilder sb = new(Regex.Replace(stage.label, "{\\d}", "_").CapitalizeFirst());

			AppendEffect(sb, stage);

			return sb.ToString();
		}

		private string TipForStage(int stageI) =>
			sel.stages[stageI]?.description;

		protected override bool DrawUnder(Listing_StandardIndent listing, bool locked)
		{
			if (sel == null) return false;

			if (!ShowMultistage(sel)) return false;

			//Buttons apparently are too tall for the line height?
			listing.Gap(listing.verticalSpacing);

			listing.NestedIndent();
			Rect nextRect = listing.GetRect(Text.LineHeight);
			listing.NestedOutdent();

			WidgetRow underRow = new(nextRect.x, nextRect.y);
			
			underRow.Label("TD.From".Translate());
			DoStageDropdown(underRow, stageRange.min, i => stageRange.min = i);

			underRow.Label("RangeTo".Translate());
			DoStageDropdown(underRow, stageRange.max, i => stageRange.max = i);
			
			return false;
		}

		private void DoStageDropdown(WidgetRow stageRow, int setI, Action<int> selectedAction)
		{
			int setStageI = orderedStages[setI];
			if (stageRow.ButtonTextNoGap(NameForStage(setStageI), TipForStage(setStageI)))
			{
				DoFloatOptions(SelectableStages, NameForStage, newValue => selectedAction(OrderedIndex(newValue)));
			}
		}

		public static IEnumerable<ThoughtDef> ThoughtsForThing(Thing t)
		{
			Pawn pawn = t as Pawn;
			if (pawn == null) yield break;

			IEnumerable<ThoughtDef> memories = pawn.needs?.TryGetNeed<Need_Mood>()?.thoughts.memories.Memories.Where(th => VisibleStage(th.CurStage)).Select(th => th.def);
			if (memories != null)
				foreach (ThoughtDef def in memories)
					yield return def;

			List<Thought> thoughts = new();
			pawn.needs?.TryGetNeed<Need_Mood>()?.thoughts.situational.AppendMoodThoughts(thoughts);
			foreach (Thought thought in thoughts.Where(th => VisibleStage(th.CurStage)))
				yield return thought.def;
		}
	}

	public class ThingQueryNeed : ThingQueryDropDown<NeedDef>
	{
		FloatRangeUB needRange = new(0, 1, 0, 0.5f);

		public ThingQueryNeed() => sel = NeedDefOf.Food;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref needRange.range, "needRange");
		}
		protected override ThingQuery Clone()
		{
			ThingQueryNeed clone = (ThingQueryNeed)base.Clone();
			clone.needRange = needRange;
			return clone;
		}

		public override bool AppliesDirectlyTo(Thing thing) =>
			thing is Pawn pawn &&
			(pawn.RaceProps.Animal || pawn.Faction != null || DebugSettings.godMode) &&
				pawn.needs?.TryGetNeed(sel) is Need need && needRange.Includes(need.CurLevelPercentage);

		public override bool DrawCustom(Rect fullRect)
		{
			return TDWidgets.FloatRangeUB(fullRect.RightHalfClamped(row.FinalX), id, ref needRange, valueStyle: ToStringStyle.PercentOne);
		}
	}

	public class ThingQueryHealth : ThingQueryDropDown<HediffDef>
	{
		FloatRangeUB severityRange;//unknown until sel set
		bool usesSeverity;

		public ThingQueryHealth()
		{
			sel = null;
		}
		protected override void PostProcess()
		{
			FloatRange? r = SeverityRangeFor(sel);
			usesSeverity = r.HasValue;
			if (usesSeverity)
				severityRange.absRange = r.Value;
		}
		protected override void PostChosen()
		{
			if (SeverityRangeFor(sel) is FloatRange range)
				severityRange.range = range;
		}

		public override void ExposeData()
		{
			base.ExposeData();

			Scribe_Values.Look(ref severityRange.range, "severityRange");
		}
		protected override ThingQuery Clone()
		{
			ThingQueryHealth clone = (ThingQueryHealth)base.Clone();
			clone.severityRange = severityRange;
			clone.usesSeverity = usesSeverity;
			return clone;
		}

		public override bool AppliesDirectlyTo(Thing thing)
		{
			Pawn pawn = thing as Pawn;
			if (pawn == null) return false;

			return
				extraOption == 1 ? pawn.health.hediffSet.hediffs.Any(h => h.Visible || DebugSettings.godMode) :
				extraOption == 2 ? pawn.health.hediffSet.hediffs.Any(h => (h.Visible || DebugSettings.godMode) && h.Bleeding) :
				extraOption == 3 ? pawn.health.hediffSet.hediffs.Any(h => (h.Visible || DebugSettings.godMode) && h.TendableNow()) :
				sel == null ? !pawn.health.hediffSet.hediffs.Any(h => h.Visible || DebugSettings.godMode) :
				(pawn.health.hediffSet.GetFirstHediffOfDef(sel, !DebugSettings.godMode) is Hediff hediff &&
				(!usesSeverity || severityRange.Includes(hediff.Severity)));
		}

		public override string NullOption() => "None".Translate();
		public override IEnumerable<HediffDef> AvailableOptions() =>
			ContentsUtility.AvailableInGame(t => (t as Pawn)?.health.hediffSet.hediffs.Select(h => h.def));

		public override bool Ordered => true;

		public override int ExtraOptionsCount => 3;
		public override string NameForExtra(int ex) =>
			ex switch
			{
				1 => "TD.AnyOption".Translate(),
				2 => "TD.AnyBleeding".Translate(),
				_ => "CanTendNow".Translate()
			};

		public override bool DrawCustom(Rect fullRect)
		{
			if (sel == null || !usesSeverity) return false;


			//<initialSeverity>1</initialSeverity> <!-- Severity is bound to level of implant -->
			if (typeof(Hediff_Level).IsAssignableFrom(sel.hediffClass))
				return TDWidgets.IntRangeUB(fullRect.RightHalfClamped(row.FinalX), id, ref severityRange);



			return TDWidgets.FloatRangeUB(fullRect.RightHalfClamped(row.FinalX), id, ref severityRange, valueStyle: ToStringStyle.PercentZero);

			// Injuries should be ToStringStyle.Float but they do not have a max/lethal value so do not display their severity here
		}

		public static FloatRange? SeverityRangeFor(HediffDef hediffDef)
		{
			if (hediffDef == null) return null;

			float min = hediffDef.minSeverity;
			float max = hediffDef.maxSeverity;
			if (hediffDef.lethalSeverity != -1f)
				max = Math.Min(max, hediffDef.lethalSeverity);

			if (max == float.MaxValue) return null;
			return new FloatRange(min, max);
		}
	}

	public class ThingQueryIncapable : ThingQueryDropDown<WorkTags>
	{
		public override string NameFor(WorkTags tags) =>
			tags.LabelTranslated().CapitalizeFirst();

		public override bool AppliesDirectlyTo(Thing thing)
		{
			Pawn pawn = thing as Pawn;
			if (pawn == null) return false;

			return 
				extraOption == 1 ? pawn.CombinedDisabledWorkTags != WorkTags.None :
				sel == WorkTags.None ? pawn.CombinedDisabledWorkTags == WorkTags.None :
				pawn.WorkTagIsDisabled(sel);
		}

		public override int ExtraOptionsCount => 1;
		public override string NameForExtra(int ex) => "TD.AnyOption".Translate();
	}

	public class ThingQueryTemp : ThingQueryFloatRange
	{
		public ThingQueryTemp() => _sel.range = new FloatRange(10, 30);

		public override float Min => -100f;
		public override float Max => 100f;
		public override ToStringStyle Style => ToStringStyle.Temperature;

		public override bool AppliesDirectlyTo(Thing thing) =>
			sel.Includes(thing.AmbientTemperature);
	}

	public enum ComfyTemp { Cold, Cool, Okay, Warm, Hot }
	public class ThingQueryComfyTemp : ThingQueryDropDown<ComfyTemp>
	{
		public override bool AppliesDirectlyTo(Thing thing)
		{
			Pawn pawn = thing as Pawn;
			if (pawn == null) return false;

			float temp = pawn.AmbientTemperature;
			FloatRange safeRange = pawn.SafeTemperatureRange();
			FloatRange comfRange = pawn.ComfortableTemperatureRange();
			switch (sel)
			{
				case ComfyTemp.Cold: return temp < safeRange.min;
				case ComfyTemp.Cool: return temp >= safeRange.min && temp < comfRange.min;
				case ComfyTemp.Okay: return comfRange.Includes(temp);
				case ComfyTemp.Warm: return temp <= safeRange.max && temp > comfRange.max;
				case ComfyTemp.Hot: return temp > safeRange.max;
			}
			return false;//???
		}
		public override string NameFor(ComfyTemp o)
		{
			switch (o)
			{
				case ComfyTemp.Cold: return "TD.Cold".Translate();
				case ComfyTemp.Cool: return "TD.ALittleCold".Translate();
				case ComfyTemp.Okay: return "TD.Comfortable".Translate();
				case ComfyTemp.Warm: return "TD.ALittleHot".Translate();
				case ComfyTemp.Hot: return "TD.Hot".Translate();
			}
			return "???";
		}
	}

	public class ThingQueryRestricted : ThingQueryDropDown<Area>
	{
		protected override Area ResolveRef(Map map) =>
			map.areaManager.GetLabeled(selName);

		public override bool AppliesDirectlyTo(Thing thing)
		{
			Area selectedArea = extraOption == 1 ? thing.MapHeld.areaManager.Home : sel;
			return thing is Pawn pawn && pawn.playerSettings is Pawn_PlayerSettings set && set.AreaRestriction == selectedArea;
		}

		public override string NullOption() => "NoAreaAllowed".Translate();

		public override IEnumerable<Area> AllOptions() =>
			Find.CurrentMap?.areaManager.AllAreas.Where(a => a is Area_Allowed);//a.AssignableAsAllowed());

		public override string NameFor(Area o) => o.Label;

		public override int ExtraOptionsCount => 1;
		public override string NameForExtra(int ex) => "Home".Translate();
	}

	public class ThingQueryMentalState : ThingQueryDropDown<MentalStateDef>
	{
		public override bool AppliesDirectlyTo(Thing thing)
		{
			Pawn pawn = thing as Pawn;
			if (pawn == null) return false;

			return
				extraOption == 1 ? pawn.MentalState != null: 
				extraOption == 2 ? pawn.MentalState?.def is MentalStateDef def && def.IsAggro : 
				sel == null ? pawn.MentalState == null : 
				pawn.MentalState?.def is MentalStateDef sDef && sDef == sel;
		}

		public override IEnumerable<MentalStateDef> AvailableOptions() =>
			ContentsUtility.AvailableInGame(t => (t as Pawn)?.MentalState?.def);

		public override bool Ordered => true;

		public override string NullOption() => "None".Translate();

		public override int ExtraOptionsCount => 2;
		public override string NameForExtra(int ex) =>
			ex == 1 ? "TD.AnyOption".Translate() : "TD.AnyAggresive".Translate();
	}

	public class ThingQueryPrisoner : ThingQueryDropDown<PrisonerInteractionModeDef>
	{
		public ThingQueryPrisoner()
		{
			sel = PrisonerInteractionModeDefOf.NoInteraction;
		}

		public override bool AppliesDirectlyTo(Thing thing)
		{
			if (extraOption == 2)
				return thing.GetRoom()?.IsPrisonCell ?? false;

			Pawn pawn = thing as Pawn;
			if (pawn == null) return false;

			if (extraOption == 1)
				return pawn.IsPrisoner;

			return pawn.IsPrisoner && pawn.guest?.interactionMode == sel;
		}
		
		public override int ExtraOptionsCount => 2;
		public override string NameForExtra(int ex) =>
			ex == 1 ? "TD.IsPrisoner".Translate() : "TD.InCell".Translate();
	}

	// the option here is default true = "Lodger", true= "Helper"
	public class ThingQueryQuest : ThingQueryWithOption<bool>
	{
		public override bool AppliesDirectlyTo(Thing thing)
		{
			Pawn pawn = thing as Pawn;
			if (pawn == null) return false;

			return sel ? pawn.IsQuestHelper() : pawn.IsQuestLodger();
		}

		public override bool DrawMain(Rect rect, bool locked, Rect fullRect)
		{
			base.DrawMain(rect, locked, fullRect);
			Rect buttRect = fullRect.RightPartClamped(0.4f, Text.CalcSize(Label).x);
			string label = sel ? "TD.IsHelper".Translate() : "TD.IsLodger".Translate();
			if (Widgets.ButtonText(buttRect, label))
			{
				sel = !sel;
				return true;
			}
			return false;
		}
	}

	public enum DraftQuery { Drafted, Undrafted, Controllable }
	public class ThingQueryDrafted : ThingQueryDropDown<DraftQuery>
	{
		public override bool AppliesDirectlyTo(Thing thing)
		{
			Pawn pawn = thing as Pawn;
			if (pawn == null) return false;

			switch (sel)
			{
				case DraftQuery.Drafted: return pawn.Drafted;
				case DraftQuery.Undrafted: return pawn.drafter != null && !pawn.Drafted;
				case DraftQuery.Controllable: return pawn.drafter != null;
			}
			return false;
		}
	}

	public class ThingQueryJob : ThingQueryDropDown<JobDef>
	{
		public override bool AppliesDirectlyTo(Thing thing)
		{
			Pawn pawn = thing as Pawn;
			if (pawn == null) return false;

			return pawn.CurJobDef == sel;
		}

		public override string NameFor(JobDef o) =>
			Regex.Replace(o.reportString.Replace(".",""), "Target(A|B|C)", "...");

		public override string NullOption() => "None".Translate();

		public override IEnumerable<JobDef> AvailableOptions() =>
			ContentsUtility.AvailableInGame(t => (t as Pawn)?.CurJobDef);

		public override bool Ordered => true;
	}

	public class ThingQueryGuestStatus : ThingQueryDropDown<GuestStatus>
	{
		public override bool AppliesDirectlyTo(Thing thing)
		{
			Pawn pawn = thing as Pawn;
			if (pawn == null) return false;

			if (pawn.GuestStatus is GuestStatus status)
			{
				if (extraOption == 1) return status == GuestStatus.Prisoner && (pawn.guest?.PrisonerIsSecure ?? false);
				if (extraOption == 2) return status == GuestStatus.Slave && (pawn.guest?.SlaveIsSecure ?? false);
				return status == sel;
			}
			return false;
		}

		public override int ExtraOptionsCount => 2;
		public override string NameForExtra(int ex) =>
			ex == 1 ? "TD.SecurePrisoner".Translate() : "TD.SecureSlave".Translate();
	}

	public class ThingQueryGender : ThingQueryDropDown<Gender>
	{
		public ThingQueryGender() => sel = Gender.Male;

		public override bool AppliesDirectlyTo(Thing thing) =>
			thing is Pawn pawn && pawn.gender == sel;
	}

	public class ThingQueryDevelopmentalStage : ThingQueryDropDown<DevelopmentalStage>
	{
		public ThingQueryDevelopmentalStage() => sel = DevelopmentalStage.Adult;

		public override bool AppliesDirectlyTo(Thing thing) =>
			thing is Pawn pawn && pawn.DevelopmentalStage == sel;
	}


	public class ThingQueryInspiration : ThingQueryDropDown<InspirationDef>
	{
		public override bool AppliesDirectlyTo(Thing thing) =>
			thing is Pawn p && 
			(extraOption == 1 ?
				p.InspirationDef != null :
				sel == p.InspirationDef);

		public override string NullOption() => "None".Translate();

		public override int ExtraOptionsCount => 1;
		public override string NameForExtra(int ex) => "TD.AnyOption".Translate();
	}


	public class ThingQueryCapacity : ThingQueryDropDown<PawnCapacityDef>
	{
		public const float MaxReasonable = 4;
		FloatRangeUB capacityRange = new(0, MaxReasonable, 1, 1);

		public ThingQueryCapacity()
		{
			sel = PawnCapacityDefOf.Manipulation;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref capacityRange.range, "capacityRange");
		}
		protected override ThingQuery Clone()
		{
			ThingQueryCapacity clone = (ThingQueryCapacity)base.Clone();
			clone.capacityRange = capacityRange;
			return clone;
		}

		public override int ExtraOptionsCount => 1;
		public override string NameForExtra(int ex) => "TD.AnyOption".Translate();

		private bool Includes(Pawn pawn, PawnCapacityDef def) =>
			capacityRange.Includes(pawn.health.capacities.GetLevel(def));

		public override bool AppliesDirectlyTo(Thing thing)
		{
			Pawn pawn = thing as Pawn;
			if (pawn == null) return false;

			if(extraOption == 1)
				foreach (PawnCapacityDef def in DefDatabase<PawnCapacityDef>.AllDefs)
					if (Includes(pawn, def))
						return true;

			return Includes(pawn, sel);
		}

		public override bool DrawCustom(Rect fullRect)
		{
			if(TDWidgets.FloatRangeUB(fullRect.RightHalfClamped(row.FinalX), id, ref capacityRange, valueStyle: ToStringStyle.PercentZero))
			{
				//round down to 1%
				capacityRange.min = (int)(100 * capacityRange.min) / 100f;
				capacityRange.max = (int)(100 * capacityRange.max) / 100f;
				return true;
			}
			return false;
		}
	}


	public class ThingQueryOutfit : ThingQueryDropDown<Outfit>
	{
		public ThingQueryOutfit()
		{
			if (Current.Game?.outfitDatabase?.DefaultOutfit() is Outfit defaultOutfit)
				sel = defaultOutfit;
			else
				selName = "Anything";//notranslate
		}

		public override bool AppliesDirectlyTo(Thing thing) =>
			(thing as Pawn)?.outfits?.CurrentOutfit == sel;

		protected override Outfit ResolveRef(Map map) =>
			Current.Game.outfitDatabase.AllOutfits.FirstOrDefault(o => o.label == selName);

		public override string NameFor(Outfit o) => o.label;
		protected override string MakeSaveName() => sel.label;

		public override IEnumerable<Outfit> AllOptions() =>
			Current.Game?.outfitDatabase?.AllOutfits;
	}


	public class ThingQueryFoodRestriction : ThingQueryDropDown<FoodRestriction>
	{
		public ThingQueryFoodRestriction()
		{
			if (Current.Game?.foodRestrictionDatabase?.DefaultFoodRestriction() is FoodRestriction defaultFood)
				sel = defaultFood;
			else
				selName = "FoodRestrictionLavish".Translate();
		}

		public override bool AppliesDirectlyTo(Thing thing) =>
			(thing as Pawn)?.foodRestriction?.CurrentFoodRestriction == sel;

		protected override FoodRestriction ResolveRef(Map map) =>
			Current.Game.foodRestrictionDatabase.AllFoodRestrictions.FirstOrDefault(o => o.label == selName);

		public override string NameFor(FoodRestriction o) => o.label;
		protected override string MakeSaveName() => sel.label;

		public override IEnumerable<FoodRestriction> AllOptions() =>
			Current.Game?.foodRestrictionDatabase?.AllFoodRestrictions;
	}


	public class ThingQueryDrugPolicy : ThingQueryDropDown<DrugPolicy>
	{
		public ThingQueryDrugPolicy()
		{
			if (Current.Game?.drugPolicyDatabase?.DefaultDrugPolicy() is DrugPolicy defaultDrug)
				sel = defaultDrug;
			else
				selName = DefDatabase<DrugPolicyDef>.GetNamed("SocialDrugs").label;//notranslate
		}

		public override bool AppliesDirectlyTo(Thing thing) =>
			(thing as Pawn)?.drugs?.CurrentPolicy == sel;

		protected override DrugPolicy ResolveRef(Map map) =>
			Current.Game.drugPolicyDatabase.AllPolicies.FirstOrDefault(o => o.label == selName);

		public override string NameFor(DrugPolicy o) => o.label;
		protected override string MakeSaveName() => sel.label;

		public override IEnumerable<DrugPolicy> AllOptions() =>
			Current.Game?.drugPolicyDatabase?.AllPolicies;
	}


	public class ThingQueryWork : ThingQueryDropDown<WorkTypeDef>
	{
		public ThingQueryWork()
		{
			sel = WorkTypeDefOf.Firefighter;
		}

		public override bool AppliesDirectlyTo(Thing thing)
		{
			Pawn pawn = thing as Pawn;
			if (pawn == null) return false;

			return pawn.workSettings?.WorkIsActive(sel) ?? false;
		}

		public override string NameFor(WorkTypeDef o) => o.pawnLabel;
	}


	[StaticConstructorOnStartup]
	public class ThingQueryAbiltyCategory : ThingQueryCategorizedDropdownHelper<AbilityDef, ModContentPack, ThingQueryAbility, ThingQueryAbiltyCategory>
	{
		public static ModContentPack CategoryFor(AbilityDef def) => def.modContentPack;

		public override bool AppliesDirectlyTo(Thing thing)
		{
			Pawn pawn = thing as Pawn;
			if (pawn == null || pawn.abilities == null) return false;

			return ParentQuery.AbilitiesInQuestion(pawn).Any(a => CategoryFor(a.def) == sel);
		}

		public override string NameFor(ModContentPack o) => o.Name;

		public override bool UsesResolveName => true;
		protected override string MakeSaveName() => sel.PackageIdPlayerFacing;

		protected override ModContentPack ResolveName() =>
			LoadedModManager.RunningMods.FirstOrDefault(mod => mod.PackageIdPlayerFacing == selName);
	}

	public class ThingQueryAbility : ThingQueryCategorizedDropdown<AbilityDef, ModContentPack, ThingQueryAbility, ThingQueryAbiltyCategory>
	{
		static ThingQueryAbility()
		{
			// Remove Query_Ability from selectable list if there's no abiliites a.k.a. no expansions
			if (DefDatabase<AbilityDef>.DefCount == 0)
				ThingQueryDefOf.Query_Ability.devOnly = true;
		}


		public enum AbilityFilterType { Has, Cooldown, CanCast, Active, Charges}
		public AbilityFilterType filterType;	
		public IntRangeUB chargeRange = new(0,5,1,5);

		public ThingQueryAbility() => extraOption = 1;

		

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref filterType, "filterType");
			Scribe_Values.Look(ref chargeRange.range, "chargeRange", new(1,5));
		}
		protected override ThingQuery Clone()
		{
			ThingQueryAbility clone = (ThingQueryAbility)base.Clone();
			clone.filterType = filterType;
			clone.chargeRange = chargeRange;
			return clone;
		}

		public IEnumerable<Ability> AbilitiesInQuestion(Pawn pawn)
		{
			if (filterType == AbilityFilterType.Active)
			{
				foreach (Ability ability in pawn.abilities.AllAbilitiesForReading)
					foreach (AbilityCompProperties props in ability.def.comps)
						if (props is CompProperties_AbilityGiveHediff propsHediff)
							if (pawn.health.hediffSet.HasHediff(propsHediff.hediffDef))
								yield return ability;
			}
			else
				foreach (Ability a in pawn.abilities.AllAbilitiesForReading.Where(a =>
			 filterType switch
			 {
				 AbilityFilterType.Cooldown => a.CooldownTicksRemaining > 0,
				 AbilityFilterType.CanCast => a.CanCast,
				 AbilityFilterType.Charges => chargeRange.Includes(a.charges),

				 _ => true // FilterType.Has 
			 }))
					yield return a;
		}

		public override bool AppliesDirectly2(Thing thing)
		{
			Pawn pawn = thing as Pawn;
			if (pawn == null || pawn.abilities == null) return false;

			var abilitiesInQuestion = AbilitiesInQuestion(pawn);

			if (extraOption == 1)
				return abilitiesInQuestion.Count() > 0;

			if (sel == null)
				return abilitiesInQuestion.Count() == 0;

			return abilitiesInQuestion.Any(a => a.def == sel);
		}

		public override string NullOption() => "None".Translate();

		public override int ExtraOptionsCount => 1;
		public override string NameForExtra(int ex) => "TD.AnyOption".Translate();

		public override ModContentPack CategoryFor(AbilityDef def) => ThingQueryAbiltyCategory.CategoryFor(def);

		public override string DropdownNameFor(AbilityDef def) =>
			def.level == 0 ? NameFor(def) : "TD.Level01".Translate(def.level, NameFor(def));
		public override Texture2D IconTexFor(AbilityDef def) => def.uiIcon;

		private readonly List<AbilityDef> orderedOptions =
			DefDatabase<AbilityDef>.AllDefs
			.OrderBy(def => def.level)
			.ThenBy(def => def.category?.displayOrder ?? 0) // (unseen category that is used only for gizmo ordering)
			.ThenBy(def => def.label).ToList();

		public override IEnumerable<AbilityDef> AllOptions() => orderedOptions;
		public override IEnumerable<AbilityDef> AvailableOptions() =>
			ContentsUtility.AvailableInGame(t => (t as Pawn)?.abilities?.AllAbilitiesForReading.Select(a => a.def));


		public override bool DrawCustom(Rect fullRect)
		{
			RowButtonFloatMenuEnum(filterType, newValue => filterType = newValue, 
				filter: newValue =>
					(!(newValue == AbilityFilterType.Charges // Can't select charges if one charge
					&& sel != null
					&& sel.charges == 1)) 
					&&
					(!(newValue == AbilityFilterType.Active // Can't select active if gives no active hediff
					&& sel != null
					&& sel.comps.All(prop => prop is not CompProperties_AbilityGiveHediff))) 
					&&
					(!(newValue == AbilityFilterType.Cooldown // Can't select Cooldown if there's no cooldown
					&& sel != null
					&& sel.cooldownTicksRange == default && (sel.groupDef?.cooldownTicks ?? 0) == 0)));


			if (filterType == AbilityFilterType.Charges)
			{
				return TDWidgets.IntRangeUB(fullRect.RightHalfClamped(row.FinalX), id, ref chargeRange);
			}
			return false;
		}
	}


	public class ThingQueryAge : ThingQueryFloatRange
	{
		public bool chronological; //default biological

		const float maxBio = 200;
		const float maxChrono = 1000;

		public override float Max => chronological ? maxChrono : maxBio;
		public override ToStringStyle Style => ToStringStyle.Integer;

		public ThingQueryAge() => sel = new(Min, Max, Min, Max / 4);

		protected override void PostProcess()
		{
			//selByRef.absRange.min = Min;//for good measure.
			selByRef.absRange.max = Max;
		}

		protected override void PostChosen()
		{
			if (chronological)
			{
				if (selByRef.range.max == maxBio)
					selByRef.range.max = maxChrono;
			}
			else
			{
				selByRef.range.min = Mathf.Min(selByRef.range.min, Max); // Bounded by new max. Already would be <= range.max
				selByRef.range.max = Mathf.Min(selByRef.range.max, Max);
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref chronological, "chronological");
		}

		protected override ThingQuery Clone()
		{
			ThingQueryAge clone = (ThingQueryAge)base.Clone();
			clone.chronological = chronological;
			return clone;
		}


		public override bool AppliesDirectlyTo(Thing thing)
		{
			Pawn pawn = thing as Pawn;
			if (pawn == null) return false;

			return sel.Includes(chronological ? pawn.ageTracker.AgeChronologicalYearsFloat : pawn.ageTracker.AgeBiologicalYearsFloat);
		}

		public override bool DrawMain(Rect rect, bool locked, Rect fullRect)
		{
			bool changed = base.DrawMain(rect, locked, fullRect);

			if (row.ButtonText(chronological ? "TD.Chronological".Translate() : "TD.Biological".Translate()))
			{
				chronological = !chronological;
				PostProcess();
				PostChosen();
			}

			return changed;
		}
	}

	// This would be better to be ThingQueryDropDown<PawnRelationDef> if people modded PawnRelationDef
	// So it can handle saving that by name. But using ThingQueryAndOrGroup is easier for UI.
	public enum RelationFilterType { Has, Any, All}
	public class ThingQueryRelation : ThingQueryAndOrGroup
	{
		public PawnRelationDef relation;
		public Gender gender; // Def is "Parent" with labels "father" and "mother". There seems to be no translation string for "parent" so I'm gonna not do that filter.
		public RelationFilterType filterType;
		// "the related pawn matches these filters" : default "this pawn has this relation"
		// "ANY related pawn matches" : default "ALL"

		public ThingQueryRelation()
		{
			relation = PawnRelationDefOf.Parent;
			gender = Gender.Male;
		}
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Defs.Look(ref relation, "relation");
			Scribe_Values.Look(ref gender, "gender");
			Scribe_Values.Look(ref filterType, "filterType");
		}
		protected override ThingQuery Clone()
		{
			ThingQueryRelation clone = (ThingQueryRelation)base.Clone();
			clone.relation = relation;
			clone.gender = gender;
			clone.filterType = filterType;
			return clone;
		}


		public bool ShouldShow(Pawn pawn, Pawn otherPawn) =>
			(gender == Gender.None || otherPawn.gender == gender) &&
			(DebugSettings.godMode || SocialCardUtility.ShouldShowPawnRelations(otherPawn, pawn));

		public IEnumerable<Pawn> RelationsFor(Pawn pawn)
		{
			if (pawn.relations == null)
				yield break;

			// Direct relations that are directly stored
			// Only some relations are here even if they seem they should be...
			if (!relation.implied)
			{
				foreach (var rel in pawn.relations.DirectRelations)
				{
					if (rel.def == relation && ShouldShow(pawn, rel.otherPawn))
						yield return rel.otherPawn;
				}
			}
			else
			{
				// Backwards relations, with <implied>true
				// are computed via PawnRelationWorker.InRelation, given another pawn's relation back to us...
				// e.g. "A person who has me as a father is my son"
				// yea that's how it works.
				foreach (var otherPawn in pawn.relations.PotentiallyRelatedPawns)
				{
					if (relation.Worker.InRelation(pawn, otherPawn) && ShouldShow(pawn, otherPawn))
						yield return otherPawn;
				}
			}
			// And this can't use the cache because that only applies when you open the social tab . . 
		}

		public override bool AppliesDirectlyTo(Thing thing)
		{
			Pawn pawn = thing as Pawn;
			if (pawn == null) return false;

			if(filterType == RelationFilterType.Has)
				return RelationsFor(pawn).Any();

			else if (filterType == RelationFilterType.Any)
				return RelationsFor(pawn).Any(otherPawn => children.AppliesTo(otherPawn));

			//else if (filterType == RelationFilterType.All)
			// Just list.All() here would return true for anyone without a father. 
			// This filter checks 1) you have at least one father 1) all those fathers match the filters.
			return RelationsFor(pawn).Count() > 0 && RelationsFor(pawn).All(otherPawn => children.AppliesTo(otherPawn));

		}


		private bool ButtonToggleType()
		{
			if (row.ButtonText(filterType.TranslateEnum()))
			{
				filterType = filterType.Next();
				return true;
			}
			return false;
		}

		public static string NameFor(PawnRelationDef def, Gender gender) =>
			gender == Gender.Female 
			? (def.labelFemale?.CapitalizeFirst() ?? def.LabelCap)
			: def.LabelCap;

		private void ButtonOptions()
		{
			if (row.ButtonTextNoGap(NameFor(relation, gender)))
			{
				List<FloatMenuOption> relationOptions = new();
				foreach (PawnRelationDef def in DefDatabase<PawnRelationDef>.AllDefsListForReading)
				{
					if(def.labelFemale == null)
						relationOptions.Add(new FloatMenuOptionAndRefresh(NameFor(def, Gender.None), () => { relation = def; gender = Gender.None; }, this));
					else
					{
						relationOptions.Add(new FloatMenuOptionAndRefresh(NameFor(def, Gender.Male), () => { relation = def; gender = Gender.Male; }, this));
						relationOptions.Add(new FloatMenuOptionAndRefresh(NameFor(def, Gender.Female), () => { relation = def; gender = Gender.Female; }, this));
					}
				}

				DoFloatOptions(relationOptions);
			}
		}

		public override bool DrawMain(Rect rect, bool locked, Rect fullRect)
		{
			row.Label(Label); // "Relation"

			bool changed = ButtonToggleType(); // "Has"
			ButtonOptions(); // "Parent"

			if (filterType == RelationFilterType.Has)
				return changed;

			// "Relation Any/All Parent Matches Any/All of these filters:"

			row.Label("TD.Matches".Translate());
			changed |= ButtonToggleAny();
			row.Label("TD.OfTheseQueries".Translate());

			return changed;
		}

		protected override bool DrawUnder(Listing_StandardIndent listing, bool locked)
		{
			if (filterType == RelationFilterType.Has)
				return false;

			return base.DrawUnder(listing, locked);
		}
	}

	public enum ScheduleFilterType { Current, AllScheduleIs, AnyScheduleNot}
	[StaticConstructorOnStartup]
	public class ThingQuerySchedule : ThingQuery
	{
		public ScheduleFilterType filterType;
		public TimeAssignmentDef assignment; //For "current" type
		public List<TimeAssignmentDef> timetable; // For "Has" type, "has this schedule"


		public ThingQuerySchedule()
		{
			assignment = TimeAssignmentDefOf.Work;
			timetable = Enumerable.Repeat<TimeAssignmentDef>(null, GenDate.HoursPerDay).ToList();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			//TODO consider modded TimeAssignmentDef ehh.
			Scribe_Values.Look(ref filterType, "filterType");
			Scribe_Defs.Look(ref assignment, "assignment");

			if (Scribe.mode == LoadSaveMode.Saving)
			{
				// Scribe_Collections calls Scribe_Def which valls Scribe_Value,
				// but Scribe_Def explicitly sets args so that a null def means NO written tag, 
				// (it doesn't have a forceSave option)
				// meaning NO <li>null</li>, meaning null defs in a list can't be save-scribed directly.
				List<string> timetableDummy = new(timetable.Select(def => def?.defName ?? "null"));
				Scribe_Collections.Look(ref timetableDummy, "timetable");
			}
			else
			{
				Scribe_Collections.Look(ref timetable, "timetable");
			}
		}

		protected override ThingQuery Clone()
		{
			ThingQuerySchedule clone = (ThingQuerySchedule)base.Clone();
			clone.filterType = filterType;
			clone.assignment = assignment;
			clone.timetable = timetable.ListFullCopy();
			return clone;
		}


		// How to cycle the assignment options
		private static List<TimeAssignmentDef> scheduleOptions;
		static ThingQuerySchedule()
		{
			scheduleOptions = new();
			scheduleOptions.Add(null);

			scheduleOptions.AddRange(DefDatabase<TimeAssignmentDef>.AllDefs);
		}

		private TimeAssignmentDef NextAssignment(TimeAssignmentDef assignment) =>
			scheduleOptions[(scheduleOptions.IndexOf(assignment) + 1) % scheduleOptions.Count];
		private TimeAssignmentDef PrevAssignment(TimeAssignmentDef assignment) =>
			scheduleOptions[(scheduleOptions.IndexOf(assignment) - 1 + scheduleOptions.Count) % scheduleOptions.Count];



		public override bool AppliesDirectlyTo(Thing thing)
		{
			Pawn pawn = thing as Pawn;
			if (pawn == null) return false;

			var time = pawn.timetable;
			if (time == null) return false;

			if (filterType == ScheduleFilterType.Current)
				return time.CurrentAssignment == assignment;


			for (int hour = 0; hour < GenDate.HoursPerDay; hour++)
			{
				if (timetable[hour] == null)
					continue;

				if (filterType == ScheduleFilterType.AllScheduleIs)
				{
					if (time.GetAssignment(hour) != timetable[hour])
						return false;
				}
				else // if (filterType == ScheduleFilterType.AnyScheduleNot)
				{
					if (time.GetAssignment(hour) != timetable[hour])
						return true;
				}
			}

			return filterType == ScheduleFilterType.AllScheduleIs;
		}


		// Drawing methods adjusted from PawnColumnWorker_Timetable
		private bool DrawHours(Rect rect)
		{
			float curX = rect.x;
			float slotWidth = rect.width / GenDate.HoursPerDay;

			bool changed = false;
			for (int hour = 0; hour < GenDate.HoursPerDay; hour++)
			{
				changed |= DrawTimeAssignment(new Rect(curX, rect.y, slotWidth, rect.height), hour);
				curX += slotWidth;
			}
			GUI.color = Color.white;

			return changed;
		}

		private void DrawHeader(Rect rect)
		{
			float curX = rect.x;
			float slotWidth = rect.width / GenDate.HoursPerDay;

			Text.Font = GameFont.Tiny;
			Text.Anchor = TextAnchor.UpperCenter;
			for (int hour = 0; hour < GenDate.HoursPerDay; hour++)
			{
				Widgets.Label(new Rect(curX, rect.y, slotWidth, rect.height + 3f), hour.ToString());
				curX += slotWidth;
			}
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperLeft;
		}


		private TimeAssignmentDef newAssignment; //To be drag-assigned to others
		private static Texture2D nullTexture = SolidColorMaterials.NewSolidColorTexture(new Color(0, 0, 0, 0.8f));
		private bool DrawTimeAssignment(Rect rect, int hour)
		{
			TimeAssignmentDef assignment = timetable[hour];
			GUI.DrawTexture(rect, assignment?.ColorTexture ?? nullTexture);
			GUI.color = Color.gray;
			Widgets.DrawBox(rect, 1);//highlight mouseover
			GUI.color = Color.white;
			if (!Mouse.IsOver(rect))
			{
				return false;
			}

			Widgets.DrawBox(rect, 2);//highlight mouseover
			if (assignment?.LabelCap is TaggedString label) // nothing for null option
				TooltipHandler.TipRegion(rect, label);

			if (Event.current.type == EventType.MouseDown)
			{
				if (Event.current.button == 0 || Event.current.button == 1)
				{
					newAssignment = Event.current.button == 0 ? NextAssignment(assignment) : PrevAssignment(assignment);

					Event.current.Use();
					timetable[hour] = newAssignment;
					return true;
				}
			}

			else if (Event.current.type == EventType.MouseDrag)
			{
				if (assignment != newAssignment)
				{
					timetable[hour] = newAssignment;
					Event.current.Use();
					return true;
				}
			}
			return false;
		}

		public override bool DrawMain(Rect rect, bool locked, Rect fullRect)
		{
			base.DrawMain(rect, locked, fullRect);

			bool changed = false;
			if(row.ButtonText(filterType.TranslateEnum()))
			{
				changed = true;
				filterType = filterType.Next();
			}

			if (filterType == ScheduleFilterType.Current)
				RowButtonFloatMenuDef(assignment, newValue => assignment = newValue);

			return changed;
		}

		protected override bool DrawUnder(Listing_StandardIndent listing, bool locked)
		{
			if (filterType == ScheduleFilterType.Current)
				return false;

			Rect rect = listing.GetRect(Text.LineHeight);
			var var = DrawHours(rect);

			//Draw hours # INSIDE the box
			DrawHeader(rect);

			return var;
		}
	}
}	
