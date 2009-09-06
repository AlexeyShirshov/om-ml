using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace CommandLine.Utility
{
    /// <summary>
    /// Arguments class
    /// Valid parameters forms:
    /// {-,/,--}param{ ,=,:}((",')value(",'))
    /// </summary> 
    /// <example>
    /// -param1 value1 --param2 /param3:"Test-:-work" /param4=happy -param5 '--=nice=--'
    /// </example>
    public class Arguments{
        // Variables
        private readonly Dictionary<String,String> _parameters;

        /// <summary>
        /// Creates Arguments class
        /// </summary>
        /// <param name="Args">command line array (separated by space)</param>
        public Arguments(string[] Args)
        {
			_parameters = new Dictionary<String,String>();
            Regex Spliter = new Regex(@"^-{1,2}|^/|=|:",
                RegexOptions.IgnoreCase|RegexOptions.Compiled);

            Regex Remover = new Regex(@"^['""]?(.*?)['""]?$",
                RegexOptions.IgnoreCase|RegexOptions.Compiled);

            string Parameter = null;
            string[] Parts;

            foreach(string Txt in Args)
            {
                // Look for new parameters (-,/ or --) and a
                // possible enclosed value (=,:)
                Parts = Spliter.Split(Txt,3);

                switch(Parts.Length){
                // Found a value (for the last parameter 
                // found (space separator))
                case 1:
                    if(Parameter != null)
                    {
                        if(!_parameters.ContainsKey(Parameter)) 
                        {
                            Parts[0] = 
                                Remover.Replace(Parts[0], "$1");

                            _parameters.Add(Parameter, Parts[0]);
                        }
                        Parameter=null;
                    }
                    // else Error: no parameter waiting for a value (skipped)
                    break;

                // Found just a parameter
                case 2:
                    // The last parameter is still waiting. 
                    // With no value, set it to true.
                    if(Parameter!=null)
                    {
                        if(!_parameters.ContainsKey(Parameter)) 
                            _parameters.Add(Parameter, "true");
                    }
                    Parameter=Parts[1];
                    break;

                // Parameter with enclosed value
                case 3:
                    // The last parameter is still waiting. 
                    // With no value, set it to true.
                    if(Parameter != null)
                    {
                        if(!_parameters.ContainsKey(Parameter)) 
                            _parameters.Add(Parameter, "true");
                    }

                    Parameter = Parts[1];

                    // Remove possible enclosing characters (",')
                    if(!_parameters.ContainsKey(Parameter))
                    {
                        Parts[2] = Remover.Replace(Parts[2], "$1");
                        _parameters.Add(Parameter, Parts[2]);
                    }

                    Parameter=null;
                    break;
                }
            }
            // In case a parameter is still waiting
            if(Parameter != null)
            {
                if(!_parameters.ContainsKey(Parameter)) 
                    _parameters.Add(Parameter, "true");
            }
        }

        // Retrieve a parameter value if it exists 
        // (overriding C# indexer property)
        public string this [string Param]
        {
            get
            {
				string s = null;
				_parameters.TryGetValue(Param, out s);
                return s;
            }
        }

		public bool TryGetParam(string param, out string value)
		{
			return _parameters.TryGetValue(param, out value);
		}
    }
}
