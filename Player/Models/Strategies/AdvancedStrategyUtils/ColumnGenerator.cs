namespace Player.Models.Strategies.AdvancedStrategyUtils
{
    public class ColumnGenerator
    {
        private readonly int initialID;
        private readonly int width;
        private readonly int numberOfPlayers;
        private readonly bool isInitialIdPrime;
        private readonly (bool isChecked, bool isPrime)[] checkedColumns;

        /// <summary>
        /// InitialID should start with 0
        /// </summary>
        public ColumnGenerator(int initialID, int width, int numberOfPlayers)
        {
            this.initialID = initialID + 1;
            this.width = width;
            this.numberOfPlayers = numberOfPlayers;
            this.isInitialIdPrime = IsPrime(this.initialID);

            this.checkedColumns = new (bool, bool)[width];
            for (int i = 2; i < width; ++i)
            {
                checkedColumns[i].isPrime = IsPrime(i);
            }
        }

        public int GetColumnToHandle(int colNum)
        {
            int result;
            int shifted = initialID + ((colNum - 1) * numberOfPlayers);
            if (width <= numberOfPlayers)
            {
                result = (initialID + colNum) % width;
            }
            else if (shifted <= width || isInitialIdPrime)
            {
                result = shifted % width;
            }
            else if (isInitialIdPrime)
            {
                result = GetFirstNotCheckedNotPrime();
            }
            else
            {
                if (initialID % 2 == 0)
                {
                    // colNum > 1 here
                    // initialID <= numberOfPlayers
                    result = GetFirstPrimeColumnIfNotTaken(width + initialID - numberOfPlayers - colNum + 1);
                }
                else
                {
                    result = GetFirstNotCheckedNotPrime(width - initialID);
                }
            }

            if (checkedColumns[result].isChecked)
            {
                result = GetFirstNotCheckedColumn(result);
            }
            checkedColumns[result].isChecked = true;

            return result;
        }

        private int GetFirstPrimeColumnIfNotTaken(int start)
        {
            int initialStart = start;
            while (!checkedColumns[start].isPrime && checkedColumns[start].isChecked)
            {
                --start;
                if (start <= numberOfPlayers)
                {
                    return initialStart;
                }
            }

            return start;
        }

        private int GetFirstNotCheckedNotPrime(int start = -1)
        {
            start = start == -1 ? width - 1 : start;
            for (int i = start % width; i >= 0; --i)
            {
                if (!checkedColumns[i].isPrime && !checkedColumns[i].isChecked)
                {
                    return i;
                }
            }

            return GetFirstNotCheckedColumn(0);
        }

        private int GetFirstNotCheckedColumn(int start, int iter = 1)
        {
            int initialStart = start;
            while (checkedColumns[start].isChecked)
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
