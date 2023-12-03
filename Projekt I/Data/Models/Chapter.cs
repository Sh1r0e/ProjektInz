using Google.Cloud.Firestore;

namespace Projekt_I.Data.Models
{
    [FirestoreData]
    public class Chapter
    {
        [FirestoreProperty]
        public string Id { get; set; }
        [FirestoreProperty]
        public string Title { get; set; }
        [FirestoreProperty]
        public int Number { get; set; }
    }
}

