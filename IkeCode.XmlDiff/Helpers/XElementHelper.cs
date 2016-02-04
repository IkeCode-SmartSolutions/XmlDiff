using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IkeCode.XmlDiff
{
    public static class XElementHelper
    {
        public static string GetPath(this XElement element)
        {
            return string.Join("\\", element.AncestorsAndSelf().Reverse()
                .Select(e =>
                {
                    var index = e.GetIndex();

                    if (index == 1)
                    {
                        return e.Name.LocalName;
                    }

                    return string.Format("{0}[{1}]", e.Name.LocalName, e.GetIndex());
                }));

        }

        private static int GetIndex(this XElement element)
        {
            var i = 1;

            if (element.Parent == null)
            {
                return 1;
            }

            foreach (var e in element.Parent.Elements(element.Name.LocalName))
            {
                if (e == element)
                {
                    break;
                }

                i++;
            }

            return i;
        }
    }
}
