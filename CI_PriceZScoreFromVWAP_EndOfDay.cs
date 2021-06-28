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
	public class CI_PriceZScoreFromVWAP_EndOfDay : Indicator
	{
		//private double SummV = 0;
		//private double SummPV = 0;
		//private double Summ = 0;
		private DateTime anchorDate;
		private Series<double> SummV;
		private Series<double> SummPV;
		private Series<double> Summ;
		private Series<double> VWAPValue;
		private Series<double> StdDevValue;

		public CI_PriceZScoreFromVWAP_EndOfDay()
		{
			VendorLicense("CrystalIndicators", "VolumeAnalysisIndicators", "www.crystalindicators.com",
				"info@crystalindicators.com", null);
		}

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "CI Price ZScore From VWAP EndOfDay";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= false;
				DrawHorizontalGridLines						= false;
				DrawVerticalGridLines						= false;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				//MaximumBarsLookBack 						= MaximumBarsLookBack.Infinite;
				
				//Year					= 2019;
				Year					= DateTime.Now.Year - 1;
				//Month					= 1;
				Month					= DateTime.Now.Month;
				Day						= 1;
				
				AddPlot(Brushes.Aqua, "PriceZScore");

				//AddLine(new Stroke(Brushes.White, DashStyleHelper.Solid, 2), 0, "ZeroLine");
				//AddLine(new Stroke(Brushes.White, DashStyleHelper.Solid, 1), 1, "PosSigmaOne");
				//AddLine(new Stroke(Brushes.Red, DashStyleHelper.Solid, 1), 2, "PosSigmaTwo");
				//AddLine(new Stroke(Brushes.Red, DashStyleHelper.Solid, 2), 3, "PosSigmaThree");
				//AddLine(new Stroke(Brushes.White, DashStyleHelper.Solid, 1), -1, "NegSigmaOne");
				//AddLine(new Stroke(Brushes.Green, DashStyleHelper.Solid, 1), -2, "NegSigmaTwo");
				//AddLine(new Stroke(Brushes.Green, DashStyleHelper.Solid, 2), -3, "NegSigmaThree");
			}
			else if (State == State.Configure)
			{
				anchorDate = new DateTime(Year, Month, Day);
				//this.Name = "";
			}
			else if (State == State.DataLoaded)
			{
				SummV = new Series<double>(this);
				SummPV = new Series<double>(this);
				Summ = new Series<double>(this);
				VWAPValue = new Series<double>(this);
				StdDevValue = new Series<double>(this);
			}
		}

		protected override void OnBarUpdate()
		{
			//if (CurrentBar <= BarsRequiredToPlot) return;
			if (CurrentBar == 0)
            {
				Draw.HorizontalLine(this, "ZeroLine", true, 0, Brushes.White, DashStyleHelper.Solid, 2);
				Draw.HorizontalLine(this, "PosSigmaOne", true, 1, Brushes.White, DashStyleHelper.Solid, 1);
				Draw.HorizontalLine(this, "PosSigmaTwo", true, 2, Brushes.Red, DashStyleHelper.Solid, 1);
				Draw.HorizontalLine(this, "PosSigmaThree", true, 3, Brushes.Red, DashStyleHelper.Solid, 2);
				Draw.HorizontalLine(this, "NegSigmaOne", true, -1, Brushes.White, DashStyleHelper.Solid, 1);
				Draw.HorizontalLine(this, "NegSigmaTwo", true, -2, Brushes.Green, DashStyleHelper.Solid, 1);
				Draw.HorizontalLine(this, "NegSigmaThree", true, -3, Brushes.Green, DashStyleHelper.Solid, 2);
			}

			if (Time[0] < anchorDate)
				return;
			else if (Time[0] == anchorDate)
            {
                SummV[0] = Volume[0];
				Print(Time[0] + ": SummV[0]: " + SummV[0]);

				SummPV[0] = Typical[0] * Volume[0];
				Print(Time[0] + ": SummPV[0]: " + SummPV[0]);

				if (SummV[0] == 0)
					VWAPValue[0] = Typical[0];
				else
					VWAPValue[0] = SummPV[0] / SummV[0];
				Print(Time[0] + ": VWAPValue[0]: " + VWAPValue[0]);

				Summ[0] = Math.Pow((Close[0] - VWAPValue[0]), 2) * Volume[0];
				Print(Time[0] + ": Summ[0]: " + Summ[0]);

				if (SummV[0] == 0)
					StdDevValue[0] = 0;
				else
					StdDevValue[0] = Math.Sqrt(Summ[0] / SummV[0]);
				Print(Time[0] + ": StdDevValue[0]: " + StdDevValue[0]);
			}
			else
            {
				// VWAP
				//SummV +=  Volume[0];
				SummV[0] = SummV[1] + Volume[0];
				Print(Time[0] + ": SummV[0]: " + SummV[0]);

				//SummPV += Typical[0] * Volume[0];
				SummPV[0] = SummPV[1] + Typical[0] * Volume[0];
				Print(Time[0] + ": SummPV[0]: " + SummPV[0]);

				//VWAPValue = SummV == 0 ? Typical[0] : SummPV / SummV;
				if (SummV[0] == 0)
					VWAPValue[0] = Typical[0];
				else
					VWAPValue[0] = SummPV[0] / SummV[0];
				Print(Time[0] + ": VWAPValue[0]: " + VWAPValue[0]);

				// Variance
				//Summ += Math.Pow((Close[0] - VWAPValue), 2) * Volume[0];
				Summ[0] = Summ[1] + Math.Pow((Close[0] - VWAPValue[0]), 2) * Volume[0];
				Print(Time[0] + ": Summ[0]: " + Summ[0]);

				// Standard Deviation
				//StdDevValue = Math.Sqrt(Summ / SummV);
				if (SummV[0] == 0)
					StdDevValue[0] = 0;
				else
					StdDevValue[0] = Math.Sqrt(Summ[0] / SummV[0]);
				Print(Time[0] + ": StdDevValue[0]: " + StdDevValue[0]);
			}

			if (StdDevValue[0] == 0)
				PriceZScore[0] = 0;
			else
				PriceZScore[0] = (Close[0] - VWAPValue[0]) / StdDevValue[0];
			Print(Time[0] + ": VScore[0]: " + PriceZScore[0]);

			//double VWAPValue;
			//double StdDevValue;

			


			//// VWAP
			//SummV += Volume[0];
			//SummPV += Typical[0] * Volume[0];
			//VWAPValue = SummV == 0 ? Typical[0] : SummPV / SummV;
			
			//// Variance
			//Summ += Math.Pow((Close[0] - VWAPValue), 2) * Volume[0];
			
			//// Standard Deviation
			//StdDevValue = Math.Sqrt(Summ / SummV);
			
			////Plots VScore
			//VScore[0] = (Close[0] - VWAPValue) / StdDevValue;
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
		public Series<double> PriceZScore
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
		private CI_PriceZScoreFromVWAP_EndOfDay[] cacheCI_PriceZScoreFromVWAP_EndOfDay;
		public CI_PriceZScoreFromVWAP_EndOfDay CI_PriceZScoreFromVWAP_EndOfDay(int year, int month, int day)
		{
			return CI_PriceZScoreFromVWAP_EndOfDay(Input, year, month, day);
		}

		public CI_PriceZScoreFromVWAP_EndOfDay CI_PriceZScoreFromVWAP_EndOfDay(ISeries<double> input, int year, int month, int day)
		{
			if (cacheCI_PriceZScoreFromVWAP_EndOfDay != null)
				for (int idx = 0; idx < cacheCI_PriceZScoreFromVWAP_EndOfDay.Length; idx++)
					if (cacheCI_PriceZScoreFromVWAP_EndOfDay[idx] != null && cacheCI_PriceZScoreFromVWAP_EndOfDay[idx].Year == year && cacheCI_PriceZScoreFromVWAP_EndOfDay[idx].Month == month && cacheCI_PriceZScoreFromVWAP_EndOfDay[idx].Day == day && cacheCI_PriceZScoreFromVWAP_EndOfDay[idx].EqualsInput(input))
						return cacheCI_PriceZScoreFromVWAP_EndOfDay[idx];
			return CacheIndicator<CI_PriceZScoreFromVWAP_EndOfDay>(new CI_PriceZScoreFromVWAP_EndOfDay(){ Year = year, Month = month, Day = day }, input, ref cacheCI_PriceZScoreFromVWAP_EndOfDay);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.CI_PriceZScoreFromVWAP_EndOfDay CI_PriceZScoreFromVWAP_EndOfDay(int year, int month, int day)
		{
			return indicator.CI_PriceZScoreFromVWAP_EndOfDay(Input, year, month, day);
		}

		public Indicators.CI_PriceZScoreFromVWAP_EndOfDay CI_PriceZScoreFromVWAP_EndOfDay(ISeries<double> input , int year, int month, int day)
		{
			return indicator.CI_PriceZScoreFromVWAP_EndOfDay(input, year, month, day);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.CI_PriceZScoreFromVWAP_EndOfDay CI_PriceZScoreFromVWAP_EndOfDay(int year, int month, int day)
		{
			return indicator.CI_PriceZScoreFromVWAP_EndOfDay(Input, year, month, day);
		}

		public Indicators.CI_PriceZScoreFromVWAP_EndOfDay CI_PriceZScoreFromVWAP_EndOfDay(ISeries<double> input , int year, int month, int day)
		{
			return indicator.CI_PriceZScoreFromVWAP_EndOfDay(input, year, month, day);
		}
	}
}

#endregion
