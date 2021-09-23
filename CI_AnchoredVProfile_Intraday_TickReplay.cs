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
	public class CI_AnchoredVProfile_Intraday_TickReplay : Indicator
	{

		private DateTime startDay;
		private DateTime endDay;
		private TimeSpan anchorTime;
		private SortedList<double, long> pricesAndVolumes;
		private double curPrice;
		private long curVolume;
		private long sumVolume;
		private int startBarIdx;

		//public CI_AnchoredVProfile_Intraday_TickReplay()
		//{
		//	VendorLicense("CrystalIndicators", "VolumeAnalysisIndicators", "www.crystalindicators.com",
		//		"info@crystalindicators.com", null);
		//}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Anchored Volume Profile Intraday TickReplay";
				Name										= "CI Anchored VProfile Intraday TickReplay";
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
				IsAutoScale									= false;
				
				TimeAnchor									= DateTime.Parse("9:30", System.Globalization.CultureInfo.InvariantCulture);
				VASizePercent								= 70;
				
				AddPlot(new Stroke(Brushes.White, 2), PlotStyle.Square, "POC");
				AddPlot(new Stroke(Brushes.Firebrick, DashStyleHelper.DashDotDot, 2), PlotStyle.Square, "VAHigh");
				AddPlot(new Stroke(Brushes.DarkGreen, DashStyleHelper.DashDotDot, 2), PlotStyle.Square, "VALow");
				

			}
			else if (State == State.Configure)
			{
				anchorTime = new TimeSpan(TimeAnchor.Hour, TimeAnchor.Minute + 1, TimeAnchor.Second);
				//this.Name = "";
				pricesAndVolumes = new SortedList<double, long>();
			}
		}

		protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
		{
			if (marketDataUpdate.MarketDataType == MarketDataType.Last) {
				curPrice = marketDataUpdate.Price;
				
				if (!pricesAndVolumes.ContainsKey(curPrice)) {
					curVolume = marketDataUpdate.Volume;
					pricesAndVolumes.Add(curPrice, curVolume);
				}
				else {
					curVolume = pricesAndVolumes[curPrice] + marketDataUpdate.Volume;
					pricesAndVolumes[curPrice] = curVolume;
				}
				
				sumVolume += marketDataUpdate.Volume;
				
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar <= BarsRequiredToPlot) return;
			
			//Set the first Anchor time
			if (CurrentBar == BarsRequiredToPlot + 1 && TimeAnchor.Date >= Time[0].Date) {
				TimeSpan nowTimeSpan = Time[0] - Time[0].Date;
				TimeSpan anchorTimeSpan = TimeAnchor - TimeAnchor.Date;
				if (nowTimeSpan < anchorTimeSpan)
					TimeAnchor = new DateTime(Time[0].Year, Time[0].Month, Time[0].Day, TimeAnchor.Hour, TimeAnchor.Minute, 0) - new TimeSpan(24, 0, 0);
				else
					TimeAnchor = new DateTime(Time[0].Year, Time[0].Month, Time[0].Day, TimeAnchor.Hour, TimeAnchor.Minute, 0);
			}
			
			//Calculate values
			POC[0] = getPOC();
			calculateVABoundaries(POC[0]);
			
			//Plot VProfile
			for (int i = CurrentBar - startBarIdx; i > 0; i--) {
				POC[i] = POC[0];
				VAHigh[i] = VAHigh[0];
				VALow[i] = VALow[0];
			}
			
			//New period
			if (TimeAnchor.Date != Time[0].Date && ToTime(TimeAnchor) == ToTime(Time[0])) {
				pricesAndVolumes = new SortedList<double, long>();
				sumVolume = 0;
				TimeAnchor = new DateTime(Time[0].Year, Time[0].Month, Time[0].Day, TimeAnchor.Hour, TimeAnchor.Minute, 0);
				startBarIdx = CurrentBar + 1;
			}
		}
		
		#region Miscellaneous
		private double getPOC() {
			long maxVol = 0;
			double POCPrice = Close[0];
			foreach (double price in pricesAndVolumes.Keys) {
				if (pricesAndVolumes[price] > maxVol) {
					maxVol = pricesAndVolumes[price];
					POCPrice = price;
				}
			}
			return POCPrice;
		}
		
		private void calculateVABoundaries(double POCprice) {
			//Check if list is empty
			if (pricesAndVolumes.Count == 0) {
				VAHigh[0] = POCprice;
				VALow[0] = POCprice;
				return;
			}
			int POCIdx = pricesAndVolumes.IndexOfKey(POCprice);
			//Check if there are enough price records to calculate vprofile
			//and set the default values for boundaries
			if (POCIdx - 2 < 0 || POCIdx + 2 >= pricesAndVolumes.Count) {
					VAHigh[0] = pricesAndVolumes.Keys[pricesAndVolumes.Count-1];
					VALow[0] = pricesAndVolumes.Keys[0];
					return;
			}
			
			long volumeArea = sumVolume * VASizePercent / 100;
			long curVolumeSum = pricesAndVolumes[POCprice];
			int lowIdx = POCIdx;
			int highIdx = POCIdx;
			bool lowEnds = false;
			bool highEnds = false;
			
			while (curVolumeSum < volumeArea) {
				
				long lowVol = 0;
				long highVol = 0;
				
				if (!lowEnds) {
					lowVol = pricesAndVolumes.Values[lowIdx - 1] + pricesAndVolumes.Values[lowIdx - 2];
				}
				if (!highEnds) {
					highVol = pricesAndVolumes.Values[highIdx + 1] + pricesAndVolumes.Values[highIdx + 2];
				}
				
				if(highVol >= lowVol) {
					curVolumeSum += highVol;
					highIdx += 2;
				} 
				else {
					curVolumeSum += lowVol;
					lowIdx -= 2;
				}
				
				if (lowIdx - 2 < 0)
					lowEnds = true;
				if (highIdx + 2 >= pricesAndVolumes.Count)
					highEnds = true;
				
				if (lowEnds && highEnds)
					break;
				
			}
			VAHigh[0] = pricesAndVolumes.Keys[highIdx];
			VALow[0] = pricesAndVolumes.Keys[lowIdx];
			
		}
		#endregion

		
		#region Properties
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="TimeAnchor", Order=1, GroupName="Parameters")]
		public DateTime TimeAnchor
		{ get; set; }

		[NinjaScriptProperty]
		[Range(70, 100)]
		[Display(Name="VASizePercent", Order=2, GroupName="Parameters")]
		public int VASizePercent
		{ get; set; }
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> POC
		{
			get { return Values[0]; }
		}
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> VAHigh
		{
			get { return Values[1]; }
		}
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> VALow
		{
			get { return Values[2]; }
		}
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private CI_AnchoredVProfile_Intraday_TickReplay[] cacheCI_AnchoredVProfile_Intraday_TickReplay;
		public CI_AnchoredVProfile_Intraday_TickReplay CI_AnchoredVProfile_Intraday_TickReplay(DateTime timeAnchor, int vASizePercent)
		{
			return CI_AnchoredVProfile_Intraday_TickReplay(Input, timeAnchor, vASizePercent);
		}

		public CI_AnchoredVProfile_Intraday_TickReplay CI_AnchoredVProfile_Intraday_TickReplay(ISeries<double> input, DateTime timeAnchor, int vASizePercent)
		{
			if (cacheCI_AnchoredVProfile_Intraday_TickReplay != null)
				for (int idx = 0; idx < cacheCI_AnchoredVProfile_Intraday_TickReplay.Length; idx++)
					if (cacheCI_AnchoredVProfile_Intraday_TickReplay[idx] != null && cacheCI_AnchoredVProfile_Intraday_TickReplay[idx].TimeAnchor == timeAnchor && cacheCI_AnchoredVProfile_Intraday_TickReplay[idx].VASizePercent == vASizePercent && cacheCI_AnchoredVProfile_Intraday_TickReplay[idx].EqualsInput(input))
						return cacheCI_AnchoredVProfile_Intraday_TickReplay[idx];
			return CacheIndicator<CI_AnchoredVProfile_Intraday_TickReplay>(new CI_AnchoredVProfile_Intraday_TickReplay(){ TimeAnchor = timeAnchor, VASizePercent = vASizePercent }, input, ref cacheCI_AnchoredVProfile_Intraday_TickReplay);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.CI_AnchoredVProfile_Intraday_TickReplay CI_AnchoredVProfile_Intraday_TickReplay(DateTime timeAnchor, int vASizePercent)
		{
			return indicator.CI_AnchoredVProfile_Intraday_TickReplay(Input, timeAnchor, vASizePercent);
		}

		public Indicators.CI_AnchoredVProfile_Intraday_TickReplay CI_AnchoredVProfile_Intraday_TickReplay(ISeries<double> input , DateTime timeAnchor, int vASizePercent)
		{
			return indicator.CI_AnchoredVProfile_Intraday_TickReplay(input, timeAnchor, vASizePercent);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.CI_AnchoredVProfile_Intraday_TickReplay CI_AnchoredVProfile_Intraday_TickReplay(DateTime timeAnchor, int vASizePercent)
		{
			return indicator.CI_AnchoredVProfile_Intraday_TickReplay(Input, timeAnchor, vASizePercent);
		}

		public Indicators.CI_AnchoredVProfile_Intraday_TickReplay CI_AnchoredVProfile_Intraday_TickReplay(ISeries<double> input , DateTime timeAnchor, int vASizePercent)
		{
			return indicator.CI_AnchoredVProfile_Intraday_TickReplay(input, timeAnchor, vASizePercent);
		}
	}
}

#endregion
