using Google.Cloud.Firestore;

namespace Projekt_I.Data.Models
{
    [FirestoreData]
    public class Library
    {
        [FirestoreProperty]
        public string ReaderId { get; set; }
        [FirestoreProperty]
        public string NovelId { get; set; }
        [FirestoreProperty]
        public string VolumeId { get; set; }
        [FirestoreProperty]
        public string ChapterId { get; set; }
        [FirestoreProperty]
        public double Bookmark { get; set; }
    }
}
