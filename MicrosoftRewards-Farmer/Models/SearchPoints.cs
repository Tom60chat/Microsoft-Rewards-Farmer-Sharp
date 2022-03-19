namespace MicrosoftRewardsFarmer.Models
{
    public struct SearchPoints
    {
        #region Constructors
        public SearchPoints(int mobileSearch, int desktopSearch, int edgeSearch)
        {
            MobileSearch = mobileSearch;
            DesktopSearch = desktopSearch;
            EdgeSearch = edgeSearch;
        }
        #endregion


        #region Variables
        public readonly int MobileSearch;
        public readonly int DesktopSearch;
        public readonly int EdgeSearch;
        #endregion


        #region Methods
        public override string ToString()
        {
            return
                "Mobile: " + MobileSearch +
                ", Desktop: " + DesktopSearch +
                ", Edge: " + EdgeSearch;
        }
        #endregion
    }
}
