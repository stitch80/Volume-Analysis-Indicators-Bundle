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
	public class CI_AnchoredStdDevBands_EndOfDay : Indicator
	{
		private double SummV = 0;
		private double SummPV = 0;
		private double Summ = 0;
		private int N = 0;
		private DateTime anchorDate;

		//public CI_AnchoredStdDevBands_EndOfDay()
		//{
		//	VendorLicense("CrystalIndicators", "VolumeAnalysisIndicators", "www.crystalindicators.com",
		//		"info@crystalindicators.com", null);
		//}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Standard Deviations Bands End Of Day";
				Name										= "CI Standard Deviations Bands EndOfDay";
				Calculate									= Calculate.OnPriceChange;
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
				
				//Year										= 2020;
				Year										= DateTime.Now.Year - 1;
				//Month										= 1;
				Month										= DateTime.Now.Month;
				Day											= 1;
				Sigmas										= 3;
				
				AddPlot(new Stroke(Brushes.Maroon, DashStyleHelper.Dash, 2), PlotStyle.Line, "HighBand");
				AddPlot(new Stroke(Brushes.DarkGreen, DashStyleHelper.Dash, 2), PlotStyle.Line, "LowBand");
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
			double StdDevValue;
			double HighBandValue;
			double LowBandValue;
			
			// VWAP
			SummV = SummV + Volume[0];
			SummPV = SummPV + Typical[0] * Volume[0];
			VWAPValue = SummV == 0 ? Typical[0] : SummPV / SummV;
			
			// Variance
			Summ += (Close[0] - VWAPValue) * (Close[0] - VWAPValue);
			N++;
			
			// Standard Deviation
			StdDevValue = Math.Sqrt(Summ / N);
			
			// Standard Deviation Bands
			HighBandValue = VWAPValue + Sigmas * StdDevValue;
			LowBandValue = VWAPValue - Sigmas * StdDevValue;
			
			//Plots
			HighBand[0] = HighBandValue;
			LowBand[0] = LowBandValue;
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Year", Order=1, GroupName="Parameters")]
		public int Year
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Month", Order=2, GroupName="Parameters")]
		public int Month
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Day", Order=3, GroupName="Parameters")]
		public int Day
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Sigmas", Order=4, GroupName="Parameters")]
		public int Sigmas
		{ get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> HighBand
		{
			get { return Values[0]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> LowBand
		{
			get { return Values[1]; }
		}
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private CI_AnchoredStdDevBands_EndOfDay[] cacheCI_AnchoredStdDevBands_EndOfDay;
		public CI_AnchoredStdDevBands_EndOfDay CI_AnchoredStdDevBands_EndOfDay(int year, int month, int day, int sigmas)
		{
			return CI_AnchoredStdDevBands_EndOfDay(Input, year, month, day, sigmas);
		}

		public CI_AnchoredStdDevBands_EndOfDay CI_AnchoredStdDevBands_EndOfDay(ISeries<double> input, int year, int month, int day, int sigmas)
		{
			if (cacheCI_AnchoredStdDevBands_EndOfDay != null)
				for (int idx = 0; idx < cacheCI_AnchoredStdDevBands_EndOfDay.Length; idx++)
					if (cacheCI_AnchoredStdDevBands_EndOfDay[idx] != null && cacheCI_AnchoredStdDevBands_EndOfDay[idx].Year == year && cacheCI_AnchoredStdDevBands_EndOfDay[idx].Month == month && cacheCI_AnchoredStdDevBands_EndOfDay[idx].Day == day && cacheCI_AnchoredStdDevBands_EndOfDay[idx].Sigmas == sigmas && cacheCI_AnchoredStdDevBands_EndOfDay[idx].EqualsInput(input))
						return cacheCI_AnchoredStdDevBands_EndOfDay[idx];
			return CacheIndicator<CI_AnchoredStdDevBands_EndOfDay>(new CI_AnchoredStdDevBands_EndOfDay(){ Year = year, Month = month, Day = day, Sigmas = sigmas }, input, ref cacheCI_AnchoredStdDevBands_EndOfDay);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.CI_AnchoredStdDevBands_EndOfDay CI_AnchoredStdDevBands_EndOfDay(int year, int month, int day, int sigmas)
		{
			return indicator.CI_AnchoredStdDevBands_EndOfDay(Input, year, month, day, sigmas);
		}

		public Indicators.CI_AnchoredStdDevBands_EndOfDay CI_AnchoredStdDevBands_EndOfDay(ISeries<double> input , int year, int month, int day, int sigmas)
		{
			return indicator.CI_AnchoredStdDevBands_EndOfDay(input, year, month, day, sigmas);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.CI_AnchoredStdDevBands_EndOfDay CI_AnchoredStdDevBands_EndOfDay(int year, int month, int day, int sigmas)
		{
			return indicator.CI_AnchoredStdDevBands_EndOfDay(Input, year, month, day, sigmas);
		}

		public Indicators.CI_AnchoredStdDevBands_EndOfDay CI_AnchoredStdDevBands_EndOfDay(ISeries<double> input , int year, int month, int day, int sigmas)
		{
			return indicator.CI_AnchoredStdDevBands_EndOfDay(input, year, month, day, sigmas);
		}
	}
}

#endregion
