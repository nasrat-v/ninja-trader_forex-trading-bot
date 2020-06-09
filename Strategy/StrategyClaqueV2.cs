#region Using declarations
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Indicator;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Strategy;
#endregion

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    /// <summary>
    /// 
    /// </summary>
    [Description("Une strategie un peu moins claqué au sol - v2")]
    public class StrategyClaqueV2 : Strategy
    {
        #region Variables
        // Wizard generated variables
		private int minimalGapForPrepaSell = 0;

        // User defined variables (add any user defined variables below)
        private bool isOnBuy;
        private bool isOnSell;
		private bool modePrepa;
		private bool modeAction;
        #endregion

        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {
			// Calculate on the close of each bar
    		CalculateOnBarClose = false;
			modePrepa = false;
			modeAction = false;
        }
		
		private double getEMA()
		{
			return (EMA(0)[0]);
		}
		
		private double getFirstHighEMA()
		{
			return (EMA(High, 1)[0]);
		}
		
		private double getSecondHighEMA()
		{
			return (EMA(High, 2)[0]);
		}
		
		private double getFirstLowEMA()
		{
			return (EMA(Low, 1)[0]);
		}
		
		private double getSecondLowEMA()
		{
			return (EMA(Low, 2)[0]);
		}
		
		private double getBarSize()
		{
            return ((Input[0] - Open[0]) / TickSize);
		}
		
        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
			Print("barsize : " + getBarSize().ToString());
			//Print("EMA : " + getEMA().ToString());
			//Print("Bleu foncé : " + getSecondHighEMA().ToString());
			if (!modePrepa && !modeAction && shouldPrepareSell())
			{
				Print("Mode prépa");
				modePrepa = true;
			}
			if (modePrepa && !modeAction && shouldEnterSell())
			{
				Print("Enter");
				modeAction = true;
				modePrepa = false;
				EnterShortStop(GetCurrentBid(), "short stop");
				Print("CurrentBid : " + GetCurrentBid().ToString());
			}
			if (modeAction && !modePrepa && shouldExitSell())
			{
				Print("Exit");
				ExitShort("short stop");
				modeAction = false;
			}
        }
		
		private bool shouldPrepareSell() 
		{
			return (getEMA() > getSecondHighEMA() && isMinimalGapForPrepaSell());
		}
		
		private bool shouldEnterSell()
		{
			return (getEMA() < getSecondHighEMA());
		}
		
		private bool isMinimalGapForPrepaSell()
		{
			return ((getFirstHighEMA() - getSecondHighEMA()) >= (minimalGapForPrepaSell * TickSize));
		}
		
		private bool shouldExitSell()
		{
			return (getFirstHighEMA() < getSecondHighEMA());
		}

        private void showDebug(string msg, double value)
        {
            Print("EMA: " + getEMA().ToString()); // violette
			Print("EMA High 1: " + getFirstHighEMA().ToString()); // bleu clair
			Print("EMA High 2: " + getSecondHighEMA().ToString()); // bleu foncé
			Print("EMA Low 1: " + getFirstLowEMA().ToString()); // vert clair
			Print("EMA Low 2: " + getSecondLowEMA().ToString()); // vert foncé
            //Print(msg + value.ToString());
        }

        #region Properties
        [Description("Ecart de points minimum requit entre l'EMA High 1 et l'EMA High 2 pour pouvoir entrer sur un ordre de vente.")]
        [GridCategory("Parameters")]
        public int MinimalGapForPrepaSell
        {
            get { return minimalGapForPrepaSell; }
            set { minimalGapForPrepaSell = value; }
        }
        #endregion
    }
}
