namespace Dice.Client.Web
{
    public static class StringExtension
    {
        /// <summary>
        /// This is here only because .NET 3.5 doesn't have the static IsNullOrWhiteSpace method
        /// </summary>
        public static bool IsNullOrWhiteSpace(this string x)
        {
            if (string.IsNullOrEmpty(x))
                return true;
            if (x.Trim() == string.Empty)
                return true;
            return false;
        }
    }
}
