#region

using System;
using System.Collections.Generic;

#endregion

namespace core.Model
{
    public class Description
    {
        public Dictionary<String, String> requisites { get; set; }

        public Description()
        {
            requisites = new Dictionary<String, String>();
        }

        public void addRequisite(String key, String value)
        {
            requisites[key] = value;
        }

        public String printDescription()
        {
            String response = "";
            foreach (String key in requisites.Keys)
                response += requisites[key] + ";";

            return response;
        }

        public String printDescriptionHead()
        {
            String response = "";
            foreach (String key in requisites.Keys)
                response += key + ";";

            return response;
        }
    }
}