namespace DarkLedger.WPF
{
    public enum StatsPeriod
    {
        Today = 0,
        Yesterday = 1,
        Last7Days = 2,
        Last30Days = 3,
        AllTime = 4
    }

    public static class StatsPeriodExtensions
    {
        public static string ToDisplayName(this StatsPeriod period)
        {
            return period switch
            {
                StatsPeriod.Today => "Сегодня",
                StatsPeriod.Yesterday => "Вчера",
                StatsPeriod.Last7Days => "7 дней",
                StatsPeriod.Last30Days => "30 дней",
                StatsPeriod.AllTime => "Всё время",
                _ => "Неизвестно"
            };
        }
    }
}