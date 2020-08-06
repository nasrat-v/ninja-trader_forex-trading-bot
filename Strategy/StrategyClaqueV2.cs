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
		enum StateAutomate 
		{
			PREPA,
			ENTER,
			FIRST_EXIT,
			SECOND_EXIT,
			WAITING
		};
		
        #region Variables
        // Wizard generated variables
		private int minimalGapForPrepaSell = 0;
		private double percentageForSecondExit = 0.5; // la moitié

        // User defined variables (add any user defined variables below)
        private bool isOnBuy;
        private bool isOnSell;
		private bool modePrepa;
		private bool modeAction;
		private bool firstVolumeExited;
		private double profitAfterFirstExit;
		private double maxProfitAfterFirstExit;
		private double baseValueEMAWhenFirstExit;
		private StateAutomate state;
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
			firstVolumeExited = false;
			profitAfterFirstExit = 0;
			maxProfitAfterFirstExit = 0;
			baseValueEMAWhenFirstExit = 0;
			state = StateAutomate.WAITING;
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
		
		private StateAutomate getNewState()
		{
			if (!modePrepa && !modeAction && shouldPrepareSell())
				return (StateAutomate.PREPA);
			if (modePrepa && !modeAction && shouldEnterSell())
				return (StateAutomate.ENTER);
			if (modeAction && !modePrepa && !firstVolumeExited && shouldExitFirstVolumeSell())
				return (StateAutomate.FIRST_EXIT);
			if (modeAction && !modePrepa && firstVolumeExited)
				return (StateAutomate.SECOND_EXIT);
			return (StateAutomate.WAITING);
		}
		
		private void preparePosition()
		{
			Print("Mode prépa");
			modePrepa = true;
		}
		
		private void enterSellPosition()
		{
			Print("Enter");
			modeAction = true;
			modePrepa = false;
			EnterShortStop(2, GetCurrentBid(), "Enter_Sell");
		}
		
		private void exitFirstSellPosition()
		{
			Print("Exit 1");
			ExitShort(1, "First_Exit", "Enter_Sell");
			firstVolumeExited = true;
		}
		
		private void exitSecondSellPosition()
		{
			Print("Exit 2");
			ExitShort(1, "Second_Exit", "Enter_Sell");
			modeAction = false;
			firstVolumeExited = false;
			profitAfterFirstExit = 0;
			maxProfitAfterFirstExit = 0;
			baseValueEMAWhenFirstExit = 0;
		}
		
		private void checkToExitSecondSellPosition()
		{
			if (profitAfterFirstExit == 0)
			{
				baseValueEMAWhenFirstExit = (getEMA() + TickSize);
				profitAfterFirstExit = 1; // on sauvegarde la valeur de l'EMA qui nous servira de reférence pour caclculer le profit
			}				
			else 
			{
				profitAfterFirstExit = (baseValueEMAWhenFirstExit - getEMA()); // on recupere la différence de profit
				if (profitAfterFirstExit == 0)
					profitAfterFirstExit = 1; // si la différence est nul on quitte et attends qu'il y ait du profit
				else if (profitAfterFirstExit < 0) 
					exitSecondSellPosition(); // si la courbe s'inverse rapidement et que l'on ne fait plus de profit on quitte
				else
				{
					if (maxProfitAfterFirstExit < profitAfterFirstExit)
						maxProfitAfterFirstExit = profitAfterFirstExit; // on sauvegarde le profit max
					if (shouldExitSecondVolumeSell())
						exitSecondSellPosition(); // si le profit actuel est redescendu du (profit max * percentageForSecondExit) on quitte
				}
			}
		}
		
        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
			state = getNewState();
			switch (state)
			{
				case StateAutomate.PREPA:
					preparePosition();
					break;
				case StateAutomate.ENTER:
					enterSellPosition();
					break;
				case StateAutomate.FIRST_EXIT:
					exitFirstSellPosition();
					break;
				case StateAutomate.SECOND_EXIT:
					checkToExitSecondSellPosition();
					break;
				case StateAutomate.WAITING:
				default:
					return;
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
		
		private bool shouldExitFirstVolumeSell()
		{
			return (getFirstHighEMA() < getSecondHighEMA());
		}
		
		private bool shouldExitSecondVolumeSell()
		{
			return (profitAfterFirstExit <= (maxProfitAfterFirstExit * percentageForSecondExit));
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
		
		[Description("Pourcentage de perte du profit avant de couper le second volume.")]
        [GridCategory("Parameters")]
        public double PercentageForSecondExit
        {
            get { return percentageForSecondExit; }
            set { percentageForSecondExit = value; }
        }
        #endregion
    }
}