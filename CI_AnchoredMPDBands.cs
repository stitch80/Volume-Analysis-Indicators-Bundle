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
	public class CI_AnchoredMPDBands : Indicator
	{
		private double SummV = 0;
		private double SummPV = 0;
		private double Summ = 0;
		private int N = 0;
		private double DailyHigh;
		private double DailyLow;
		private TimeSpan anchorTime;

		//public CI_AnchoredMPDBands()
		//{
		//	VendorLicense("CrystalIndicators", "VolumeAnalysisIndicators", "www.crystalindicators.com",
		//		"info@crystalindicators.com", null);
		//}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Maximum Permissible Deviation Bands";
				Name										= "CI MPD Bands";
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
				IsAutoScale									= false;
				
				TimeAnchor									= DateTime.Parse("9:30", System.Globalization.CultureInfo.InvariantCulture);
				
				AddPlot(new Stroke(Brushes.Maroon, DashStyleHelper.Dot, 2), PlotStyle.Line, "HighMPDBand");
				AddPlot(new Stroke(Brushes.DarkGreen, DashStyleHelper.Dot, 2), PlotStyle.Line, "LowMPDBand");
				
			}
			else if (State == State.Configure)
			{
				anchorTime = new TimeSpan(TimeAnchor.Hour, TimeAnchor.Minute + 1, TimeAnchor.Second);
				//this.Name = "";
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar <= BarsRequiredToPlot) return;
			
			double VWAPValue;
			double StdDevValue;
			double HighMPDBandValue;
			double LowMPDBandValue;
			
			if (ToDay(Time[0] - anchorTime) == ToDay(Time[1]) && ToDay(Time[1] - anchorTime) < ToDay(Time[1])) // New Period
			{
				SummV = 0;
				SummPV = 0;
				Summ = 0;
				N = 0;
				DailyHigh = High[0];
				DailyLow = Low[0];
			}
			else {
				DailyHigh = High[0] > DailyHigh ? High[0] : DailyHigh;
				DailyLow = Low[0] < DailyLow ? Low[0] : DailyLow;
			}
			
			// VWAP
			SummV = SummV + Volume[0];
			SummPV = SummPV + Typical[0] * Volume[0];
			VWAPValue = SummV == 0 ? Typical[0] : SummPV / SummV;
			
			// MPD Bands
			HighMPDBandValue = VWAPValue + (DailyHigh - DailyLow) / 2;
			LowMPDBandValue = VWAPValue - (DailyHigh - DailyLow) / 2;
			
			//Plots
			HighMPDBand[0] = HighMPDBandValue;
			LowMPDBand[0] = LowMPDBandValue;
		}

		#region Properties
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="TimeAnchor", Order=1, GroupName="Parameters")]
		public DateTime TimeAnchor
		{ get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> HighMPDBand
		{
			get { return Values[0]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> LowMPDBand
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
		private CI_AnchoredMPDBands[] cacheCI_AnchoredMPDBands;
		public CI_AnchoredMPDBands CI_AnchoredMPDBands(DateTime timeAnchor)
		{
			return CI_AnchoredMPDBands(Input, timeAnchor);
		}

		public CI_AnchoredMPDBands CI_AnchoredMPDBands(ISeries<double> input, DateTime timeAnchor)
		{
			if (cacheCI_AnchoredMPDBands != null)
				for (int idx = 0; idx < cacheCI_AnchoredMPDBands.Length; idx++)
					if (cacheCI_AnchoredMPDBands[idx] != null && cacheCI_AnchoredMPDBands[idx].TimeAnchor == timeAnchor && cacheCI_AnchoredMPDBands[idx].EqualsInput(input))
						return cacheCI_AnchoredMPDBands[idx];
			return CacheIndicator<CI_AnchoredMPDBands>(new CI_AnchoredMPDBands(){ TimeAnchor = timeAnchor }, input, ref cacheCI_AnchoredMPDBands);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.CI_AnchoredMPDBands CI_AnchoredMPDBands(DateTime timeAnchor)
		{
			return indicator.CI_AnchoredMPDBands(Input, timeAnchor);
		}

		public Indicators.CI_AnchoredMPDBands CI_AnchoredMPDBands(ISeries<double> input , DateTime timeAnchor)
		{
			return indicator.CI_AnchoredMPDBands(input, timeAnchor);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.CI_AnchoredMPDBands CI_AnchoredMPDBands(DateTime timeAnchor)
		{
			return indicator.CI_AnchoredMPDBands(Input, timeAnchor);
		}

		public Indicators.CI_AnchoredMPDBands CI_AnchoredMPDBands(ISeries<double> input , DateTime timeAnchor)
		{
			return indicator.CI_AnchoredMPDBands(input, timeAnchor);
		}
	}
}

#endregion
