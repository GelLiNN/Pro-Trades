using System;
namespace Sociosearch.NET.Models
{
    public class FinraRecord
    {
        /*
         * Date|Symbol|ShortVolume|ShortExemptVolume|TotalVolume
         */

        public DateTime Date { get; set; }

        public string Symbol { get; set; }

        public decimal ShortVolume { get; set; }

        public decimal ShortExemptVolume { get; set; }

        public decimal TotalVolume { get; set; }

        public string Market { get; set; }
    }
}
