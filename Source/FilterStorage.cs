﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;
using CloneArgs = TD_Find_Lib.FindDescription.CloneArgs;

namespace TD_Find_Lib
{
	public static class FilterStorageUtil
	{
		public static void ButtonOpenSettings(WidgetRow row)
		{
			if (row.ButtonIcon(FindTex.Book))
				Find.WindowStack.Add(new TDFindLibListWindow());
		}



		public static void ButtonChooseLoadFilter(WidgetRow row, Action<FindDescription> onLoad, string source, CloneArgs cloneArgs = default)
		{
			if (row.ButtonIcon(FindTex.Import, "Import from..."))
				ChooseLoadFilter(onLoad, source, cloneArgs);
		}
		public static void ChooseLoadFilter(Action<FindDescription> onLoad, string source, CloneArgs cloneArgs = default)
		{
			List<FloatMenuOption> loadOptions = new();

			//Load from saved groups
			foreach (FilterGroup group in Mod.settings.groupedFilters)
			{
				loadOptions.Add(new FloatMenuOption(group.name, () => LoadFromGroup(group, onLoad, cloneArgs)));
			}

			//Load from clipboard
			string clipboard = GUIUtility.systemCopyBuffer;
			if (ScribeXmlFromString.IsValid<FindDescription>(clipboard))
				loadOptions.Add(new FloatMenuOption("Paste from clipboard", () =>
				{
					FindDescription desc = ScribeXmlFromString.LoadFromString<FindDescription>(clipboard);
					onLoad(desc.Clone(cloneArgs));
				}));



			//Except don't load yourself
			loadOptions.RemoveAll(o => o.Label == source);

			Find.WindowStack.Add(new FloatMenu(loadOptions));
		}

		public static void LoadFromGroup(FilterGroup group, Action<FindDescription> onLoad, CloneArgs cloneArgs = default)
		{
			List<FloatMenuOption> descOptions = new();
			foreach (FindDescription desc in group)
				descOptions.Add(new FloatMenuOption(desc.name, () => onLoad(desc.Clone(cloneArgs))));

			Find.WindowStack.Add(new FloatMenu(descOptions));
		}



		public static void ButtonChooseExportFilter(WidgetRow row, FindDescription desc, string source)
		{
			if (row.ButtonIcon(FindTex.Export, "Export to..."))
				ChooseExportFilter(desc, source);
		}

		public static void ChooseExportFilter(FindDescription desc, string source)
		{
			List<FloatMenuOption> exportOptions = new();

			//Save to groups
			exportOptions.Add(new FloatMenuOption("Save", () =>
			{
				if (Mod.settings.groupedFilters.Count == 1)
				{
					SaveToGroup(desc, Mod.settings.groupedFilters[0]);
				}
				else
				{
					List<FloatMenuOption> groupOptions = new();

					foreach (FilterGroup group in Mod.settings.groupedFilters)
					{
						groupOptions.Add(new FloatMenuOption(group.name, () => SaveToGroup(desc, group)));
					}

					Find.WindowStack.Add(new FloatMenu(groupOptions));
				}
			}));


			// Export to Clipboard
			exportOptions.Add(new FloatMenuOption("Copy to clipboard", () =>
			{
				GUIUtility.systemCopyBuffer = ScribeXmlFromString.SaveAsString(desc.CloneForSave());
			}));


			// Export to File?
			//TODO: Other options to export to!

			//Except don't export to yourself
			exportOptions.RemoveAll(o => o.Label == source);

			Find.WindowStack.Add(new FloatMenu(exportOptions));
		}

		public static void SaveToGroup(FindDescription desc, FilterGroup group, CloneArgs cloneArgs = default)
		{
			if (cloneArgs.newName != null)
				group.TryAdd(desc.Clone(cloneArgs));
			else
				Find.WindowStack.Add(new Dialog_Name(desc.name, n => { cloneArgs.newName = n; group.TryAdd(desc.Clone(cloneArgs)); }, $"Save to {group.name}"));
		}



		public static void ButtonChooseLoadFilterGroup(WidgetRow row, Action<FilterGroup> onLoad, string source)
		{
			if (row.ButtonIcon(FindTex.Import, "Import from..."))
				ChooseLoadFilterGroup(onLoad, source);
		}

		public static void ChooseLoadFilterGroup(Action<FilterGroup> onLoad, string source, CloneArgs cloneArgs = default)
		{
			List<FloatMenuOption> importOptions = new();

			//Load from saved groups
			//todo: in submenu "Load"
			/*
			foreach (FilterGroup group in Mod.settings.groupedFilters)
			{
				importOptions.Add(new FloatMenuOption(group.name, () => onLoad(group)));
			}
			*/


			//Load from clipboard
			string clipboard = GUIUtility.systemCopyBuffer;
			if (ScribeXmlFromString.IsValid<FilterGroup>(clipboard))
				importOptions.Add(new FloatMenuOption("Paste from clipboard", () =>
				{
					FilterGroup group = ScribeXmlFromString.LoadFromString<FilterGroup>(clipboard, null, null);
					onLoad(group.Clone(cloneArgs));
				}));



			//Except don't export to where this comes from
			importOptions.RemoveAll(o => o.Label == source);

			Find.WindowStack.Add(new FloatMenu(importOptions));
		}



		public static void ButtonChooseExportFilterGroup(WidgetRow row, FilterGroup desc, string source)
		{
			if (row.ButtonIcon(FindTex.Export, "Export to..."))
				ChooseExportFilterGroup(desc, source);
		}

		public static void ChooseExportFilterGroup(FilterGroup group, string source)
		{
			List<FloatMenuOption> exportOptions = new();

			// Export to Clipboard
			exportOptions.Add(new FloatMenuOption("Copy to clipboard", () =>
			{
				GUIUtility.systemCopyBuffer = ScribeXmlFromString.SaveAsString(group);
				//todo CloneForSave each? Right now they're in storage, already inactive.
				//Or can this just work actually if you copy/paste into same active map . . ?
			}));


			// Export to File?
			//TODO: Other options to export to!

			//Except don't export to where this comes from
			exportOptions.RemoveAll(o => o.Label == source);

			Find.WindowStack.Add(new FloatMenu(exportOptions));
		}
	}
}
