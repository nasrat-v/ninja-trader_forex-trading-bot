#region Using declarations
using System;
using System.Collections;
using System.ComponentModel;
using System.Text;
using System.Diagnostics;  
using System.Windows.Forms;  
using System.Globalization;
using System.IO; 
using NinjaTrader;
using NinjaTrader.Cbi;
//using Woodies.Analytics.Monitor;

#endregion

namespace NinjaTrader.Data
{
	
	/// <summary>
	/// Unirenko Version public source 
	/// </summary>
    public class Unirenko : BarsType
    {
        bool isValid = true;
        static bool registered = Register(new Unirenko());
        double barOpen;
        double barMax;
        double barMin;
        double fakeOpen = 0;

        int barDirection = 0;
        double openOffset = 0;
        double trendOffset = 0;
        double reversalOffset = 0;

        bool maxExceeded = false;
        bool minExceeded = false;

        double tickSize = 0.01;

        private int tmpCount = 0; //  added from RENKO
        private double offset;  //  added from RENKO
        bool configured = false;
        bool tracked = false;

        //PeriodType.Final3


        public Unirenko()
            : base(PeriodType.Custom4)
        {
            try
            {
                



            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());
                throw new Exception(ex.ToString());
            }
        }

       

        public override void Add(Bars bars, double open, double high, double low, double close, DateTime time, long volume, bool isRealtime)
        {
   
			
			//	******************* ADDED FROM RENKO CODE TO SOLVE MEMORY LEAK	****************************		
			// brick size is the trendOffset (Value) + openOffset (BasePeriodValue)
			offset = (bars.Period.Value + bars.Period.BasePeriodValue) * bars.Instrument.MasterInstrument.TickSize; 

			if (bars.Count < tmpCount && bars.Count > 0) // reset cache when bars are trimmed
			{

				barMax	= bars.GetClose(bars.Count - 1) + offset;
				barMin	= bars.GetClose(bars.Count - 1) - offset;
						
				
			}
			//	 ********************************************************************************************
			
			
			//### First Bar
            if ((bars.Count == 0) || bars.IsNewSession(time, isRealtime))
            {
				tickSize = bars.Instrument.MasterInstrument.TickSize;

					//### Parse Long Param Specification
				if ( bars.Period.Value >= 1000000 ) {
					int d; string str = bars.Period.Value.ToString("000000000");
					d=0; Int32.TryParse(str.Substring(0,3), out d); bars.Period.Value  = d;
					d=0; Int32.TryParse(str.Substring(3,3), out d); bars.Period.Value2 = d;
					d=0; Int32.TryParse(str.Substring(6,3), out d); bars.Period.BasePeriodValue = d;
				}
				
				//****** ADDED FROM RENKO *****************************************************************
				if (bars.Count != 0)
				{
					// close out last bar in session and set open == close
					Bar lastBar = (Bar)bars.Get(bars.Count - 1);
					bars.RemoveLastBar();  // Note: bar is now just a local var and not in series!
					AddBar(bars, lastBar.Close, lastBar.High, lastBar.Low, lastBar.Close, lastBar.Time, lastBar.Volume, isRealtime);

					
				}				
		
				//****************************************************************************************
				

                trendOffset    = bars.Period.Value  * bars.Instrument.MasterInstrument.TickSize;
                reversalOffset = bars.Period.Value2 * bars.Instrument.MasterInstrument.TickSize;
				//bars.Period.BasePeriodValue = bars.Period.Value;	//### Remove to customize OpenOffset
				openOffset = Math.Ceiling((double)bars.Period.BasePeriodValue * 1) * bars.Instrument.MasterInstrument.TickSize;

                barOpen = close;
                barMax  = barOpen + (trendOffset * barDirection);
                barMin  = barOpen - (trendOffset * barDirection);


                AddBar(bars, barOpen, barOpen, barOpen, barOpen, time, volume, isRealtime);
            }
            	//### Subsequent Bars
            else
            {
                Data.Bar bar = (Bar)bars.Get(bars.Count - 1);
				// *************ADDED FROM RENKO CODE (to deal with '0' values at Market Replay) ************
				if (barMax == 0 || barMin == 0)  //Not sure why, but happens
				{
					


					// trendOffset was also '0', so need to reinitialize
					trendOffset    = bars.Period.Value  * bars.Instrument.MasterInstrument.TickSize;
					reversalOffset = bars.Period.Value2 * bars.Instrument.MasterInstrument.TickSize;
					openOffset = Math.Ceiling((double)bars.Period.BasePeriodValue * 1) * bars.Instrument.MasterInstrument.TickSize;					

					if (bars.Count == 1)
					{
						barMax  = bar.Open + trendOffset;
               			barMin  = bar.Open - trendOffset;
					}
					else if (bars.GetClose(bars.Count - 2) > bars.GetOpen(bars.Count - 2))
					{
						barMax	= bars.GetClose(bars.Count - 2) + trendOffset;
						barMin	= bars.GetClose(bars.Count - 2) - trendOffset * 2;
					}
					else
					{
						barMax	= bars.GetClose(bars.Count - 2) + trendOffset * 2;
						barMin	= bars.GetClose(bars.Count - 2) - trendOffset;
					}
					
				}
		
				// ************************************************************************************************
				
                maxExceeded  = bars.Instrument.MasterInstrument.Compare(close, barMax) > 0 ? true : false;
                minExceeded  = bars.Instrument.MasterInstrument.Compare(close, barMin) < 0 ? true : false;

                //### Defined Range Exceeded?
                if ( maxExceeded || minExceeded )
                {
                    double thisClose = maxExceeded ? Math.Min(close, barMax) : minExceeded ? Math.Max(close, barMin) : close;// thisClose is the minimum of BarMax and close (maxExceeded)


					barDirection     = maxExceeded ? 1 : minExceeded ? -1 : 0;
                    fakeOpen = thisClose - (openOffset * barDirection);		//### Fake Open is halfway down the bar

                    	//### Close Current Bar
                    UpdateBar(bars, bar.Open, (maxExceeded ? thisClose : bar.High), (minExceeded ? thisClose : bar.Low), thisClose, time, volume, isRealtime);

                    	//### Add New Bar
					barOpen = close;
					barMax  = thisClose + ((barDirection>0 ? trendOffset : reversalOffset) );
					barMin  = thisClose - ((barDirection>0 ? reversalOffset : trendOffset) );
					


					AddBar(bars, fakeOpen, (maxExceeded ? thisClose : fakeOpen), (minExceeded ? thisClose : fakeOpen), thisClose, time, volume, isRealtime);
                }
                	//### Current Bar Still Developing
                else
                {
                    UpdateBar(bars, bar.Open, (close > bar.High ? close : bar.High), (close < bar.Low ? close : bar.Low), close, time, volume, isRealtime);
                }
            }

            bars.LastPrice = close;
			
			tmpCount		= bars.Count; // ADDED FROM RENKO CODE
        }

        public override PropertyDescriptorCollection GetProperties(PropertyDescriptor propertyDescriptor, Period period, Attribute[] attributes)
        {
            PropertyDescriptorCollection properties = base.GetProperties(propertyDescriptor, period, attributes);
			properties.Remove(properties.Find("BasePeriodType",  true));
			properties.Remove(properties.Find("PointAndFigurePriceType", true));
			properties.Remove(properties.Find("ReversalType", true));

			
			Gui.Design.DisplayNameAttribute.SetDisplayName(properties, "Value2", "\r\rReversal");
			Gui.Design.DisplayNameAttribute.SetDisplayName(properties, "Value",  "\r\rTrend");
			Gui.Design.DisplayNameAttribute.SetDisplayName(properties, "BasePeriodValue",  "\r\rOpen Offset");
			
            return properties;
        }

        public override void ApplyDefaults(Gui.Chart.BarsData barsData)
        {
            barsData.Period.Value = 4;				//### Trend    Value
            barsData.Period.Value2 = 20;				//### Reversal Value
            barsData.Period.BasePeriodValue = 10;	//### Open Offset Value
            barsData.DaysBack = 16;
        }

        public override string ToString(Period period)
        {
            return "CPC URenko:" + period.Value + " R:" + period.Value2;
        }
		

        public override PeriodType BuiltFrom
        {
            get { return PeriodType.Tick; }
        }

        public override string ChartDataBoxDate(DateTime time)
        {
            return time.ToString(Cbi.Globals.CurrentCulture.DateTimeFormat.ShortDatePattern);
        }

        public override string ChartLabel(Gui.Chart.ChartControl chartControl, DateTime time)
        {
            return time.ToString(chartControl.LabelFormatTick, Cbi.Globals.CurrentCulture);
        }

        public override object Clone()
        {

            return new Unirenko();
        }

        public override int GetInitialLookBackDays(Period period, int barsBack)
        {
            return 8;
        }

        public override int DefaultValue
        {
            get { return 10; }
        }

        public override string DisplayName
        {
            get { return "Uni Renko"; }
        }


        public override double GetPercentComplete(Bars bars, DateTime now)
        {
            return 0;
        }

        public override bool IsIntraday
        {
            get { return true; }
        }

//        public override Gui.Chart.ChartStyleType[] ChartStyleTypesSupported
//        {
//            get { return new Gui.Chart.ChartStyleType[] { Gui.Chart.ChartStyleType.Custom9, Gui.Chart.ChartStyleType.Box, Gui.Chart.ChartStyleType.LineOnClose, Gui.Chart.ChartStyleType.HiLoBars }; }
//        }


    }
}