using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace sampleQuizCsharp
{
    public class Item
    {
        const string ABBREVIATION = "Abbreviation";
        const string CAPITAL = "Capital";
        const string STATEHOODYEAR = "StatehoodYear";
        const string STATEHOODORDER = "StatehoodOrder";
        const string STATE_HOOD_YEAR = "Statehood Year";
        const string STATE_HOOD_ORDER = "Statehood Order";
        const string STATENAME = "StateName";
        const string STATE_NAME = "State Name";
        const string RESPONSE = "response";
        const string QUIZITEM = "quizitem";
        const string QUIZPROPERTY = "quizproperty";
        const string QUIZSCORE = "quizscore";
        const string COUNTER = "counter";


        public string StateName { get; set; }
        public string Abbreviation { get; set; }
        public string Capital { get; set; }
        public int StatehoodYear { get; set; }
        public int StatehoodOrder { get; set; }

        public Item( string _stateName, string _abbreviation, string _capital, int _statehoodYear, int _statehoodOrder)
        {
            StateName = _stateName;
            Abbreviation = _abbreviation;
            Capital = _capital;
            StatehoodYear = _statehoodYear;
            StatehoodOrder = _statehoodOrder;
        }

        /// <summary>
        /// return the string value of the specific property
        /// </summary>
        /// <param name="property"></param>
        /// <returns>string</returns>
        public string PropertyValue(string property)
        {
            string ret = "";
            switch(property)
            {
                case ABBREVIATION: ret = this.Abbreviation;  break;
                case CAPITAL: ret = this.Capital; break;
                case STATE_HOOD_YEAR:
                case STATEHOODYEAR: ret = this.StatehoodYear.ToString(); break;
                case STATE_HOOD_ORDER:
                case STATEHOODORDER: ret = this.StatehoodOrder.ToString(); break;
                case STATE_NAME: 
                case STATENAME: ret = this.StateName; break;
            }
            return ret;
        }

        public static string[] PropertyNames = new string[5] { ABBREVIATION, CAPITAL, STATE_HOOD_YEAR, STATE_HOOD_ORDER, STATE_NAME };

        /// <summary>
        /// return the names and values of the properties as newline separated string
        /// </summary>
        /// <param name="property"></param>
        /// <returns>string</returns>
        public static string GetFormatedText (Item item)
        {
            string text = STATE_NAME +": " + item.StateName +"\n";
            foreach(string name in Item.PropertyNames)
            {
                text += name + ": " +  item.PropertyValue(name) + "\n";
            }
            return text;
        }
        /// <summary>
        /// return the list of states
        /// </summary>
        /// <param name="property"></param>
        /// <returns>List<Item> </returns>
        public static List<Item> ItemsArray()
        {
            List<Item> theItems =  new List<Item>();
            theItems = new List<Item>();
            theItems.Add(new Item("Alabama", "AL", "Montgomery", 1819, 22));
            theItems.Add(new Item("Alaska", "AK", "Juneau", 1959, 49));
            theItems.Add(new Item("Arizona", "AZ", "Phoenix", 1912, 48));
            theItems.Add(new Item("Arkansas", "AR", "Little Rock", 1836, 25));
            theItems.Add(new Item("California", "CA", "Sacramento", 1850, 31));
            theItems.Add(new Item("Colorado", "CO", "Denver", 1876, 38));
            theItems.Add(new Item("Connecticut", "CT", "Hartford", 1788, 5));
            theItems.Add(new Item("Delaware", "DE", "Dover", 1787, 1));
            theItems.Add(new Item("Florida", "FL", "Tallahassee", 1845, 27));
            theItems.Add(new Item("Georgia", "GA", "Atlanta", 1788, 4));
            theItems.Add(new Item("Hawaii", "HI", "Honolulu", 1959, 50));
            theItems.Add(new Item("Idaho", "ID", "Boise", 1890, 43));
            theItems.Add(new Item("Illinois", "IL", "Springfield", 1818, 21));
            theItems.Add(new Item("Indiana", "IN", "Indianapolis", 1816, 19));
            theItems.Add(new Item("Iowa", "IA", "Des Moines", 1846, 29));
            theItems.Add(new Item("Kansas", "KS", "Topeka", 1861, 34));
            theItems.Add(new Item("Kentucky", "KY", "Frankfort", 1792, 15));
            theItems.Add(new Item("Louisiana", "LA", "Baton Rouge", 1812, 18));
            theItems.Add(new Item("Maine", "ME", "Augusta", 1820, 23));
            theItems.Add(new Item("Maryland", "MD", "Annapolis", 1788, 7));
            theItems.Add(new Item("Massachusetts", "MA", "Boston", 1788, 6));
            theItems.Add(new Item("Michigan", "MI", "Lansing", 1837, 26));
            theItems.Add(new Item("Minnesota", "MN", "St. Paul", 1858, 32));
            theItems.Add(new Item("Mississippi", "MS", "Jackson", 1817, 20));
            theItems.Add(new Item("Missouri", "MO", "Jefferson City", 1821, 24));
            theItems.Add(new Item("Montana", "MT", "Helena", 1889, 41));
            theItems.Add(new Item("Nebraska", "NE", "Lincoln", 1867, 37));
            theItems.Add(new Item("Nevada", "NV", "Carson City", 1864, 36));
            theItems.Add(new Item("New Hampshire", "NH", "Concord", 1788, 9));
            theItems.Add(new Item("New Jersey", "NJ", "Trenton", 1787, 3));
            theItems.Add(new Item("New Mexico", "NM", "Santa Fe", 1912, 47));
            theItems.Add(new Item("New York", "NY", "Albany", 1788, 11));
            theItems.Add(new Item("North Carolina", "NC", "Raleigh", 1789, 12));
            theItems.Add(new Item("North Dakota", "ND", "Bismarck", 1889, 39));
            theItems.Add(new Item("Ohio", "OH", "Columbus", 1803, 17));
            theItems.Add(new Item("Oklahoma", "OK", "Oklahoma City", 1907, 46));
            theItems.Add(new Item("Oregon", "OR", "Salem", 1859, 33));
            theItems.Add(new Item("Pennsylvania", "PA", "Harrisburg", 1787, 2));
            theItems.Add(new Item("Rhode Island", "RI", "Providence", 1790, 13));
            theItems.Add(new Item("South Carolina", "SC", "Columbia", 1788, 8));
            theItems.Add(new Item("South Dakota", "SD", "Pierre", 1889, 40));
            theItems.Add(new Item("Tennessee", "TN", "Nashville", 1796, 16));
            theItems.Add(new Item("Texas", "TX", "Austin", 1845, 28));
            theItems.Add(new Item("Utah", "UT", "Salt Lake City", 1896, 45));
            theItems.Add(new Item("Vermont", "VT", "Montpelier", 1791, 14));
            theItems.Add(new Item("Virginia", "VA", "Richmond", 1788, 10));
            theItems.Add(new Item("Washington", "WA", "Olympia", 1889, 42));
            theItems.Add(new Item("West Virginia", "WV", "Charleston", 1863, 35));
            theItems.Add(new Item("Wisconsin", "WI", "Madison", 1848, 30));
            theItems.Add(new Item("Wyoming", "WY", "Cheyenne", 1890, 44));
            return theItems;
        }
    }
}
