using IkeCode.XmlDiff.Models;
using System;
using System.Collections.Generic;

namespace IkeCode.XmlDiff
{
    public static class GitHubCredentials
    {
        public static Uri GitUrl { get; set; }
        public static string GitUser { get; set; }
        public static string GitPassword { get; set; }
        public static string GitOwner
        {
            get
            {
                return GitUrl.Segments[1].Replace("/", "");
            }
        }
        public static string GitName
        {
            get
            {
                return GitUrl.Segments[2].Replace("/", "").Replace(".git", "");
            }
        }
    }
}
