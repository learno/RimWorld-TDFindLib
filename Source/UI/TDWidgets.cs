﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TD_Find_Lib
{
	public static class TDWidgets
	{
		//public static float HorizontalSlider_NewTemp(Rect rect, float value, float min, float max, bool middleAlignment = false, string label = null, string leftAlignedLabel = null, string rightAlignedLabel = null, float roundTo = -1f)
		public static bool Slider(Rect rect, ref float value, float min = 0, float max = 1, bool middleAlignment = false, string label = null, string leftAlignedLabel = null, string rightAlignedLabel = null, float roundTo = -1f)
		{
			float newVal = Widgets.HorizontalSlider_NewTemp(rect, value, min, max, middleAlignment, label, leftAlignedLabel, rightAlignedLabel, roundTo);
			if(newVal != value)
			{
				value = newVal;
				return true;
			}
			return false;
		}
		public static bool Slider(Rect rect, ref int value, int min, int max, bool middleAlignment = false, string label = null, string leftAlignedLabel = null, string rightAlignedLabel = null)
		{
			int newVal = (int)Widgets.HorizontalSlider_NewTemp(rect, value, min, max, middleAlignment, label, leftAlignedLabel, rightAlignedLabel, 1);
			if (newVal != value)
			{
				value = newVal;
				return true;
			}
			return false;
		}

		public static bool FloatRange(Rect rect, int id, ref FloatRange range, float min = 0f, float max = 1f, string labelKey = null, ToStringStyle valueStyle = ToStringStyle.FloatTwo, float gap = 0f, GameFont sliderLabelFont = GameFont.Tiny, Color? sliderLabelColor = null)
		{
			FloatRange newRange = range;
			Widgets.FloatRange(rect, id, ref newRange, min, max, labelKey, valueStyle, gap, sliderLabelFont, sliderLabelColor);
			if (range != newRange)
			{
				range = newRange;
				return true;
			}
			return false;
		}
		// Unbounded range variant
		public static bool FloatRangeUB(Rect rect, int id, ref FloatRangeUB rangeUB, ToStringStyle valueStyle = ToStringStyle.FloatTwo, Func<float, string> writer = null)
		{
			// simply call FloatRange above, but set label when needed, e.g. ">= min" 
			string labelKey = null;
			if (rangeUB.range.max == rangeUB.absRange.max && rangeUB.range.min == rangeUB.absRange.min)
				labelKey = "TD.AnyValue";
			else if (rangeUB.range.min == rangeUB.absRange.min)
				labelKey = $"≤ {(writer != null ? writer(rangeUB.range.max) : rangeUB.range.max.ToStringByStyle(valueStyle))}";
			else if (rangeUB.range.max == rangeUB.absRange.max)
				labelKey = $"≥ {(writer != null ? writer(rangeUB.range.min) : rangeUB.range.min.ToStringByStyle(valueStyle))}";
			else if (writer != null)
				labelKey = writer(rangeUB.range.min) + " - " + writer(rangeUB.range.max);

			return FloatRange(rect, id, ref rangeUB.range, rangeUB.absRange.min, rangeUB.absRange.max, labelKey, valueStyle);
		}


		public static bool IntRange(Rect rect, int id, ref IntRange range, int min = 0, int max = 100, string labelKey = null, int minWidth = 0)
		{
			IntRange newRange = range;
			Widgets.IntRange(rect, id, ref newRange, min, max, labelKey, minWidth);
			if (range != newRange)
			{
				range = newRange;
				return true;
			}
			return false;
		}
		// Unbounded range variant
		public static bool IntRangeUB(Rect rect, int id, ref IntRangeUB rangeUB, Func<int, string> writer = null)
		{
			// simply call IntRange above, but set label when needed, e.g. ">= min" 
			string labelKey = null;
			if (rangeUB.range.max == rangeUB.absRange.max && rangeUB.range.min == rangeUB.absRange.min)
				labelKey = "TD.AnyValue";
			else if (rangeUB.range.min == rangeUB.absRange.min)
				labelKey = $"≤ {(writer != null ? writer(rangeUB.range.max) : rangeUB.range.max)}";
			else if (rangeUB.range.max == rangeUB.absRange.max)
				labelKey = $"≥ {(writer != null ? writer(rangeUB.range.min) : rangeUB.range.min)}";
			else if(writer!= null)
				labelKey = writer(rangeUB.range.min) + " - " + writer(rangeUB.range.max);

			return IntRange(rect, id, ref rangeUB.range, rangeUB.absRange.min, rangeUB.absRange.max, labelKey);
		}
		public static bool IntRangeUB(Rect rect, int id, ref FloatRangeUB rangeUB, Func<int, string> writer = null)
		{
			IntRangeUB temp = rangeUB;
			bool changed = IntRangeUB(rect, id, ref temp, writer);
			if(changed)
			{
				rangeUB = temp;
			}
			return changed;
		}
	}
}
