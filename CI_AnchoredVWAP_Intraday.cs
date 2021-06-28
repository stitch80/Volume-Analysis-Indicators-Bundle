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
	public class CI_AnchoredVWAP_Intraday : Indicator
	{
		private double SummV = 0;
		private double SummPV = 0;
		private TimeSpan anchorTime;

		public CI_AnchoredVWAP_Intraday()
		{
			VendorLicense("CrystalIndicators", "VolumeAnalysisIndicators", "www.crystalindicators.com",
				"info@crystalindicators.com", null);
		}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Anchored Volume Weighted Avarage Price Intraday";
				Name										= "CI Anchored VWAP Intraday";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				IsAutoScale									= false;
				
				TimeAnchor						= DateTime.Parse("9:30", System.Globalization.CultureInfo.InvariantCulture);
				
				AddPlot(Brushes.Turquoise, "VWAP");
			}
			else if (State == State.Configure)
			{
				//Shift in time
				anchorTime = new TimeSpan(TimeAnchor.Hour, TimeAnchor.Minute + 1, TimeAnchor.Second);
				//this.Name = "";
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar <= BarsRequiredToPlot) return;
			
			double VWAPValue;
			
			if (ToDay(Time[0] - anchorTime) == ToDay(Time[1]) && ToDay(Time[1] - anchorTime) < ToDay(Time[1])) // New Period
			{
				SummV = 0;
				SummPV = 0;
			}
			
			// VWAP
			SummV = SummV + Volume[0];
			SummPV = SummPV + Typical[0] * Volume[0];
			VWAPValue = SummPV / SummV;
		
			// Plots
			VWAP[0] = VWAPValue;
			
		}

		#region Properties
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="TimeAnchor", Order=1, GroupName="Parameters")]
		public DateTime TimeAnchor
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
		private CI_AnchoredVWAP_Intraday[] cacheCI_AnchoredVWAP_Intraday;
		public CI_AnchoredVWAP_Intraday CI_AnchoredVWAP_Intraday(DateTime timeAnchor)
		{
			return CI_AnchoredVWAP_Intraday(Input, timeAnchor);
		}

		public CI_AnchoredVWAP_Intraday CI_AnchoredVWAP_Intraday(ISeries<double> input, DateTime timeAnchor)
		{
			if (cacheCI_AnchoredVWAP_Intraday != null)
				for (int idx = 0; idx < cacheCI_AnchoredVWAP_Intraday.Length; idx++)
					if (cacheCI_AnchoredVWAP_Intraday[idx] != null && cacheCI_AnchoredVWAP_Intraday[idx].TimeAnchor == timeAnchor && cacheCI_AnchoredVWAP_Intraday[idx].EqualsInput(input))
						return cacheCI_AnchoredVWAP_Intraday[idx];
			return CacheIndicator<CI_AnchoredVWAP_Intraday>(new CI_AnchoredVWAP_Intraday(){ TimeAnchor = timeAnchor }, input, ref cacheCI_AnchoredVWAP_Intraday);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.CI_AnchoredVWAP_Intraday CI_AnchoredVWAP_Intraday(DateTime timeAnchor)
		{
			return indicator.CI_AnchoredVWAP_Intraday(Input, timeAnchor);
		}

		public Indicators.CI_AnchoredVWAP_Intraday CI_AnchoredVWAP_Intraday(ISeries<double> input , DateTime timeAnchor)
		{
			return indicator.CI_AnchoredVWAP_Intraday(input, timeAnchor);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.CI_AnchoredVWAP_Intraday CI_AnchoredVWAP_Intraday(DateTime timeAnchor)
		{
			return indicator.CI_AnchoredVWAP_Intraday(Input, timeAnchor);
		}

		public Indicators.CI_AnchoredVWAP_Intraday CI_AnchoredVWAP_Intraday(ISeries<double> input , DateTime timeAnchor)
		{
			return indicator.CI_AnchoredVWAP_Intraday(input, timeAnchor);
		}
	}
}

#endregion
