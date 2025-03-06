namespace UltraPaste
{
    public static class L
    {
        public static string Font, UltraPaste, UltraPasteWindow;

        // Some text localization.
        public static void Localize()
        {
            string language = UltraPasteCommon.Settings?.CurrentLanguage ?? System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            switch (language)
            {
                case "zh":
                    Font = "Microsoft Yahei UI";
                    UltraPaste = "����ճ����"; UltraPasteWindow = "����ճ����- ����";
                    break;

                default:
                    Font = "Arial";
                    UltraPaste = "Ultra Paste!"; UltraPasteWindow = "Ultra Paste! - Window";
                    break;
            }
        }
    }
}