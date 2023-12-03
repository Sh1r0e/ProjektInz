using Google.Cloud.Firestore;

namespace Projekt_I.Data.Models
{
    [FirestoreData]
    public class Workshop
    {
        [FirestoreProperty]
        public string Id { get; set; }

        [FirestoreProperty]
        public List<Novel> Novels { get; set; } = new List<Novel>();
    }
}
