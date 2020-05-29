﻿using System.Collections.Generic;

namespace Player.Models.Strategies.AdvancedStrategyUtils
{
    public class ColumnGenerator
    {
        private readonly int initialID;
        private readonly int width;
        private readonly int numberOfPlayers;
        private readonly bool isInitialIdPrime;
        private readonly bool isWidthNotDivisibleByInitialId;
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
            this.isWidthNotDivisibleByInitialId = width % this.initialID != 0;

            this.checkedColumns = new (bool, bool)[width];
            for (int i = 2; i < width; ++i)
            {
                checkedColumns[i].isPrime = IsPrime(i);
            }
        }

        public int GetColumnToHandle(int colNum)
        {
            int result;
            if (width <= numberOfPlayers)
            {
                result = (initialID + colNum) % width;
            }
            else if (isInitialIdPrime && isWidthNotDivisibleByInitialId)
            {
                result = (initialID * colNum) % width;
            }
            else if (isInitialIdPrime)
            {
                int multipliedID = colNum * initialID;
                if (multipliedID <= width)
                {
                    result = multipliedID % width;
                }
                else
                {
                    result = GetFirstNotCheckedNotPrime();
                }
            }
            else
            {
                result = GetFirstPrimeColumnIfNotTaken(initialID - numberOfPlayers + width - 1);
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
            while (!checkedColumns[start].isPrime)
            {
                --start;
                if (start <= numberOfPlayers)
                {
                    return initialStart;
                }
            }

            return start;
        }

        private int GetFirstNotCheckedNotPrime()
        {
            for (int i = checkedColumns.Length - 1; i >= 0; --i)
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
