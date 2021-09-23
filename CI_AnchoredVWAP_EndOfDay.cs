#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class CI_AnchoredVWAP_EndOfDay : Indicator
	{
		private double SummV = 0;
		private double SummPV = 0;
		private DateTime anchorDate;

		//public CI_AnchoredVWAP_EndOfDay()
		//{
		//	VendorLicense("CrystalIndicators", "VolumeAnalysisIndicators", "www.crystalindicators.com",
		//		"info@crystalindicators.com", null);
		//}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Anchored Volume Weighted Avarage Price End Of Day";
				Name										= "CI Anchored VWAP EndOfDay";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				//MaximumBarsLookBack 						= MaximumBarsLookBack.Infinite;
				
				//Year										= 2020;
				Year										= DateTime.Now.Year - 1;
				//Month										= 1;
				Month										= DateTime.Now.Month;
				Day											= 1;
				
				AddPlot(new Stroke(Brushes.Turquoise, 2), PlotStyle.Line, "VWAP");
			}
			else if (State == State.Configure)
			{
				anchorDate = new DateTime(Year, Month, Day);
				//this.Name = "";
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar <= BarsRequiredToPlot) return;
			
			if (Time[0] < anchorDate)
				return;
			
			double VWAPValue;
			// VWAP
			SummV = SummV + Volume[0];
			SummPV = SummPV + Typical[0] * Volume[0];
			VWAPValue = SummPV / SummV;
		
			// Plots
			VWAP[0] = VWAPValue;
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Year", Order=1, GroupName="Parameters")]
		public int Year
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, 12)]
		[Display(Name="Month", Order=2, GroupName="Parameters")]
		public int Month
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, 31)]
		[Display(Name="Day", Order=3, GroupName="Parameters")]
		public int Day
		{ get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> VWAP
		{
			get { return Values[0]; }
		}
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private CI_AnchoredVWAP_EndOfDay[] cacheCI_AnchoredVWAP_EndOfDay;
		public CI_AnchoredVWAP_EndOfDay CI_AnchoredVWAP_EndOfDay(int year, int month, int day)
		{
			return CI_AnchoredVWAP_EndOfDay(Input, year, month, day);
		}

		public CI_AnchoredVWAP_EndOfDay CI_AnchoredVWAP_EndOfDay(ISeries<double> input, int year, int month, int day)
		{
			if (cacheCI_AnchoredVWAP_EndOfDay != null)
				for (int idx = 0; idx < cacheCI_AnchoredVWAP_EndOfDay.Length; idx++)
					if (cacheCI_AnchoredVWAP_EndOfDay[idx] != null && cacheCI_AnchoredVWAP_EndOfDay[idx].Year == year && cacheCI_AnchoredVWAP_EndOfDay[idx].Month == month && cacheCI_AnchoredVWAP_EndOfDay[idx].Day == day && cacheCI_AnchoredVWAP_EndOfDay[idx].EqualsInput(input))
						return cacheCI_AnchoredVWAP_EndOfDay[idx];
			return CacheIndicator<CI_AnchoredVWAP_EndOfDay>(new CI_AnchoredVWAP_EndOfDay(){ Year = year, Month = month, Day = day }, input, ref cacheCI_AnchoredVWAP_EndOfDay);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.CI_AnchoredVWAP_EndOfDay CI_AnchoredVWAP_EndOfDay(int year, int month, int day)
		{
			return indicator.CI_AnchoredVWAP_EndOfDay(Input, year, month, day);
		}

		public Indicators.CI_AnchoredVWAP_EndOfDay CI_AnchoredVWAP_EndOfDay(ISeries<double> input , int year, int month, int day)
		{
			return indicator.CI_AnchoredVWAP_EndOfDay(input, year, month, day);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.CI_AnchoredVWAP_EndOfDay CI_AnchoredVWAP_EndOfDay(int year, int month, int day)
		{
			return indicator.CI_AnchoredVWAP_EndOfDay(Input, year, month, day);
		}

		public Indicators.CI_AnchoredVWAP_EndOfDay CI_AnchoredVWAP_EndOfDay(ISeries<double> input , int year, int month, int day)
		{
			return indicator.CI_AnchoredVWAP_EndOfDay(input, year, month, day);
		}
	}
}

#endregion
