using Google.Cloud.Firestore;

namespace Projekt_I.Data.Models
{
    [FirestoreData]
    public class Content
    {
        [FirestoreProperty]
        public string Id {  get; set; }
        [FirestoreProperty]
        public string ChapterContent { get; set; }

    }
}
