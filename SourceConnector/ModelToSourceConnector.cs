using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WXML.Model.Descriptors;
using WXML.Model.Database.Providers;

namespace WXML.Model.SourceConnector
{
    public class ModelToSourceConnector
    {
        private readonly SourceView _db;
        private readonly WXMLModel _model;

        public ModelToSourceConnector(SourceView db, WXMLModel model)
        {
            _db = db;
            _model = model;
        }

        public string GenerateSourceScript(ISourceProvider provider, bool unicodeStrings)
        {
            var props = _model.GetActiveEntities().SelectMany(item =>
                item.SelfProperties.Where(p => !p.Disabled && p.SourceFragment != null)
            );

            StringBuilder script = new StringBuilder();

            foreach (SourceFragmentDefinition s in props.Select(item => item.SourceFragment).Distinct())
            {
                SourceFragmentDefinition sf = s;

                var targetSF = _db.GetTables().SingleOrDefault(item => 
                    item.Name == sf.Name && item.Selector == sf.Selector);

                if (targetSF == null)
                {
                    provider.GenerateCreateScript(props.Where(item => item.SourceFragment == sf), script, unicodeStrings);
                }
            }

            return script.ToString();
        }
    }
}
