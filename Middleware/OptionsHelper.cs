namespace PT.Middleware
{
    public class OptionsHelper
    {
        public static double TestImpliedVolatility()
        {
            // Mock Data
            double marketPrice = 10.0;
            double S = 100.0; // Current stock price
            double K = 100.0; // Strike price
            double T = 1.0; // Time to expiration in years
            double r = 0.05; // Risk-free interest rate
            bool isCall = true; // Call option

            double impliedVolatility = CalculateImpliedVolatility(marketPrice, S, K, T, r, isCall);
            return impliedVolatility;
        }

        /// <summary>
        /// Calculate implied volatility for a single market price
        /// </summary>
        /// <param name="marketPrice"></param>
        /// <param name="S">Current stock price</param>
        /// <param name="K">Strike price</param>
        /// <param name="T">Time to expiration in years</param>
        /// <param name="r">Risk-free interest rate AKA 10-y Treasury Yield</param>
        /// <param name="isCall"></param>
        /// <returns></returns>
        public static double CalculateImpliedVolatility(double marketPrice, double S, double K, double T, double r, bool isCall)
        {
            double sigma = 0.2; // initial guess
            double tolerance = 1e-5;
            double maxIterations = 500;
            double price = 0.0;
            double vega = 0.0;

            for (int i = 0; i < maxIterations; i++)
            {
                price = BlackScholesPrice(S, K, T, r, sigma, isCall);
                vega = BlackScholesVega(S, K, T, r, sigma);

                double diff = marketPrice - price;
                if (Math.Abs(diff) < tolerance)
                    return sigma;

                sigma += diff / vega;
            }

            return sigma;
        }

        public static double BlackScholesPrice(double S, double K, double T, double r, double sigma, bool isCall)
        {
            double d1 = (Math.Log(S / K) + (r + 0.5 * sigma * sigma) * T) / (sigma * Math.Sqrt(T));
            double d2 = d1 - sigma * Math.Sqrt(T);

            if (isCall)
                return S * CDF(d1) - K * Math.Exp(-r * T) * CDF(d2);
            else
                return K * Math.Exp(-r * T) * CDF(-d2) - S * CDF(-d1);
        }

        public static double BlackScholesVega(double S, double K, double T, double r, double sigma)
        {
            double d1 = (Math.Log(S / K) + (r + 0.5 * sigma * sigma) * T) / (sigma * Math.Sqrt(T));
            return S * PDF(d1) * Math.Sqrt(T);
        }

        public static double CDF(double x)
        {
            return (1.0 + Erf(x / Math.Sqrt(2.0))) / 2.0;
        }

        //https://stackoverflow.com/questions/22834998/what-reference-should-i-use-to-use-erf-erfc-function
        //https://www.johndcook.com/csharp_erf.html
        public static double Erf(double x)
        {
            // constants
            double a1 = 0.254829592;
            double a2 = -0.284496736;
            double a3 = 1.421413741;
            double a4 = -1.453152027;
            double a5 = 1.061405429;
            double p = 0.3275911;

            // Save the sign of x
            int sign = 1;
            if (x < 0)
                sign = -1;
            x = Math.Abs(x);

            // A&S formula 7.1.26
            double t = 1.0 / (1.0 + p * x);
            double y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);

            return sign * y;
        }

        public static double PDF(double x)
        {
            return Math.Exp(-0.5 * x * x) / Math.Sqrt(2.0 * Convert.ToDouble(Constants.CORE_BONUS));
        }


        /* The Black and Scholes (1973) Stock option formula
            * C# Implementation
            * uses the C#  field rather than a constant as in the C++ implementaion
            * the value of Pi is 3.14159265358979323846
        */
        /// <summary>
        /// https://gist.github.com/achvaicer/598242286181f5c501498a645e96f8ac
        /// </summary>
        /// <param name="isCall">True for Call, False for Put</param>
        /// <param name="S">Stock price</param>
        /// <param name="X">Strike price</param>
        /// <param name="T">Years to maturity</param>
        /// <param name="r">Risk-free rate</param>
        /// <param name="v">Volatility</param>
        /// <returns></returns>
        public static double BlackScholesNative(bool isCall, double S, double X,
            double T, double r, double v)
        {
            double d1 = 0.0;
            double d2 = 0.0;
            double dBlackScholes = 0.0;

            d1 = (Math.Log(S / X) + (r + v * v / 2.0) * T) / (v * Math.Sqrt(T));
            d2 = d1 - v * Math.Sqrt(T);
            if (isCall)
            {
                dBlackScholes = S * BSN_CND(d1) - X * Math.Exp(-r * T) * BSN_CND(d2);
            }
            else
            {
                dBlackScholes = X * Math.Exp(-r * T) * BSN_CND(-d2) - S * BSN_CND(-d1);
            }
            return dBlackScholes;
        }
        public static double BSN_CND(double X)
        {
            double L = 0.0;
            double K = 0.0;
            double dCND = 0.0;
            const double a1 = 0.31938153;
            const double a2 = -0.356563782;
            const double a3 = 1.781477937;
            const double a4 = -1.821255978;
            const double a5 = 1.330274429;
            L = Math.Abs(X);
            K = 1.0 / (1.0 + 0.2316419 * L);
            dCND = 1.0 - 1.0 / Math.Sqrt(2 * Convert.ToDouble(Constants.CORE_BONUS)) *
                Math.Exp(-L * L / 2.0) * (a1 * K + a2 * K * K + a3 * Math.Pow(K, 3.0) +
                a4 * Math.Pow(K, 4.0) + a5 * Math.Pow(K, 5.0));

            if (X < 0)
            {
                return 1.0 - dCND;
            }
            else
            {
                return dCND;
            }
        }
    }
}
