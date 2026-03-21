namespace PT.Core
{
    /// <summary>
    /// Class containing custom mathematical functions.
    /// </summary>
    public static class Maths
    {
        /// <summary>
        /// Get the dx/dy or slope value for the input X list and Y list
        /// </summary>
        /// <param name="xList"></param>
        /// <param name="yList"></param>
        /// <returns>dx/dy</returns>
        public static decimal GetSlope(List<decimal> xList, List<decimal> yList)
        {
            //"zip" xs and ys to make the sum of products easier
            var xys = Enumerable.Zip(xList, yList, (x, y) => new { x = x, y = y });
            decimal xbar = xList.Average();
            decimal ybar = yList.Average();
            decimal slope = xys.Sum(xy => (xy.x - xbar) * (xy.y - ybar)) / xList.Sum(x => (x - xbar) * (x - xbar));
            return slope;
        }

        /// <summary>
        /// Get transformed input decimal numbers into normalized (or scaled) numbers
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static List<decimal> GetNormalizedData(List<decimal> input)
        {
            if (IsAllZeroes(input)) return input;

            // Protect against divide by 0 errors
            if (input.Count == 0)
                return new List<decimal>();

            // Estimate min and max from the input values using standard deviation
            decimal mean = input.Sum() / input.Count;

            decimal stdDev = GetStandardDeviation(input, true);

            decimal setMax = input.Max();
            decimal setMin = input.Min();
            decimal range = setMax - setMin;
            decimal stdDevScalar = GetStdDevScalar(range, stdDev);

            decimal estMax = mean + (stdDev * stdDevScalar);
            decimal estMin = mean - (stdDev * stdDevScalar);

            List<decimal> normalized = new List<decimal>();

            //below is using a difference quotient to get results for normalization
            for (int i = 0; i < input.Count; i++)
            {
                decimal curScore = (input[i] - estMin) / (estMax - estMin);
                normalized.Add(curScore);
            }

            return normalized;
        }

        /// <summary>
        /// Calculate and return the list of z-scores for the input list
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static List<decimal> GetZScores(List<decimal> input)
        {
            if (IsAllZeroes(input)) return input;

            // Find standard deviation and compute Z Scores
            decimal mean = input.Sum() / input.Count;

            decimal stdDev = GetStandardDeviation(input, true);

            decimal estMin = mean - (stdDev);
            decimal estMax = mean + (stdDev);

            decimal setMax = input.Max();
            decimal setMin = input.Min();
            decimal range = setMax - setMin;
            decimal zScoreScalar = GetStdDevScalar(range, stdDev);

            List<decimal> zScores = new List<decimal>();

            // Normally z-score tells you how many stdDev away from the mean this value is
            // In this case, we're finding how many (stdDev * zScoreScalar) away from the mean this value is
            for (int i = 0; i < input.Count; i++)
            {
                //OR compare the range to the stdDev to decide on our Z Score multiplier
                decimal curZ = (input[i] - mean) / (stdDev * zScoreScalar);
                zScores.Add(curZ);
            }

            return zScores;
        }

        /// <summary>
        /// Is the list of decimals passed in all zeroes or not?
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsAllZeroes(List<decimal> input)
        {
            bool allZeroes = true;
            foreach (decimal cur in input)
            {
                allZeroes = cur == 0;
            }
            return allZeroes;
        }

        /// <summary>
        /// Get the scalar as the number of standard deviations in the range
        /// </summary>
        /// <param name="range"></param>
        /// <param name="stdDev"></param>
        /// <returns></returns>
        public static decimal GetStdDevScalar(decimal range, decimal stdDev)
        {
            //how many stdDevs do you need to cover the entire range?
            return stdDev == 0 ? 0 : range / stdDev;
        }

        /// <summary>
        /// Return the standard deviation of a list of decimals, evaluated as sample or population
        /// </summary>
        /// <param name="values"></param>
        /// <param name="isSample">True for sample, False for population</param>
        /// <returns></returns>
        public static decimal GetStandardDeviation(List<decimal> values, bool isSample)
        {
            // Get the mean
            decimal sum = 0;
            for (int i = 0; i < values.Count; i++)
                sum += values[i];
            decimal mean = sum / values.Count;

            // Get the sum of the squares of the differences between each value and the mean
            decimal sumOfSquares = 0;
            for (int i = 0; i < values.Count; i++)
                sumOfSquares += (values[i] - mean) * (values[i] - mean);

            if (isSample)
                return GetDecimalSqrt(sumOfSquares / (values.Count() - 1));
            else
                return GetDecimalSqrt(sumOfSquares / values.Count());
        }

        /// <summary>
        /// https://stackoverflow.com/questions/4124189/performing-math-operations-on-decimal-datatype-in-c
        /// Calculate the square root of a number.
        /// Result of calculation will differ from an actual root value less than epslion.
        /// </summary>
        /// <param name="x">Number for which to calculate the square root</param>
        /// <param name="epsilon">Accuracy of calculation of the root from our number</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static decimal GetDecimalSqrt(decimal x, decimal epsilon = 0.0M)
        {
            if (x < 0) throw new Exception("EXCEPTION: Cannot calculate square root from a negative number");
            decimal current = (decimal)Math.Sqrt((double)x), previous;
            do
            {
                previous = current;
                if (previous == 0.0M) return 0;
                current = (previous + x / previous) / 2;
            }
            while (Math.Abs(previous - current) > epsilon);
            return current;
        }

        /// <summary>
        /// Calculates the percentage difference between two decimal values.
        /// </summary>
        /// <param name="V1">The first decimal value (old value)</param>
        /// <param name="V2">The second decimal value (new value)</param>
        /// <returns>The percentage difference as a decimal. Returns 0 if V1 is 0 to avoid division by zero.</returns>
        public static decimal GetPercentDiff(decimal V1, decimal V2)
        {
            // Check for zero to avoid division by zero
            if (V1 == 0) return 0m;

            //https://stackoverflow.com/questions/1376507/calculating-the-percentage-difference-between-two-values
            decimal change = ((V2 - V1) / Math.Abs(V1)) * 100; // Calculate percent difference
            return change;
        }

        /// <summary>
        /// GRU gated slope multiplier function
        /// </summary>
        /// <param name="slope"></param>
        /// <returns></returns>
        public static decimal GetSlopeMultiplier(decimal slope)
        {
            //Positive cases
            if (slope > 0 && slope < 0.25M)
                return 30.0M;
            else if (slope >= 0.25M && slope < 0.5M)
                return 20.0M;
            else if (slope >= 0.5M && slope < 1)
                return 10.0M;
            else if (slope >= 1 && slope < 5)
                return 4.0M;
            else if (slope >= 5 && slope < 10)
                return 1.7M;
            else if (slope >= 10 && slope < 20)
                return 1.0M;
            else if (slope >= 20)
                return 1.0M;

            //Negative cases
            else if (slope < 0 && slope > -0.25M)
                return -30.0M;
            else if (slope <= -0.25M && slope > -0.5M)
                return -20.0M;
            else if (slope <= -0.5M && slope > -1)
                return -10.0M;
            else if (slope <= -1 && slope > -5)
                return -4.0M;
            else if (slope <= -5 && slope > -10)
                return -1.7M;
            else if (slope <= -10 && slope > -20)
                return -1.0M;
            else if (slope <= -20)
                return -1.0M;
            else
                return 0;
        }

        /// <summary>
        /// Get random integer between upper and lower bounds inclusive.
        /// Return 0 if the lower bound is higher (incorrect), and return the integer if equal.
        /// </summary>
        /// <param name="lowerBoundInclusive"></param>
        /// <param name="upperBoundInclusive"></param>
        /// <returns></returns>
        public static int GetRandomInt(int lowerBoundInclusive, int upperBoundInclusive)
        {
            if (lowerBoundInclusive > upperBoundInclusive)
                return 0;
            else if (lowerBoundInclusive == upperBoundInclusive)
                return lowerBoundInclusive;
            else
            {
                Random random = new Random();
                return random.Next(lowerBoundInclusive, upperBoundInclusive + 1);
            }
        }
    }
}
