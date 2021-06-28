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
	public class CI_AnchoredVProfile_EndOfDay : Indicator
	{
		private SortedList<double, long> pricesAndVolumes;
		private double curPrice;
		private long curVolume;
		private long sumVolume;
		private int startBarIdx;
		private DateTime anchorDate;
		private bool isAllowedTimeframe;

		public CI_AnchoredVProfile_EndOfDay()
		{
			VendorLicense("CrystalIndicators", "VolumeAnalysisIndicators", "www.crystalindicators.com",
				"info@crystalindicators.com", null);
		}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Anchored Volume Profile End Of Day";
				Name										= "CI Anchored VProfile End Of Day";
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
				
				//Year										= 2019;
				Year										= DateTime.Now.Year;
				//Month										= 12;
				Month										= DateTime.Now.Month - 1;
				Day											= 1;
				VASizePercent								= 70;
				
				AddPlot(new Stroke(Brushes.White, 2), PlotStyle.Line, "POC");
				AddPlot(new Stroke(Brushes.Firebrick, DashStyleHelper.DashDotDot, 2), PlotStyle.Line, "VAHigh");
				AddPlot(new Stroke(Brushes.DarkGreen, DashStyleHelper.DashDotDot, 2), PlotStyle.Line, "VALow");
			}
			else if (State == State.Configure)
			{
				//this.Name = "";
				pricesAndVolumes = new SortedList<double, long>();
				anchorDate = new DateTime(Year, Month, Day);
				AddDataSeries(BarsPeriodType.Minute, 1);
				if (BarsArray[0].BarsPeriod.BarsPeriodType == BarsPeriodType.Day
				&& BarsArray[0].BarsPeriod.Value >=1)
					isAllowedTimeframe = true;
			}
		}

		//protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
		//{
		//	if (marketDataUpdate.MarketDataType == MarketDataType.Last) {
		//		curPrice = marketDataUpdate.Price;
				
		//		if (!pricesAndVolumes.ContainsKey(curPrice)) {
		//			curVolume = marketDataUpdate.Volume;
		//			pricesAndVolumes.Add(curPrice, curVolume);
		//		}
		//		else {
		//			curVolume = pricesAndVolumes[curPrice] + marketDataUpdate.Volume;
		//			pricesAndVolumes[curPrice] = curVolume;
		//		}
				
		//		sumVolume += marketDataUpdate.Volume;
				
		//	}
		//}

		protected override void OnBarUpdate()
		{
			//if (CurrentBar <= BarsRequiredToPlot) return;
			if (isAllowedTimeframe)
            {
				if (CurrentBars[0] >=0 && Times[0][0].Date < anchorDate)
				{
					pricesAndVolumes = new SortedList<double, long>();
					sumVolume = 0;
					startBarIdx = CurrentBars[0] + 1;
					return;
				}

				if (BarsInProgress == 1)
				{
					fillMap();
				}

				if (BarsInProgress == 0)
                {
					//Calculate values
					POC[0] = getPOC();
					calculateVABoundaries(POC[0]);


					//Plot VProfile
					for (int i = CurrentBars[0] - startBarIdx; i >= 0; i--)
					{
						POC[i] = POC[0];
						VAHigh[i] = VAHigh[0];
						VALow[i] = VALow[0];
					}
				}

					
			}

			
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			base.OnRender(chartControl, chartScale);

			if (!isAllowedTimeframe)
				Draw.TextFixed(this, "ErrorMessage", "Please use timeframes less than 60 minutes for this indicator", TextPosition.Center);
		}

		#region Miscellaneous
		private void fillMap()
		{
			if (State == State.Realtime && BarsArray[1].PercentComplete < 1)
				return;

			double curHigh = BarsArray[1].GetHigh(CurrentBars[1]);
			double curLow = BarsArray[1].GetLow(CurrentBars[1]);
			double curOpen = BarsArray[1].GetOpen(CurrentBars[1]);
			double curClose = BarsArray[1].GetClose(CurrentBars[1]);
			long curVolume = BarsArray[1].GetVolume(CurrentBars[1]);

			//Summurizing total volume of the period (day)
			sumVolume += curVolume;

			//Calculate number of records of prices in Map
			int bodySteps = (int)(Math.Abs(curOpen - curClose) / Instrument.MasterInstrument.TickSize) + 1;
			int highShadowSteps = curClose > curOpen ?
				(int)(Math.Abs(curHigh - curClose) / Instrument.MasterInstrument.TickSize)
				: (int)(Math.Abs(curHigh - curOpen) / Instrument.MasterInstrument.TickSize);
			int lowShadowSteps = curClose > curOpen ?
				(int)(Math.Abs(curOpen - curLow) / Instrument.MasterInstrument.TickSize)
				: (int)(Math.Abs(curClose - curLow) / Instrument.MasterInstrument.TickSize);

			//Calculate atomic volume
			long atomicVolume = curVolume / (2 * bodySteps + highShadowSteps + lowShadowSteps);
			long curVolumeForPrice = 0;

			//Filling number for prices in the high shadow of the bar into the map
			for (int i = 0; i < highShadowSteps; i++)
			{
				double curPrice = curHigh - i * Instrument.MasterInstrument.TickSize;
				if (!pricesAndVolumes.ContainsKey(curPrice))
				{
					curVolumeForPrice = atomicVolume;
					pricesAndVolumes.Add(curPrice, curVolumeForPrice);
				}
				else
				{
					curVolumeForPrice = pricesAndVolumes[curPrice] + atomicVolume;
					pricesAndVolumes[curPrice] = curVolumeForPrice;
				}
			}

			//Filling number for prices in the low shadow of the bar into the map
			for (int i = 0; i < lowShadowSteps; i++)
			{
				double curPrice = curLow + i * Instrument.MasterInstrument.TickSize;
				if (!pricesAndVolumes.ContainsKey(curPrice))
				{
					curVolumeForPrice = atomicVolume;
					pricesAndVolumes.Add(curPrice, curVolumeForPrice);
				}
				else
				{
					curVolumeForPrice = pricesAndVolumes[curPrice] + atomicVolume;
					pricesAndVolumes[curPrice] = curVolumeForPrice;
				}
			}

			//Filling number for prices in the bar into the map
			if (curClose > curOpen)
			{
				for (int i = 0; i < bodySteps; i++)
				{
					double curPrice = curOpen + i * Instrument.MasterInstrument.TickSize;
					if (!pricesAndVolumes.ContainsKey(curPrice))
					{
						curVolumeForPrice = atomicVolume * 2;
						pricesAndVolumes.Add(curPrice, curVolumeForPrice);
					}
					else
					{
						curVolumeForPrice = pricesAndVolumes[curPrice] + atomicVolume * 2;
						pricesAndVolumes[curPrice] = curVolumeForPrice;
					}
				}
			}
			else
			{
				for (int i = 0; i < bodySteps; i++)
				{
					double curPrice = curClose + i * Instrument.MasterInstrument.TickSize;
					if (!pricesAndVolumes.ContainsKey(curPrice))
					{
						curVolumeForPrice = atomicVolume * 2;
						pricesAndVolumes.Add(curPrice, curVolumeForPrice);
					}
					else
					{
						curVolumeForPrice = pricesAndVolumes[curPrice] + atomicVolume * 2;
						pricesAndVolumes[curPrice] = curVolumeForPrice;
					}
				}
			}


		}

		//private double getPOC() {
		//	long maxVol = 0;
		//	double POCPrice = Close[0];
		//	foreach (double price in pricesAndVolumes.Keys) {
		//		if (pricesAndVolumes[price] > maxVol) {
		//			maxVol = pricesAndVolumes[price];
		//			POCPrice = price;
		//		}
		//	}
		//	return POCPrice;
		//}

		private double getPOC()
		{
			long maxVol = 0;
			double POCPrice = Closes[1][0];
			foreach (double price in pricesAndVolumes.Keys)
			{
				if (pricesAndVolumes[price] > maxVol)
				{
					maxVol = pricesAndVolumes[price];
					POCPrice = price;
				}
			}
			return POCPrice;
		}

		private void calculateVABoundaries(double POCprice)
		{
			//Check if list is empty
			if (pricesAndVolumes.Count == 0)
			{
				VAHigh[0] = POCprice;
				VALow[0] = POCprice;
				return;
			}
			int POCIdx = pricesAndVolumes.IndexOfKey(POCprice);
			//Check if there are enough price records to calculate vprofile
			//and set the default values for boundaries
			if (POCIdx - 2 < 0 || POCIdx + 2 >= pricesAndVolumes.Count)
			{
				VAHigh[0] = pricesAndVolumes.Keys[pricesAndVolumes.Count - 1];
				VALow[0] = pricesAndVolumes.Keys[0];
				return;
			}

			long volumeArea = sumVolume * VASizePercent / 100;
			long curVolumeSum = pricesAndVolumes[POCprice];
			int lowIdx = POCIdx;
			int highIdx = POCIdx;
			bool lowEnds = false;
			bool highEnds = false;

			while (curVolumeSum < volumeArea)
			{
				long lowVol = 0;
				long highVol = 0;

				if (!lowEnds)
					lowVol = pricesAndVolumes.Values[lowIdx - 1] + pricesAndVolumes.Values[lowIdx - 2];
				if (!highEnds)
					highVol = pricesAndVolumes.Values[highIdx + 1] + pricesAndVolumes.Values[highIdx + 2];

				if (highVol >= lowVol)
				{
					curVolumeSum += highVol;
					highIdx += 2;
				}
				else
				{
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

			//Print("===========================================");
			//Print(Times[0][0] + " - " + Times[1][0]);
			//Print("sumVolume - " + sumVolume);
			//Print("volumeArea - " + volumeArea);
			//Print("curVolumeSum - " + curVolumeSum);
			//Print("POCIdx - " + POCIdx);
			//Print("lowIdx - " + lowIdx);
			//Print("highIdx - " + highIdx);
			//Print("pricesAndVolumes.size - " + pricesAndVolumes.Count);
			//Print("++++++++++++++++++++++++++++++++");
			//for (int i = 0; i < pricesAndVolumes.Count; i++)
			//         {
			//	Print(pricesAndVolumes.Keys[i] + " - " + pricesAndVolumes.Values[i]);
			//         }
		}

		//private void calculateVABoundaries(double POCprice) {
		//	//Check if list is empty
		//	if (pricesAndVolumes.Count == 0) {
		//		VAHigh[0] = POCprice;
		//		VALow[0] = POCprice;
		//		return;
		//	}
		//	int POCIdx = pricesAndVolumes.IndexOfKey(POCprice);
		//	//Check if there are enough price records to calculate vprofile
		//	//and set the default values for boundaries
		//	if (POCIdx - 2 < 0 || POCIdx + 2 >= pricesAndVolumes.Count) {
		//			VAHigh[0] = pricesAndVolumes.Keys[pricesAndVolumes.Count-1];
		//			VALow[0] = pricesAndVolumes.Keys[0];
		//			return;
		//	}

		//	long volumeArea = sumVolume * VASizePercent / 100;
		//	long curVolumeSum = pricesAndVolumes[POCprice];
		//	int lowIdx = POCIdx;
		//	int highIdx = POCIdx;
		//	bool lowEnds = false;
		//	bool highEnds = false;

		//	while (curVolumeSum < volumeArea) {

		//		long lowVol = 0;
		//		long highVol = 0;

		//		if (!lowEnds) {
		//			lowVol = pricesAndVolumes.Values[lowIdx - 1] + pricesAndVolumes.Values[lowIdx - 2];
		//		}
		//		if (!highEnds) {
		//			highVol = pricesAndVolumes.Values[highIdx + 1] + pricesAndVolumes.Values[highIdx + 2];
		//		}

		//		if(highVol >= lowVol) {
		//			curVolumeSum += highVol;
		//			highIdx += 2;
		//		} 
		//		else {
		//			curVolumeSum += lowVol;
		//			lowIdx -= 2;
		//		}

		//		if (lowIdx - 2 < 0)
		//			lowEnds = true;
		//		if (highIdx + 2 >= pricesAndVolumes.Count)
		//			highEnds = true;

		//		if (lowEnds && highEnds)
		//			break;

		//	}
		//	VAHigh[0] = pricesAndVolumes.Keys[highIdx];
		//	VALow[0] = pricesAndVolumes.Keys[lowIdx];

		//}

		#endregion


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

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name="VASizePercent", Order=4, GroupName="Parameters")]
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
		private CI_AnchoredVProfile_EndOfDay[] cacheCI_AnchoredVProfile_EndOfDay;
		public CI_AnchoredVProfile_EndOfDay CI_AnchoredVProfile_EndOfDay(int year, int month, int day, int vASizePercent)
		{
			return CI_AnchoredVProfile_EndOfDay(Input, year, month, day, vASizePercent);
		}

		public CI_AnchoredVProfile_EndOfDay CI_AnchoredVProfile_EndOfDay(ISeries<double> input, int year, int month, int day, int vASizePercent)
		{
			if (cacheCI_AnchoredVProfile_EndOfDay != null)
				for (int idx = 0; idx < cacheCI_AnchoredVProfile_EndOfDay.Length; idx++)
					if (cacheCI_AnchoredVProfile_EndOfDay[idx] != null && cacheCI_AnchoredVProfile_EndOfDay[idx].Year == year && cacheCI_AnchoredVProfile_EndOfDay[idx].Month == month && cacheCI_AnchoredVProfile_EndOfDay[idx].Day == day && cacheCI_AnchoredVProfile_EndOfDay[idx].VASizePercent == vASizePercent && cacheCI_AnchoredVProfile_EndOfDay[idx].EqualsInput(input))
						return cacheCI_AnchoredVProfile_EndOfDay[idx];
			return CacheIndicator<CI_AnchoredVProfile_EndOfDay>(new CI_AnchoredVProfile_EndOfDay(){ Year = year, Month = month, Day = day, VASizePercent = vASizePercent }, input, ref cacheCI_AnchoredVProfile_EndOfDay);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.CI_AnchoredVProfile_EndOfDay CI_AnchoredVProfile_EndOfDay(int year, int month, int day, int vASizePercent)
		{
			return indicator.CI_AnchoredVProfile_EndOfDay(Input, year, month, day, vASizePercent);
		}

		public Indicators.CI_AnchoredVProfile_EndOfDay CI_AnchoredVProfile_EndOfDay(ISeries<double> input , int year, int month, int day, int vASizePercent)
		{
			return indicator.CI_AnchoredVProfile_EndOfDay(input, year, month, day, vASizePercent);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.CI_AnchoredVProfile_EndOfDay CI_AnchoredVProfile_EndOfDay(int year, int month, int day, int vASizePercent)
		{
			return indicator.CI_AnchoredVProfile_EndOfDay(Input, year, month, day, vASizePercent);
		}

		public Indicators.CI_AnchoredVProfile_EndOfDay CI_AnchoredVProfile_EndOfDay(ISeries<double> input , int year, int month, int day, int vASizePercent)
		{
			return indicator.CI_AnchoredVProfile_EndOfDay(input, year, month, day, vASizePercent);
		}
	}
}

#endregion
