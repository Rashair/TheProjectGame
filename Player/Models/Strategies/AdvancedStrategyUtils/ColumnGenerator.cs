namespace Player.Models.Strategies.AdvancedStrategyUtils
{
    public class ColumnGenerator
    {
        private readonly int initialID;
        private readonly int width;
        private readonly int numberOfPlayers;
        private readonly bool isInitialIdPrime;
        private readonly bool isWidthNotDivisibleByInitialId;
        private readonly bool[] checkedColumns;

        /// <summary>
        /// InitialID should start with 0
        /// </summary>
        public ColumnGenerator(int initialID, int width, int numberOfPlayers)
        {
            this.initialID = initialID + 1;
            this.width = width;
            this.numberOfPlayers = numberOfPlayers;
            this.isInitialIdPrime = IsPrime(this.initialID);
            this.isWidthNotDivisibleByInitialId = width % initialID != 0;
            this.checkedColumns = new bool[width];
        }

        public int GetColumnToHandle(int colNum)
        {
            if (width <= numberOfPlayers)
            {
                return (initialID + colNum) % width;
            }

            int result;
            if (isInitialIdPrime && isWidthNotDivisibleByInitialId)
            {
                result = (initialID * colNum) % width;
            }
            else if (isInitialIdPrime)
            {
                int multipliedID = colNum * initialID;
                if (multipliedID < width)
                {
                    result = multipliedID;
                }
                else
                {
                    int start = (initialID + 1) % width;
                    result = GetFirstNotCheckedColumn(start, 1);
                }
            }
            else
            {
                result = GetFirstNotCheckedColumn(initialID, -numberOfPlayers);
            }

            if (checkedColumns[result])
            {
                result = GetFirstNotCheckedColumn(initialID, 1);
            }

            checkedColumns[result] = true;
            return initialID;
        }

        public int GetFirstNotCheckedColumn(int start, int iter)
        {
            int initialStart = start;
            while (checkedColumns[start])
            {
                start += iter;
                if (start >= checkedColumns.Length)
                {
                    start -= checkedColumns.Length;
                }
                else if (start < 0)
                {
                    start += checkedColumns.Length;
                }

                if (start == initialStart)
                {
                    iter = 1;
                    start = initialStart + 1;
                }
            }

            return start;
        }

        public static bool IsPrime(int candidate)
        {
            if ((candidate & 1) == 0)
            {
                return candidate == 2;
            }

            for (int i = 3; (i * i) <= candidate; i += 2)
            {
                if ((candidate % i) == 0)
                {
                    return false;
                }
            }

            return candidate != 1;
        }
    }
}
