﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TIPS
{
    public class ReportSettings
    {
		public string Title = "Report";

		public List<ReportColumn> Columns = new();

		public List<List<string>> TagGroups = new();

		public ReportSettings Clone()
		{
			ReportSettings rs = new();
			rs.Title = Title;

			foreach (ReportColumn rc in Columns)
				rs.Columns.Add(rc.Clone());
			foreach (List<string> tg in TagGroups)
				rs.TagGroups.Add(tg.ToList());

			return rs;
		}

		public void CopyFrom(ReportSettings other)
		{
			Title = other.Title;
			Columns = new();
			foreach (ReportColumn rc in other.Columns)
				Columns.Add(rc.Clone());
			TagGroups = new();
			foreach (List<string> tg in other.TagGroups)
				TagGroups.Add(tg.ToList());
		}

		public JsonObject ToJson()
		{
			JsonObject j = new JsonObject();
			j["title"] = Title;
			j["columns"] = new JsonArray(Columns.Select((c) => (JsonNode)c.ToJson()).ToArray());
			j["rows"] = new JsonArray(TagGroups.Select((g) =>
				 new JsonArray(g.Select((t) => (JsonNode)t!).ToArray())
			).ToArray());

			return j;
		}

		public static ReportSettings FromJson(JsonObject j)
		{
			ReportSettings settings = new ReportSettings();
			settings.Title = (string)j["title"]!;
			foreach (JsonNode? n in (JsonArray)j["columns"]!)
			{
				settings.Columns.Add(ReportColumn.FromJson(n!.AsObject()));
			}
			foreach (JsonNode? n in (JsonArray)j["rows"]!)
			{
				settings.TagGroups.Add(n!.AsArray().Select((t) => (string)t!).ToList());
			}

			return settings;
		}
	}

	public class ReportColumn
	{
		public string Header = "?";

		public bool IsRolling = false;
		public RecurringExpense.FrequencyUnits BaseUnit = RecurringExpense.FrequencyUnits.Months;
		public int NumForAverage = 12;


		public DateOnly BeginningOfPeriod
		{
			get
			{
				if (!IsRolling)
				{
					if (BaseUnit == RecurringExpense.FrequencyUnits.Days)
						return Today;
					else if (BaseUnit == RecurringExpense.FrequencyUnits.Weeks)
						return Today.AddDays(-(int)Today.DayOfWeek);
					else if (BaseUnit == RecurringExpense.FrequencyUnits.Months)
						return Today.AddDays(-(Today.Day - 1));
					else
						return Today.AddDays(-(Today.Day - 1)).AddMonths(-(Today.Month - 1));
				}
				else
					return Add(Today, BaseUnit, -NumForAverage).AddDays(1);
			}
		}
		private DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

		private DateOnly Add(DateOnly date, RecurringExpense.FrequencyUnits unit, int amount)
		{
			if (unit == RecurringExpense.FrequencyUnits.Days)
				date = date.AddDays(amount);
			else if (unit == RecurringExpense.FrequencyUnits.Weeks)
				date = date.AddDays(amount * 7);
			else if (unit == RecurringExpense.FrequencyUnits.Months)
				date = date.AddMonths(amount);
			else if (unit == RecurringExpense.FrequencyUnits.Years)
				date = date.AddYears(amount);

			return date;
		}

		public ReportColumn Clone()
		{
			return new ReportColumn()
			{
				Header = Header,
				IsRolling = IsRolling,
				BaseUnit = BaseUnit,
				NumForAverage = NumForAverage,
			};
		}

		public JsonObject ToJson()
		{
			JsonObject j = new JsonObject();
			j["header"] = Header;
			j["rolling"] = IsRolling;
			j["unit"] = (int)BaseUnit;
			j["num"] = NumForAverage;

			return j;
		}

		public static ReportColumn FromJson(JsonObject j)
		{
			return new ReportColumn()
			{
				Header = (string)j["header"]!,
				IsRolling = (bool)j["rolling"]!,
				BaseUnit = (RecurringExpense.FrequencyUnits)(int)j["unit"]!,
				NumForAverage = (int)j["num"]!,
			};
		}
	}
}
