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
	public class CI_AnchoredVProfile_Intraday : Indicator
	{

		private DateTime startDay;
		private DateTime endDay;
		private TimeSpan anchorTime;
		private SortedList<double, long> pricesAndVolumes;
		private double curPrice;
		private long curVolume;
		private long sumVolume;
		private int startBarIdx;
		private bool isAllowedTimeframe;

		//public CI_AnchoredVProfile_Intraday()
		//{
		//	VendorLicense("CrystalIndicators", "VolumeAnalysisIndicators", "www.crystalindicators.com",
		//		"info@crystalindicators.com", null);
		//}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Anchored Volume Profile Intraday";
				Name										= "CI Anchored VProfile Intraday";
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
				AddDataSeries(BarsPeriodType.Minute, 1);
				//startPlotTime = TimeAnchor;
				if (BarsArray[0].BarsPeriod.BarsPeriodType == BarsPeriodType.Minute
				&& BarsArray[0].BarsPeriod.Value < 60)
					isAllowedTimeframe = true;
			}
			
		}
		
		protected override void OnBarUpdate()
		{
			//if (Calculate != Calculate.OnBarClose)
			//	Calculate = Calculate.OnBarClose;

			if (isAllowedTimeframe)
            {
				if (BarsInProgress == 1)
				{
					fillMap();
				}

				if (BarsInProgress == 0)
				{
					//Set the first Anchor time
					if (CurrentBars[0] == 0 && TimeAnchor.Date >= Times[0][0].Date)
					{
						TimeSpan nowTimeSpan = Times[0][0] - Times[0][0].Date;
						TimeSpan anchorTimeSpan = TimeAnchor - TimeAnchor.Date;
						if (nowTimeSpan < anchorTimeSpan)
							TimeAnchor = new DateTime(Times[0][0].Year, Times[0][0].Month, Times[0][0].Day, TimeAnchor.Hour, TimeAnchor.Minute, 0) - new TimeSpan(24, 0, 0);
						else
							TimeAnchor = new DateTime(Times[0][0].Year, Times[0][0].Month, Times[0][0].Day, TimeAnchor.Hour, TimeAnchor.Minute, 0);
					}

					//Calculate values
					POC[0] = getPOC();
					calculateVABoundaries(POC[0]);

					//Plot VProfile
					for (int i = CurrentBars[0] - startBarIdx; i > 0; i--)
					{
						POC[i] = POC[0];
						VAHigh[i] = VAHigh[0];
						VALow[i] = VALow[0];
					}

					//New period
					if (TimeAnchor.Date != Times[0][0].Date
						&& ToTime(TimeAnchor + new TimeSpan(0, 1, 0)) == ToTime(Times[0][0]))
					{
						pricesAndVolumes = new SortedList<double, long>();
						sumVolume = 0;
						TimeAnchor = new DateTime(Times[1][0].Year, Times[1][0].Month, Times[1][0].Day, TimeAnchor.Hour, TimeAnchor.Minute, 0);
						startBarIdx = CurrentBars[0];
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

		private double getPOC() {
			long maxVol = 0;
			double POCPrice = Closes[1][0];
			foreach (double price in pricesAndVolumes.Keys) {
				if (pricesAndVolumes[price] > maxVol) {
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
        #endregion


        #region Properties
        [NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Time Anchor", Order=1, GroupName="Parameters")]
		public DateTime TimeAnchor
		{ get; set; }

		[NinjaScriptProperty]
		[Range(70, 100)]
		[Display(Name="Value Area Size (Percent)", Order=2, GroupName="Parameters")]
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
		private CI_AnchoredVProfile_Intraday[] cacheCI_AnchoredVProfile_Intraday;
		public CI_AnchoredVProfile_Intraday CI_AnchoredVProfile_Intraday(DateTime timeAnchor, int vASizePercent)
		{
			return CI_AnchoredVProfile_Intraday(Input, timeAnchor, vASizePercent);
		}

		public CI_AnchoredVProfile_Intraday CI_AnchoredVProfile_Intraday(ISeries<double> input, DateTime timeAnchor, int vASizePercent)
		{
			if (cacheCI_AnchoredVProfile_Intraday != null)
				for (int idx = 0; idx < cacheCI_AnchoredVProfile_Intraday.Length; idx++)
					if (cacheCI_AnchoredVProfile_Intraday[idx] != null && cacheCI_AnchoredVProfile_Intraday[idx].TimeAnchor == timeAnchor && cacheCI_AnchoredVProfile_Intraday[idx].VASizePercent == vASizePercent && cacheCI_AnchoredVProfile_Intraday[idx].EqualsInput(input))
						return cacheCI_AnchoredVProfile_Intraday[idx];
			return CacheIndicator<CI_AnchoredVProfile_Intraday>(new CI_AnchoredVProfile_Intraday(){ TimeAnchor = timeAnchor, VASizePercent = vASizePercent }, input, ref cacheCI_AnchoredVProfile_Intraday);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.CI_AnchoredVProfile_Intraday CI_AnchoredVProfile_Intraday(DateTime timeAnchor, int vASizePercent)
		{
			return indicator.CI_AnchoredVProfile_Intraday(Input, timeAnchor, vASizePercent);
		}

		public Indicators.CI_AnchoredVProfile_Intraday CI_AnchoredVProfile_Intraday(ISeries<double> input , DateTime timeAnchor, int vASizePercent)
		{
			return indicator.CI_AnchoredVProfile_Intraday(input, timeAnchor, vASizePercent);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.CI_AnchoredVProfile_Intraday CI_AnchoredVProfile_Intraday(DateTime timeAnchor, int vASizePercent)
		{
			return indicator.CI_AnchoredVProfile_Intraday(Input, timeAnchor, vASizePercent);
		}

		public Indicators.CI_AnchoredVProfile_Intraday CI_AnchoredVProfile_Intraday(ISeries<double> input , DateTime timeAnchor, int vASizePercent)
		{
			return indicator.CI_AnchoredVProfile_Intraday(input, timeAnchor, vASizePercent);
		}
	}
}

#endregion
