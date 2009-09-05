using System.Linq;
using System.Text;
using WXML.Model;
using WXML.Model.Descriptors;

namespace WXML.SourceConnector
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

                var targetSF = _db.GetSourceFragments().SingleOrDefault(item => 
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
