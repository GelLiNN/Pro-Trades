using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Sociosearch.NET.Models;

namespace Sociosearch.NET.Middleware
{
    //Module for getting all or most company symbols and names from all exchanges
    //Nasdaq FTP data dump files for loading large datasets
    //ftp://ftp.nasdaqtrader.com and ftp://ftp.nasdaqtrader.com/SymbolDirectory and
    //OTCMarkets raw securities download from https://www.otcmarkets.com/research/stock-screener/api/downloadCSV
    public class Companies
    {
        public static readonly string NasdaqSymbolsUri = @"ftp://ftp.nasdaqtrader.com/SymbolDirectory/nasdaqtraded.txt";
        public static readonly string OtcSymbolsUri = @"ftp://ftp.nasdaqtrader.com/SymbolDirectory/otclist.txt";
        public static readonly string OtcMarketsUri = @"https://www.otcmarkets.com/research/stock-screener/api/downloadCSV";

        public static string GetFromFtpUri(string uri)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(uri);
            request.UseBinary = true;
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            string responseStr = string.Empty;

            //Read the file from the server & write to destination                
            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse()) // Error here
            using (Stream responseStream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(responseStream))
            {
                responseStr = reader.ReadToEnd();
            }
            return responseStr;
        }

        public static string GetFromUri(string uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = "GET";
            string responseStr = string.Empty;

            //Read the file from the server & write to destination                
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) // Error here
            using (Stream responseStream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(responseStream))
            {
                responseStr = reader.ReadToEnd();
            }
            return responseStr;
        }
    }
}
