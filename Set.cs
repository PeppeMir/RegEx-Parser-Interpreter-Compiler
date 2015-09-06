using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Regex
{
    /*** Generic set of elements (using object to maximize reutilize via casting) ***/
    public class Set : CollectionBase
    {
        /*** Get the number of element of the set ***/
        public int NumOfElement
        {
            get { return List.Count; }
        }

        /*** Get/Set the index-th element of the set ***/
        public object this[int index]
        {
            get
            {
                return ((object)List[index]);
            }
            set
            {
                List[index] = value;
            }
        }

        /*** Two sets are equal if they have the same number of element and one is subset of the other (so if have the same elements) ***/
        public bool equalTo(Set set)
        {
            return (NumOfElement == set.NumOfElement && checkSubset(set));
        }

        /*** Check if a element is contained in the set ***/
        public bool Contains(object value)
        {
            return List.Contains(value);
        }

        /*** Perform the set theory "Add" for adding a element to the set ***/
        public int addElement(object element)
        {
            if (!Contains(element))
            {
                return List.Add(element);
            }

            return -1;
        }

        /*** Perform the set theory "Add" for adding a range element to the set ***/
        public void addElementRange(object[] arrOfEl)
        {
            foreach (object obj in arrOfEl)
            {
                if (!Contains(obj))
                {
                    List.Add(obj);
                }
            }
        }

        /*** Remove a element from the set ***/
        public bool removeElFromSet(object el)
        {
            if (Contains(el))
            {
                List.Remove(el);
                return true;
            }

            return false;
        }

        /*** Perform the set theory "Union" between the parameter set and "this" ***/
        public void setUnion(Set set)
        {
            foreach (object obj in set)
            {
                if (!Contains(obj))
                {
                    List.Add(obj);
                }
            }
        }

        /*** Perform the set theory "Subset checking" between the parameter set and "this" ***/
        public bool checkSubset(Set set)
        {
            foreach (object obj in set)
            {
                if (!Contains(obj))
                {
                    return false;
                }
            }
            return true;
        }
        
    }
}
