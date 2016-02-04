namespace IkeCode.XmlDiff
{
    public static class SelectedFolders
    {
        public static string FolderOld { get; set; }
        public static string FolderNew { get; set; }

        public static bool IsFile { get; set; }
        public static bool IsFolder { get; set; }
        public static bool IncludeSubFolders { get; set; }

        public static bool UseExample { get; set; }
    }
}
