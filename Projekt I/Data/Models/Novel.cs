using Google.Cloud.Firestore;

namespace Projekt_I.Data.Models
{
    [FirestoreData]
    public class Novel
    {
        [FirestoreProperty]
        public string Id { get; set; }
        [FirestoreProperty]
        public string Title { get; set; }
        [FirestoreProperty]
        public string Description { get; set; }
        [FirestoreProperty]
        public string[] Tags { get; set; }
        [FirestoreProperty]
        public string AuthorId { get; set; }
        [FirestoreProperty]
        public string AuthorUsername { get; set; }

    }
}

