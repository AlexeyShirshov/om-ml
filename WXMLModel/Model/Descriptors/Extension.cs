using System.Collections.Generic;
using System.Xml.Linq;
namespace WXML.Model.Descriptors
{
    public interface IExtensible
    {
        Dictionary<Extension, XElement> Extensions { get;}
        XElement GetExtension(string name);
    }
    public class Extension
    {
        public MergeAction Action { get; set; }
        public string Name { get; set; }

        public Extension()
        {
        }

        public Extension(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Extension);
        }

        public bool Equals(Extension obj)
        {
            if (obj == null)
                return false;
            return Name.Equals(obj.Name);
        }
    }
}
