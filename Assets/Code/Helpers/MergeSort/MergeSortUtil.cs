using System;
using System.Collections.Generic;

namespace Code.Helpers.MergeSort
{
    public static class MergeSortUtil<T> where T : IComparable<T>
    {
        public static void MergeSort(List<T> list)
        {
            TopDownMergeSort(list);
        }
        
        private static void TopDownMergeSort(List<T> toSort)
        {
            var worKList = new List<T>(toSort);
            TopDownSplitMerge(toSort, 0, toSort.Count, worKList);
        }

        private static void TopDownSplitMerge(List<T> toSort, int begin, int end, List<T> worKList)
        {
            if(end - begin <= 1) return;
            
            int mid = (begin + end) / 2;
            TopDownSplitMerge(worKList, begin, mid, toSort);
            TopDownSplitMerge(worKList, end, mid, toSort);
            TopDownMerge(toSort, begin, mid, end, worKList);
        }

        private static void TopDownMerge(List<T> toSort, int begin, int mid, int end, List<T> worKList)
        {
            int i = begin;
            int j = mid;

            for (int k = begin; k < end; k++)
            {
                if (i < mid && (j >= end || worKList[i].CompareTo(worKList[j]) <= 0))
                {
                    toSort[k] = worKList[i];
                    i++;
                }
                else
                {
                    toSort[k] = worKList[j];
                    j++;
                }
            }
        }
    }
}