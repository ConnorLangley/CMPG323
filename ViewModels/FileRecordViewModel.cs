namespace Proj_Frame.ViewModels
{
    using System;

    public class FileRecordViewModel
    {
        public int File_ID { get; set; }
        public string FileName { get; set; }
        public string Keywords { get; set; }
        public string Subject { get; set; }
        public DateTime DateCreated { get; set; }
        public int Sum_Reviews { get; set; }
        public int Review_Amount { get; set; }
        public double Rating { get; set; }
        public int Grade { get; set; }
        public int Reports { get; set; }

        // Adding Storage_Loc property to support file download by full path
        public string Storage_Loc { get; set; }
    }
}
