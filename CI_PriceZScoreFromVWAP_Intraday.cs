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
	public class CI_PriceZScoreFromVWAP_Intraday : Indicator
	{
		private TimeSpan anchorTime;
		private Series<double> SummV;
		private Series<double> SummPV;
		private Series<double> Summ;
		private Series<double> VWAPValue;
		private Series<double> StdDevValue;

		public CI_PriceZScoreFromVWAP_Intraday()
		{
			VendorLicense("CrystalIndicators", "VolumeAnalysisIndicators", "www.crystalindicators.com",
				"info@crystalindicators.com", null);
		}


		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "CI Price ZScore From VWAP Intraday";
				Calculate									= Calculate.OnPriceChange;
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
				IsAutoScale									= true;
				
				TimeAnchor									= DateTime.Parse("9:30", System.Globalization.CultureInfo.InvariantCulture);
				
				AddPlot(new Stroke(Brushes.DarkCyan, 2), PlotStyle.Line, "PriceZScore");

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
				//Shift in time
				anchorTime = new TimeSpan(TimeAnchor.Hour, TimeAnchor.Minute + 1, TimeAnchor.Second);
				//this.Name = "";
				
				
			}
			else if(State == State.DataLoaded)
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

			Print("Inside OnBarUpdate, Current Bar - " + CurrentBar);
						
			Print("Lines Drawn");

			if(CurrentBar == 0)
            {
                Draw.HorizontalLine(this, "ZeroLine", true, 0, Brushes.White, DashStyleHelper.Solid, 2);
                Draw.HorizontalLine(this, "PosSigmaOne", true, 1, Brushes.White, DashStyleHelper.Solid, 1);
                Draw.HorizontalLine(this, "PosSigmaTwo", true, 2, Brushes.Red, DashStyleHelper.Solid, 1);
                Draw.HorizontalLine(this, "PosSigmaThree", true, 3, Brushes.Red, DashStyleHelper.Solid, 2);
                Draw.HorizontalLine(this, "NegSigmaOne", true, -1, Brushes.White, DashStyleHelper.Solid, 1);
                Draw.HorizontalLine(this, "NegSigmaTwo", true, -2, Brushes.Green, DashStyleHelper.Solid, 1);
                Draw.HorizontalLine(this, "NegSigmaThree", true, -3, Brushes.Green, DashStyleHelper.Solid, 2);

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

				if(SummV[0] == 0)
					StdDevValue[0] = 0;
                else
					StdDevValue[0] = Math.Sqrt(Summ[0] / SummV[0]);
				Print(Time[0] + ": StdDevValue[0]: " + StdDevValue[0]);
			}
			else
            {
				//if (ToDay(Time[0] - anchorTime) == ToDay(Time[1]) && ToDay(Time[1] - anchorTime) < ToDay(Time[1])) // New Period
				if (Bars.GetTime(CurrentBar).Hour == TimeAnchor.Hour && Bars.GetTime(CurrentBar).Minute == TimeAnchor.Minute+1)
                {
					Print("Close[0]: " + Close[0] + " Volume[0]: " + Volume[0]);

					SummV[0] = Volume[0];
					Print(Time[0] + ": SummV[0]: " + SummV[0]);

					SummPV[0] = Typical[0] * Volume[0];
					Print(Time[0] + ": SummPV[0]: " + SummPV[0]);

					if(SummV[0] == 0)
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
					if(SummV[0] == 0)
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
			}

			//Plots VScore
			//VScore[0] = (Close[0] - VWAPValue) / StdDevValue;
			//VScore[0] = Math.Round((Close[0] - VWAPValue[0]) / StdDevValue[0], 2);
			if(StdDevValue[0] == 0)
				PriceZScore[0] = 0;
			else
				PriceZScore[0] = (Close[0] - VWAPValue[0]) / StdDevValue[0];
			Print(Time[0] + ": VScore[0]: " + PriceZScore[0]);

			//Print(Time[0] + ": " + SummV[0] + " " + SummPV[0] + " " + VWAPValue[0] + " " + Summ[0] + " " + StdDevValue[0]
			//	+ " " + VScore[0]);
		}

		#region Properties
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="TimeAnchor", Order=1, GroupName="Parameters")]
		public DateTime TimeAnchor
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
		private CI_PriceZScoreFromVWAP_Intraday[] cacheCI_PriceZScoreFromVWAP_Intraday;
		public CI_PriceZScoreFromVWAP_Intraday CI_PriceZScoreFromVWAP_Intraday(DateTime timeAnchor)
		{
			return CI_PriceZScoreFromVWAP_Intraday(Input, timeAnchor);
		}

		public CI_PriceZScoreFromVWAP_Intraday CI_PriceZScoreFromVWAP_Intraday(ISeries<double> input, DateTime timeAnchor)
		{
			if (cacheCI_PriceZScoreFromVWAP_Intraday != null)
				for (int idx = 0; idx < cacheCI_PriceZScoreFromVWAP_Intraday.Length; idx++)
					if (cacheCI_PriceZScoreFromVWAP_Intraday[idx] != null && cacheCI_PriceZScoreFromVWAP_Intraday[idx].TimeAnchor == timeAnchor && cacheCI_PriceZScoreFromVWAP_Intraday[idx].EqualsInput(input))
						return cacheCI_PriceZScoreFromVWAP_Intraday[idx];
			return CacheIndicator<CI_PriceZScoreFromVWAP_Intraday>(new CI_PriceZScoreFromVWAP_Intraday(){ TimeAnchor = timeAnchor }, input, ref cacheCI_PriceZScoreFromVWAP_Intraday);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.CI_PriceZScoreFromVWAP_Intraday CI_PriceZScoreFromVWAP_Intraday(DateTime timeAnchor)
		{
			return indicator.CI_PriceZScoreFromVWAP_Intraday(Input, timeAnchor);
		}

		public Indicators.CI_PriceZScoreFromVWAP_Intraday CI_PriceZScoreFromVWAP_Intraday(ISeries<double> input , DateTime timeAnchor)
		{
			return indicator.CI_PriceZScoreFromVWAP_Intraday(input, timeAnchor);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.CI_PriceZScoreFromVWAP_Intraday CI_PriceZScoreFromVWAP_Intraday(DateTime timeAnchor)
		{
			return indicator.CI_PriceZScoreFromVWAP_Intraday(Input, timeAnchor);
		}

		public Indicators.CI_PriceZScoreFromVWAP_Intraday CI_PriceZScoreFromVWAP_Intraday(ISeries<double> input , DateTime timeAnchor)
		{
			return indicator.CI_PriceZScoreFromVWAP_Intraday(input, timeAnchor);
		}
	}
}

#endregion
