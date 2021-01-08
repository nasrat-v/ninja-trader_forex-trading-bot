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
    [Description("Statrégie v2 avec ajout d'un stop loss et second exit en fonction de l'entrée et non plus en fonction de la premiere sortie - v2.1 - BUY")]
    public class StrategyClaqueV2_1_Buy : Strategy
    {
		enum StateAutomate 
		{
			PREPA,
			ENTER,
			FIRST_EXIT,
			SECOND_EXIT,
			STOP_LOSS_EXIT,
			WAITING
		};
		
        #region Variables
        // Wizard generated variables
		private int minimalGapForPrepaBuy = 0;
		private double percentageForSecondExit = 0.5; // la moitié
		private int pointsStopLoss = 10;
		private int totalVolumes = 2000;
		private int pointsForFirstExit = 10;

        // User defined variables (add any user defined variables below)
		private bool modePrepa;
		private bool modeAction;
		private bool firstVolumeExited;
		private double profitAfterFirstExit;
		private double maxProfitAfterFirstExit;
		private double baseValueEMAWhenEnter;
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
			resetAfterAllPositionsExited();
			state = StateAutomate.WAITING;
        }
		
		private void resetAfterAllPositionsExited()
		{
			modePrepa = false;
			modeAction = false;
			firstVolumeExited = false;
			profitAfterFirstExit = 0;
			maxProfitAfterFirstExit = 0;
			baseValueEMAWhenEnter = 0;
			baseValueEMAWhenFirstExit = 0;
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
		
		private double getBaseValue()
		{
			return (getEMA() + TickSize);
		}
		
		private StateAutomate getNewState()
		{
			if (baseValueEMAWhenEnter != 0 && shouldExitStopLoss()) // doit toujours être testé en premier
				return (StateAutomate.STOP_LOSS_EXIT);
			if (!modePrepa && !modeAction && shouldPrepareBuy())
				return (StateAutomate.PREPA);
			if (modePrepa && !modeAction && shouldEnterBuy())
				return (StateAutomate.ENTER);
			if (modeAction && !modePrepa && !firstVolumeExited && shouldExitFirstVolumeBuy())
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
		
		private void enterBuyPosition()
		{
			Print("Enter");
			modeAction = true;
			modePrepa = false;
			EnterLongStop(totalVolumes, GetCurrentBid(), "Enter_Buy");
			baseValueEMAWhenEnter = getBaseValue();
		}
		
		private void exitFirstBuyPosition()
		{
			Print("Exit 1");
			ExitLong((totalVolumes / 2), "First_Exit", "Enter_Buy");
			firstVolumeExited = true;
			baseValueEMAWhenFirstExit = getBaseValue();
		}
		
		private void exitSecondBuyPosition()
		{
			Print("Exit 2");
			ExitLong((totalVolumes / 2), "Second_Exit", "Enter_Buy");
			resetAfterAllPositionsExited();
		}
		
		private void exitStopLoss()
		{
			Print("Exit Stop Loss");
			ExitLong(totalVolumes, "Stop_Loss_Exit", "Enter_Buy");
			resetAfterAllPositionsExited();
		}
		
		private void checkToExitSecondBuyPosition()
		{
			profitAfterFirstExit = (getEMA() - baseValueEMAWhenFirstExit); // on recupere la différence de profit
			if (profitAfterFirstExit != 0 && getEMA() > baseValueEMAWhenFirstExit) // on evite une valeur négative
			{
				if (maxProfitAfterFirstExit < profitAfterFirstExit)
					maxProfitAfterFirstExit = profitAfterFirstExit; // on sauvegarde le profit max
				if (shouldExitSecondVolumeBuy())
					exitSecondBuyPosition(); // si le profit actuel est redescendu du (profit max * percentageForSecondExit) on quitte
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
					enterBuyPosition();
					break;
				case StateAutomate.FIRST_EXIT:
					exitFirstBuyPosition();
					break;
				case StateAutomate.SECOND_EXIT:
					checkToExitSecondBuyPosition();
					break;
				case StateAutomate.STOP_LOSS_EXIT:
					exitStopLoss();
					break;
				case StateAutomate.WAITING:
				default:
					return;
			}
        }
		
		private bool shouldPrepareBuy() 
		{
			return (getEMA() < getSecondLowEMA() && isMinimalGapForPrepaBuy());
		}
		
		private bool shouldEnterBuy()
		{
			return (getEMA() > getSecondLowEMA());
		}
		
		private bool isMinimalGapForPrepaBuy()
		{
			return ((getSecondLowEMA() - getFirstLowEMA()) >= (minimalGapForPrepaBuy * TickSize));
		}
		
		private bool shouldExitFirstVolumeBuy()
		{
			return (getBaseValue() >= (baseValueEMAWhenEnter + (pointsForFirstExit * TickSize)));
		}
		
		private bool shouldExitSecondVolumeBuy()
		{
			return (profitAfterFirstExit <= (maxProfitAfterFirstExit * percentageForSecondExit));
		}
		
		private bool shouldExitStopLoss()
		{
			return (getBaseValue() <= (baseValueEMAWhenEnter - (pointsStopLoss * TickSize)));
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
        [Description("Ecart de points minimum requit entre l'EMA Low 1 et l'EMA Low 2 pour pouvoir entrer sur un ordre d'achat.")]
        [GridCategory("Parameters")]
        public int MinimalGapForPrepaBuy
        {
            get { return minimalGapForPrepaBuy; }
            set { minimalGapForPrepaBuy = value; }
        }
		
		[Description("Pourcentage de perte du profit avant de couper le second volume.")]
        [GridCategory("Parameters")]
        public double PercentageForSecondExit
        {
            get { return percentageForSecondExit; }
            set { percentageForSecondExit = value; }
        }
		
		[Description("Nombre de points de perte maximum avant de couper toute position.")]
        [GridCategory("Parameters")]
        public int PointsStopLoss
        {
            get { return pointsStopLoss; }
            set { pointsStopLoss = value; }
        }
		
		[Description("Volumes total joué. Le volume est divisé par deux, un volume pour chaque exit.")]
        [GridCategory("Parameters")]
        public int TotalVolumes
        {
            get { return totalVolumes; }
            set { totalVolumes = value; }
        }
		
		[Description("Nombre de points a atteindre pour déclencher le premier exit.")]
        [GridCategory("Parameters")]
        public int PointsForFirstExit
        {
            get { return pointsForFirstExit; }
            set { pointsForFirstExit = value; }
        }
        #endregion
    }
}