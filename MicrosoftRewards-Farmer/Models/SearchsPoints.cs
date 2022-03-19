using System;

namespace MicrosoftRewardsFarmer.Models
{
    public struct SearchsPoints
    {
        #region Constructors
        public SearchsPoints(SearchPoints current, SearchPoints total)
        {
            Current = current;
            Total = total;
        }
        #endregion

        #region Variables
        public readonly SearchPoints Current;
        public readonly SearchPoints Total;
        #endregion

        #region Methods
        public override string ToString()
        {
            return
                "Current( " + Current.ToString() + " )" + Environment.NewLine +
                "Total( " + Current.ToString() + " )";
        }
        #endregion
    }
}
