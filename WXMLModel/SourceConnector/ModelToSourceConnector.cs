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

        public ModelToSourceConnector(SourceView sourceView, WXMLModel model)
        {
            _db = sourceView;
            _model = model;
        }

        public SourceView SourceView
        {
            get { return _db; }
        }

        public WXMLModel Model
        {
            get { return _model; }
        }

        public string GenerateSourceScript(ISourceProvider provider, bool unicodeStrings)
        {
            var props = Model.GetActiveEntities().SelectMany(item =>
                item.OwnProperties.Where(p => !p.Disabled && p.SourceFragment != null)
            );

            StringBuilder script = new StringBuilder();

            foreach (SourceFragmentDefinition s in props.Select(item => item.SourceFragment).Distinct())
            {
                SourceFragmentDefinition sf = s;

                var targetSF = SourceView.GetSourceFragments().SingleOrDefault(item => 
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
