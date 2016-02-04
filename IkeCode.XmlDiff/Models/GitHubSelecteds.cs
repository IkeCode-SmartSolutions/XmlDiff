using IkeCode.XmlDiff.Models;
using System;
using System.Collections.Generic;

namespace IkeCode.XmlDiff
{
    public static class GitHubSelecteds
    {
        private static List<TagsModel> _tagsAndBranches = new List<TagsModel>();
        public static List<TagsModel> TagsAndBranches { get { return _tagsAndBranches; } set { _tagsAndBranches = value ?? new List<TagsModel>(); } }

        public static TagsModel OldTagSelected { get; set; }
        public static TagsModel NewTagSelected { get; set; }

        private static List<TreeViewModel> _oldTree = new List<TreeViewModel>();
        public static List<TreeViewModel> OldTree { get { return _oldTree; } set { _oldTree = value ?? new List<TreeViewModel>(); } }

        private static List<TreeViewModel> _newTree = new List<TreeViewModel>();
        public static List<TreeViewModel> NewTree { get { return _newTree; } set { _newTree = value ?? new List<TreeViewModel>(); } }
    }
}
