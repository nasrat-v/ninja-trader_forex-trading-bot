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
    [Description("Une strategie claqué au sol - v1")]
    public class StrategyClaqueAuSol : Strategy
    {
        #region Variables
        // Wizard generated variables
        private int sellThreshold = 25000; // Default setting for SellThreshold
		private int sellLimitOrder = 0; // Default setting for SellLimitOrder
        private int sellTrailStop = 0; // Default setting for SellTrailStop
        private int sellTakeProfit = 10000; // Default setting for SellTakeProfit
		private double sellEnsureProfit = 1000; // Default setting for SellEnsureProfit
		
        private int buyThreshold = -25000; // Default setting for BuyThreshold
		private int buyLimitOrder = 0; // Default setting for LuyLimitOrder
        private int buyTrailStop = 0; // Default setting for BuyTrailStop
        private int buyTakeProfit = -10000; // Default setting for BuyTakeProfit
		private double buyEnsureProfit = 1000; // Default setting for BuyEnsureProfit
		
        // User defined variables (add any user defined variables below)
		private double barSize;
		private double emaSize;
		private double emaOpenSize;
        private bool offerPlaced;
		private bool isOnBuy;
		private bool isOnSell;
		private bool asMadeProfit;
		private double bestProfitBuy;
		private double bestProfitSell;
		private double buyEntryPrice;
		private double sellEntryPrice;
        #endregion

        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {
            offerPlaced = false;
        }

		private void initOnBarUpdate()
		{
			emaSize = EMA(0)[0];
			emaOpenSize = Open[0];
			barSize = ((Input[0] - emaOpenSize) / TickSize);
		}
		
		private void initAfterOfferClosed()
		{
			isOnBuy = false;
			isOnSell = false;
			asMadeProfit = false;
			bestProfitBuy = Double.MinValue;
			bestProfitSell = Double.MaxValue;
		}
		
        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
			if (Historical) 
				return;
			initOnBarUpdate();
			if (!offerPlaced)
			{
				initAfterOfferClosed();
				checkToPlaceOffer();
			}
			else
				checkProfit();
			// lorsqu'il créer un nouveau bloc, il recommence a chercher a buy ou sell
			/*if (FirstTickOfBar)
				offerPlaced = false;*/
        }
		
		private void checkToPlaceOffer()
		{
			if (shouldBuy())
				handleBuying();
			else if (shouldSell())
				handleSelling();
		}
		
		private void handleBuying()
		{
            double trailStop = (buyTrailStop * TickSize);
			double takeProfitValue = (emaOpenSize + (buyTakeProfit * TickSize));
			buyEntryPrice = (emaOpenSize + (buyLimitOrder * TickSize));
			
			EnterLongStop(buyEntryPrice); // achete lorsque entryPrice est atteint
			SetTrailStop(CalculationMode.Ticks, buyTrailStop); // exit l'action,
			SetProfitTarget(CalculationMode.Price, takeProfitValue); // exit l'action,
			offerPlaced = true;
			isOnBuy = true;
		}
		
		private void handleSelling() 
		{
            double trailStop = (sellTrailStop * TickSize);
			double takeProfitValue = (emaOpenSize + (sellTakeProfit * TickSize));
			sellEntryPrice = (emaOpenSize + (sellLimitOrder * TickSize));
			
			EnterShortStop(sellEntryPrice);
			SetTrailStop(CalculationMode.Ticks, sellTrailStop);  /// voir stop loss
			SetProfitTarget(CalculationMode.Price, takeProfitValue);
			offerPlaced = true;
			isOnSell = true;
		}
		
		private void checkProfit() 
		{
			if (isOnBuy)
				checkBuyProfit();
			else if (isOnSell)
				checkSellProfit();
		}
		
		private void checkBuyProfit()
		{
			double tmpProfit = emaSize;
			double triggerEnsureProfit = (bestProfitBuy - (buyEnsureProfit * TickSize));
			
			if (isMakingProfitBuy()) 
			{
				if (tmpProfit >= bestProfitBuy) // on recupere le plus haut profit
					bestProfitBuy = tmpProfit;
			}
			if (tmpProfit <= triggerEnsureProfit) // on stop si on atteint le trigger EnsureProfit
			{
				ExitLong();
				offerPlaced = false;
			}
		}
		
		private void checkSellProfit()
		{
			double tmpProfit = emaSize;
			double triggerEnsureProfit = (bestProfitSell + (sellEnsureProfit * TickSize));
			
			if (isMakingProfitSell()) 
			{
				if (tmpProfit <= bestProfitSell)
					bestProfitSell = tmpProfit;
			}
			if (tmpProfit >= triggerEnsureProfit)
			{
				ExitShort();
				offerPlaced = false;
			}
		}
		
		private bool shouldBuy()
		{
			return (barSize <= buyThreshold);
		}
		
		private bool shouldSell() 
		{
			return (barSize >= sellThreshold);
		}
		
		private bool isMakingProfitBuy() 
		{
			return (emaSize > buyEntryPrice);
		}
		
		private bool isMakingProfitSell() 
		{
			return (emaSize < sellEntryPrice);
		}

        #region Properties
        [Description("Trigger qui place un ordre à une valeur donné, si il est atteint.")]
        [GridCategory("Parameters")]
        public int SellThreshold
        {
            get { return sellThreshold; }
            set { sellThreshold = value; }
        }
		
        [Description("Valeur de l'ordre lorque le trigger Threshold est déclenché.")]
        [GridCategory("Parameters")]
        public int SellLimitOrder
        {
            get { return sellLimitOrder; }
            set { sellLimitOrder = value; }
        }

        [Description("Trigger qui close un ordre lorsque sa valeur est atteinte. Il empêche le déficit. Il est dynamique en fonction du prix. Si il fait du bénéfice (en fonction de sa valeur) il se place à zéro.")]
        [GridCategory("Parameters")]
        public int SellTrailStop
        {
            get { return sellTrailStop; }
            set { sellTrailStop = value; }
        }

        [Description("Trigger qui close un ordre lorsque sa valeur est atteinte. Il permet de sécuriser le bénéfice. Il n'est pas dynamique.")]
        [GridCategory("Parameters")]
        public int SellTakeProfit
        {
            get { return sellTakeProfit; }
            set { sellTakeProfit = value; }
        }
		
		[Description("Trigger qui close un ordre lorque sa valeur est atteinte. Il permet de sécuriser le bénéfice ou d'empecher le déficit. Lorsque l'on fait un bénéfice, si l'on se met à perdre il se déclenche à la valeur donnée.")]
        [GridCategory("Parameters")]
        public double SellEnsureProfit
        {
            get { return sellEnsureProfit; }
            set { sellEnsureProfit = value; }
        }
		
        [Description("Trigger qui place un ordre à une valeur donné, si il est atteint.")]
        [GridCategory("Parameters")]
        public int BuyThreshold
        {
            get { return buyThreshold; }
            set { buyThreshold = value; }
        }

        [Description("Valeur de l'ordre lorque le trigger Threshold est déclenché.")]
        [GridCategory("Parameters")]
        public int BuyLimitOrder
        {
            get { return buyLimitOrder; }
            set { buyLimitOrder = value; }
        }

        [Description("Trigger qui close un ordre lorsque sa valeur est atteinte. Il empêche le déficit. Il est dynamique en fonction du prix. Si il fait du bénéfice (en fonction de sa valeur) il se place à zéro.")]
        [GridCategory("Parameters")]
        public int BuyTrailStop
        {
            get { return buyTrailStop; }
            set { buyTrailStop = value; }
        }

        [Description("Trigger qui close un ordre lorsque sa valeur est atteinte. Il permet de sécuriser le bénéfice. Il n'est pas dynamique.")]
        [GridCategory("Parameters")]
        public int BuyTakeProfit
        {
            get { return buyTakeProfit; }
            set { buyTakeProfit = value; }
        }
		
		[Description("Trigger qui close un ordre lorque sa valeur est atteinte. Il permet de sécuriser le bénéfice ou d'empecher le déficit. Lorsque l'on fait un bénéfice, si l'on se met à perdre il se déclenche à la valeur donnée.")]
        [GridCategory("Parameters")]
        public double BuyEnsureProfit
        {
            get { return buyEnsureProfit; }
            set { buyEnsureProfit = value; }
        }
        #endregion
    }
}
