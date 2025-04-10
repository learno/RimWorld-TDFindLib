﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;

namespace TD_Find_Lib
{
	// The holder of the receivers and providers
	public static class SearchTransfer
	{
		public static List<ISearchReceiver> receivers = new();
		public static List<ISearchGroupReceiver> groupReceivers = new();
		public static List<ISearchProvider> providers = new();
		//providers work overtime, providing none/single/list/group-of-lists


		// I would love to auto-create and register all subclasses of ISearchReceiver,
		// but any class could implement it and we can't be sure it's okay to just create a new one (e.g. Settings)
		// Plus, many receivers are invalidated when a game ends.
		// It's not the easiest thing to hook into "on game end"
		// So it's easier to keep a static receiver that checks if a game exists in CanReceive()
		public static void Register(object obj)
		{
			if (obj is ISearchReceiver receiver)
				receivers.Add(receiver);
			if (obj is ISearchGroupReceiver greceiver)
				groupReceivers.Add(greceiver);
			if (obj is ISearchProvider provider)
				providers.Add(provider);
		}
		public static void Deregister(object obj)
		{
			receivers.Remove(obj as ISearchReceiver);
			groupReceivers.Remove(obj as ISearchGroupReceiver);
			providers.Remove(obj as ISearchProvider);
		}
	}


	// You can export  to  a receiver
	// You can import from a provider
	public interface ISearchProvider
	{
		public enum Method { None, Single, Selection, Grouping }

		public string Source { get; }
		public string ProvideName { get; }
		public Method ProvideMethod();

		public QuerySearch ProvideSingle();
		public SearchGroup ProvideGroup();
		public List<SearchGroup> ProvideLibrary();
	}

	public interface ISearchReceiver
	{
		public string Source { get; }
		public string ReceiveName { get; }
		public QuerySearch.CloneArgs CloneArgs { get; }
		public bool CanReceive();
		public void Receive(QuerySearch search);
	}

	public interface ISearchGroupReceiver
	{
		public string Source { get; }
		public string ReceiveName { get; }
		public QuerySearch.CloneArgs CloneArgs { get; }
		public bool CanReceive();
		public void Receive(SearchGroup search);
	}


	[StaticConstructorOnStartup]
	public class ClipboardTransfer : ISearchReceiver, ISearchProvider, ISearchGroupReceiver
	{
		static ClipboardTransfer()
		{
			SearchTransfer.Register(new ClipboardTransfer());
		}


		public string Source => null;	//always used

		public string ReceiveName => "TD.CopyToClipboard".Translate();
		public string ProvideName => "TD.PasteFromClipboard".Translate();


		public QuerySearch.CloneArgs CloneArgs => default; //save
		public bool CanReceive() => true;

		public void Receive(QuerySearch search)
		{
			GUIUtility.systemCopyBuffer = ScribeXmlFromString.SaveAsString(search);
		}

		public void Receive(SearchGroup group)
		{
			GUIUtility.systemCopyBuffer = ScribeXmlFromString.SaveAsString(group);
		}


		public ISearchProvider.Method ProvideMethod()
		{
			string clipboard = GUIUtility.systemCopyBuffer;
			return ScribeXmlFromString.IsValid<QuerySearch>(clipboard) ? ISearchProvider.Method.Single
				: ScribeXmlFromString.IsValid<SearchGroup>(clipboard) ? ISearchProvider.Method.Selection
				: ISearchProvider.Method.None;
		}

		public QuerySearch ProvideSingle()
		{
			string clipboard = GUIUtility.systemCopyBuffer;
			return ScribeXmlFromString.LoadFromString<QuerySearch>(clipboard);
		}

		public SearchGroup ProvideGroup()
		{
			string clipboard = GUIUtility.systemCopyBuffer;
			return ScribeXmlFromString.LoadFromString<SearchGroup>(clipboard, null, null);
		}

		public List<SearchGroup> ProvideLibrary() => null;
	}
}
