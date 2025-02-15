namespace Proj_Frame.ViewModels
{
    using System.Collections.Generic;

    public class SubjectViewModel
    {
        // Represents the name of the subject
        public string Subject { get; set; }
        public string FileName { get; set; }

        // A list of files associated with the subject
        public List<FileRecordViewModel> Files { get; set; }
    }
}