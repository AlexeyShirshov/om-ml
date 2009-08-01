using System;
using System.Collections.Generic;
using System.Text;

namespace WXML.Model
{
    public class WXMLModelWriterSettings: ICloneable
    {
        private IncludeGenerationBehaviour m_includeBehaviour;
        private string m_includeFileNamePattern;
        private string m_defaultIncludeFileName;
        private string m_defaultMainFileName;
        private string m_includeFolderName;
        
        public WXMLModelWriterSettings()
        {
            IncludeBehaviour = IncludeGenerationBehaviour.None;
            IncludeFileNamePattern = "%MAIN_FILENAME%.%INCLUDE_NAME%";
            DefaultIncludeFileName = "inclide_";
            DefaultMainFileName = "schema";
        }

        /// <summary>
        /// Gets or sets the include behaviour.
        /// </summary>
        /// <value>The include behaviour.</value>
        public IncludeGenerationBehaviour IncludeBehaviour
        {
            get { return m_includeBehaviour; }
            set { m_includeBehaviour = value; }
        }

        /// <summary>
        /// Pattern for include file name.
        /// </summary>
        /// <value>The include file name pattern.</value>
        /// <remarks>
        /// Available pattern variables:
        /// %MAIN_FILENAME% -   main file name
        /// %INCLUDE_NAME%  -   include file name
        /// </remarks>
        public string IncludeFileNamePattern
        {
            get { return m_includeFileNamePattern; }
            set { m_includeFileNamePattern = value; }
        }

        /// <summary>
        /// Gets or sets the default name of the include file.
        /// </summary>
        /// <value>The name of the default include file.</value>
        public string DefaultIncludeFileName
        {
            get { return m_defaultIncludeFileName; }
            set { m_defaultIncludeFileName = value; }
        }

        /// <summary>
        /// Gets or sets the default name of the main file.
        /// </summary>
        /// <value>The name of the default main file.</value>
        public string DefaultMainFileName
        {
            get { return m_defaultMainFileName; }
            set { m_defaultMainFileName = value; }
        }

        /// <summary>
        /// Gets or sets the name of the include folder.
        /// </summary>
        /// <value>The name of the include folder.</value>
        public string IncludeFolderName
        {
            get { return m_includeFolderName; }
            set { m_includeFolderName = value; }
        }

        #region ICloneable Members

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        #endregion
    }

    [Flags]
    public enum IncludeGenerationBehaviour
    {
        None = 0x0000,
        /// <summary>
        /// Generate inline include document or external.(<see cref="IncludeGenerationBehaviour.Content"/> also set)
        /// </summary>
        Inline = 0x0001,
        /// <summary>
        /// Generate content of the include file.
        /// </summary>
        Content = 0x0002,
        /// <summary>
        /// Place or not all include files in separate folder.(doesn't work with <see cref="IncludeGenerationBehaviour.Inline"/> and <see cref="IncludeGenerationBehaviour.Content"/>
        /// </summary>
        PlaceInFolder = 0x0004,

        Default = Content
    }
}
