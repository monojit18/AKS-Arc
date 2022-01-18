using System;
namespace ValidateOCRArcApp.Models
{
    public class UploadImageModel
    {

        public bool IsApproved { get; set; }
        public byte[] BlobContents { get; set; }
        public string ImageName { get; set; }

    }
}
