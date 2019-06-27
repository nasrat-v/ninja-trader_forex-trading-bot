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
    [Description("")]
    public class akiraPit : Strategy
    {
        #region Variables
        // Wizard generated variables
        private int sellThreshold = 25000; // Default setting for SellThreshold
		private int sellLimitOrder = 0; // Default setting for SellLimitOrder
        private int sellTrailStop = 0; // Default setting for SellTrailStop
        private int sellTakeProfit = 10000; // Default setting for SellTakeProfit
		
        private int buyThreshold = -25000; // Default setting for BuyThreshold
		private int buyLimitOrder = 0; // Default setting for LuyLimitOrder
        private int buyTrailStop = 0; // Default setting for BuyTrailStop
        private int buyTakeProfit = -10000; // Default setting for BuyTakeProfit
        // User defined variables (add any user defined variables below)
		private double barSize;
        private bool isWorking;
        #endregion

        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {
            isWorking = false;
        }

        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
			if (Historical) return;
			
			if (!isWorking) {
				barSize = (Input[0] - Open[0]) / TickSize;
				
				if (shouldBuy()) {
					handleBuying();
				} else if (shouldSell()) {
					handleSelling();
				}
			} else if (FirstTickOfBar) {
				isWorking = false;
			}
        }
		
		private void handleBuying() {
			double entryPrice = Open[0] + (buyLimitOrder * TickSize);
            double trailStop = buyTrailStop * TickSize;
			double takeProfitValue = Open[0] + (buyTakeProfit * TickSize);
			
			EnterLongStop(entryPrice);
			SetTrailStop(CalculationMode.Ticks, buyTrailStop);
			SetProfitTarget(CalculationMode.Price, takeProfitValue);
			
			isWorking = true;
		}
		
		private void handleSelling() {
			double entryPrice = Open[0] + (sellLimitOrder * TickSize);
            double trailStop = sellTrailStop * TickSize;
			double takeProfitValue = Open[0] + (sellTakeProfit * TickSize);
			
			EnterShortStop(entryPrice);
			SetTrailStop(CalculationMode.Ticks, sellTrailStop);
			SetProfitTarget(CalculationMode.Price, takeProfitValue);
			
			isWorking = true;
		}
		
		private bool shouldBuy() {
			return barSize <= buyThreshold;	
		}
		
		private bool shouldSell() {
			return barSize >= sellThreshold;
		}

        #region Properties
        [Description("")]
        [GridCategory("Parameters")]
        public int SellThreshold
        {
            get { return sellThreshold; }
            set { sellThreshold = value; }
        }
		
        [Description("")]
        [GridCategory("Parameters")]
        public int SellLimitOrder
        {
            get { return sellLimitOrder; }
            set { sellLimitOrder = value; }
        }

        [Description("")]
        [GridCategory("Parameters")]
        public int SellTrailStop
        {
            get { return sellTrailStop; }
            set { sellTrailStop = value; }
        }

        [Description("")]
        [GridCategory("Parameters")]
        public int SellTakeProfit
        {
            get { return sellTakeProfit; }
            set { sellTakeProfit = value; }
        }

        [Description("")]
        [GridCategory("Parameters")]
        public int BuyThreshold
        {
            get { return buyThreshold; }
            set { buyThreshold = value; }
        }

        [Description("")]
        [GridCategory("Parameters")]
        public int BuyLimitOrder
        {
            get { return buyLimitOrder; }
            set { buyLimitOrder = value; }
        }

        [Description("")]
        [GridCategory("Parameters")]
        public int BuyTrailStop
        {
            get { return buyTrailStop; }
            set { buyTrailStop = value; }
        }

        [Description("")]
        [GridCategory("Parameters")]
        public int BuyTakeProfit
        {
            get { return buyTakeProfit; }
            set { buyTakeProfit = value; }
        }
        #endregion
    }
}
