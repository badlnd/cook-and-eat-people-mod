using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace LC.CEPM.CEPMLoggingUtils
{
    public class Logger
    {

        /// <summary>
        /// The default colour used to colour the logger text. White by default.
        /// </summary>
        public Color defaultLoggingColour = Color.white;

        /// <summary>
        /// The default colour used to colour the error text. Yellow by default.
        /// </summary>
        public Color defaultWarningColour = Color.yellow;

        /// <summary>
        /// The default colour used to colour the error text. Red by default.
        /// </summary>
        public Color defaultErrorColour = Color.red;

        private bool init = false;

        /// <summary>
        /// Returns the readonly value of init.
        /// </summary>
        /// <returns></returns>
        public bool HasInit() { return init; }

        private List<string> output = new List<string>();

        /// <summary>
        /// The UID used for the logger.
        /// </summary>
        public string loggerUID;

        /// <summary>
        /// Returns the full list of this logger's outputs.
        /// </summary>
        /// <returns>output</returns>
        public List<string> LoggerOutput() { return output; }

        private void _init(string uid, Color defaultColour, Color errorColour, Color warningColour)
        {
            if (init)
            {
                return;
                Warning("Logger already initialised! No need to do this!");
            }

            init = true;
            
            defaultLoggingColour = defaultColour;
            defaultWarningColour = warningColour;
            defaultErrorColour = errorColour;

            loggerUID = uid;
            Log("Logger Initialised");
        }

        /// <summary>
        /// Default
        /// </summary>
        /// <param name="uid"></param>
        public void Init(string uid)
        {
            _init(uid, defaultLoggingColour, defaultErrorColour, defaultWarningColour);
        } 

        /// <summary>
        /// Initialises an empty logger.
        /// </summary>
        public Logger() { }

        /// <summary>
        /// Create a logger.
        /// </summary>
        /// <param name="uid"></param>
        public Logger(string uid) { _init(uid, Color.white, Color.red, Color.yellow); }

        /// <summary>
        /// Create a logger with a specified default colour.
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="defaultColour"></param>
        public Logger(string uid, Color defaultColour) { _init(uid, defaultColour, Color.red, Color.yellow); }

        /// <summary>
        /// Create a logger with a specified default colour and error colour.
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="defaultColour"></param>
        /// <param name="errorColour"></param>
        public Logger(string uid, Color defaultColour, Color errorColour) { _init(uid, defaultColour, errorColour, Color.yellow); }

        /// <summary>
        /// Create a logger with a specified default colour, error colour and warning colour.
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="defaultColour"></param>
        /// <param name="errorColour"></param>
        /// <param name="warningColour"></param>
        public Logger(string uid, Color defaultColour, Color errorColour, Color warningColour) { _init(uid, defaultColour, errorColour, warningColour); }

        /// <summary>
        /// Create a logger with a specified default colour, error colour and warning colour, respectively.
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="colours"></param>
        public Logger(string uid, Color[] colours) { _init(uid, colours[0], colours[1], colours[2]); }

        /// <summary>
        /// Logs text with a custom colour and adds it to the output list.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public string Log(object obj, Color c)
        {
            if (!init) { return null; }
            try
            {
                string s = LoggingUtils.Colour(obj, c);
                s = "[" + loggerUID + "] " + obj;
                output.Add(s);
                Debug.Log(s);
                return s;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                return "STRINGERROR";
            }
        }

        /// <summary>
        /// Logs text and adds it to the output list.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public string Log(object obj)
        {
            return (init != null) ? Log(obj, defaultLoggingColour) : null;
        } 
        
        /// <summary>
        /// Logs an error and adds it to the output list.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public string Error(object obj)
        {
            return (init != null) ? Log("[ERROR] " + obj, defaultErrorColour) : null;
        }

        /// <summary>
        /// Logs a warning and adds it to the output list.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public string Warning(object obj)
        {
            return (init != null) ? Log("[WARNING] " + obj, defaultWarningColour) : null;
        }
    }
}
