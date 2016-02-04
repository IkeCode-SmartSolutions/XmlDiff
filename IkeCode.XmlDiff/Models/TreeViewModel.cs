using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IkeCode.XmlDiff.Models
{
    public class TreeViewModel
    {
        public string Title { get; set; }
        public string FullPath { get; set; }
        public List<TreeViewModel> Children { get; set; }
    }
}
